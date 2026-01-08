# MorePlanters

A Techtonica mod that adds advanced Planter machines with enhanced growth speeds and integrated processing capabilities.

## Overview

MorePlanters extends the base game's farming system by introducing two upgraded Planter variants: the **Planter MKII** and **Planter MKIII**. These machines offer faster plant growth, increased yields, and in the case of the MKIII, built-in threshing functionality that outputs processed materials directly.

Whether you're looking to optimize your farming operations or simply want to reduce the number of machines in your production lines, MorePlanters provides flexible solutions that scale with your factory's needs.

---

## Features

### Planter MKII
- **2x Growth Speed** - Plants grow twice as fast as the standard Planter (configurable)
- **Double Yield** - Produces two plants per seed instead of one (configurable)
- **Same Footprint** - Uses the same physical space as the base Planter

### Planter MKIII
- **2x Growth Speed** - Matches the MKII's accelerated growth rate (configurable)
- **Integrated Thresher** - Automatically processes harvested plants into refined materials
- **Direct Output** - Outputs processed materials instead of raw plants:
  - Kindlevine → Kindlevine Stems
  - Shiverthorn → Shiverthorn Buds
  - Plantmatter → Plantmatter Fiber
- **Streamlined Production** - Eliminates the need for separate Thresher machines in your plant processing lines

### Research Unlocks
Both planter variants are unlocked through the Modded tech tree:

| Technology | Research Tier | Core Cost |
|------------|---------------|-----------|
| Planter MKII Tech | Tier 5 | 100 Blue Cores |
| Planter MKIII Tech | Tier 7 | 250 Blue Cores |

### Crafting Recipes

**Planter MKII:**
- 1x Planter
- 5x Mechanical Components
- 10x Copper Wire

**Planter MKIII:**
- 1x Planter MKII
- 1x Thresher
- 2x Processor Unit

---

## How to Use

### Building Planter MKII
1. Research "Planter MKII Tech" in the Modded category of the tech tree
2. Craft Planter MKII in an Assembler
3. Place the Planter MKII like a standard Planter
4. Insert seeds and water as normal
5. Harvest double the plants at twice the speed

### Building Planter MKIII
1. First research and craft a Planter MKII
2. Research "Planter MKIII Tech" in the Modded category
3. Craft Planter MKIII in an Assembler
4. Place and use like a standard Planter
5. Collect processed materials directly from the output - no Thresher needed!

### Tips
- Connect Inserters to the MKIII output for fully automated processed material production
- The MKII's double yield stacks well with the faster growth speed for maximum raw plant output
- Use MKIII when you need processed materials; use MKII when you need raw plants in bulk

---

## Configuration

Configuration options are available in the BepInEx config file located at:
```
BepInEx/config/com.equinox.MorePlanters.cfg
```

### Available Options

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `Speed Multiplier` | 2.0 | 1.0 - 10.0 | Growth speed multiplier for upgraded planters. Higher values = faster growth. |
| `Double Plants` | true | true/false | Whether Planter MKII produces two plants per seed. |

### Example Configuration
```ini
[General]

## Speed multiplier for upgraded planters (default 2x).
# Setting type: Single
# Default value: 2
# Acceptable value range: From 1 to 10
Speed Multiplier = 2

## Whether the Planter MKII should produce two plants per seed.
# Setting type: Boolean
# Default value: true
Double Plants = true
```

---

## Installation

### Using r2modman (Recommended)
1. Open r2modman
2. Select Techtonica as your game
3. Search for "MorePlanters" in the online tab
4. Click "Download" to install the mod and all dependencies

### Manual Installation
1. Ensure all required dependencies are installed (see Requirements below)
2. Download the latest release of MorePlanters
3. Extract the `MorePlanters.dll` file
4. Place the DLL in your `BepInEx/plugins` folder
5. Launch the game

---

## Requirements

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21+ | Mod loading framework |
| [EquinoxsModUtils (EMU)](https://github.com/CubeSuite/TTMod-EquinoxsModUtils) | 6.1.3+ | Core modding utilities |
| [EMUAdditions](https://github.com/CubeSuite/TTMod-EMUAdditions) | 2.0.0+ | Machine and unlock registration |
| [TechtonicaFramework](https://github.com/CertiFried/TechtonicaFramework) | Latest | Tech tree integration |

All dependencies can be automatically installed via r2modman.

---

## Compatibility

- **Game Version:** Compatible with the current version of Techtonica
- **Multiplayer:** Not tested in multiplayer environments
- **Save Games:** Safe to add to existing saves. Removing the mod may leave orphaned machines.

---

## Known Issues

- None currently reported. Please submit issues on the GitHub repository.

---

## Changelog

### [3.0.9] - Latest
- Current stable release

### [3.0.5] - 2025-01-05
- Version bump for bulk update

### [3.0.3] - 2025-01-03
- Added configurable speed multiplier (default 2.0x, range 1-10x)

### [3.0.0] - 2025-01-02
- API migration to EMU 6.1.3 nested class structure
- Added TechtonicaFramework dependency
- Improved tech tree integration

---

## Credits

- **Original Mod:** [Equinox](https://github.com/CubeSuite/TTMod-MorePlanters) - Original concept and implementation
- **Maintainer:** CertiFried - Updates, maintenance, and new features
- **Development Assistance:** Claude Code by Anthropic - Code review, documentation, and development support

### Special Thanks
- The Techtonica modding community for their support and feedback
- Fire Hose Games for creating Techtonica

---

## License

This project is licensed under the **GNU General Public License v3.0** (GPL-3.0).

You are free to:
- Use this mod for any purpose
- Study how the mod works and modify it
- Distribute copies of the mod
- Distribute modified versions of the mod

Under the following conditions:
- You must include the original license and copyright notice
- You must disclose the source code of any modifications
- Modified versions must also be licensed under GPL-3.0

For the full license text, see: [GNU GPL v3.0](https://www.gnu.org/licenses/gpl-3.0.en.html)

---

## Links

- **Source Code:** [GitHub Repository](https://github.com/CubeSuite/TTMod-MorePlanters)
- **Thunderstore:** [MorePlanters on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/MorePlanters/)
- **Bug Reports:** [GitHub Issues](https://github.com/CubeSuite/TTMod-MorePlanters/issues)
- **Techtonica Discord:** [Official Discord](https://discord.gg/techtonica)
- **EMU Documentation:** [EquinoxsModUtils Wiki](https://github.com/CubeSuite/TTMod-EquinoxsModUtils/wiki)

---

## Support

If you encounter any issues or have feature requests:

1. Check the [Known Issues](#known-issues) section above
2. Search existing [GitHub Issues](https://github.com/CubeSuite/TTMod-MorePlanters/issues)
3. If your issue isn't listed, create a new issue with:
   - A clear description of the problem
   - Steps to reproduce
   - Your mod list and game version
   - Any relevant log files from `BepInEx/LogOutput.log`

---

*Happy farming!*
