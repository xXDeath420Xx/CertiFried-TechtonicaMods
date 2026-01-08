# Advanced Machines

[![Version](https://img.shields.io/badge/version-2.4.2-blue.svg)](https://github.com/)
[![License](https://img.shields.io/badge/license-GPL%20v3-green.svg)](LICENSE)
[![Game](https://img.shields.io/badge/game-Techtonica-orange.svg)](https://store.steampowered.com/app/1457320/Techtonica/)

A comprehensive mod for **Techtonica** that adds high-tier production machines (MKIII, MKIV, and MKV variants) with significantly increased processing speeds, as well as unlocking hidden machine variants that exist in the game files but are not normally accessible.

---

## Table of Contents

- [Features](#features)
- [Machine Tiers](#machine-tiers)
  - [Tier 3 Machines (MKIII)](#tier-3-machines-mkiii)
  - [Tier 4 Machines (MKIV)](#tier-4-machines-mkiv)
  - [Tier 5 Machines (MKV)](#tier-5-machines-mkv)
- [Power Consumption](#power-consumption)
- [Tech Tree Integration](#tech-tree-integration)
- [Crafting Recipes](#crafting-recipes)
- [Installation](#installation)
- [Configuration](#configuration)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Screenshots](#screenshots)
- [Known Issues](#known-issues)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

- **High-Performance Machines**: Adds MKIII, MKIV, and MKV versions of core production machines with 1.5x to 4x processing speeds
- **Hidden Variants Enabler**: Unlocks all hidden machine color variants and skins that exist in the game files but are not normally accessible
- **Balanced Progression**: Machines are properly integrated into the tech tree with appropriate research requirements
- **Save Compatible**: Works with existing save files and includes toolbar crash protection for renamed machines
- **Fully Configurable**: Enable or disable individual machine tiers and features via configuration file

---

## Machine Tiers

### Tier 3 Machines (MKIII)

Mid-game machines providing 1.5-2x base speed improvements. Unlocked in Research Tier 10 (late Floor 2 / VICTOR).

| Machine | Speed Multiplier | Description |
|---------|------------------|-------------|
| **Assembler MKIII** | 2.0x | High-performance assembler with doubled crafting speed |
| **Mining Drill MKIII** | 2.0x | High-performance mining drill with doubled dig speed |
| **Thresher MKIII** | 1.5x | Improved thresher with 50% faster processing |

### Tier 4 Machines (MKIV)

Late-game machines providing 2-2.5x base speed improvements. Unlocked in Research Tier 14 (mid Floor 3 / XRAY). Requires Gold Research Cores.

| Machine | Speed Multiplier | Description |
|---------|------------------|-------------|
| **Smelter MKIV** | 2.0x | Advanced smelter with doubled processing speed |
| **Assembler MKIV** | 3.0x | Ultimate assembler with tripled crafting speed |
| **Mining Drill MKIV** | 3.0x | Ultimate mining drill with tripled dig speed |
| **Thresher MKIV** | 2.0x | Advanced thresher with doubled processing speed |
| **Planter MKIV** | 2.5x | Advanced planter with 2.5x growth speed (extends MorePlanters MKIII) |

### Tier 5 Machines (MKV)

End-game machines providing 3-4x base speed improvements. Unlocked in Research Tier 18 (early-mid Floor 4 / SIERRA). Requires Green Research Cores and Atlantum materials.

| Machine | Speed Multiplier | Description |
|---------|------------------|-------------|
| **Smelter MKV** | 3.0x | Ultimate smelter with tripled processing speed for large-scale operations |
| **Thresher MKV** | 3.0x | Ultimate thresher with tripled processing speed |
| **Planter MKV** | 4.0x | Ultimate planter with quadrupled growth speed for peak agricultural performance |

---

## Power Consumption

Higher tier machines consume more power to balance their increased output. Power consumption scales based on the base machine's power draw.

| Machine Type | Base Power | MKIII Power | MKIV Power | MKV Power |
|--------------|------------|-------------|------------|-----------|
| Smelter | 150 kW | - | 225 kW (1.5x) | 300 kW (2.0x) |
| Assembler | 200 kW | 300 kW (1.5x) | 400 kW (2.0x) | - |
| Mining Drill | 100 kW | 150 kW (1.5x) | 200 kW (2.0x) | - |
| Thresher | 80 kW | 120 kW (1.5x) | 120 kW (1.5x) | 160 kW (2.0x) |
| Planter | 60 kW | - | 90 kW (1.5x) | 120 kW (2.0x) |

**Note**: Fuel-burning smelters also have increased fuel consumption rates at higher tiers.

---

## Tech Tree Integration

All Advanced Machines are placed in a dedicated **Modded** category in the tech tree for easy access.

### Research Requirements

| Tier | Research Tier | Floor | Core Type | Core Count |
|------|---------------|-------|-----------|------------|
| MKIII | Tier 10 | Floor 2 (VICTOR) | Gold | 500 |
| MKIV | Tier 14 | Floor 3 (XRAY) | Gold | 500 |
| MKV | Tier 18 | Floor 4 (SIERRA) | Green | 1000 |

---

## Crafting Recipes

All machines are crafted in Assemblers. Each machine requires its previous tier variant plus additional advanced materials.

### MKIII Machines

**Assembler MKIII**
- 2x Assembler MKII
- 10x Steel Frame
- 5x Processor Unit
- 5x Shiverthorn Coolant

**Mining Drill MKIII**
- 2x Mining Drill MKII
- 10x Steel Frame
- 10x Mechanical Components
- 5x Shiverthorn Coolant

**Thresher MKIII**
- 2x Thresher
- 5x Steel Frame
- 3x Mechanical Components
- 2x Processor Unit

### MKIV Machines

**Smelter MKIV**
- 2x Smelter MKIII
- 10x Steel Frame
- 5x Processor Unit
- 5x Cooling System

**Assembler MKIV**
- 2x Assembler MKIII
- 10x Atlantum Mixture Brick
- 10x Processor Unit
- 10x Shiverthorn Coolant

**Mining Drill MKIV**
- 2x Mining Drill MKIII
- 10x Atlantum Mixture Brick
- 20x Mechanical Components
- 10x Shiverthorn Coolant

**Thresher MKIV**
- 2x Thresher MKIII
- 10x Steel Frame
- 5x Mechanical Components
- 3x Processor Unit

**Planter MKIV**
- 2x Planter MKIII (from MorePlanters mod)
- 10x Steel Frame
- 10x Mechanical Components
- 5x Shiverthorn Coolant

### MKV Machines

**Smelter MKV**
- 2x Smelter MKIV
- 10x Atlantum Mixture Brick
- 10x Processor Unit
- 10x Shiverthorn Coolant

**Thresher MKV**
- 2x Thresher MKIV
- 10x Atlantum Mixture Brick
- 10x Mechanical Components
- 5x Processor Unit

**Planter MKV**
- 2x Planter MKIV
- 10x Atlantum Mixture Brick
- 50x Kindlevine Extract
- 10x Shiverthorn Coolant

---

## Installation

### Prerequisites

1. **BepInEx 5.x** for Techtonica must be installed
2. **EquinoxsModUtils (EMU)** version 6.1.3 or higher
3. **EMUAdditions** version 2.0.0 or higher
4. **TechtonicaFramework** (com.certifired.TechtonicaFramework)
5. **MorePlanters** (optional, required for Planter MKIV/MKV functionality)

### Installation Steps

1. Download the latest release of `AdvancedMachines.dll`
2. Navigate to your Techtonica installation folder
3. Place `AdvancedMachines.dll` in `BepInEx/plugins/` folder
4. Launch Techtonica
5. The mod will generate its configuration file on first run

### Manual Installation Path
```
Techtonica/
└── BepInEx/
    └── plugins/
        └── AdvancedMachines.dll
```

---

## Configuration

Configuration options are available in `BepInEx/config/com.certifired.AdvancedMachines.cfg` after first launch.

### Available Options

```ini
[General]

## Unlock all hidden machine variants (color skins, etc.)
# Setting type: Boolean
# Default value: true
Enable Hidden Variants = true

## Add Tier 4 (MK IV) versions of machines
# Setting type: Boolean
# Default value: true
Enable Tier 4 Machines = true

## Add Tier 5 (MK V) versions of machines
# Setting type: Boolean
# Default value: true
Enable Tier 5 Machines = true
```

### Configuration Notes

- Changes to configuration require a game restart to take effect
- Disabling machine tiers after placing them in the world may cause issues
- It is recommended to keep all options enabled for the best experience

---

## Requirements

### Required Dependencies

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.x | Mod loader framework |
| [EquinoxsModUtils (EMU)](https://github.com/) | 6.1.3+ | Core modding utilities |
| [EMUAdditions](https://github.com/) | 2.0.0+ | Machine and recipe registration |
| [TechtonicaFramework](https://github.com/) | Latest | Tech tree and build menu integration |

### Optional Dependencies

| Dependency | Purpose |
|------------|---------|
| [MorePlanters](https://github.com/) | Required for Planter MKIV/MKV (provides Planter MKII/MKIII base) |

---

## Compatibility

- **Techtonica**: Compatible with the current game version
- **Save Files**: Fully compatible with existing saves; includes toolbar crash protection
- **Other Mods**: Works alongside most other Techtonica mods
- **MorePlanters**: Integrates with and extends the Planter progression from MorePlanters

### Known Mod Interactions

- Planter MKIV/MKV require MorePlanters mod for the Planter MKIII prerequisite
- Hidden Variants feature may interact with other mods that modify machine variants

---

## Screenshots

*Screenshots coming soon*

<!--
![Tech Tree Integration](screenshots/tech_tree.png)
![MKIV Smelter](screenshots/smelter_mkiv.png)
![MKV Assembler](screenshots/assembler_mkv.png)
![Factory Setup](screenshots/factory_setup.png)
-->

---

## Known Issues

1. **Toolbar Compatibility**: If upgrading from an older version, machines with renamed internal names may cause brief toolbar refresh errors (automatically suppressed and non-breaking)

2. **Sprite Display**: Some machine icons may display the base machine sprite rather than a unique icon

3. **Planter Dependencies**: Planter MKIV/MKV will not function correctly without the MorePlanters mod installed

4. **Save Data Tier Override**: The mod forces correct tech tree tiers after save data loads to prevent research appearing in wrong locations

---

## Changelog

### [2.4.2] - Current
- Latest stable release
- Full MKIII/MKIV/MKV machine implementations
- Tech tree tier forcing for save compatibility

### [2.0.6] - 2025-01-05
- Fixed toolbar patch compatibility
- Save compatibility unlocks for existing worlds

### [2.0.0] - 2025-01-04
- Full Phase 2 implementation
- Hidden Variants Enabler - Unlock all hidden machine variants
- Tier 4 Machines - Smelter MKIV, Assembler MKIII, Mining Drill MKIII, Thresher MKII, Planter MKIV (2x speed)
- Tier 5 Machines - Smelter MKV, Assembler MKIV, Mining Drill MKIV, Thresher MKIII, Planter MKV (3x speed, requires Atlantum)
- Tech tree integration with proper positioning

### [1.0.0] - 2025-01-03
- Initial release

---

## Credits

### Development

- **certifired** - Primary mod developer
- **Claude Code (Anthropic)** - AI development assistance for code architecture, documentation, and debugging

### Special Thanks

- **Equinox** - For EquinoxsModUtils and EMUAdditions frameworks
- **Fire Hose Games** - For creating Techtonica
- **Techtonica Modding Community** - For testing, feedback, and support

---

## License

This project is licensed under the **GNU General Public License v3.0** (GPL-3.0).

```
Copyright (C) 2025 certifired

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

---

## Links

### Project Links

- **Source Code**: [GitHub Repository](https://github.com/)
- **Bug Reports**: [GitHub Issues](https://github.com/issues)
- **Releases**: [GitHub Releases](https://github.com/releases)

### Related Resources

- **Techtonica**: [Steam Store Page](https://store.steampowered.com/app/1457320/Techtonica/)
- **BepInEx**: [GitHub](https://github.com/BepInEx/BepInEx)
- **Techtonica Modding Discord**: [Discord Server](https://discord.gg/)
- **Thunderstore**: [Mod Page](https://thunderstore.io/)

---

*Last updated: January 2025*
