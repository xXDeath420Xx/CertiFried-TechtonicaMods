# Rebuild mods.yml for r2modman with correct CertiFried mod entries
# This script updates the mods.yml to reflect current mod versions and metadata

$ProfilePath = "C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default"
$ModsYmlPath = Join-Path $ProfilePath "mods.yml"
$PluginsPath = Join-Path $ProfilePath "BepInEx\plugins"
$SourceRoot = "C:\Users\crawf\TechtonicaMods\NewMods"

# CertiFried mod mappings (source folder -> r2modman folder)
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
    "SurvivalElements" = "CertiFried-SurvivalElements"
    "TechtonicaFramework" = "CertiFried-TechtonicaFramework"
    "TurretDefense" = "CertiFried-TurretDefense"
    "WormholeChests" = "CertiFried-WormholeChests_Updated"
}

# Old mods to remove (duplicates/old versions)
$ModsToRemove = @(
    "Equinox-AtlantumReactor"  # Old version, replaced by CertiFried-AtlantumReactor_Updated
)

Write-Host "Rebuilding mods.yml..." -ForegroundColor Cyan

# Backup current mods.yml
$backupPath = "$ModsYmlPath.backup_$(Get-Date -Format 'yyyyMMdd_HHmmss')"
Copy-Item $ModsYmlPath $backupPath
Write-Host "  Backed up to: $backupPath" -ForegroundColor DarkGray

# Read current mods.yml as text and parse YAML entries
$content = Get-Content $ModsYmlPath -Raw

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

# Function to create a mod entry
function New-ModEntry {
    param(
        [string]$name,
        [string]$author,
        [string]$displayName,
        [string]$description,
        [int]$major,
        [int]$minor,
        [int]$patch,
        [string]$icon,
        [string[]]$dependencies
    )

    $depYaml = ""
    foreach ($dep in $dependencies) {
        $depYaml += "  - $dep`n"
    }
    if ($depYaml -eq "") { $depYaml = "  []`n" }

    $entry = @"
- manifestVersion: 1
  name: $author-$name
  authorName: $author
  websiteUrl: https://thunderstore.io/c/techtonica/p/$author/$name/
  displayName: $displayName
  description: $description
  gameVersion: "0"
  networkMode: both
  packageType: other
  installMode: managed
  installedAtTime: $([DateTimeOffset]::Now.ToUnixTimeMilliseconds())
  loaders: []
  dependencies:
$depYaml  incompatibilities: []
  optionalDependencies: []
  versionNumber:
    major: $major
    minor: $minor
    patch: $patch
  enabled: true
  icon: $icon

"@
    return $entry
}

# Split mods.yml into individual entries
$entries = $content -split '(?=^- manifestVersion:)' | Where-Object { $_ -match '\S' }

Write-Host "  Found $($entries.Count) entries in mods.yml" -ForegroundColor DarkGray

# Filter out CertiFried entries and mods to remove
$filteredEntries = @()
$removedCount = 0

foreach ($entry in $entries) {
    $keepEntry = $true

    # Check if it's a CertiFried mod (we'll rebuild these)
    if ($entry -match 'name:\s*CertiFried-') {
        $keepEntry = $false
        $removedCount++
    }

    # Check if it's in the removal list
    foreach ($modToRemove in $ModsToRemove) {
        if ($entry -match "name:\s*$modToRemove") {
            $keepEntry = $false
            $removedCount++
        }
    }

    if ($keepEntry) {
        $filteredEntries += $entry
    }
}

Write-Host "  Removed $removedCount old CertiFried/duplicate entries" -ForegroundColor Yellow

# Generate new CertiFried entries
$newEntries = @()
foreach ($mod in $CertiFriedMods.GetEnumerator()) {
    $sourceName = $mod.Key
    $r2Folder = $mod.Value

    $manifestPath = Join-Path $SourceRoot "$sourceName\manifest.json"
    $iconPath = Join-Path $SourceRoot "$sourceName\icon.png"

    if (Test-Path $manifestPath) {
        $manifest = Get-Content $manifestPath | ConvertFrom-Json

        # Parse version
        $versionParts = $manifest.version_number -split '\.'
        $major = [int]$versionParts[0]
        $minor = [int]$versionParts[1]
        $patch = [int]$versionParts[2]

        # Get icon
        $iconBase64 = Get-ImageBase64 -imagePath $iconPath
        if (-not $iconBase64) {
            # Use a default icon if none exists
            $iconBase64 = "data:image/png;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg=="
        }

        # Get display name from folder name
        $displayName = $r2Folder -replace '^CertiFried-', '' -replace '_Updated$', ''

        # Create entry
        $entry = New-ModEntry `
            -name $displayName `
            -author "CertiFried" `
            -displayName $displayName `
            -description $manifest.description `
            -major $major `
            -minor $minor `
            -patch $patch `
            -icon $iconBase64 `
            -dependencies $manifest.dependencies

        $newEntries += $entry
        Write-Host "  [OK] $sourceName -> v$($manifest.version_number)" -ForegroundColor Green
    }
    else {
        Write-Host "  [SKIP] $sourceName - no manifest.json" -ForegroundColor DarkYellow
    }
}

# Combine filtered entries with new entries
$finalContent = ($filteredEntries -join "") + ($newEntries -join "")

# Write new mods.yml
Set-Content -Path $ModsYmlPath -Value $finalContent -NoNewline

Write-Host ""
Write-Host "mods.yml rebuilt successfully!" -ForegroundColor Green
Write-Host "  Kept: $($filteredEntries.Count) non-CertiFried entries" -ForegroundColor DarkGray
Write-Host "  Added: $($newEntries.Count) CertiFried entries" -ForegroundColor DarkGray
Write-Host ""
Write-Host "Please restart r2modman to see the changes." -ForegroundColor White
