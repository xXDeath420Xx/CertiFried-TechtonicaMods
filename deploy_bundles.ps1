# Techtonica Mods - Asset Bundle Deployment Script
# Copies all required asset bundles from Unity project to mod folders

$ErrorActionPreference = "Continue"

# Source: Unity project bundles folder
$BundleSource = "B:\Code\Tower Defense\Tower Defense\Assets\TechtonicaBundles"

# Destination: Each mod's Bundles folder
$ModsRoot = "C:\Users\crawf\TechtonicaMods\NewMods"

# Define which bundles each mod needs
$ModBundles = @{
    # TurretDefense - Full combat system
    "TurretDefense" = @(
        # Turrets
        "autarca_ships",
        "laser_cannon",
        "turrets_gatling",
        "turrets_rocket",
        "turrets_railgun",
        "turrets_lightning",
        "turrets_scifi",
        "turrets_tdtk",
        "turrets_nuke",
        "turrets_flamethrower",
        "artillery_outpost",
        "artillery_cannons",
        # Enemies
        "drones_scifi",
        "creatures_gamekit",
        "creatures_beetles",
        "creatures_spiders",
        "creatures_tortoise",
        "creature_mimic",
        "creature_arachnid",
        "enemy_robots",
        # Hive structures
        "alien_buildings",
        "cthulhu_idols",
        # Hazards
        "mushroom_forest",
        "lava_plants",
        # Effects
        "effects_tdtk",
        # Icons
        "icons_ammo",
        "icons_skymon",
        "icons_heathen",
        "icons_turret_gatling",
        "icons_turret_rocket",
        "icons_turret_railgun",
        "icons_turret_lightning",
        "icons_turret_laser"
    )

    # DroneLogistics - Drone automation
    "DroneLogistics" = @(
        "drones_voodooplay",
        "drones_scifi",
        "drones_simple",
        "robot_sphere",
        "robot_metallic",
        "scifi_machines",
        "icons_skymon"
    )

    # HazardousWorld - Environmental hazards and hostile flora
    "HazardousWorld" = @(
        "mushroom_forest",
        "lava_plants",
        "creatures_spiders",
        "alien_buildings"
    )

    # AmbientLife - Creatures and wildlife
    "AmbientLife" = @(
        "creatures_beetles",
        "creatures_spiders",
        "creatures_tortoise",
        "creatures_animals",
        "creatures_wolf",
        "fauna_turtle",
        "creatures_gamekit"
    )

    # MobilityPlus - Vehicles and movement
    "MobilityPlus" = @(
        "drones_scifi",
        "drones_voodooplay",
        "scifi_machines"
    )

    # PetsCompanions - Pet animals
    "PetsCompanions" = @(
        "creatures_animals",
        "creatures_wolf",
        "fauna_turtle",
        "mech_companion"
    )

    # NarrativeExpansion - Story and NPCs
    "NarrativeExpansion" = @(
        "alien_buildings",
        "cthulhu_idols",
        "icons_skymon"
    )

    # SurvivalElements - Health and survival mechanics
    "SurvivalElements" = @(
        "icons_skymon",
        "icons_heathen"
    )

    # BaseBuilding - Decorative structures from Sci-Fi Modular Pack
    "BaseBuilding" = @(
        "scifi_modular"
    )

    # MechSuit - Player mech suits
    "MechSuit" = @(
        "mech_companion"
    )
}

# Statistics
$totalCopied = 0
$totalSkipped = 0
$totalErrors = 0

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "TECHTONICA MODS - BUNDLE DEPLOYMENT" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Source: $BundleSource" -ForegroundColor Gray
Write-Host "Target: $ModsRoot" -ForegroundColor Gray
Write-Host ""

# Check if source exists
if (-not (Test-Path $BundleSource)) {
    Write-Host "ERROR: Bundle source folder not found!" -ForegroundColor Red
    Write-Host "Please ensure Unity project is at: B:\Code\Tower Defense\Tower Defense" -ForegroundColor Yellow
    exit 1
}

