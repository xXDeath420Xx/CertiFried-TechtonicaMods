# Changelog

All notable changes to EnhancedLogistics will be documented in this file.

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
