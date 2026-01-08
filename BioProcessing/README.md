# BioProcessing

A comprehensive biological processing system mod for Techtonica that introduces organic farming, renewable fuel production, and environmental remediation mechanics.

## Overview

BioProcessing adds a complete organic production chain to Techtonica, allowing players to cultivate biological resources and convert them into valuable biofuel and biogas. This mod creates a sustainable, renewable energy ecosystem that integrates seamlessly with other mods in the Techtonica modding ecosystem.

---

## Features

### Bio-Processing Facilities

#### Algae Vat
A cylindrical cultivation tank for growing raw algae through photosynthesis.

| Specification | Value |
|---------------|-------|
| Storage Capacity | 100 units |
| Base Production Rate | 1 unit/second |
| Requirements | Water, Light |

**Key Features:**
- Continuous algae production while water and light conditions are met
- Visual feedback with color-changing tank (green intensity indicates fill level)
- Animated bubble particle effects during active production
- Harvest all or partial amounts of stored algae

#### Mushroom Farm
A cultivation plot for growing mushrooms in controlled dark conditions.

| Specification | Value |
|---------------|-------|
| Max Mushrooms | 20 |
| Growth Time | 30 seconds per mushroom |
| Fertilizer per Mushroom | 2 units |
| Fertilizer Capacity | 50 units |

**Key Features:**
- Requires fertilizer input (from Composter) to grow mushrooms
- Individual mushroom visuals appear as they mature
- Produces both mushrooms and spores
- Can receive compost directly as fertilizer input

#### Bio-Reactor
The central processing unit that converts organic matter into usable fuel products.

| Specification | Value |
|---------------|-------|
| Organic Matter Capacity | 200 units |
| Biofuel Capacity | 100 units |
| Biogas Capacity | 100 units |
| Biofuel Conversion Rate | 30% |
| Biogas Conversion Rate | 50% |
| Processing Rate | 5 units/second |

**Key Features:**
- Accepts multiple organic inputs: raw algae, mushrooms, and organic waste
- Dual output: Biofuel (liquid fuel) and Biogas (for power generation)
- Visual reactor glow when actively processing
- Steam particle effects during operation
- Each mushroom contributes 5 units of organic matter

#### Composter
Converts organic waste into valuable fertilizer for mushroom cultivation.

| Specification | Value |
|---------------|-------|
| Waste Capacity | 100 units |
| Fertilizer Capacity | 50 units |
| Conversion Efficiency | 50% |
| Processing Time | 60 seconds per batch (10 units) |

**Key Features:**
- Processes organic waste in batches of 10 units
- Visual fill indicator for organic matter level
- Produces fertilizer to feed Mushroom Farms
- Creates a closed-loop resource cycle

### Bio-Remediation System
An advanced environmental cleanup feature using biological processes.

- Clean up hazardous and contaminated zones
- Accelerates natural decay of radiation from nuclear processes
- Visual particle effects indicate active remediation
- Can be enabled/disabled via configuration

---

## Resource Flow Diagram

```
+----------------+     +------------------+     +---------------+
|  Water + Light |---->|    Algae Vat     |---->|   Raw Algae   |
+----------------+     +------------------+     +-------+-------+
                                                        |
                                                        v
+----------------+     +------------------+     +---------------+
|   Fertilizer   |---->|  Mushroom Farm   |---->|   Mushrooms   |
+-------^--------+     +------------------+     +-------+-------+
        |                                               |
        |                                               v
+-------+--------+     +------------------+     +---------------+
|   Composter    |<----|  Organic Waste   |<----|  Bio-Reactor  |
+----------------+     +------------------+     +-------+-------+
                                                        |
                                               +--------+--------+
                                               |                 |
                                               v                 v
                                          +---------+      +---------+
                                          | Biofuel |      | Biogas  |
                                          +---------+      +---------+
```

**Simplified Flow:**
```
Water + Light -> Algae Vat -> Raw Algae -+
                                         |
Fertilizer -> Mushroom Farm -> Mushrooms-+-> Bio-Reactor -> Biofuel + Biogas
     ^                                   |
     |                                   |
Composter <---- Organic Waste <----------+
```

---

## How to Use

### Getting Started

1. **Build an Algae Vat** - This is your primary organic matter source. Place it in a well-lit area with access to water.

2. **Set up a Composter** - Feed it organic waste to produce fertilizer for your mushroom farms.

3. **Create a Mushroom Farm** - Supply it with fertilizer from your composter to begin mushroom cultivation.

4. **Construct a Bio-Reactor** - Connect your organic outputs (algae and mushrooms) to begin fuel production.

### Production Tips

- **Maximize Algae Production**: Ensure continuous water and light supply for uninterrupted growth
- **Fertilizer Loop**: Connect Composters to Mushroom Farms for an automated fertilizer supply
- **Efficient Processing**: Balance your organic matter input to avoid Bio-Reactor overflow
- **Harvest Timing**: Harvest algae before vats reach capacity to maintain production efficiency

### Integration with Other Mods

- **Recycler Mod**: Organic waste from recycling operations can feed directly into Composters
- **DroneLogistics**: Biofuel produced can power drone operations
- **AtlantumEnrichment**: Bio-remediation can clean up radiation zones created by nuclear processes

---

## Installation

### Prerequisites

Ensure you have the following installed before adding BioProcessing:

1. **BepInEx** (version 5.4.2100 or higher)
   - The modding framework required for all Techtonica mods

