# Sync all built mod DLLs and metadata to r2modman profile
# Run this after building mods to update your local r2modman profile

$SourceRoot = "C:\Users\crawf\TechtonicaMods\NewMods"
$DestRoot = "C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default\BepInEx\plugins"

# Map of mod folder names to their r2modman plugin folder names and current versions
$ModMappings = @{
    "AdvancedMachines" = @{ Folder = "CertiFried-AdvancedMachines"; Version = "2.4.2" }
    "AmbientLife" = @{ Folder = "CertiFried-AmbientLife"; Version = "2.1.0" }
    "AtlantumEnrichment" = @{ Folder = "CertiFried-AtlantumEnrichment"; Version = "1.0.0" }
    "AtlantumReactor" = @{ Folder = "CertiFried-AtlantumReactor_Updated"; Version = "4.1.0" }
    "BaseBuilding" = @{ Folder = "CertiFried-BaseBuilding"; Version = "1.0.0" }
    "BeltImmunity" = @{ Folder = "CertiFried-BeltImmunity_Updated"; Version = "1.0.4" }
    "BioProcessing" = @{ Folder = "CertiFried-BioProcessing"; Version = "1.0.0" }
    "ChainExplosives" = @{ Folder = "CertiFried-ChainExplosives_Updated"; Version = "1.0.4" }
    "CoreComposerPlus" = @{ Folder = "CertiFried-CoreComposerPlus"; Version = "1.0.0" }
    "DevTools" = @{ Folder = "CertiFried-DevTools"; Version = "2.2.0" }
    "DroneLogistics" = @{ Folder = "CertiFried-DroneLogistics"; Version = "1.1.0" }
    "EnhancedLogistics" = @{ Folder = "CertiFried-EnhancedLogistics"; Version = "3.2.2" }
    "HazardousWorld" = @{ Folder = "CertiFried-HazardousWorld"; Version = "1.5.0" }
    "MechSuit" = @{ Folder = "CertiFried-MechSuit"; Version = "1.0.0" }
    "MobilityPlus" = @{ Folder = "CertiFried-MobilityPlus"; Version = "1.7.0" }
    "MorePlanters" = @{ Folder = "CertiFried-MorePlanters_Updated"; Version = "3.0.9" }
    "MultiplayerFixes" = @{ Folder = "CertiFried-MultiplayerFixes"; Version = "2.0.0" }
    "NarrativeExpansion" = @{ Folder = "CertiFried-NarrativeExpansion"; Version = "2.7.0" }
    "OmniseekerPlus" = @{ Folder = "CertiFried-OmniseekerPlus"; Version = "1.0.7" }
    "PetsCompanions" = @{ Folder = "CertiFried-PetsCompanions"; Version = "1.4.0" }
    "PlanterCoreClusters" = @{ Folder = "CertiFried-PlanterCoreClusters_Updated"; Version = "3.0.7" }
    "Recycler" = @{ Folder = "CertiFried-Recycler"; Version = "1.0.0" }
    "Restock" = @{ Folder = "CertiFried-Restock_Updated"; Version = "3.0.8" }
    "SeamlessWorld" = @{ Folder = "CertiFried-SeamlessWorld"; Version = "0.1.0" }
    "SurvivalElements" = @{ Folder = "CertiFried-SurvivalElements"; Version = "2.6.3" }
    "TechtonicaFramework" = @{ Folder = "CertiFried-TechtonicaFramework"; Version = "1.4.0" }
    "TurretDefense" = @{ Folder = "CertiFried-TurretDefense"; Version = "4.5.4" }
    "WormholeChests" = @{ Folder = "CertiFried-WormholeChests_Updated"; Version = "3.1.1" }
}

Write-Host "Syncing mod DLLs and metadata to r2modman profile..." -ForegroundColor Cyan
Write-Host ""

$synced = 0
$failed = 0

foreach ($mod in $ModMappings.GetEnumerator()) {
    $modName = $mod.Key
    $folderName = $mod.Value.Folder
    $version = $mod.Value.Version

    $srcDir = Join-Path $SourceRoot $modName
    $srcDll = Join-Path $srcDir "bin\Release\net472\$modName.dll"

    # Also check Debug folder if Release doesn't exist
    if (-not (Test-Path $srcDll)) {
        $srcDll = Join-Path $srcDir "bin\Debug\net472\$modName.dll"
    }

    $destFolder = Join-Path $DestRoot $folderName
    $destDll = Join-Path $destFolder "$modName.dll"

    if (Test-Path $srcDll) {
        if (-not (Test-Path $destFolder)) {
            New-Item -Path $destFolder -ItemType Directory -Force | Out-Null
            Write-Host "  Created folder: $folderName" -ForegroundColor Yellow
        }

        # Copy DLL
        Copy-Item -Path $srcDll -Destination $destDll -Force

        # Copy README if exists
        $srcReadme = Join-Path $srcDir "README.md"
        if (Test-Path $srcReadme) {
            Copy-Item -Path $srcReadme -Destination $destFolder -Force
        }

        # Copy CHANGELOG if exists
        $srcChangelog = Join-Path $srcDir "CHANGELOG.md"
        if (Test-Path $srcChangelog) {
            Copy-Item -Path $srcChangelog -Destination $destFolder -Force
        }

        # Copy manifest.json if exists in source
        $srcManifest = Join-Path $srcDir "manifest.json"
        if (Test-Path $srcManifest) {
            Copy-Item -Path $srcManifest -Destination $destFolder -Force
        }

        # Copy icon.png if exists
        $srcIcon = Join-Path $srcDir "icon.png"
        if (Test-Path $srcIcon) {
            Copy-Item -Path $srcIcon -Destination $destFolder -Force
        }

        # Copy Bundles folder if exists
        $srcBundles = Join-Path $srcDir "Bundles"
        if (Test-Path $srcBundles) {
            $destBundles = Join-Path $destFolder "Bundles"
            if (-not (Test-Path $destBundles)) {
                New-Item -Path $destBundles -ItemType Directory -Force | Out-Null
            }
            Copy-Item -Path "$srcBundles\*" -Destination $destBundles -Force -Recurse
        }

        Write-Host "  [OK] $modName -> $folderName (v$version)" -ForegroundColor Green

        $srcTime = (Get-Item $srcDll).LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
        Write-Host "       Built: $srcTime" -ForegroundColor DarkGray
        $synced++
    }
    else {
        Write-Host "  [SKIP] $modName - DLL not found (not built yet?)" -ForegroundColor DarkYellow
        $failed++
    }
}

Write-Host ""
Write-Host "Sync complete: $synced synced, $failed skipped" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now launch Techtonica through r2modman to test the latest builds." -ForegroundColor White
