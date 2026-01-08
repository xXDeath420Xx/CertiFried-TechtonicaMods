# PetsCompanions

**Companion Pets for Techtonica**

[![Version](https://img.shields.io/badge/Version-1.4.0-blue.svg)]()
[![License](https://img.shields.io/badge/License-GPL%20v3-green.svg)](LICENSE)
[![Game](https://img.shields.io/badge/Game-Techtonica-orange.svg)](https://store.steampowered.com/app/1457320/Techtonica/)

A comprehensive companion pet mod for Techtonica that adds five unique robotic companions to accompany you on your factory-building adventures. Each pet features distinct behaviors, visual styles, and passive buffs to enhance your gameplay experience.

---

## Table of Contents

- [Features](#features)
  - [Companion Types](#companion-types)
  - [Pet Behaviors](#pet-behaviors)
  - [Buff System](#buff-system)
- [Getting Started](#getting-started)
  - [Unlocking Companion Technology](#unlocking-companion-technology)
  - [Crafting Companions](#crafting-companions)
  - [Using Your Pets](#using-your-pets)
- [Controls](#controls)
- [Configuration](#configuration)
- [Installation](#installation)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Troubleshooting](#troubleshooting)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

### Companion Types

PetsCompanions introduces five unique robotic companions, each with their own appearance, movement style, and special abilities:

| Companion | Type | Movement | Buff | Color |
|-----------|------|----------|------|-------|
| **Companion Drone** | Flying | Aerial orbit | Movement Speed | Blue |
| **Companion Crawler** | Ground | Surface patrol | Mining Speed | Orange |
| **Companion Floater** | Hovering | Mid-level float | Inventory Capacity | Purple |
| **Guardian Bot** | Combat | Low hover | Damage Reduction | Green |
| **Scout Drone** | Reconnaissance | Fast aerial | Resource Detection | Yellow |

#### Companion Drone
A small flying drone that gracefully orbits around you at head height. Designed for speed enthusiasts, it provides a passive movement speed buff, helping you traverse your factory more efficiently.

#### Companion Crawler
A spider-like ground companion that scuttles along the terrain near your feet. Built for miners, it enhances your mining speed, making resource extraction faster and more productive.

#### Companion Floater
A hovering companion with integrated storage technology. It floats at mid-level and increases your inventory capacity, perfect for extended mining expeditions or factory reorganization.

#### Guardian Bot
A sturdy, robust guardian bot that maintains a protective stance near you. Its reinforced design provides damage reduction, keeping you safer during hazardous exploration.

#### Scout Drone
A fast, sensor-equipped scout drone that zips around at high altitude. Its advanced detection systems highlight nearby resources and points of interest, making exploration more rewarding.

### Pet Behaviors

All companions feature sophisticated AI behaviors:

- **Orbital Movement**: Pets smoothly orbit around the player at a configurable distance
- **Natural Motion**: Each pet type has unique movement characteristics:
  - Flying pets maintain higher altitude with gentle bobbing
  - Hovering pets float at mid-level with subtle oscillation
  - Ground pets stay close to terrain level
- **Smooth Following**: Pets use interpolated movement for natural, non-jerky following
- **Directional Facing**: Companions automatically rotate to face their movement direction
- **Height Variation**: Different pet types maintain appropriate heights based on their design
- **Bobbing Animation**: Subtle vertical oscillation adds life-like movement to airborne pets

### Buff System

When enabled, each companion provides passive buffs while active:

| Companion | Buff Type | Effect |
|-----------|-----------|--------|
| Companion Drone | Movement Speed | Faster player movement |
| Companion Crawler | Mining Speed | Increased mining efficiency |
| Companion Floater | Inventory | Expanded carrying capacity |
| Guardian Bot | Defense | Reduced damage taken |
| Scout Drone | Detection | Highlights nearby resources |

*Note: Buff effects can be toggled on/off in the configuration.*

---

## Getting Started

### Unlocking Companion Technology

Before you can craft any companions, you must first unlock the **Companion Technology** research:

1. Progress to **Tier 12 (XRAY)** in the tech tree
2. Navigate to the **Modded** category in the research menu
3. Research **Companion Technology** using **150 Purple Research Cores**

### Crafting Companions

Once unlocked, companions can be crafted in **Assemblers**. Each companion requires different materials:

#### Companion Drone
| Material | Quantity |
|----------|----------|
| Processor Unit | 5 |
| Electric Motor | 3 |
| Copper Wire | 20 |
| Steel Frame | 2 |

#### Companion Crawler
| Material | Quantity |
|----------|----------|
| Processor Unit | 3 |
| Iron Components | 15 |
| Electric Motor | 4 |
| Steel Frame | 4 |

#### Companion Floater
| Material | Quantity |
|----------|----------|
| Processor Unit | 4 |
| Electric Motor | 2 |
| Copper Wire | 30 |
| Iron Frame | 3 |

#### Guardian Bot
| Material | Quantity |
|----------|----------|
| Processor Unit | 6 |
| Steel Frame | 8 |
| Electric Motor | 4 |
| Iron Components | 20 |

#### Scout Drone
| Material | Quantity |
|----------|----------|
| Processor Unit | 8 |
| Electric Motor | 2 |
| Copper Wire | 40 |
| Shiverthorn Extract | 5 |

*Crafting time: 60 seconds per companion*

### Using Your Pets

1. **Summon**: Press the **Summon Key** (default: `,` Comma) to spawn your first companion
2. **Cycle**: Press the **Summon Key** again to dismiss current pet and spawn the next type
3. **Dismiss**: Press the **Dismiss Key** (default: `.` Period) to dismiss your active pet

---

## Controls

| Action | Default Key | Description |
|--------|-------------|-------------|
| Summon/Cycle Pet | `,` (Comma) | Summon a pet or cycle to the next type |
| Dismiss Pet | `.` (Period) | Dismiss the currently active pet |

*All keybindings are configurable in the mod configuration file.*

---

## Configuration

The mod configuration file is located at:
```
BepInEx/config/com.certifired.PetsCompanions.cfg
```

### Configuration Options

#### General Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| Enable Pets | `true` | true/false | Enable or disable the entire pet system |
| Max Active Pets | `1` | 1-3 | Maximum number of pets active simultaneously |
| Debug Mode | `false` | true/false | Enable detailed logging for troubleshooting |

#### Behavior Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| Follow Distance | `3.0` | 1.0-10.0 | Distance pets maintain from the player (in units) |
| Pet Speed | `8.0` | 3.0-15.0 | Movement speed of pets when following |

#### Buff Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Pets Provide Buffs | `true` | Whether pets apply their passive buff effects |

#### Control Settings

| Setting | Default | Description |
|---------|---------|-------------|
| Summon Pet Key | `Comma` | Key to summon or cycle through pets |
| Dismiss Pet Key | `Period` | Key to dismiss the active pet |

### Example Configuration

```ini
[General]
Enable Pets = true
Max Active Pets = 1
Debug Mode = false

[Behavior]
Follow Distance = 3
Pet Speed = 8

[Buffs]
Pets Provide Buffs = true

[Controls]
Summon Pet Key = Comma
Dismiss Pet Key = Period
```

---

## Installation

### Prerequisites

Ensure you have the following installed before proceeding:

1. [BepInEx 5.4.2100](https://github.com/BepInEx/BepInEx/releases) or newer
2. [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) v6.1.3 or newer
3. [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) v2.0.0 or newer
4. [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/Certifired/TechtonicaFramework/) v1.0.2 or newer

### Manual Installation

1. **Download** the latest release of PetsCompanions
2. **Extract** the contents of the archive
3. **Copy** `PetsCompanions.dll` to your `BepInEx/plugins` folder
4. **Launch** Techtonica

### Installation via Thunderstore (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager)
2. Search for "PetsCompanions" in the mod browser
3. Click "Install" - dependencies will be installed automatically
4. Launch the game through the mod manager

---

## Requirements

### Required Dependencies

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| BepInEx | 5.4.2100+ | Mod loading framework |
| EquinoxsModUtils | 6.1.3+ | Core modding utilities |
| EMUAdditions | 2.0.0+ | Resource and recipe registration |
| TechtonicaFramework | 1.0.2+ | Tech tree and build menu integration |

### Game Version

- Tested with Techtonica v0.3.x and later
- Compatible with the latest stable release

---

## Compatibility

### Known Compatible Mods

PetsCompanions is designed to work alongside other Techtonica mods. The mod uses the Modded category for tech tree entries to avoid conflicts.

### Potential Conflicts

- Mods that significantly alter player movement may affect pet following behavior
- Mods that modify the tech tree structure should be tested for compatibility

---

## Troubleshooting

### Pets Not Spawning

1. Verify that `Enable Pets` is set to `true` in the configuration
2. Check that all dependencies are installed and up to date
3. Enable `Debug Mode` and check the BepInEx console for error messages

### Pets Moving Erratically

1. Adjust the `Pet Speed` setting to a lower value
2. Increase the `Follow Distance` to give pets more room
3. Verify no other mods are conflicting with player transforms

### Research Not Appearing

1. Ensure you have progressed to at least Tier 6 in the tech tree
2. Check the Modded category in the research menu
3. Verify TechtonicaFramework is properly installed

### Configuration Not Saving

1. Close the game completely before editing the configuration file
2. Ensure the config file path is correct: `BepInEx/config/com.certifired.PetsCompanions.cfg`
3. Check file permissions on the BepInEx folder

---

## Changelog

### v1.4.0 (Current)
- Tech tree positioning improvements
- Enhanced stability and compatibility
- Configuration system refinements

### v1.3.0
- Added debug mode for troubleshooting
- Improved pet following behavior
- Fixed unlock linking issues

### v1.2.0
- Added Guardian Bot and Scout Drone companions
- Implemented buff system framework
- Enhanced visual feedback for pet types

### v1.1.0
- Added configuration options for controls
- Improved pet movement smoothing
- Added pet cycling functionality

### v1.0.0
- Initial release
- 5 companion pet types (Drone, Crawler, Floater, Guardian, Scout)
- Following AI with orbital behavior
- Configurable keybinds and behavior settings
- Research unlock and crafting recipes

---

## Credits

### Development

- **Certifired** - Lead Developer
- **Claude Code** (Anthropic) - AI Development Assistant

### Special Thanks

- **Equinox** - For creating EquinoxsModUtils and EMUAdditions, essential frameworks for Techtonica modding
- **Fire Totem Games** - For creating Techtonica
- **The Techtonica Modding Community** - For testing, feedback, and support

### Tools & Resources

- [BepInEx](https://github.com/BepInEx/BepInEx) - Unity modding framework
- [Harmony](https://github.com/pardeike/Harmony) - Runtime patching library
- [dnSpy](https://github.com/dnSpy/dnSpy) - .NET decompiler for reverse engineering

---

## License

This mod is licensed under the **GNU General Public License v3.0**.

```
PetsCompanions - Companion Pets for Techtonica
Copyright (C) 2024 Certifired

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program. If not, see <https://www.gnu.org/licenses/>.
```

### What This Means

- You are free to use, modify, and distribute this mod
- Any derivative works must also be licensed under GPL v3
- You must provide attribution to the original author
- Source code must be made available for any distributed versions

---

## Links

### Official Resources

- **Thunderstore**: [PetsCompanions on Thunderstore](https://thunderstore.io/c/techtonica/p/Certifired/PetsCompanions/)
- **GitHub**: [Source Code Repository](https://github.com/Certifired/TechtonicaMods)
- **Discord**: [Techtonica Modding Discord](https://discord.gg/techtonica)

### Dependencies

- [BepInEx](https://github.com/BepInEx/BepInEx/releases)
- [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
- [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/Certifired/TechtonicaFramework/)

### Game

- [Techtonica on Steam](https://store.steampowered.com/app/1457320/Techtonica/)
- [Official Techtonica Website](https://www.techtonicagame.com/)

---

*Made with passion for the Techtonica community.*
