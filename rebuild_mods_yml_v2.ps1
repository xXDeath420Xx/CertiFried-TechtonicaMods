# Rebuild mods.yml for r2modman - Version 2 (proper parsing)
$ProfilePath = "C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default"
$ModsYmlPath = Join-Path $ProfilePath "mods.yml"
$SourceRoot = "C:\Users\crawf\TechtonicaMods\NewMods"

# Install powershell-yaml module if needed
if (-not (Get-Module -ListAvailable -Name powershell-yaml)) {
    Write-Host "Installing powershell-yaml module..." -ForegroundColor Yellow
    Install-Module -Name powershell-yaml -Force -Scope CurrentUser
}
Import-Module powershell-yaml

Write-Host "Rebuilding mods.yml..." -ForegroundColor Cyan

# Backup current mods.yml
$backupPath = "$ModsYmlPath.backup_v2_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $ModsYmlPath $backupPath
Write-Host "  Backed up to: $backupPath" -ForegroundColor DarkGray

# CertiFried mod mappings
$CertiFriedMods = @{
    "AdvancedMachines" = "CertiFried-AdvancedMachines"
    "AmbientLife" = "CertiFried-AmbientLife"
    "AtlantumEnrichment" = "CertiFried-AtlantumEnrichment"
    "AtlantumReactor" = "CertiFried-AtlantumReactor_Updated"
    "BaseBuilding" = "CertiFried-BaseBuilding"
    "BeltImmunity" = "CertiFried-BeltImmunity_Updated"
    "BioProcessing" = "CertiFried-BioProcessing"
    "ChainExplosives" = "CertiFried-ChainExplosives_Updated"
    "CoreComposerPlus" = "CertiFried-CoreComposerPlus"
    "DevTools" = "CertiFried-DevTools"
    "DroneLogistics" = "CertiFried-DroneLogistics"
    "EnhancedLogistics" = "CertiFried-EnhancedLogistics"
    "HazardousWorld" = "CertiFried-HazardousWorld"
    "MechSuit" = "CertiFried-MechSuit"
    "MobilityPlus" = "CertiFried-MobilityPlus"
    "MorePlanters" = "CertiFried-MorePlanters_Updated"
    "MultiplayerFixes" = "CertiFried-MultiplayerFixes"
    "NarrativeExpansion" = "CertiFried-NarrativeExpansion"
    "OmniseekerPlus" = "CertiFried-OmniseekerPlus"
    "PetsCompanions" = "CertiFried-PetsCompanions"
    "PlanterCoreClusters" = "CertiFried-PlanterCoreClusters_Updated"
    "Recycler" = "CertiFried-Recycler"
    "Restock" = "CertiFried-Restock_Updated"
    "SeamlessWorld" = "CertiFried-SeamlessWorld"
    "SurvivalElements" = "CertiFried-SurvivalElements"
    "TechtonicaFramework" = "CertiFried-TechtonicaFramework"
    "TurretDefense" = "CertiFried-TurretDefense"
    "WormholeChests" = "CertiFried-WormholeChests_Updated"
}

# Mods to completely remove (old duplicates)
$ModsToRemove = @(
    "Equinox-AtlantumReactor"
)

# Function to convert image to base64
function Get-ImageBase64 {
    param([string]$imagePath)
    if (Test-Path $imagePath) {
        $bytes = [System.IO.File]::ReadAllBytes($imagePath)
        $base64 = [Convert]::ToBase64String($bytes)
        return "data:image/png;base64,$base64"
    }
    return $null
}

# Read and parse YAML
Write-Host "  Parsing mods.yml (this may take a moment)..." -ForegroundColor DarkGray
$yamlContent = Get-Content $ModsYmlPath -Raw
$mods = ConvertFrom-Yaml $yamlContent

Write-Host "  Found $($mods.Count) mod entries" -ForegroundColor DarkGray

# Filter mods - keep non-CertiFried and non-duplicate
$filteredMods = @()
$removedNames = @()

foreach ($mod in $mods) {
    $name = $mod.name
    $keep = $true

    # Remove CertiFried mods (we'll recreate them)
    if ($name -match '^CertiFried-') {
        $keep = $false
        $removedNames += $name
    }

    # Remove specific duplicates
    foreach ($toRemove in $ModsToRemove) {
        if ($name -eq $toRemove) {
            $keep = $false
            $removedNames += $name
        }
    }

    if ($keep) {
        $filteredMods += $mod
    }
}

Write-Host "  Removed $($removedNames.Count) entries (CertiFried + duplicates)" -ForegroundColor Yellow
Write-Host "  Keeping $($filteredMods.Count) dependency/other mods" -ForegroundColor DarkGray

# Create new CertiFried mod entries
$newMods = @()
foreach ($modEntry in $CertiFriedMods.GetEnumerator()) {
    $sourceName = $modEntry.Key
    $r2Name = $modEntry.Value

    $manifestPath = Join-Path $SourceRoot "$sourceName\manifest.json"
    $iconPath = Join-Path $SourceRoot "$sourceName\icon.png"

    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath | ConvertFrom-Json

        # Parse version
        $versionParts = $manifest.version_number -split '\.'
        $major = [int]$versionParts[0]
        $minor = [int]$versionParts[1]
        $patch = if ($versionParts.Length -gt 2) { [int]$versionParts[2] } else { 0 }

        # Get icon
        $iconBase64 = Get-ImageBase64 -imagePath $iconPath
        if (-not $iconBase64) {
            $iconBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        }

        # Create mod object
        $displayName = $r2Name -replace '^CertiFried-', '' -replace '_Updated$', ''

        $newMod = [ordered]@{
            manifestVersion = 1
            name = $r2Name
            authorName = "CertiFried"
            websiteUrl = "https://thunderstore.io/c/techtonica/p/CertiFried/$displayName/"
            displayName = $displayName
            description = $manifest.description
            gameVersion = "0"
            networkMode = "both"
            packageType = "other"
            installMode = "managed"
            installedAtTime = [DateTimeOffset]::Now.ToUnixTimeMilliseconds()
            loaders = @()
            dependencies = if ($manifest.dependencies) { $manifest.dependencies } else { @() }
            incompatibilities = @()
            optionalDependencies = @()
            versionNumber = [ordered]@{
                major = $major
                minor = $minor
                patch = $patch
            }
            enabled = $true
            icon = $iconBase64
        }

        $newMods += $newMod
        Write-Host "  [OK] $sourceName v$major.$minor.$patch" -ForegroundColor Green
    }
    else {
        Write-Host "  [SKIP] $sourceName - no manifest" -ForegroundColor DarkYellow
    }
}

# Combine all mods
$allMods = $filteredMods + $newMods

# Convert back to YAML and save
Write-Host "  Writing new mods.yml..." -ForegroundColor DarkGray
$newYaml = ConvertTo-Yaml $allMods
Set-Content -Path $ModsYmlPath -Value $newYaml

Write-Host ""
Write-Host "mods.yml rebuilt!" -ForegroundColor Green
Write-Host "  Total entries: $($allMods.Count)" -ForegroundColor DarkGray
Write-Host "  Dependencies/other: $($filteredMods.Count)" -ForegroundColor DarkGray
Write-Host "  CertiFried mods: $($newMods.Count)" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Restart r2modman to see changes." -ForegroundColor White
