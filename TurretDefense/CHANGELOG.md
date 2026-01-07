# Changelog

All notable changes to TurretDefense will be documented in this file.

## [3.3.0] - 2025-01-06

### Added
- **Custom Defense Tab** in tech tree UI
  - New 8th category tab via Harmony reflection patches
  - All turret unlocks now appear in dedicated Defense tab
  - Proper navigation wrapping between categories
- Defense category button with "Defense" label
- `DefenseTabPatch.cs` for tech tree UI modifications

### Changed
- Turret unlocks moved from Energy category to Defense category (index 7)
- Version bump to 3.3.0

### Technical
- Added patches for TechTreeUI.Init, TechTreeGrid.Init, TechTreeCategoryButton.InitButton
- Added GetNeighboringCategories prefix for navigation handling
- Added TechTreeState patches for category mapping
- Added TextMeshPro and UnityEngine.UI references

## [3.2.0] - 2025-01-05

### Changed
- Fixed tech tree positions to avoid overlap with other mods
- Position range 210-220 reserved for defense systems
- Updated unlock category grouping with subHeaderTitle

## [3.1.0] - 2025-01-04

### Added
- Artillery system with Light, Medium, Heavy, and Auto variants
- Ground enemy types: Chomper, Spitter, Grenadier, Gunner, Mimic, Arachnid
- Alien Hive structures: Birther, Brain, Stomach, Terraformer, Claw, Crystal
- Hazard zone spawning from Terraformer hives

### Changed
- Improved alien AI targeting
- Enhanced material system for URP compatibility

## [3.0.0] - 2025-01-03

### Added
- Full buildable turret machines (Gatling, Rocket, Laser, Railgun, Lightning)
- Power grid integration for all turrets
- Turret machine patches for power consumption
- Visual customization based on turret type

### Changed
- Turrets now require power to operate
- Turrets appear in building menu under Defense Systems

## [2.1.0] - 2025-01-02

### Added
- Alien wave system with configurable intervals
- Multiple alien ship types (Fighter, Destroyer, Bio-Torpedo variants)
- Loot drops (Alien Power Core, Alien Alloy)
- Floating health bars for enemies
- Damage number display

## [2.0.0] - 2025-01-01

### Added
- Initial turret defense system
- Basic turret spawning
- Asset bundle loading system
- URP-compatible material handling

## [1.0.0] - 2024-12-30

### Added
- Initial release
- Basic mod structure
- Configuration system
