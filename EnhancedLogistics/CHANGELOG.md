# Changelog

All notable changes to EnhancedLogistics will be documented in this file.

## [2.0.3] - 2026-01-04

### Fixed
- **CRITICAL**: Fixed broken Harmony patches targeting non-existent `InserterInstance.CheckPlacement` method
- This was causing the mod to fail to load with "Undefined target method" error

### Changed
- Inserter speed boost now properly patches `InserterDefinition.InitOverrideSettings` to multiply `cyclesPerMinute`
- Inserter range extension now properly patches `InserterDefinition.InitInstance` to extend `armLength`
- Both patches include proper error handling and logging

## [2.0.2] - 2026-01-04

### Fixed
- Changelog now properly included in Thunderstore package

## [2.0.1] - 2026-01-04

### Added
- Changelog metadata field added

## [2.0.0] - 2026-01-04

### Added
- **Drone Relay Network System**
  - Create relay stations by assigning storage chests to color-coded networks
  - 5 network colors: Cyan, Magenta, Orange, Lime, Purple
  - Relays within range automatically connect to extend coverage
  - Visual connection lines between linked relays
  - Multi-hop routing for extended delivery range

- **Drone Research Tier System (5 Tiers)**
  - Tier 1 (Basic): Base stats (x1.0)
  - Tier 2 (Improved): +25% speed/capacity/range, +2 drones
  - Tier 3 (Advanced): +50% all stats, +4 drones
  - Tier 4 (Superior): +100% all stats, +6 drones
  - Tier 5 (Ultimate): +150% all stats, +10 drones
  - Unlock tiers through the Research tab in Drone Menu

- **New Drone Menu Tabs (Press J)**
  - Status: View active drones, spawn/clear, monitor deliveries
  - Research: Unlock drone technology tiers
  - Relays: Configure relay networks, view coverage statistics

### Changed
- Drone stats now scale with research tier
- Drone window expanded with tabbed interface
- Maximum drones per network affected by research tier

## [1.0.1] - 2025-12-XX

### Added
- Custom mod icon

### Fixed
- Minor bug fixes

## [1.0.0] - 2025-01-04

### Added - Phase 3: Search UI System
- Universal search UI accessible via Ctrl+F
- Search across multiple categories:
  - Items (all game resources)
  - Recipes (crafting recipes with ingredients)
  - Tech Tree (unlocks and technologies)
  - Machines (buildable structures)
- Real-time search as you type
- Category tabs for filtered results
- Results sorted by relevance (name match priority)
- Draggable search window
- Item icons in search results
- Truncated descriptions for clean display

### Added - Phase 4: Storage Network
- Network channel system for connected storage
- Chest registration to network channels
- Auto-routing of items between networked chests
- Distance-based network limits (configurable 10-500m)
- Network visualization toggle

### Added - Phase 4: Better Logistics
- Inserter range multiplier (1-3x)
- Inserter speed multiplier (1-3x)
- Smart filtering system for inserters
- Configurable default filter stack sizes (1-100)
- Harmony patches for inserter enhancements

### Added - Phase 5: Drone Delivery System
- Complete drone delivery framework
- DroneManager for spawning and controlling drones
- Drone states: Idle, MovingToPickup, Loading, MovingToDropoff, Unloading, Returning
- Visual drone objects (glowing spheres)
- Drone capacity configuration (8-128 items)
- Drone speed configuration (5-50 units/second)
- Drone range configuration (50-500m)
- Delivery task queue system
- Drone management GUI (press J)
- Test drone spawning
- Drone cargo tracking
- Smooth movement with hover animation

### Configuration
- Full BepInEx configuration support
- Separate sections for each feature
- All features can be enabled/disabled independently
- Extensive range settings for customization
