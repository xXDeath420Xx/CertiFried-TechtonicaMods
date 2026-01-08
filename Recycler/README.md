# Recycler

**A comprehensive resource recycling system for Techtonica**

Recycler adds a four-tier recycling machine system that allows players to break down crafted items back into their component resources. Upgrade through tiers to increase material recovery efficiency and processing speed, creating a sustainable circular economy in your factory.

---

## Table of Contents

- [Features](#features)
- [Recycler Tiers](#recycler-tiers)
- [How to Use](#how-to-use)
- [Installation](#installation)
- [Configuration](#configuration)
- [Mod Integration](#mod-integration)
- [Requirements](#requirements)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

### Core Functionality

- **Four-Tier Recycling System**: Progress from basic to perfect recyclers with increasing efficiency
- **Material Recovery**: Break down crafted items into their original component resources
- **Automatic Recipe Detection**: Recycling recipes are automatically generated based on game crafting data
- **Statistics Tracking**: Track total items recycled and resources recovered

### Resource Processing

- **Efficiency-Based Returns**: Each tier returns a percentage of original materials
- **Scrap Production**: Inefficient recycling produces scrap as a byproduct
- **Bonus Chance System**: Configurable chance to recover rare components as bonus drops
- **Processing Speed**: Higher tier recyclers process items faster

### Organic Waste System

- **Organic Material Detection**: Automatically identifies organic items (plants, seeds, fibers)
- **Compost Production**: Recycling organic items produces organic waste/compost
- **BioProcessing Integration**: Organic waste can be fed into composters for fertilizer production

---

## Recycler Tiers

| Tier | Name | Efficiency | Speed Modifier | Description |
|------|------|------------|----------------|-------------|
| MKI | Basic Recycler | 25% | 1.5x (slower) | Entry-level recycler, returns 1/4 of materials |
| MKII | Advanced Recycler | 50% | 1.0x (normal) | Improved recycler, returns 1/2 of materials |
| MKIII | Quantum Recycler | 75% | 0.75x (faster) | High-tech recycler, returns 3/4 of materials |
| MKIV | Perfect Recycler | 100% | 0.5x (fastest) | Ultimate recycler, full material recovery - requires Atlantum power |

### Efficiency Examples

For an item crafted with 8 Iron Ingots:

| Tier | Iron Ingots Returned |
|------|----------------------|
| Basic (25%) | 2 |
| Advanced (50%) | 4 |
| Quantum (75%) | 6 |
| Perfect (100%) | 8 |

---

## How to Use

### Basic Operation

1. **Craft a Recycler**: Build the appropriate tier recycler for your current technology level
2. **Place the Recycler**: Position it in your factory layout
3. **Connect Input**: Use conveyors or inserters to feed items into the recycler
4. **Connect Output**: Set up conveyors or inserters to collect recovered materials
5. **Power the Machine**: Ensure adequate power supply (Perfect tier requires Atlantum power)

### Best Practices

- **Start with Basic**: The Basic Recycler is cost-effective for bulk recycling of low-value items
- **Upgrade Strategically**: Save higher-tier recyclers for valuable items where efficiency matters
- **Manage Scrap**: Plan for scrap output from non-perfect recyclers
- **Organic Processing**: Route organic waste to composters if using BioProcessing mod

### Recyclable Items

The mod automatically generates recycling recipes for craftable items including:
- Machines (Inserters, Conveyors, Smelters, Assemblers, etc.)
- Components and intermediate products
- Organic materials (when enabled in config)

---

## Installation

### Prerequisites

Ensure you have the following installed:
1. BepInEx 5.4.2100 or newer
2. EquinoxsModUtils 6.1.3 or newer
3. EMUAdditions 2.0.0 or newer (optional, for enhanced features)

### Installation Steps

1. **Download** the latest release of Recycler.dll
2. **Navigate** to your Techtonica installation directory
3. **Locate** the `BepInEx/plugins` folder
4. **Copy** `Recycler.dll` into the plugins folder
5. **Launch** Techtonica - the mod will initialize automatically

### Folder Structure

```
Techtonica/
  BepInEx/
    plugins/
      Recycler.dll        <-- Place here
      EquinoxsModUtils.dll
      EMUAdditions.dll
```

### Verifying Installation

1. Launch the game
2. Check the BepInEx console/log for: `[Recycler] Recycler v1.0.0 loaded successfully!`
3. The recycler machines should appear in your build menu

---

## Configuration

Configuration is managed through BepInEx's configuration system. After first launch, a config file is generated at:

```
BepInEx/config/com.certifried.recycler.cfg
```

### Efficiency Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| `BasicRecycler` | 0.25 | 0.1 - 1.0 | Resource recovery rate for Basic Recycler (0.25 = 25%) |
| `AdvancedRecycler` | 0.50 | 0.1 - 1.0 | Resource recovery rate for Advanced Recycler |
| `QuantumRecycler` | 0.75 | 0.1 - 1.0 | Resource recovery rate for Quantum Recycler |

### Processing Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| `RecycleTime` | 5.0 | 1.0 - 30.0 | Base time in seconds to recycle one item |
| `AllowOrganic` | true | true/false | Allow recycling of organic materials (produces compost) |
| `BonusChance` | 0.1 | 0.0 - 0.5 | Chance to recover rare components as bonus (0.1 = 10%) |

### Example Configuration

```ini
[Efficiency]
# Resource recovery rate for Basic Recycler (0.25 = 25%)
BasicRecycler = 0.25

# Resource recovery rate for Advanced Recycler
AdvancedRecycler = 0.50

# Resource recovery rate for Quantum Recycler
QuantumRecycler = 0.75

[Processing]
# Base time in seconds to recycle one item
RecycleTime = 5

# Allow recycling of organic materials (produces compost for BioProcessing)
AllowOrganic = true

# Chance to recover rare components as bonus
BonusChance = 0.1
```

---

## Mod Integration

### BioProcessing Integration

When both Recycler and BioProcessing are installed:

- Organic items recycled in any tier produce organic waste
- Organic waste quantity scales with input amount (approximately 1 waste per 4 items)
- Organic waste can be fed directly into BioProcessing composters
- Composters convert organic waste into fertilizer
- Enables a complete circular economy for organic resources

### Integration Workflow

```
Organic Items --> Recycler --> Organic Waste --> Composter --> Fertilizer --> Plants --> Organic Items
```

---

## Requirements

### Hard Dependencies

| Mod | Version | Purpose |
|-----|---------|---------|
| BepInEx | 5.4.2100+ | Mod loading framework |
| EquinoxsModUtils | 6.1.3+ | Core utilities and game event hooks |

### Soft Dependencies

| Mod | Version | Purpose |
|-----|---------|---------|
| EMUAdditions | 2.0.0+ | Enhanced mod utilities (optional) |
| BioProcessing | Any | Organic waste integration (optional) |

### Game Compatibility

- **Game**: Techtonica
- **Framework**: .NET Framework 4.7.2
- **Unity**: Compatible with current Techtonica Unity version

---

## Changelog

### [1.0.0] - 2025-01-05

**Initial Release**

- Four recycler tiers with configurable efficiency (25%, 50%, 75%, 100%)
- Automatic recycling recipe generation from game data
- Organic waste output system for plant-based materials
- BioProcessing mod integration for waste composting
- Configurable processing times and bonus drop chances
- Statistics tracking for recycled items and recovered resources
- Scrap production from non-perfect efficiency recycling

---

## Credits

### Author

- **Certifried** - Primary developer

### Development Assistance

- **Claude Code** (Anthropic) - AI-assisted development and code generation

### Special Thanks

- **Equinox** - For EquinoxsModUtils and EMUAdditions frameworks
- **Techtonica Modding Community** - For support, testing, and feedback
- **Fire Hose Games** - For creating Techtonica

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

```
Recycler - A recycling mod for Techtonica
Copyright (C) 2025 Certifried

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

### License Summary

- You are free to use, modify, and distribute this mod
- Any derivative works must also be licensed under GPL-3.0
- Source code must be made available when distributing
- No warranty is provided

---

## Links

### Download & Source

- **GitHub Repository**: [TechtonicaMods/Recycler](https://github.com/certifried/TechtonicaMods)
- **Thunderstore**: Coming soon
- **NexusMods**: Coming soon

### Required Mods

- **BepInEx**: [GitHub](https://github.com/BepInEx/BepInEx)
- **EquinoxsModUtils**: [GitHub](https://github.com/CubeSuite/TechtonicaMods)

### Community

- **Techtonica Discord**: [Official Discord](https://discord.gg/techtonica)
- **Techtonica Modding Discord**: Check Techtonica Discord for modding channels

### Bug Reports & Feature Requests

Please report bugs and request features through the GitHub Issues page.

---

*Happy recycling!*
