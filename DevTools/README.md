# DevTools

**Comprehensive Developer and Cheat Tools for Techtonica**

A powerful all-in-one mod providing extensive developer tools, cheat options, and game adjustments through an intuitive in-game GUI. Perfect for testing, debugging, experimenting with game mechanics, or simply enjoying Techtonica with additional customization options.

---

## Table of Contents

- [Features](#features)
  - [Player Tab](#player-tab)
  - [Game Mode Tab](#game-mode-tab)
  - [Machines Tab](#machines-tab)
  - [Power Tab](#power-tab)
  - [Special Tab](#special-tab)
- [Installation](#installation)
  - [Using r2modman (Recommended)](#using-r2modman-recommended)
  - [Manual Installation](#manual-installation)
- [How to Use](#how-to-use)
- [Configuration](#configuration)
  - [Configuration File Location](#configuration-file-location)
  - [Configuration Options](#configuration-options)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Troubleshooting](#troubleshooting)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

DevTools provides a comprehensive suite of developer and cheat tools organized into five intuitive tabs, all accessible through a draggable in-game GUI.

**Press `Insert` to open the DevTools GUI** (configurable)

### Player Tab

Player-focused cheats and settings:

| Feature | Description |
|---------|-------------|
| **Infinite Crafting** | Build machines without consuming resources |
| **Max Power** | All machines operate at full power regardless of network status |
| **Ultra Pickaxe** | Enhanced mining speed and power for faster resource gathering |
| **Faster MOLE** | MOLE vehicle operates at 4x normal speed |
| **Disable Encumbrance** | Remove all weight and inventory capacity limits |
| **Show Debug Coordinates** | Display debug position information on screen |
| **Hide Machine Lights** | Disable machine light effects for improved performance |
| **Hide Machine Particles** | Disable machine particle effects for improved performance |
| **Simulation Speed** | Adjust game simulation speed from 0.1x to 10x (slider + manual input) |
| **Free Camera Mode** | Switch between camera modes: Normal, Free, Scripted Animation, Benchmark, Cheat Animation |

### Game Mode Tab

World and game rule modifications:

| Feature | Description |
|---------|-------------|
| **All Doors Unlocked** | Instantly unlock all doors throughout the game world |
| **MOLE Bits Always Available** | All MOLE drill bit types are always accessible |
| **Infinite Ore** | Ore veins never deplete - unlimited mining |
| **Player Speed** | Adjust player movement speed from 50% to 1000% |
| **MOLE Speed** | Adjust MOLE digging speed from 50% to 1000% |

### Machines Tab

Fine-tune machine performance:

| Feature | Range | Description |
|---------|-------|-------------|
| **Smelter Speed** | 50% - 1000% | Adjust smelting speed for all smelter tiers |
| **Assembler Speed** | 50% - 1000% | Adjust assembly speed for all assembler tiers |
| **Thresher Speed** | 50% - 1000% | Adjust threshing speed for all thresher tiers |
| **Planter Speed** | 50% - 1000% | Adjust growth speed for planters |
| **Inserter Base Stack Size** | 1 - 100 | Set the base stack size for all inserters |

### Power Tab

Control power and fuel mechanics:

| Feature | Range | Description |
|---------|-------|-------------|
| **Fuel Consumption** | 10% - 500% | Lower values reduce fuel consumption |
| **Power Consumption** | 10% - 500% | Lower values reduce machine power draw |
| **Power Generation** | 50% - 1000% | Higher values increase power output from generators |

### Special Tab

Unique and fun features:

| Feature | Description |
|---------|-------------|
| **Cat Sounds** | Replace machine sounds with cat meowing via FMOD integration |
| **Rainbow Cores** | Memory Tree cores cycle through vibrant rainbow colors with live preview |
| **Placeholder Voice** | Enable developer placeholder voice lines (original temp audio recordings) |
| **Status Display** | Real-time status of Player Cheats and Game Mode Settings initialization |

---

## Installation

### Using r2modman (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) if you haven't already
2. Open r2modman and select Techtonica
3. Search for "DevTools" in the online mod browser
4. Click "Download" to install the mod and its dependencies automatically
5. Launch the game through r2modman

### Manual Installation

1. Install [BepInEx 5.4.21](https://github.com/BepInEx/BepInEx/releases) or newer
2. Install [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) 6.1.3 or newer
3. Download the DevTools DLL from the releases page
4. Place `DevTools.dll` in your `BepInEx/plugins` folder:
   ```
   Techtonica/
   └── BepInEx/
       └── plugins/
           └── DevTools.dll
   ```
5. Launch the game

---

## How to Use

1. **Launch Techtonica** with the mod installed
2. **Load a save file** or start a new game
3. **Press `Insert`** (default key) to open the DevTools GUI
4. **Navigate tabs** by clicking the tab buttons at the top of the window
5. **Toggle features** by clicking checkboxes
6. **Adjust sliders** by dragging or entering exact values in text fields
7. **Drag the window** by clicking and dragging the title bar
8. **Close the window** by pressing `Insert` again or clicking the "Close" button

### Tips

- All settings are saved automatically and persist between game sessions
- The GUI displays real-time status information in the Special tab
- Performance options (Hide Machine Lights/Particles) can significantly improve FPS in large factories
- Simulation Speed affects all game logic - use with caution at extreme values
- The window automatically stays within screen bounds

---

## Configuration

### Configuration File Location

Configuration files are generated on first launch and stored at:
```
BepInEx/config/com.certifired.DevTools.cfg
```

### Configuration Options

The configuration file is organized into the following sections:

#### [GUI]
| Option | Default | Description |
|--------|---------|-------------|
| `Toggle Key` | `Insert` | Key to toggle the DevTools GUI |
| `Show GUI` | `false` | Whether the GUI is visible on startup |

#### [PlayerCheats]
| Option | Default | Description |
|--------|---------|-------------|
| `Infinite Crafting` | `false` | Build without consuming resources |
| `Max Power` | `false` | Full power regardless of network |
| `Ultra Pickaxe` | `false` | Enhanced mining capability |
| `Faster MOLE` | `false` | 4x MOLE speed |
| `Disable Encumbrance` | `false` | No inventory limits |
| `Show Debug Coordinates` | `false` | Display position debug info |
| `Hide Machine Lights` | `false` | Disable light effects |
| `Hide Machine Particles` | `false` | Disable particle effects |
| `Simulation Speed` | `1` | Game speed (0.1 - 10.0) |
| `Free Camera Mode` | `0` | Camera mode (0-4) |

#### [GameModeSettings]
| Option | Default | Description |
|--------|---------|-------------|
| `All Doors Unlocked` | `false` | Unlock all doors |
| `MOLE Bits Always Available` | `false` | Access all drill bits |
| `Infinite Ore` | `false` | Ore never depletes |
| `Player Speed Percent` | `100` | Movement speed (50-1000%) |
| `MOLE Speed Percent` | `100` | Digging speed (50-1000%) |

#### [MachineSettings]
| Option | Default | Description |
|--------|---------|-------------|
| `Smelter Speed Percent` | `100` | Smelting speed (50-1000%) |
| `Assembler Speed Percent` | `100` | Assembly speed (50-1000%) |
| `Thresher Speed Percent` | `100` | Threshing speed (50-1000%) |
| `Planter Speed Percent` | `100` | Growing speed (50-1000%) |
| `Inserter Base Stack Size` | `1` | Stack size (1-100) |

#### [PowerSettings]
| Option | Default | Description |
|--------|---------|-------------|
| `Fuel Consumption Percent` | `100` | Fuel usage (10-500%) |
| `Power Consumption Percent` | `100` | Power draw (10-500%) |
| `Power Generation Percent` | `100` | Power output (50-1000%) |

#### [SpecialCheats]
| Option | Default | Description |
|--------|---------|-------------|
| `Cat Sounds` | `false` | Enable cat sound effects |
| `Rainbow Cores` | `false` | Enable rainbow Memory Tree cores |
| `Placeholder Voice` | `false` | Enable developer voice lines |

---

## Requirements

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21+ | Mod framework |
| [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) | 6.1.3+ | Utility library |

---

## Compatibility

- **Techtonica Version**: Compatible with the latest version
- **Multiplayer**: Not tested - use at your own risk in multiplayer
- **Other Mods**: Generally compatible with other mods; load order typically does not matter
- **.NET Framework**: 4.7.2

---

## Troubleshooting

### GUI doesn't appear when pressing Insert
- Ensure the mod is properly installed in the `BepInEx/plugins` folder
- Check the BepInEx console/log for error messages
- Verify that another mod hasn't bound the same key
- Try changing the toggle key in the config file

### Settings don't seem to apply
- Wait until you've fully loaded into a save (not the main menu)
- Check the Status section in the Special tab - both should show "Active"
- Some settings require active gameplay to take effect

### Game performance issues
- Try enabling "Hide Machine Lights" and "Hide Machine Particles"
- Reduce Simulation Speed if running at high values
- Rainbow Cores may impact performance in areas with many Memory Trees

### Configuration file issues
- Delete the config file to regenerate defaults
- Ensure values are within the acceptable ranges listed above

---

## Changelog

### [2.2.0] - Latest
- Enhanced configuration system
- Improved GUI stability

### [2.1.7] - 2025-01-05
- Updated mod icon

### [2.0.0] - 2025-01-04
- Complete GUI overhaul with tabbed interface
- Draggable window system
- Live value updates and real-time preview
- All cheat categories organized into intuitive tabs
- Added Special tab with Cat Sounds, Rainbow Cores, and Placeholder Voice

### [1.0.0] - 2025-01-03
- Initial release
- PlayerCheats integration
- GameModeSettings integration
- Basic cheat functionality

---

## Credits

### Development
- **Certifired** - Primary developer and maintainer

### Development Assistance
- **Claude Code** (Anthropic) - AI-assisted development, code optimization, and documentation

### Special Thanks
- **Fire Hose Games** - For creating Techtonica
- **Equinox** - For EquinoxsModUtils library
- **Techtonica Modding Community** - For support and feedback

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

You are free to:
- Use, copy, and distribute this mod
- Modify and create derivative works
- Use for commercial purposes

Under the following conditions:
- Source code must be made available when distributing
- Modifications must be released under the same license
- Original copyright and license notices must be preserved

For the full license text, see: [GNU GPL v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html)

---

## Links

- **Thunderstore**: [DevTools on Thunderstore](https://thunderstore.io/c/techtonica/p/Certifired/DevTools/)
- **BepInEx**: [BepInEx GitHub](https://github.com/BepInEx/BepInEx)
- **EquinoxsModUtils**: [EquinoxsModUtils on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- **Techtonica**: [Official Website](https://www.techtonicagame.com/)
- **Techtonica Discord**: Community support and discussion

---

*DevTools is a fan-made mod and is not affiliated with or endorsed by Fire Hose Games.*
