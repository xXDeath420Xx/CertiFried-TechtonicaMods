# EnhancedLogistics

A comprehensive logistics enhancement mod for Techtonica featuring Drone Delivery with Relay Networks, Research Tiers, Search UI, and Storage Networks!

## Features

### Drone Delivery System (Press J)

Automated aerial item transport using floating drones with upgradeable technology!

**Core Features:**
- **Visual Drones** - Glowing orb drones with smooth movement
- **Point-to-Point Delivery** - Route items between storage locations
- **Configurable Settings** - Capacity, speed, and range options

**NEW: Drone Relay Networks**
- **Relay Stations** - Paint storage chests with special colors to create relay points
- **Extended Coverage** - Chain relays together to extend drone network range
- **Multiple Networks** - 5 color-coded networks (Cyan, Magenta, Orange, Lime, Purple)
- **Visual Connections** - See network links between relays
- **Multi-Hop Routing** - Drones navigate through connected relay chains

**NEW: Drone Research Tiers (1-5)**

| Tier | Name | Speed | Capacity | Range | Bonus Drones |
|------|------|-------|----------|-------|--------------|
| 1 | Basic | x1.0 | x1.0 | x1.0 | +0 |
| 2 | Improved | x1.25 | x1.25 | x1.25 | +2 |
| 3 | Advanced | x1.5 | x1.5 | x1.5 | +4 |
| 4 | Superior | x2.0 | x2.0 | x2.0 | +6 |
| 5 | Ultimate | x2.5 | x2.5 | x2.5 | +10 |

### Search UI System (Ctrl+F)

A powerful search function that works across all game data:

- **Search Items** - Find any item by name or description
- **Search Recipes** - Locate crafting recipes
- **Search Tech Tree** - Find technologies and unlocks
- **Search Machines** - Locate buildable machines

### Storage Network

Extend the wormhole chest concept with automatic item routing:

- **Network Channels** - Connect multiple chests to the same network
- **Auto-Routing** - Items automatically flow to chests with space
- **Visual Connections** - See network connections between storage

### Better Logistics

Enhanced inserter capabilities:

- **Extended Inserter Range** - 1.5x default reach (up to 3x)
- **Faster Inserters** - 1.5x operation speed (up to 3x)
- **Smart Filtering** - Intelligent item filtering

## Hotkeys

| Key | Action |
|-----|--------|
| Ctrl+F | Toggle Search UI |
| J | Toggle Drone Menu |
| Escape | Close Search UI |

## Drone Menu Tabs

1. **Status** - View active drones, spawn/clear, monitor deliveries
2. **Research** - Unlock drone technology tiers for better stats
3. **Relays** - Configure relay networks, view coverage

## Creating Drone Relays

1. Place a Storage Chest
2. Paint it with a relay color (paint indices 10-14):
   - 10: Cyan (Network A)
   - 11: Magenta (Network B)
   - 12: Orange (Network C)
   - 13: Lime (Network D)
   - 14: Purple (Network E)
3. Chests on the same color network will link automatically
4. Relays within range of each other extend the network
5. Drones can deliver anywhere within a connected relay chain

## Configuration

### Drone System
- `Enable Drone System` - Enable drone delivery
- `Drone Capacity` - Base items per drone (8-128)
- `Drone Speed` - Base movement speed (5-50 u/s)
- `Drone Range` - Base delivery range (50-500m)

### Drone Relays
- `Enable Drone Relays` - Enable relay system
- `Relay Base Range` - Base range per relay (50-500m)
- `Show Relay Network` - Display visual connections
- `Max Drones Per Network` - Base drone limit (1-20)

### Drone Research
- `Current Drone Tier` - Active research tier (1-5)
- `Auto Unlock Tiers` - Dev mode: auto-unlock all

### Search/Storage/Logistics
- See config file for full options

## Installation

1. Install BepInEx 5.4.2100
2. Place `EnhancedLogistics.dll` in `BepInEx/plugins/`
3. Launch the game and press J to access drone menu

## Changelog

### v2.0.0
- Added Drone Relay Network system with color-coded networks
- Added 5-tier Drone Research system with stat multipliers
- New tabbed Drone Menu with Status/Research/Relays tabs
- Visual relay connections and network coverage display
- Tiered stats affect speed, capacity, range, and max drones

### v1.0.1
- Added custom icon
- Minor fixes

### v1.0.0
- Initial release with Search UI, Storage Network, Better Logistics, Drone System

## Credits

- **CertiFried** - Development
