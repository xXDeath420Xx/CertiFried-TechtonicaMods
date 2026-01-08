# Changelog

All notable changes to AdvancedMachines will be documented in this file.

## [2.4.2] - 2026-01-07

### Changed
- All items now appear in unified "Modded" build menu tab
- Updated dependency on TechtonicaFramework 1.2.0
- Improved compatibility with other CertiFried mods

### Fixed
- Build menu organization for cleaner UI

## [2.4.0] - 2026-01-06

### Changed
- Moved to Modded tech tree category
- Updated tier requirements

## [2.0.0] - 2025-01-04

### Added
- Full Phase 2 implementation from Techtonica Mod Development Plan
- **Hidden Variants Enabler**
  - Patch for `BuilderInfo.IsVariantAvailable` to unlock all hidden machine variants
  - Patch for `TechTreeState.IsVariantAvailable` for tech tree integration
  - Configurable via BepInEx config

### Tier 4 Machines
- **Smelter MKIV** - 2x processing speed, 225kW power
- **Assembler MKIII** - 2x crafting speed, 300kW power
- **Mining Drill MKIII** - 2x dig speed, 150kW power
- **Thresher MKII** - 2x processing speed, 120kW power
- **Planter MKIV** - 2.5x growth speed, 90kW power

### Tier 5 Machines (Endgame)
- **Smelter MKV** - 3x processing speed, 300kW power (requires Atlantum)
- **Assembler MKIV** - 3x crafting speed, 400kW power (requires Atlantum)
- **Mining Drill MKIV** - 3x dig speed, 200kW power (requires Atlantum)
- **Thresher MKIII** - 3x processing speed, 160kW power (requires Atlantum)
- **Planter MKV** - 4x growth speed, 120kW power (requires Atlantum)

### Tech Tree Integration
- Tier 4 machines unlock after existing Mk3 versions (Purple cores, Tier 2)
- Tier 5 machines unlock after Tier 4 + Atlantum research (Purple cores, Tier 3)
- Proper tech tree positioning relative to vanilla machines

### Configuration
- `Enable Hidden Variants` - Toggle hidden variant unlock
- `Enable Tier 4 Machines` - Toggle Tier 4 machine registration
- `Enable Tier 5 Machines` - Toggle Tier 5 machine registration

## [1.0.1] - 2025-01-03

### Changed
- Updated custom icon
- Minor bugfixes

## [1.0.0] - 2025-01-03

### Added
- Initial release
- Basic Tier 4/5 machine framework
- EMU 6.1.3 compatibility
