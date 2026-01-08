# Atlantum Reactor

[![License: GPL v3](https://img.shields.io/badge/License-GPLv3-blue.svg)](https://www.gnu.org/licenses/gpl-3.0)
[![Techtonica](https://img.shields.io/badge/Game-Techtonica-orange.svg)](https://store.steampowered.com/app/1457320/Techtonica/)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.21+-yellow.svg)](https://github.com/BepInEx/BepInEx)

A high-output endgame power generation mod for **Techtonica** that adds the Atlantum Reactor - an advanced nuclear reactor capable of generating massive amounts of power using Atlantum Mixture Bricks and Shiverthorn Coolant.

---

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Reactor Specifications](#reactor-specifications)
- [Requirements](#requirements)
- [Installation](#installation)
- [How to Use](#how-to-use)
- [Configuration](#configuration)
- [Crafting Recipe](#crafting-recipe)
- [Technical Details](#technical-details)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Overview

The Atlantum Reactor is designed for players who have reached the endgame and need substantial power generation to support large-scale factory operations. With a default output of **20 MW (20,000 kW)**, a single reactor can replace dozens of conventional generators, significantly reducing complexity and improving base aesthetics.

The reactor utilizes a dual-fuel system requiring both **Atlantum Mixture Bricks** as the primary fuel source and **Shiverthorn Coolant** to maintain safe operating temperatures. This creates interesting logistics challenges while providing immense power output.

---

## Features

### Power Generation
- **20 MW Default Output** - Massive power generation suitable for endgame bases
- **Configurable Power Output** - Adjust from 1 MW to 100 MW via configuration file
- **Continuous Operation** - Generates power as long as fuel is supplied

### Dual Fuel System
- **Primary Fuel: Atlantum Mixture Brick** - High-density fuel with extended burn time (fuel value: 200)
- **Secondary Fuel: Shiverthorn Coolant** - Required coolant to maintain reactor operation (fuel value: 100)
- **Balanced Consumption** - Both fuels are consumed during operation

### Visual Effects
- **Distinctive Green Glow** - Bright radioactive green color scheme for easy identification
- **Emission Effects** - Glowing emission materials for an authentic reactor appearance
- **Custom Icon** - Nuclear/radiation symbol with green tint in research tree and inventory

### Research Integration
- **Tier 18 Research** - Located in the endgame research tier
- **Modded Category** - Placed in the dedicated Modded tech tree tab
- **Blue Core Requirement** - Requires 1,000 Blue Research Cores to unlock

---

## Reactor Specifications

| Specification | Value |
|--------------|-------|
| Power Output | 20,000 kW (20 MW) default |
| Power Range | 1,000 - 100,000 kW (configurable) |
| Primary Fuel | Atlantum Mixture Brick |
| Secondary Fuel | Shiverthorn Coolant |
| Research Tier | Tier 18 |
| Research Cost | 1,000 Blue Cores |
| Tech Category | Modded |
| Crafting Method | Assembler |
| Craft Time | 30 seconds |
| Stack Size | 1 |

---

## Requirements

### Required Dependencies

| Mod | Minimum Version | Purpose |
|-----|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21+ | Mod framework |
| [EquinoxsModUtils (EMU)](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) | 6.1.3+ | Core modding utilities |
| [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) | 2.0.0+ | Machine and recipe creation |
| [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/) | Latest | Tech tree and build menu integration |

### Game Requirements
- Techtonica (Steam)
- Access to endgame content (Atlantum Mixture Bricks, Shiverthorn Coolant)

---

## Installation

### Method 1: r2modman / Thunderstore Mod Manager (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
2. Select **Techtonica** as your game
3. Search for "**Atlantum Reactor**" in the mod browser
4. Click **Install** - dependencies will be installed automatically
5. Launch the game through the mod manager

### Method 2: Manual Installation

1. Ensure all required dependencies are installed
2. Download the latest release from Thunderstore
3. Extract the contents to your BepInEx plugins folder:
   ```
   [Game Directory]/BepInEx/plugins/AtlantumReactor/
   ```
4. The folder should contain:
   - `AtlantumReactor.dll`
5. Launch the game

### Verifying Installation

1. Start the game and load a save
2. Open the console (if enabled) to verify the mod loaded successfully
3. Check the research tree under the **Modded** category for "Atlantum Reactor"

---

## How to Use

### Step 1: Research the Reactor

1. Progress through the game until you reach **Tier 18** research
2. Navigate to the **Modded** category in the tech tree
3. Research the **Atlantum Reactor** unlock (requires 1,000 Blue Cores)

### Step 2: Craft the Reactor

Once researched, craft the reactor in an **Assembler** using the following recipe:

| Ingredient | Quantity |
|------------|----------|
| Crank Generator MKII | 5 |
| Steel Frame | 20 |
| Processor Unit | 10 |
| Cooling System | 10 |
| Atlantum Mixture Brick | 5 |

### Step 3: Place and Connect

1. Select the Atlantum Reactor from your build menu
2. Place it in your factory
3. Connect to your power grid using power lines

### Step 4: Supply Fuel

1. Connect **Atlantum Mixture Brick** supply via conveyor belt to the reactor's fuel input
2. Connect **Shiverthorn Coolant** supply to the reactor's secondary fuel input
3. The reactor will begin generating power once both fuels are available

### Tips for Optimal Use

- **Automate fuel production** - Set up dedicated production lines for both fuel types
- **Use storage buffers** - Keep fuel reserves to prevent power interruptions
- **Monitor consumption** - The dual-fuel system means you need to balance both supply chains
- **Plan power distribution** - 20 MW is substantial; ensure your grid can handle the output

---

## Configuration

The mod creates a configuration file at:
```
BepInEx/config/com.certifired.AtlantumReactor.cfg
```

### Available Options

#### [Power] Section

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `Power Output (kW)` | 20000 | 1000 - 100000 | Power output in kilowatts. 20000 = 20 MW |

#### [General] Section

| Option | Default | Description |
|--------|---------|-------------|
| `Debug Mode` | false | Enable verbose debug logging to console |

### Example Configuration

```ini
[Power]
## Power output in kW (20000 = 20MW)
# Setting type: Int32
# Default value: 20000
# Acceptable value range: From 1000 to 100000
Power Output (kW) = 20000

[General]
## Enable verbose debug logging
# Setting type: Boolean
# Default value: false
Debug Mode = false
```

### Adjusting Power Output

To change the reactor's power output:

1. Close the game
2. Open the configuration file in a text editor
3. Modify `Power Output (kW)` to your desired value
4. Save the file and restart the game

**Note:** Changes take effect on game restart. Existing reactors will use the new power value.

---

## Crafting Recipe

### Atlantum Reactor

**Crafting Station:** Assembler
**Craft Time:** 30 seconds

| Ingredient | Quantity | Notes |
|------------|----------|-------|
| Crank Generator MKII | 5 | Base power generation structure |
| Steel Frame | 20 | Structural support |
| Processor Unit | 10 | Control systems |
| Cooling System | 10 | Heat management |
| Atlantum Mixture Brick | 5 | Nuclear core material |

---

## Technical Details

### Architecture

The Atlantum Reactor is built on Techtonica's `PowerGeneratorDefinition` system, extending the game's native power generation mechanics. This ensures compatibility with the game's power grid and simulation systems.

### Fuel System

- Uses the game's built-in fuel consumption mechanics
- Both fuel types are registered with `fuelAmount` values:
  - Atlantum Mixture Brick: 200 (long burn time)
  - Shiverthorn Coolant: 100 (moderate burn time)

### Power Generation

- Power is generated via Harmony patches on `PowerGeneratorInstance.SimUpdate`
- Power output is set as negative consumption (generation) on the power grid
- Fully compatible with the game's power distribution system

### Visual System

- Uses `MaterialPropertyBlock` for efficient visual modifications
- Applies green tint and emission to reactor mesh renderers
- Tracks tinted instances to prevent redundant processing

---

## Changelog

### [4.0.11] - Current
- Stable release with all features working

### [4.0.7] - 2025-01-05
- Fixed icon path for embedded resource
- Version sync with Thunderstore

### [4.0.0] - 2025-01-04
- Complete rewrite with proper `PowerGeneratorDefinition` architecture
- Fixed power generation (was completely non-functional in previous versions)
- Working dual-fuel system with Atlantum Mixture Brick and Shiverthorn Coolant
- Green visual tint with emission glow effects
- Configurable power output (default 20 MW)
- Custom nuclear/radiation icon

### [3.0.0] - Previous
- Architecture issues identified and documented

### [1.0.0] - Initial Release
- First implementation attempt (incorrect architecture)

---

## Credits

### Original Author
- **Equinox** - [Original AtlantumReactor](https://thunderstore.io/c/techtonica/p/Equinox/AtlantumReactor/) - Original mod concept and implementation

### Updated By
- **CertiFried** - Updated for EMU 6.x API compatibility and maintained

### Development Assistance
- **Claude Code** (Anthropic) - AI-assisted development, code architecture, and documentation

### Dependencies
- **Equinox** - [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) and [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
- **CertiFried** - [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/)
- **BepInEx Team** - [BepInEx](https://github.com/BepInEx/BepInEx) modding framework
- **Harmony** - [Harmony](https://github.com/pardeike/Harmony) patching library

### Special Thanks
- The Techtonica modding community
- Fire Hose Games for creating Techtonica

---

## License

This project is licensed under the **GNU General Public License v3.0** (GPL-3.0).

This means you are free to:
- **Use** - Use the mod for any purpose
- **Study** - Access and learn from the source code
- **Share** - Redistribute the mod
- **Modify** - Create derivative works

Under the following conditions:
- **Disclose Source** - Source code must be made available when distributing
- **License and Copyright Notice** - Include the original license and copyright
- **Same License** - Derivative works must use the same license
- **State Changes** - Document changes made to the code

For the full license text, see: [GNU GPL v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html)

---

## Links

### Download & Updates
- **Thunderstore:** [Atlantum Reactor on Thunderstore](https://thunderstore.io/c/techtonica/p/CertiFried/AtlantumReactor/)

### Dependencies
- **EquinoxsModUtils:** [Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- **EMUAdditions:** [Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
- **TechtonicaFramework:** [Thunderstore](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/)
- **BepInEx:** [GitHub](https://github.com/BepInEx/BepInEx)

### Community
- **Techtonica Discord:** [Join the Community](https://discord.gg/techtonica)
- **Techtonica Modding Discord:** Check Thunderstore for current invite links

### Game
- **Techtonica on Steam:** [Store Page](https://store.steampowered.com/app/1457320/Techtonica/)
- **Fire Hose Games:** [Official Website](https://www.firehosegames.com/)

---

*Last updated: January 2025*
