# CertiFried Techtonica Mods

A collection of mods for [Techtonica](https://store.steampowered.com/app/1457320/Techtonica/), a first-person factory automation game set in an alien underground.

## Mod List

### Core Framework
| Mod | Description |
|-----|-------------|
| **TechtonicaFramework** | Core framework providing shared systems for all CertiFried mods. Includes health, narrative, equipment, environment systems, recipe cache fixes, and multiplayer compatibility. |
| **MultiplayerFixes** | Comprehensive multiplayer stability fixes and error handling. |

### Production & Machines
| Mod | Description |
|-----|-------------|
| **AdvancedMachines** | Adds Mk4 and Mk5 variants of smelters, assemblers, drills, threshers, and planters. |
| **AtlantumReactor** | 20MW endgame power generator using Atlantum Mixture Bricks and Shiverthorn Coolant. |
| **AtlantumEnrichment** | Adds enrichment recipes for Atlantum processing. |
| **MorePlanters** | Additional planter varieties for advanced farming. |
| **PlanterCoreClusters** | Planter optimizations and core cluster support. |

### Logistics & Automation
| Mod | Description |
|-----|-------------|
| **EnhancedLogistics** | Improved inserters and logistics systems. |
| **WormholeChests** | Linked storage containers for item teleportation. |
| **DroneLogistics** | Autonomous drone delivery systems. |
| **CoreComposerPlus** | Batch processing and speed options for the Memory Tree (Core Composer). |

### Quality of Life
| Mod | Description |
|-----|-------------|
| **DevTools** | Developer/debugging utilities for modders and players. |
| **OmniseekerPlus** | Enhanced scanning capabilities. |
| **BeltImmunity** | Player immunity to belt movement. |
| **Restock** | Quick restocking utilities. |
| **ChainExplosives** | Chain reaction demolition. |

### Survival & Combat
| Mod | Description |
|-----|-------------|
| **SurvivalElements** | Machine health, repair mechanics, and survival systems. |
| **HazardousWorld** | Environmental hazards and protective equipment. |
| **TurretDefense** | Automated defense turrets (Work in Progress). |

### Exploration & Movement
| Mod | Description |
|-----|-------------|
| **MobilityPlus** | Enhanced movement equipment: Stilts Mk2/Mk3, Speed Boots, Jump Pack. |

### Narrative & World
| Mod | Description |
|-----|-------------|
| **NarrativeExpansion** | Extended dialogue and story content. |
| **AmbientLife** | Ambient creatures and environmental life. |
| **PetsCompanions** | Pet companion system. |

### Processing
| Mod | Description |
|-----|-------------|
| **BioProcessing** | Biological processing recipes and machines. |
| **Recycler** | Resource recycling systems. |

## Installation

1. Install [BepInEx](https://github.com/BepInEx/BepInEx) for Techtonica
2. Install [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
3. Install [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
4. Download desired mods from [Thunderstore](https://thunderstore.io/c/techtonica/)
5. Place mod DLLs in `BepInEx/plugins/`

## Dependencies

Most mods require:
- BepInEx 5.4.21+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+
- TechtonicaFramework 1.4.0+ (for CertiFried mods)

## Multiplayer Support

All CertiFried mods are designed with multiplayer in mind:
- MultiplayerFixes provides stability patches
- TechtonicaFramework handles mod registration and sync
- Graceful handling of mod mismatches between players

## License

All mods are released under GPL 3.0 unless otherwise specified.

## Contributing

Issues and pull requests welcome at [GitHub](https://github.com/xXDeath420Xx/CertiFried-TechtonicaMods).

## Credits

- **CertiFried** - Mod author
- **Equinox** - EquinoxsModUtils and EMUAdditions
- **Fire Hose Games** - Techtonica developers