# Deploy bundles to each mod
foreach ($mod in $ModBundles.Keys) {
    $modPath = Join-Path $ModsRoot $mod
    $bundlesPath = Join-Path $modPath "Bundles"

    Write-Host ""
    Write-Host "[$mod]" -ForegroundColor Green

    # Check if mod folder exists
    if (-not (Test-Path $modPath)) {
        Write-Host "  SKIP: Mod folder not found" -ForegroundColor Yellow
        continue
    }

    # Create Bundles folder if it doesn't exist
    if (-not (Test-Path $bundlesPath)) {
        New-Item -ItemType Directory -Path $bundlesPath -Force | Out-Null
        Write-Host "  Created Bundles folder" -ForegroundColor Gray
    }

    # Copy each required bundle
    foreach ($bundle in $ModBundles[$mod]) {
        $sourcePath = Join-Path $BundleSource $bundle
        $destPath = Join-Path $bundlesPath $bundle

        if (Test-Path $sourcePath) {
            # Check if file needs updating (compare timestamps)
            $sourceFile = Get-Item $sourcePath
            $needsCopy = $true

            if (Test-Path $destPath) {
                $destFile = Get-Item $destPath
                if ($destFile.LastWriteTime -ge $sourceFile.LastWriteTime) {
                    $needsCopy = $false
                    $totalSkipped++
                }
            }

            if ($needsCopy) {
                try {
                    Copy-Item $sourcePath $destPath -Force
                    Write-Host "  + $bundle" -ForegroundColor White
                    $totalCopied++
                }
                catch {
                    Write-Host "  ! $bundle (ERROR: $($_.Exception.Message))" -ForegroundColor Red
                    $totalErrors++
                }
            }
        }
        else {
            Write-Host "  ? $bundle (not found in source)" -ForegroundColor DarkYellow
            $totalErrors++
        }
    }
}

# Summary
Write-Host ""
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "DEPLOYMENT COMPLETE" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host "  Copied:  $totalCopied bundles" -ForegroundColor Green
Write-Host "  Skipped: $totalSkipped bundles (up to date)" -ForegroundColor Gray
Write-Host "  Errors:  $totalErrors bundles" -ForegroundColor $(if ($totalErrors -gt 0) { "Red" } else { "Gray" })
Write-Host ""

# Also copy to r2modman plugin folder if it exists
$r2modmanPath = "C:\Users\crawf\AppData\Roaming\r2modmanPlus-local\Techtonica\profiles\not default\BepInEx\plugins"
if (Test-Path $r2modmanPath) {
    Write-Host "Deploying to r2modman profile..." -ForegroundColor Cyan

    foreach ($mod in $ModBundles.Keys) {
        $r2modPath = Join-Path $r2modmanPath "CertiFried-$mod"
        if (-not (Test-Path $r2modPath)) {
            $r2modPath = Join-Path $r2modmanPath $mod
        }

        if (Test-Path $r2modPath) {
            $r2bundlesPath = Join-Path $r2modPath "Bundles"
            $sourceBundles = Join-Path $ModsRoot "$mod\Bundles"

            if ((Test-Path $sourceBundles) -and (Get-ChildItem $sourceBundles -ErrorAction SilentlyContinue)) {
                if (-not (Test-Path $r2bundlesPath)) {
                    New-Item -ItemType Directory -Path $r2bundlesPath -Force | Out-Null
                }
                Copy-Item "$sourceBundles\*" $r2bundlesPath -Force -ErrorAction SilentlyContinue
                Write-Host "  [$mod] -> r2modman" -ForegroundColor Gray
            }
        }
    }

    Write-Host "r2modman deployment complete!" -ForegroundColor Green
}

Write-Host ""
Write-Host "All done!" -ForegroundColor Green
