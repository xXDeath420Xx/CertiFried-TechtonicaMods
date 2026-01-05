# Changelog

All notable changes to AtlantumReactor_Updated will be documented in this file.

## [4.0.4] - 2026-01-04

### Fixed
- Removed broken `MachineManager.RemoveMachine` Harmony patch (method doesn't exist in game)
- Reactor tint cleanup now happens naturally when visual is destroyed

## [4.0.3] - 2026-01-04

### Fixed
- Changelog now properly included in Thunderstore package

## [4.0.2] - 2026-01-04

### Added
- Changelog metadata field added

## [4.0.1] - 2026-01-04

### Changed
- Updated custom icon
- Republished as AtlantumReactor_Updated
- Maintained attribution to Equinox (original author)

## [4.0.0] - 2025-01-04

### Changed - COMPLETE REWRITE
- **Fixed fundamental architecture** - Now properly extends PowerGeneratorDefinition instead of MemoryTreeDefinition
- **Proper power generation** - Uses RuntimePowerSettings with negative kWPowerConsumption for generation
- **Working fuel system** - Accepts Atlantum Mixture Brick and Shiverthorn Coolant as fuel
- **Fuel consumption** - Proper fuel burn rate tied to power generation

### Added
- **Green visual tint** - Bright radioactive green material on reactor model
- **Emission glow** - Green emissive shader effect for visibility
- **OmniseekerPlus integration** - AtlantumOreVein scanning support
- **Configurable power output** - 1000kW default, adjustable via config
- **Fuel burn rate config** - Adjustable fuel consumption speed

### Technical Details
- Uses `isGenerator = true` flag for proper power network integration
- MaterialPropertyBlock for shader modifications (performance-friendly)
- Harmony patches for:
  - Power generation (ReactorPowerPatches)
  - Fuel acceptance (AcceptCustomFuels)
  - Visual effects (ReactorVisualPatches)
  - Omniseeker support (OmniseekerPatches)

### Fixed
- Reactor now actually generates power (was completely non-functional before)
- Fuel items can now be inserted via inserters
- Power shows correctly in power network UI
- Works with existing power infrastructure

## [3.0.0] - Previous

### Known Issues (Now Fixed)
- Extended MemoryTreeInstance instead of PowerGeneratorInstance
- Had no fuel inventory system
- Had no actual power generation capability
- Items couldn't be inserted into reactor

## [1.0.0] - Initial Release

- First attempt at Atlantum Reactor
- Based on Memory Tree model (incorrect approach)