2. **EquinoxsModUtils** (version 6.1.3 or higher)
   - Core utility library for Techtonica mods

3. **EMUAdditions** (version 2.0.0 or higher)
   - Extended utilities for mod development

### Installation Steps

1. Download the latest release of BioProcessing
2. Locate your Techtonica installation directory
3. Navigate to the `BepInEx/plugins` folder
4. Extract or copy `BioProcessing.dll` into the plugins folder
5. (Optional) Copy the `Bundles` folder if custom assets are included
6. Launch Techtonica - the mod will load automatically

### Folder Structure
```
Techtonica/
  BepInEx/
    plugins/
      BioProcessing.dll
      Bundles/              (optional)
        mushroom_forest
        lava_plants
        fauna_turtle
```

---

## Configuration

BioProcessing can be customized through the BepInEx configuration system. After first launch, a configuration file will be created at:

```
BepInEx/config/com.certifried.bioprocessing.cfg
```

### Production Settings

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `AlgaeGrowthRate` | 1.0 | 0.1 - 5.0 | Multiplier for algae growth speed |
| `MushroomGrowthRate` | 1.0 | 0.1 - 5.0 | Multiplier for mushroom growth speed |
| `BiofuelYield` | 1.0 | 0.5 - 3.0 | Multiplier for biofuel output |
| `CompostEfficiency` | 1.0 | 0.5 - 2.0 | Multiplier for compost conversion rate |

### Feature Toggles

| Option | Default | Description |
|--------|---------|-------------|
| `EnableBioRemediation` | true | Allow biological cleanup of hazardous zones |

### Example Configuration
```ini
[Production]
## Multiplier for algae growth speed
# Setting type: Single
# Default value: 1
# Acceptable value range: From 0.1 to 5
AlgaeGrowthRate = 1.5

## Multiplier for mushroom growth speed
# Setting type: Single
# Default value: 1
# Acceptable value range: From 0.1 to 5
MushroomGrowthRate = 1.2

## Multiplier for biofuel output
# Setting type: Single
# Default value: 1
# Acceptable value range: From 0.5 to 3
BiofuelYield = 1

## Multiplier for compost conversion rate
# Setting type: Single
# Default value: 1
# Acceptable value range: From 0.5 to 2
CompostEfficiency = 1

[Features]
## Allow biological cleanup of hazardous zones
# Setting type: Boolean
# Default value: true
EnableBioRemediation = true
```

---

## Requirements

### Hard Dependencies

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| BepInEx | 5.4.2100 | Mod loading framework |
| EquinoxsModUtils | 6.1.3 | Core mod utilities |
| EMUAdditions | 2.0.0 | Extended mod utilities |

### Soft Dependencies (Optional)

| Mod | Integration |
|-----|-------------|
| Recycler | Organic waste → Composter input |
| DroneLogistics | Biofuel → Drone power |
| AtlantumEnrichment | Bio-remediation of radiation |
| HazardousWorld | Hazard zone cleanup |

---

## Changelog

### [1.0.0] - 2025-01-05
**Initial Release**
- Added Algae Vat facility for continuous algae production
- Added Mushroom Farm with fertilizer-based growth system
- Added Bio-Reactor for biofuel and biogas conversion
- Added Composter for organic waste processing
- Implemented Bio-remediation system for hazardous zone cleanup
- Added configurable production rates and efficiencies
- Created visual feedback systems (particle effects, color changes, lighting)
- Established integration points with Recycler, DroneLogistics, and AtlantumEnrichment mods
- Asset bundle support for custom 3D models (mushroom_forest, lava_plants, fauna_turtle)

---

## Troubleshooting

### Common Issues

**Mod not loading:**
- Verify BepInEx is correctly installed
- Check that all dependencies are present and up to date
- Review the BepInEx log file for error messages

**Facilities not producing:**
- Algae Vat: Ensure water and light conditions are met
- Mushroom Farm: Check fertilizer levels
- Bio-Reactor: Verify organic matter input is available
- Composter: Requires minimum 10 units of waste to begin processing

**Configuration not applying:**
- Delete the config file and restart the game to regenerate defaults
- Ensure values are within acceptable ranges

---

## Credits and Attribution

### Development
- **Certifried** - Primary mod developer and concept design

### AI Assistance
- **Claude Code** (Anthropic) - Development assistance, code architecture, and documentation

### Special Thanks
- The Techtonica modding community
- EquinoxsModUtils developers for the modding framework
- All beta testers and contributors

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

```
BioProcessing - A biological processing mod for Techtonica
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

---

## Links

- **Source Code**: [GitHub Repository](https://github.com/certifried/BioProcessing)
- **Issue Tracker**: [Report Bugs](https://github.com/certifried/BioProcessing/issues)
- **Techtonica Modding Discord**: [Join the Community](https://discord.gg/techtonica)
- **BepInEx Documentation**: [BepInEx Wiki](https://docs.bepinex.dev/)
- **EquinoxsModUtils**: [EMU GitHub](https://github.com/equinox/EquinoxsModUtils)

---

## Support

If you encounter issues or have suggestions:

1. Check the [Troubleshooting](#troubleshooting) section above
2. Search existing [GitHub Issues](https://github.com/certifried/BioProcessing/issues)
3. Create a new issue with:
   - Your game version
   - Mod version
   - BepInEx log file
   - Steps to reproduce the problem

---

*Made with care for the Techtonica community*
