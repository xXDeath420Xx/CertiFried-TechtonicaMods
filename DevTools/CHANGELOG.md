# Changelog

All notable changes to DevTools will be documented in this file.

## [2.2.0] - 2026-01-07

### Changed
- Minor improvements and stability fixes
- Compatible with TechtonicaFramework 1.2.0

## [2.0.0] - 2025-01-04

### Added
- **Complete In-Game GUI** (Press Insert to toggle)
  - Draggable window with tabbed interface
  - Live value updates - changes apply immediately
  - Visual feedback for all settings
  - Status indicators for active systems

### GUI Tabs
- **Player Tab**
  - Toggle cheats: Infinite Crafting, Max Power, Ultra Pickaxe, Faster MOLE
  - Toggle: Disable Encumbrance, Show Debug Coordinates
  - Toggle: Hide Machine Lights/Particles (performance)
  - Simulation Speed slider (0.1x - 10x)
  - Free Camera Mode buttons (Normal, Free, Scripted, Benchmark, Cheat)

- **Game Mode Tab**
  - Toggle: All Doors Unlocked, MOLE Bits Always Available, Infinite Ore
  - Player Speed slider (50% - 1000%)
  - MOLE Speed slider (50% - 1000%)

- **Machines Tab**
  - Smelter Speed slider (50% - 1000%)
  - Assembler Speed slider (50% - 1000%)
  - Thresher Speed slider (50% - 1000%)
  - Planter Speed slider (50% - 1000%)
  - Inserter Base Stack Size slider (1 - 100)

- **Power Tab**
  - Fuel Consumption slider (10% - 500%)
  - Power Consumption slider (10% - 500%)
  - Power Generation slider (50% - 1000%)

- **Special Tab**
  - Cat Sounds toggle with FMOD integration
  - Rainbow Cores toggle with live color preview
  - Placeholder Voice toggle for developer audio
  - Status display showing active systems

### Changed
- Settings now apply continuously for live updates
- Configurable GUI toggle hotkey (default Insert)
- GUI state persists in config

## [1.1.0] - 2025-01-04

### Added
- Cat Sounds special cheat (FMOD "Cat Cheat" parameter)
- Rainbow Cores visual effect for Memory Trees
- Placeholder Voice toggle for developer audio
- Harmony patches for special features

## [1.0.1] - 2025-01-03

### Changed
- Updated custom icon

## [1.0.0] - 2025-01-03

### Added
- Initial release
- PlayerCheats integration
- GameModeSettings integration
- Full BepInEx configuration
