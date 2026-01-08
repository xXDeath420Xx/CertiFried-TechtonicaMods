# Enhanced Logistics

**Advanced logistics and automation systems for Techtonica**

Enhanced Logistics is a comprehensive mod that adds powerful logistics features to Techtonica, including a universal search system, storage network management, inserter enhancements, and a fully-featured drone delivery system with multiple drone types.

---

## Table of Contents

- [Features](#features)
  - [Universal Search System](#universal-search-system)
  - [Storage Network](#storage-network)
  - [Better Inserters](#better-inserters)
  - [Drone Delivery System](#drone-delivery-system)
- [Installation](#installation)
- [Configuration](#configuration)
- [New Machines](#new-machines)
- [Tech Tree Integration](#tech-tree-integration)
- [Mod Compatibility](#mod-compatibility)
- [Requirements](#requirements)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

### Universal Search System

Press **Ctrl+F** to open a powerful in-game search interface that lets you find anything quickly.

**Capabilities:**
- **Real-time search** - Results update as you type (minimum 2 characters)
- **Category filtering** - Filter by All, Items, Recipes, Tech, or Machines
- **Comprehensive coverage** - Searches through:
  - Inventory items and resources
  - Crafting recipes
  - Tech tree unlocks (shows unlock status)
  - Buildable machines
- **Smart sorting** - Results prioritized by name match relevance
- **Draggable window** - Position the search window anywhere on screen
- **Resolution independent** - UI scales properly for any screen size

**Search UI Configuration Options:**
- Toggle Key (default: F, used with Ctrl modifier)
- Enable/Disable search functionality
- Toggle searching in Inventory, Crafting, Tech Tree, and Build Menu independently

---

### Storage Network

A channel-based storage network system that extends the wormhole chest concept to create interconnected storage systems.

**Features:**
- **Network channels** - Connect storage containers to named network channels
- **Auto-routing** - Automatically route items between networked chests
- **Configurable range** - Set maximum network distance (10-500 meters)
- **Visual connections** - Optional visualization of network connections between storage

**Storage Network Configuration:**
- Enable/Disable storage network system
- Max Network Distance (10-500m, default: 100m)
- Auto Route Items toggle
- Show Network Visualization toggle

---

### Better Inserters

Enhance your factory's inserters with improved performance and smart features.

**Enhancements:**
- **Extended range** - Inserter reach multiplier (1x to 3x, default: 1.5x)
- **Faster operation** - Inserter speed multiplier (1x to 3x, default: 1.5x)
- **Smart filtering** - Intelligent item filtering for more precise control
- **Configurable stack sizes** - Set default filter stack sizes (1-100 items)

**Better Logistics Configuration:**
- Enable/Disable better logistics features
- Inserter Range Multiplier (1.0x - 3.0x)
- Inserter Speed Multiplier (1.0x - 3.0x)
- Smart Filtering toggle
- Default Filter Stack Size (1-100)

---

### Drone Delivery System

A comprehensive drone system featuring three specialized drone types, each with their own port machines and behaviors.

Press **J** to open the Drone Management window.

#### Drone Types

**Delivery Drones** (Blue)
- Automatically transport items between connected storage
- Configurable capacity (8-128 items per trip)
- Configurable speed (5-30 units/second)
- Configurable range (50-500 meters)

**Combat Drones** (Red)
- Patrol designated areas and engage hostile targets
- Laser weapon with visual effects
- Configurable damage, fire rate, and movement speed
- 15% critical hit chance for 2x damage
- Integrates with TurretDefense mod for targeting aliens

**Repair Drones** (Green)
- Automatically detect and repair damaged machines
- Visual repair beam effect
- Configurable repair rate and speed
- Designed to integrate with SurvivalElements mod

#### Drone Port Machines

| Machine | Power Draw | Research Required | Description |
|---------|------------|-------------------|-------------|
| Drone Port | 30 kW | Drone Technology | Deploys delivery drones |
| Combat Drone Port | 60 kW | Advanced Drone Systems | Deploys combat drones |
| Repair Drone Port | 45 kW | Advanced Drone Systems | Deploys repair drones |

Each port can deploy up to 3 drones (configurable, 1-10).

#### Drone Management UI

The Drone Management window (press J) provides:
- Active drone count by type
- Drone port status
- Pending delivery queue
- Individual drone status (ID, type, current state)
- Test buttons to spawn drones for testing
- Clear all drones function

**Drone System Configuration:**
- Enable/Disable drone system
- Drone Capacity (8-128 items)
- Drone Speed (5-50 units/second)
- Drone Range (50-500 meters)
- Drone Menu Key (default: J)
- Delivery Drone Speed (5-30)
- Combat Drone Speed (5-40)
- Combat Drone Damage (5-100)
- Combat Drone Fire Rate (0.5-10 shots/second)
- Repair Drone Speed (3-20)
- Repair Drone Rate (1-50 health/second)
- Max Drones Per Port (1-10)

---

## Installation

### Using r2modman (Recommended)

1. Open r2modman
2. Select Techtonica as your game
3. Search for "EnhancedLogistics" in the online mods
4. Click Install

### Manual Installation

1. Ensure BepInEx is installed for Techtonica
2. Download the latest release
3. Extract `EnhancedLogistics.dll` to your `BepInEx/plugins` folder
4. (Optional) Extract the `Bundles` folder to the same location for custom drone visuals

**Folder Structure:**
```
BepInEx/
  plugins/
    EnhancedLogistics/
      EnhancedLogistics.dll
      Bundles/                    (optional)
        drones_voodooplay
        drones_scifi
        drones_simple
```

---

## Configuration

All settings can be modified via the BepInEx configuration file located at:
```
BepInEx/config/com.certifired.EnhancedLogistics.cfg
```

Configuration categories:

| Category | Description |
|----------|-------------|
| Search UI | Universal search settings and keybinds |
| Storage Network | Network range and routing options |
| Better Logistics | Inserter enhancements |
| Drone System | Core drone settings |
| Drone Types | Type-specific drone parameters |

---

## New Machines

### Drone Port
- **Description:** Deploys delivery drones that automatically transport items between connected storage
- **Power Consumption:** 30 kW
- **Recipe:** Steel Frame (15), Copper Wire (25), Processor Unit (5), Iron Components (20)
- **Crafting Method:** Assembler
- **Unlock:** Drone Technology

### Combat Drone Port
- **Description:** Deploys combat drones that patrol and engage hostile targets
- **Power Consumption:** 60 kW
- **Recipe:** Steel Frame (20), Copper Wire (40), Processor Unit (8), Iron Components (30)
- **Crafting Method:** Assembler
- **Unlock:** Advanced Drone Systems

### Repair Drone Port
- **Description:** Deploys repair drones that automatically fix damaged machines
- **Power Consumption:** 45 kW
- **Recipe:** Steel Frame (15), Copper Wire (30), Processor Unit (6), Iron Components (25)
- **Crafting Method:** Assembler
- **Unlock:** Advanced Drone Systems

---

## Tech Tree Integration

Enhanced Logistics adds two new research unlocks in the Modded category:

| Research | Tier | Cost | Unlocks |
|----------|------|------|---------|
| Drone Technology | VICTOR (Tier 8) | 150 Gold Cores | Drone Port |
| Advanced Drone Systems | XRAY (Tier 13) | 200 Green Cores | Combat Drone Port, Repair Drone Port |

---

## Mod Compatibility

Enhanced Logistics is designed to work with and enhance other mods:

| Mod | Integration |
|-----|-------------|
| **TechtonicaFramework** | Required - Provides modded tech tree category and build menu integration |
| **TurretDefense** | Optional - Combat drones can target and damage aliens spawned by TurretDefense |
| **SurvivalElements** | Optional - Repair drones designed to work with machine damage systems |

---

## Requirements

| Dependency | Version | Required |
|------------|---------|----------|
| BepInEx | 5.4.21+ | Yes |
| EquinoxsModUtils | 6.1.3+ | Yes |
| EMUAdditions | 2.0.0+ | Yes |
| TechtonicaFramework | Latest | Yes |
| TurretDefense | Latest | No (Soft dependency) |

---

## Changelog

### [3.2.2] - Latest
- Current stable release

### [2.0.4] - 2025-01-05
- Updated icon

### [1.0.0] - 2025-01-04
- Initial release
- Universal search UI (Ctrl+F)
- Storage network with channels
- Inserter enhancements
- Drone delivery framework with GUI
- Three drone types: Delivery, Combat, Repair
- Three drone port machines
- TurretDefense integration for combat drones
- Asset bundle support for custom drone models

---

## Credits

- **certifired** - Primary developer
- **Equinox** - EquinoxsModUtils and EMUAdditions frameworks
- **Claude Code** - Development assistance and code generation (Anthropic)
- **Techtonica Community** - Testing and feedback

Special thanks to the Techtonica modding community for their support and the game developers at Fire Hose Games for creating Techtonica.

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

You are free to:
- Use this mod for personal and commercial purposes
- Modify and distribute this mod
- Include this mod in modpacks

Under the following conditions:
- You must include the original license and copyright notice
- Derivative works must also be licensed under GPL-3.0
- Source code must be made available when distributing

For the full license text, see: https://www.gnu.org/licenses/gpl-3.0.en.html

---

## Links

- **Thunderstore:** [Enhanced Logistics on Thunderstore](https://thunderstore.io/c/techtonica/p/certifired/EnhancedLogistics/)
- **GitHub:** [Source Repository](https://github.com/certifired/TechtonicaMods)
- **Techtonica Discord:** [Community Discord](https://discord.gg/techtonica)
- **EquinoxsModUtils:** [EMU on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- **EMUAdditions:** [EMUAdditions on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)

---

*Enhanced Logistics v3.2.2 - Making factory automation smarter*
