# EnhancedLogistics

A comprehensive logistics enhancement mod for Techtonica featuring Search UI, Storage Networks, Better Logistics, and a Drone Delivery System!

## Features

### Phase 3: Search UI System (Ctrl+F)

A powerful search function that works across all game UIs:

- **Search Items** - Find any item by name or description
- **Search Recipes** - Locate crafting recipes and their ingredients
- **Search Tech Tree** - Find technologies and unlocks
- **Search Machines** - Locate buildable machines

**Usage:**
- Press `Ctrl+F` to open the search window
- Type to search in real-time
- Use category tabs to filter results
- Results are sorted by relevance

### Phase 4: Storage Network

Extend the wormhole chest concept with automatic item routing:

- **Network Channels** - Connect multiple chests to the same network
- **Auto-Routing** - Items automatically flow to chests with space
- **Visual Connections** - See network connections between storage
- **Distance-Based** - Networks work within configurable range

### Phase 4: Better Logistics

Enhanced inserter and logistics capabilities:

- **Extended Inserter Range** - 1.5x default reach (configurable up to 3x)
- **Faster Inserters** - 1.5x operation speed (configurable up to 3x)
- **Smart Filtering** - Intelligent item filtering for inserters
- **Configurable Stack Sizes** - Set default filter stack sizes

### Phase 5: Drone Delivery System

Automated aerial item transport using floating drones:

- **Drone Hubs** - Deploy and manage delivery drones
- **Point-to-Point Delivery** - Route items between storage locations
- **Visual Drones** - Glowing orb drones with smooth movement
- **Configurable Capacity** - 8-128 items per drone
- **Adjustable Speed** - 5-50 units/second
- **Range Limits** - 50-500m maximum delivery distance

**Drone Controls:**
- Press `J` to open Drone Management menu
- Create test drones for experimentation
- Monitor active drones and pending deliveries
- View drone cargo and status

## Hotkeys

| Key | Action |
|-----|--------|
| Ctrl+F | Toggle Search UI |
| J | Toggle Drone Menu |
| Escape | Close Search UI |

## Configuration

All features can be configured via BepInEx config:

### Search UI
- `Toggle Key` - Key to toggle search (default: F)
- `Enable Search UI` - Enable/disable search feature
- `Search Inventory/Crafting/TechTree/BuildMenu` - Toggle search categories

### Storage Network
- `Enable Storage Network` - Enable network features
- `Max Network Distance` - Maximum connection distance (10-500m)
- `Auto Route Items` - Enable automatic item routing
- `Show Network Visualization` - Display network connections

### Better Logistics
- `Enable Better Logistics` - Enable logistics enhancements
- `Inserter Range Multiplier` - Reach multiplier (1-3x)
- `Inserter Speed Multiplier` - Speed multiplier (1-3x)
- `Smart Filtering` - Enable intelligent filtering
- `Default Filter Stack Size` - Default stack size (1-100)

### Drone System
- `Enable Drone System` - Enable drone delivery
- `Drone Capacity` - Items per drone (8-128)
- `Drone Speed` - Movement speed (5-50 u/s)
- `Drone Range` - Max delivery range (50-500m)
- `Drone Menu Key` - Key to open drone menu (default: J)

## Installation

1. Install BepInEx 5.4.2100
2. Place `EnhancedLogistics.dll` in `BepInEx/plugins/EnhancedLogistics/`
3. Launch the game

## Changelog

See CHANGELOG.md for version history.

## Credits

- **CertiFried** - Development
- **Techtonica Community** - Testing and feedback

## License

GNU General Public License v3.0 (GPL-3.0)
