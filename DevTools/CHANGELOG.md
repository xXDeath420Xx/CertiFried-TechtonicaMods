# Changelog

All notable changes to DevTools will be documented in this file.

## [2.1.4] - 2026-01-04

### Fixed
- Removed broken `GameState.OnGameModeSettingsChanged` Harmony patch (method doesn't exist in game)
- Cat Sounds setting now applied via Update loop instead

## [2.1.3] - 2026-01-04

### Fixed
- Changelog now properly included in Thunderstore package

## [2.1.2] - 2026-01-04

### Fixed
- F8 hotkey now has fallback detection (also try Shift+F7 as alternative)
- Added error handling to prevent Harmony patch failures from breaking the mod
- Improved logging for debugging infinite crafting and hotkey issues

### Changed
- Better error handling throughout Update loop
- Added detailed logging when cheats are applied

## [2.1.1] - 2026-01-04

### Added
- Changelog now included in Thunderstore package

## [2.1.0] - 2026-01-04

### Added
- **Protection Tab** - CasperProtections features integrated
  - Disable Protection Zones: Dig/build anywhere, ignore protected areas
  - Disable Safe Sprint: Allow sprinting inside buildings
  - Disable Prebuild Protections: Allow erasing prebuilt/developer objects
  - Sprint Hold Mode: Only sprint while holding the sprint key

### Technical
- Harmony patches for GridManager, VoxelManager, SafeSpeedTriggerZoneData, PlayerFirstPersonController
- Full integration with existing DevTools GUI
- All settings persist via BepInEx config

## [2.0.0] - 2025-01-04

### Added
- **Complete In-Game GUI** (Press F8 to toggle)
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
- Configurable GUI toggle hotkey (default F8)
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
