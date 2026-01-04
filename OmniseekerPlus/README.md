# OmniseekerPlus

Enhanced Omniseeker scanner with extended range, more scannable types, and a powerful quick-scan feature!

## Features

### Extended Scanner Range
- **3x default range** (configurable up to 10x)
- Maximum scan range up to 2000 meters
- Find resources from much further away!

### New Scannable Types
- **Hard Drives** - Find data storage devices hidden in the world
- **Loot Containers** - Locate chests with items
- **Memory Trees** - Track all research core producers
- **Rare Ores** - Enhanced detection for Atlantum and other rare materials
- **All Ore Types** - Copper, Iron, Limestone, and more

### Scan Modes
Cycle through different scan modes with a hotkey:
1. **All** - Scan everything
2. **Ores Only** - Focus on resource veins
3. **Hard Drives** - Hunt for data storage
4. **Loot** - Find containers with items
5. **Memory Trees** - Locate research producers
6. **Custom** - Use your individual config settings

### Quick Scan Feature
Press a hotkey to instantly scan your surroundings and see:
- All nearby scannable objects
- Distance to each object
- Color-coded results (green=close, red=far)
- Object types

### In-Game GUI
Press **Shift + O** (default) to open the OmniseekerPlus GUI:
- View last scan results sorted by distance
- Change scan modes on the fly
- Toggle settings
- Perform quick scans

## Hotkeys

| Key | Action |
|-----|--------|
| O | Cycle scan mode |
| P | Perform quick scan |
| Shift+O | Toggle GUI |

## Configuration

All settings can be adjusted in the BepInEx config file or via a config manager:

### Scanner Range
- `Range Multiplier` - Multiply default range (1-10x)
- `Max Scan Range` - Maximum range in meters (100-2000m)
- `Extended Range Enabled` - Toggle extended range

### Scannable Types
- `Scan Hard Drives` - Enable hard drive detection
- `Scan Hidden Chests` - Enable hidden chest detection
- `Scan Rare Ores` - Enable rare ore detection
- `Scan All Ore Types` - Enable all ore types
- `Scan Loot Containers` - Enable loot container detection
- `Scan Memory Trees` - Enable Memory Tree detection

### Visual Settings
- `Show Distance on HUD` - Display distance to targets
- `Color Code by Distance` - Color results by range
- `Show Scan Pulse Effect` - Visual effect on scan
- `Pulse Intensity` - Effect intensity

## Installation

1. Install BepInEx 5.4.2100
2. Place `OmniseekerPlus.dll` in your `BepInEx/plugins/OmniseekerPlus/` folder
3. Launch the game and start scanning!

## Changelog

See CHANGELOG.md for version history.

## Credits

- **CertiFried** - Mod development
- **Techtonica Community** - Testing and feedback

## License

This mod is released under the GNU General Public License v3.0 (GPL-3.0).
