# Changelog

All notable changes to SeamlessWorld will be documented in this file.

## [0.1.0] - 2026-01-07

### Added
- Initial release
- **FlowManager Patches** - Intercept strata transitions to load without unloading
- **VoxelManager Patches** - Expand voxel bounds across multiple strata
- **Player Patches** - Allow free vertical movement between floors
- **Machine Patches** - Keep machines active across all loaded strata
- **Power Network Patches** - Foundation for cross-strata power grids
- Configurable floor separation distance
- Memory management via Max Loaded Strata setting
- Debug logging mode

### Known Issues
- Elevator UI displays original floor selection (cosmetic)
- Cross-strata conveyor connections not yet implemented
- Power line visual rendering may not extend across floor boundaries
