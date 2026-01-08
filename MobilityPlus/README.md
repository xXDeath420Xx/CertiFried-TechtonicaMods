# MobilityPlus

**Enhanced Movement Equipment and Mobility Options for Techtonica**

MobilityPlus is a comprehensive mobility expansion mod that adds new equipment, vehicles, and traversal options to Techtonica. From enhanced stilts and speed boots to personal hover vehicles and placeable movement pads, this mod transforms how you navigate your factory and the world beyond.

---

## Table of Contents

- [Features](#features)
  - [Equipment](#equipment)
  - [Placeable Items](#placeable-items)
  - [Vehicles](#vehicles)
- [How to Unlock](#how-to-unlock)
- [How to Use](#how-to-use)
- [Installation](#installation)
- [Configuration](#configuration)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Known Issues](#known-issues)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Features

### Equipment

#### Stilts MKII
- **Description:** Enhanced stilts that extend to 4m height (configurable)
- **Benefits:** Faster extension speed and improved stability over standard stilts
- **Unlock:** Advanced Mobility (Tier 6)
- **Recipe:** 1x Hover Pack, 5x Steel Frame, 10x Iron Components

#### Stilts MKIII
- **Description:** Maximum height stilts extending to 6m (configurable)
- **Benefits:** Includes gyroscopic stabilization for rough terrain navigation
- **Unlock:** Extreme Speed Tech (Tier 9)
- **Recipe:** 1x Hover Pack, 8x Processor Unit, 5x Electric Motor, 15x Steel Frame

#### Speed Boots
- **Description:** Motorized boots that increase base movement speed by 25%
- **Benefits:** Passive speed increase while worn, requires battery power
- **Unlock:** Advanced Mobility (Tier 6)
- **Recipe:** 5x Iron Frame, 2x Electric Motor, 15x Copper Wire, 10x Plantmatter Fiber

#### Jump Pack
- **Description:** Compressed air-powered jump assist providing 50% jump boost
- **Benefits:** Double-tap jump for a powerful boost; limited charges that recharge over time
- **Unlock:** Extreme Speed Tech (Tier 9)
- **Recipe:** 10x Steel Frame, 5x Mechanical Components, 2x Processor Unit, 2x Electric Motor

#### Rail Runner MKII
- **Description:** Enhanced grappling hook with 50% longer range and faster retraction
- **Benefits:** Improved grip for heavy loads, superior mobility for vertical traversal
- **Unlock:** Extreme Speed Tech (Tier 9)
- **Recipe:** 1x Railrunner, 5x Steel Frame, 3x Electric Motor, 20x Copper Wire

### Placeable Items

#### Speed Pad
- **Description:** A placeable pad that boosts movement speed when walked over
- **Speed Multiplier:** 1.5x (configurable from 1.1x to 3x)
- **Max Stack:** 20 per slot
- **Unlock:** Advanced Mobility (Tier 6)
- **Recipe:** 2x Iron Frame, 1x Electric Motor, 5x Copper Wire (yields 2 pads)

#### Jump Pad
- **Description:** A placeable pad that launches the player upward when stepped on
- **Launch Force:** 20 (configurable from 5 to 50)
- **Max Stack:** 20 per slot
- **Unlock:** Extreme Speed Tech (Tier 9)
- **Recipe:** 3x Steel Frame, 2x Electric Motor, 3x Mechanical Components (yields 2 pads)

### Vehicles

#### Hover Pod
- **Description:** A personal hover vehicle for fast long-distance travel
- **Base Speed:** 15 m/s (configurable from 5 to 30 m/s)
- **Hover Height:** 1.5m (configurable from 0.5m to 5m)
- **Unlock:** Personal Vehicle Tech (Tier 13)
- **Recipe:** 2x Hover Pack, 20x Steel Frame, 5x Electric Motor, 5x Processor Unit, 30x Copper Wire

---

## How to Unlock

MobilityPlus items are unlocked through the tech tree under the **Modded** category. There are three research nodes:

| Research | Tier | Core Type | Core Count | Prerequisites |
|----------|------|-----------|------------|---------------|
| Advanced Mobility | Tier 6 (VICTOR) | Green | 150 | None |
| Extreme Speed Tech | Tier 9 (VICTOR) | Blue | 300 | Advanced Mobility |
| Personal Vehicle Tech | Tier 13 (XRAY) | Blue | 500 | None |

### Unlocks by Research

**Advanced Mobility:**
- Stilts MKII
- Speed Boots
- Speed Pad

**Extreme Speed Tech:**
- Stilts MKIII
- Jump Pack
- Rail Runner MKII
- Jump Pad

**Personal Vehicle Tech:**
- Hover Pod

---

## How to Use

### Equipment Usage

1. **Craft** the desired equipment at an Assembler after researching the appropriate unlock
2. **Equip** the item from your inventory
3. Equipment effects are automatically applied:
   - **Speed Boots:** 25% movement speed increase (passive)
   - **Jump Pack:** 50% jump height increase (passive)
   - **Stilts MKII/MKIII:** Extended height when using stilt function
   - **Rail Runner MKII:** Extended grapple range and speed

### Placeable Pads

1. **Craft** Speed Pads or Jump Pads at an Assembler
2. **Place** them on the ground like any other placeable item
3. **Walk over** Speed Pads for a temporary speed boost
4. **Step on** Jump Pads to be launched upward

### Hover Pod Vehicle

1. **Craft** a Hover Pod at an Assembler
2. **Press V** to summon your vehicle (spawns 3m in front of you)
3. **Press V** again when near the vehicle to mount it
4. **Controls while mounted:**
   - **WASD** - Movement (camera-relative)
   - **Space** - Ascend (increase hover height)
   - **Left Ctrl** - Descend (decrease hover height)
5. **Press B** to dismount
6. **Press B** again to dismiss the vehicle entirely
7. **Press V** when far from vehicle to recall it to your location

---

## Installation

### Using r2modman (Recommended)

1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/) if you haven't already
2. Search for "MobilityPlus" in the mod browser
3. Click "Install with Dependencies"
4. Launch the game through r2modman

### Manual Installation

1. Install [BepInEx 5.4.21+](https://github.com/BepInEx/BepInEx/releases) if not already installed
2. Install all required dependencies (see [Requirements](#requirements))
3. Download the latest MobilityPlus release
4. Extract `MobilityPlus.dll` to your `BepInEx/plugins` folder
5. Launch the game

**Typical folder structure:**
```
Techtonica/
├── BepInEx/
│   ├── plugins/
│   │   ├── MobilityPlus.dll
│   │   ├── EquinoxsModUtils.dll
│   │   ├── EMUAdditions.dll
│   │   └── TechtonicaFramework.dll
│   └── config/
│       └── com.certifired.MobilityPlus.cfg
└── ...
```

---

## Configuration

After running the game once with the mod installed, a configuration file will be created at:
`BepInEx/config/com.certifired.MobilityPlus.cfg`

### Configuration Options

#### [General]

| Option | Default | Description |
|--------|---------|-------------|
| Debug Mode | `false` | Enable debug logging for troubleshooting |

#### [Stilts]

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| Enable Enhanced Stilts | `true` | - | Enable Stilts MKII and MKIII variants |
| Stilts MKII Height | `4` | 2-10 | Height increase for Stilts MKII (meters) |
| Stilts MKIII Height | `6` | 3-15 | Height increase for Stilts MKIII (meters) |

#### [Speed]

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| Enable Speed Zones | `true` | - | Enable placeable speed boost zones |
| Speed Boost Multiplier | `1.5` | 1.1-3.0 | Speed multiplier when in speed zones |

#### [Jump]

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| Enable Jump Pads | `true` | - | Enable placeable jump pads |
| Jump Pad Force | `20` | 5-50 | Upward force applied by jump pads |

#### [Vehicle]

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| Enable Vehicles | `true` | - | Enable personal hover vehicle |
| Vehicle Speed | `15` | 5-30 | Base vehicle movement speed (m/s) |
| Hover Height | `1.5` | 0.5-5.0 | Height vehicle hovers above ground (meters) |
| Summon Vehicle Key | `V` | - | Key to summon/mount personal vehicle |
| Dismiss Vehicle Key | `B` | - | Key to dismiss/exit vehicle |

---

## Requirements

This mod requires the following dependencies:

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21+ | Mod loading framework |
| [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) | 6.1.3+ | Core modding utilities |
| [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) | 2.0.0+ | Content addition framework |
| [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/Certifired/TechtonicaFramework/) | 1.0.0+ | Equipment and movement API |

---

## Compatibility

- **Techtonica Version:** Compatible with the latest version of Techtonica
- **Multiplayer:** Not tested in multiplayer; use at your own risk
- **Other Mods:** Should be compatible with most mods; designed to work alongside TechtonicaFramework-based mods

---

## Known Issues

- Harmony patches for Stilts and RailRunner classes are currently disabled pending verification of correct class signatures
- Modded resources cannot currently be used as ingredients in recipes (causes crafting UI issues)
- Vehicle mounting/dismounting may have edge cases in certain terrain configurations

---

## Changelog

### [1.7.0] - Latest
- Added Hover Pod personal vehicle system
- Added Personal Vehicle Tech research unlock
- Vehicle controls: summon, mount, dismount, recall, altitude control
- Visual hover pod model with engine glow effects

### [1.0.0] - 2025-01-05
- Initial release
- Stilts MKII and MKIII with configurable heights
- Speed Boots with 25% passive speed boost
- Jump Pack with 50% jump boost
- Rail Runner MKII with extended range
- Speed Pad placeable with configurable boost multiplier
- Jump Pad placeable with configurable launch force
- Full tech tree integration with tiered unlocks

---

## Credits

- **Mod Author:** Certifired
- **Development Assistance:** Claude Code (Anthropic) - AI-assisted development and documentation
- **Framework Dependencies:**
  - Equinox - EquinoxsModUtils and EMUAdditions
  - Certifired - TechtonicaFramework
- **Special Thanks:** The Techtonica modding community

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

You are free to:
- Use, modify, and distribute this mod
- Create derivative works based on this mod

Under the following conditions:
- You must provide appropriate credit
- You must distribute derivative works under the same license
- You must make the source code available

For the full license text, see: https://www.gnu.org/licenses/gpl-3.0.en.html

---

## Links

- **Source Code:** [GitHub Repository](https://github.com/certifired/MobilityPlus) *(if applicable)*
- **Bug Reports:** Please report issues on GitHub or the Thunderstore page
- **Thunderstore:** [MobilityPlus on Thunderstore](https://thunderstore.io/c/techtonica/p/Certifired/MobilityPlus/)
- **Techtonica Modding Discord:** [Join the community](https://discord.gg/techtonica)

---

*Made with passion for the Techtonica community*
