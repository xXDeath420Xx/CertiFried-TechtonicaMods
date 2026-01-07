# Changelog

All notable changes to EMUBuilder (Community Patched) will be documented in this file.

## [1.1.2] - 2026-01-03

### Changed
- Published to Thunderstore with proper packaging and metadata
- Verified compatibility with latest EMU 6.1.3

## [1.1.1] - 2026-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [1.1.0] - 2026-01-02

### Added
- **Extended machine type support for Blueprints copy/paste:**
  - `MachineTypeEnum.Chest` (3) - Enables WormholeChests blueprint support
  - `MachineTypeEnum.Planter` (10) - Enables MorePlanters MKII/III blueprint support
  - `MachineTypeEnum.PowerGenerator` (11) - Power generator blueprint support
  - `MachineTypeEnum.WaterWheel` (23) - Water wheel blueprint support
  - `MachineTypeEnum.Accumulator` (24) - Accumulator blueprint support
  - `MachineTypeEnum.HighVoltageCable` (25) - HV cable blueprint support
  - `MachineTypeEnum.VoltageStepper` (26) - Voltage stepper blueprint support
  - `MachineTypeEnum.MemoryTree` (30) - Enables AtlantumReactor blueprint support

### Changed
- **Patched `MachineBuilder.SupportedMachineTypes` array:**
  - Original array had 15 machine types
  - Patched array now has 23 machine types
  - Added via IL patching of static constructor

- **Patched `MachineBuilder.BuildMachine()` switch statement:**
  - Added cases for all 8 new machine types
  - All new machine types use `DoSimpleBuild()` method for construction
  - Enables Blueprints mod to properly reconstruct these machines from saved blueprints

### Technical Details
- Patching performed via dnlib IL manipulation
- Static field `SupportedMachineTypes` patched in `.cctor` (static constructor)
- Switch statement patched by injecting additional case labels pointing to `DoSimpleBuild` call
- Original EMUBuilder.dll backed up before patching

### Compatibility
- Requires EquinoxsModUtils 6.1.3+
- Requires EquinoxsDebuggingTools 2.0.0+
- Compatible with Blueprints mod for extended copy/paste functionality

## [1.0.0] - Original Release by Equinox

- Initial release of EMUBuilder
- Base machine type support for Blueprints mod
