# Changelog

All notable changes to AtlantumReactor will be documented in this file.

## [4.1.0] - 2026-01-07

### Fixed
- **Fuel input ports** - Changed visual model from Crank Generator to Smelter MKII for proper conveyor/inserter fuel input
- **Inventory stacking** - Reactor now properly stacks to 50 in inventory (was incorrectly set to 1)
- **Fuel consumption rate** - Added proper fuelConsumptionRate (0.1f) for slow, efficient fuel burning

### Changed
- Visual model now uses Smelter MKII instead of Crank Generator MKII
- Model has proper input ports for fuel via belts and inserters

## [4.0.11] - 2026-01-07

### Changed
- All items now appear in unified "Modded" build menu tab
- Updated dependency on TechtonicaFramework 1.2.0

### Fixed
- Build menu organization for cleaner UI

## [4.0.10] - 2025-01-06

### Fixed
- **Moved to Modded category** - Reactor unlock now appears in the Modded tech tree tab instead of vanilla Energy category
- **Fixed vanilla tech tree conflicts** - No longer overlaps with HVC Reach IV and other vanilla items
- **Added TechtonicaFramework dependency** - Uses ModdedTabModule for proper tech tree placement

### Changed
- Research tier set to Tier7 (endgame)
- Tree position set to 100 for clear visibility in Modded tab

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
