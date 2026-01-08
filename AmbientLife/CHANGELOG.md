# Changelog

All notable changes to AmbientLife will be documented in this file.

## v2.0.4 - 2026-01-07

### Added
- Animation support for creature prefabs (spiders, wolves, etc.)
- Automatic detection of Animator components on loaded models
- Support for common animation parameters (Speed, IsWalking, IsRunning, Velocity)
- Creatures now animate properly when moving
- **Story-Based Spawning System**: Creatures tie into game progression!
  - Pre-anomaly: Organic wildlife (spiders, bugs, animals)
  - Post-anomaly (reach -200m depth): Organic life dies off, replaced by mechanical entities
  - New mechanical creatures: Scout Drones, Repair Bots, Sentinel Mechs, Alien Gliders
  - Visual death effect when anomaly triggers
  - Configurable anomaly depth threshold
- Config options for story integration (can be disabled)

### Fixed
- Spiders and other crawling creatures now play walk/idle animations
- Flying creatures animate based on movement speed

## v2.0.3 - 2026-01-07

### Fixed
- Rebuilt all creature AssetBundles with scripts stripped at build time
- Removed ithappy.Animals_FREE scripts (CreatureMover, MovePlayerInput) from prefabs before bundling
- Eliminates "missing script" warnings at runtime - no more log spam
- Bundles are now clean and load without any script reference errors

## v2.0.2 - 2026-01-07

### Fixed
- Fixed "Object reference not set" errors when spawning creatures with missing scripts
- Improved error handling for AssetBundle prefabs with missing script components
- Creatures now spawn reliably even if prefab has broken script references

## v2.0.1 - 2026-01-07

### Fixed
- Included creature AssetBundles in package (spiders, beetles, wolf, tortoise, animals, gamekit)
- Creatures now use real 3D models instead of fallback procedural geometry

## v2.0.0 - 2026-01-07

### Added
- **Smart AI System** - Complete rewrite with intelligent pathfinding
- **13 Creature Types** across 4 categories
- **Wall Crawling** - Spiders climb walls and ceilings
- **Terrain Following** - Ground creatures follow slopes properly
- **Obstacle Avoidance** - 360-degree collision detection
- **In-Game UI** - Press F8 to configure (per-creature toggles, density sliders)
- **AssetBundle Support** - Real 3D models when available
- **Dynamic Fleeing** - Shy creatures flee from player

### Creatures
- Flying: Butterflies, Fireflies, Flying Beetles, Nightmare Beetles
- Spiders: Brown, Green, Black (wall crawlers)
- Ground: Dogs, Cats, Chickens, Deer, Wolves, Penguins

## v1.0.2 - 2025-01-05
- Added CHANGELOG.md
- Updated icon format

## v1.0.1 - 2025-01-05
- Updated icon

## v1.0.0 - 2025-01-04
- Initial release
- 5 ambient creature types (Butterflies, Birds, Fireflies, Moths, Dragonflies)
- Wandering AI behavior
- Wing animation simulation
- Firefly glow effects
- Distance-based spawn/despawn
