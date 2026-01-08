# Atlantum Enrichment

A dangerous Atlantum enrichment system mod for Techtonica. Process raw Atlantum through a multi-stage enrichment pipeline to create highly valuable enriched Atlantum - but beware of explosions, radiation, and containment failures!

---

## Table of Contents

- [Overview](#overview)
- [Warning](#warning)
- [Features](#features)
  - [Facilities](#facilities)
  - [Safety System](#safety-system)
  - [Radiation System](#radiation-system)
- [Processing Pipeline](#processing-pipeline)
- [How to Use](#how-to-use)
- [Risk Management](#risk-management)
- [Configuration](#configuration)
- [Installation](#installation)
- [Requirements](#requirements)
- [Mod Integration](#mod-integration)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)
- [Links](#links)

---

## Overview

Atlantum Enrichment adds a complete nuclear-style processing chain to Techtonica, allowing players to refine raw Atlantum into highly valuable enriched Atlantum. The system features realistic risk mechanics including explosions, radiation zones, containment integrity management, and safety module systems.

This mod is designed for players who want a challenging, high-risk/high-reward gameplay experience with dangerous industrial processes.

---

## Warning

**ATLANTUM ENRICHMENT IS DANGEROUS!**

- Facilities can malfunction and explode during operation
- Explosions create persistent radiation zones that damage nearby structures and players
- Enrichment chambers have containment systems that degrade over time
- Containment breaches cause immediate radiation leaks
- Radiation zones spread damage to all nearby facilities

**Always install safety modules and monitor containment integrity!**

---

## Features

### Facilities

#### Centrifuge

The first stage of the enrichment process - separates Atlantum isotopes from raw material.

| Property | Value |
|----------|-------|
| **Input** | Raw Atlantum |
| **Output** | Concentrated Atlantum (40%) + Atlantum Waste (50%) |
| **Processing Rate** | 2 units/second |
| **Risk Level** | LOW (0.5x base explosion risk) |
| **Max Safety Modules** | 2 |
| **Max Health** | 100 |

**Storage Capacities:**
- Raw Atlantum: 100 units
- Concentrated Atlantum: 50 units
- Waste: 50 units

#### Enrichment Chamber

The main enrichment process - converts concentrated Atlantum into highly valuable enriched Atlantum. This is the most dangerous facility.

| Property | Value |
|----------|-------|
| **Input** | Concentrated Atlantum |
| **Output** | Enriched Atlantum (20%) + Atlantum Waste (70%) |
| **Processing Rate** | 0.5 units/second |
| **Risk Level** | HIGH (1.5x base explosion risk) |
| **Max Safety Modules** | 4 |
| **Max Health** | 100 |
| **Containment System** | Yes (degrades during operation) |

**Storage Capacities:**
- Concentrated Atlantum: 50 units
- Enriched Atlantum: 20 units
- Waste: 100 units

**Special Features:**
- Containment integrity that degrades at 0.1 units/second during processing
- Safety modules reduce containment decay rate by 20% each
- Containment breach at 0% integrity causes immediate radiation zone
- Visual indicators show critical status when containment drops below 30%

#### Waste Processor

Safely handles radioactive waste produced by enrichment facilities.

| Property | Value |
|----------|-------|
| **Input** | Atlantum Waste |
| **Output** | Depleted Atlantum (80%) + Recovered Material (5%) |
| **Processing Rate** | 1 unit/second |
| **Risk Level** | MINIMAL (no explosions) |
| **Max Health** | 100 |

**Storage Capacities:**
- Waste: 200 units
- Depleted Atlantum: 100 units
- Recovered Material: 10 units

**Notes:**
- Depleted Atlantum is safe for disposal or composting
- Recovered Material can be recycled back into the production chain
- Destruction releases a small radiation zone

---

### Safety System

#### Safety Modules

Safety modules are critical components that reduce explosion risk and improve facility stability.

| Effect | Value |
|--------|-------|
| **Risk Reduction** | 10% per module (configurable) |
| **Containment Decay Reduction** | 20% per module (Enrichment Chamber only) |

**Installation Limits:**
- Centrifuge: Maximum 2 safety modules
- Enrichment Chamber: Maximum 4 safety modules

#### Containment Integrity (Enrichment Chamber Only)

The Enrichment Chamber features a containment system that must be monitored:

- **Maximum Integrity:** 100%
- **Decay Rate:** 0.1 units/second during processing
- **Critical Threshold:** 30% (visual warning)
- **Breach Threshold:** 0% (causes radiation zone + 50 damage)

**Managing Containment:**
- Install safety modules to slow degradation
- Repair containment regularly using the `RepairContainment()` method
- Monitor the core light color - it shifts to red when critical

---

### Radiation System

#### Radiation Zones

Radiation zones are hazardous areas created by explosions and containment breaches.

| Property | Default Value |
|----------|---------------|
| **Radius** | 15 meters (configurable) |
| **Damage** | 5 HP/second (configurable) |
| **Decay Time** | 300 seconds / 5 minutes (configurable) |

**Effects:**
- Damages all facilities within radius every second
- Centrifuges receive 50% of radiation damage
- Enrichment Chambers receive 50% damage + 20% containment degradation
- Waste Processors receive 30% of radiation damage
- Zone intensity and visual effects decay over time

**Visual Indicators:**
- Green particle effects throughout the zone
- Glowing green light at zone center
- Particle density and light intensity decrease as zone decays

#### Remediation

Radiation zones can be cleaned up through:
- **Natural Decay:** Zones automatically dissipate over time
- **Remediation:** Accelerate decay using the `Remediate(amount)` method
- **Cleansing:** Immediately remove a zone using the `Cleanse()` method
- **BioProcessing Integration:** Bio-remediation systems can accelerate cleanup

---

## Processing Pipeline

```
Raw Atlantum
      |
      v
 [Centrifuge]
      |
      +---> Concentrated Atlantum (40%)
      |            |
      |            v
      |    [Enrichment Chamber]
      |            |
      |            +---> Enriched Atlantum (20%) [VALUABLE!]
      |            |
      |            +---> Atlantum Waste (70%)
      |                        |
      +---> Atlantum Waste (50%) ----+
                                     |
                                     v
                            [Waste Processor]
                                     |
                                     +---> Depleted Atlantum (80%) [Safe]
                                     |
                                     +---> Recovered Material (5%)
```

**Yield Calculations (per 100 Raw Atlantum):**
- Concentrated Atlantum: 40 units
- Enriched Atlantum: 8 units (40 x 0.2)
- Total Waste: 50 + 28 = 78 units
- Depleted Atlantum: 62.4 units (78 x 0.8)
- Recovered Material: 3.9 units (78 x 0.05)

---

## How to Use

### Setting Up Your Enrichment Facility

1. **Place a Centrifuge**
   - Position away from other critical infrastructure
   - Install 1-2 safety modules before operation
   - Connect raw Atlantum supply

2. **Place an Enrichment Chamber**
   - Keep significant distance from Centrifuge (radiation spreads!)
   - Install all 4 safety modules - this is critical!
   - Connect output from Centrifuge

3. **Place a Waste Processor**
   - Position to receive waste from both Centrifuge and Chamber
   - Can be placed closer as it has minimal risk

4. **Monitor Operations**
   - Watch containment integrity on Enrichment Chambers
   - Repair containment before it reaches critical levels
   - Respond quickly to any radiation zones

### Operating Facilities

**Adding Resources:**
```csharp
centrifuge.AddRawAtlantum(amount);
chamber.AddConcentratedAtlantum(amount);
wasteProcessor.AddWaste(amount);
```

**Extracting Products:**
```csharp
float concentrated = centrifuge.TakeConcentrated(amount);
float enriched = chamber.TakeEnriched(amount);
float depleted = wasteProcessor.TakeDepleted(amount);
float recovered = wasteProcessor.TakeRecovered(amount);
```

**Maintenance:**
```csharp
facility.InstallSafetyModule();
facility.Repair(amount);
chamber.RepairContainment(amount);
```

---

## Risk Management

### Explosion Risk by Facility

| Facility | Base Risk | With Max Safety Modules |
|----------|-----------|------------------------|
| Centrifuge | 12.5% | 2.5% (with 2 modules) |
| Enrichment Chamber | 37.5% | 7.5% (with 4 modules) |

*Note: Enrichment Chamber risk increases significantly when containment is degraded*

### Containment Risk Factor

The Enrichment Chamber's explosion risk is multiplied by containment status:

| Containment | Risk Multiplier |
|-------------|-----------------|
| 100% | 1.0x |
| 50% | 2.0x |
| 0% | 3.0x |

### Best Practices

1. **Always install maximum safety modules** before starting operations
2. **Space facilities apart** - at least 20 meters between Centrifuge and Chamber
3. **Monitor containment constantly** - repair before dropping below 50%
4. **Process waste immediately** - don't let it accumulate
5. **Have escape routes planned** in case of catastrophic failure
6. **Keep repair resources on hand** for emergency containment repairs

---

## Configuration

All configuration options are available in the BepInEx configuration file after first run.

### Risk Settings

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `BaseExplosionRisk` | 0.25 (25%) | 0.0 - 1.0 | Base explosion risk per enrichment cycle |
| `SafetyModuleReduction` | 0.10 (10%) | 0.05 - 0.25 | Risk reduction per safety module installed |
| `EnableExplosions` | true | true/false | Enable/disable explosions entirely |

### Radiation Settings

| Option | Default | Range | Description |
|--------|---------|-------|-------------|
| `RadiationDamage` | 5.0 | 1.0 - 20.0 | Damage per second in radiation zones |
| `RadiationRadius` | 15.0 | 5.0 - 50.0 | Radius of radiation zones in meters |
| `RadiationDecayTime` | 300.0 | 60.0 - 600.0 | Time in seconds for radiation to decay |

### Example Configuration

```ini
[Risk]
BaseExplosionRisk = 0.25
SafetyModuleReduction = 0.10
EnableExplosions = true

[Radiation]
RadiationDamage = 5
RadiationRadius = 15
RadiationDecayTime = 300
```

---

## Installation

### Prerequisites

Ensure you have the following installed:
1. [BepInEx 5.4.2100](https://github.com/BepInEx/BepInEx/releases) or newer
2. [EquinoxsModUtils 6.1.3](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) or newer
3. [EMUAdditions 2.0.0](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) or newer

### Installation Steps

1. **Install BepInEx** (if not already installed)
   - Download BepInEx for Unity Mono
   - Extract to your Techtonica game folder
   - Run the game once to generate BepInEx folders

2. **Install Dependencies**
   - Download EquinoxsModUtils and EMUAdditions
   - Place their DLL files in `BepInEx/plugins/`

3. **Install Atlantum Enrichment**
   - Download `AtlantumEnrichment.dll`
   - Place it in `BepInEx/plugins/`

4. **Launch Techtonica**
   - Configuration file will be generated on first run
   - Find it at `BepInEx/config/com.certifried.atlantumenrichment.cfg`

### Verifying Installation

Check the BepInEx console or log file for:
```
[AtlantumEnrichment] AtlantumEnrichment v1.0.0 loading...
[AtlantumEnrichment] AtlantumEnrichment loaded successfully!
[AtlantumEnrichment] WARNING: Atlantum enrichment is dangerous! Use safety modules!
```

---

## Requirements

| Dependency | Minimum Version | Type |
|------------|-----------------|------|
| BepInEx | 5.4.2100 | Required |
| EquinoxsModUtils | 6.1.3 | Required |
| EMUAdditions | 2.0.0 | Required |
| Techtonica | Latest | Required |

---

## Mod Integration

### BioProcessing Integration

Atlantum Enrichment is designed to integrate with BioProcessing mod:

- **Bio-remediation:** Accelerates radiation zone cleanup
- **Waste Composting:** Depleted Atlantum can be safely processed by bio systems
- **Recovery Recycling:** Recovered materials can feed back into bio-production chains

### API for Other Mods

**Spawning Facilities:**
```csharp
var centrifuge = AtlantumEnrichmentPlugin.SpawnCentrifuge(position);
var chamber = AtlantumEnrichmentPlugin.SpawnEnrichmentChamber(position);
var processor = AtlantumEnrichmentPlugin.SpawnWasteProcessor(position);
```

**Creating Radiation:**
```csharp
var zone = AtlantumEnrichmentPlugin.CreateRadiationZone(position, intensity);
```

**Triggering Explosions:**
```csharp
AtlantumEnrichmentPlugin.TriggerExplosion(position, radius, damage);
```

**Accessing Statistics:**
```csharp
float totalEnriched = AtlantumEnrichmentPlugin.TotalEnrichedAtlantum;
int explosions = AtlantumEnrichmentPlugin.TotalExplosions;
float totalWaste = AtlantumEnrichmentPlugin.TotalWasteProduced;
```

---

## Changelog

### [1.0.0] - 2025-01-05

**Initial Release**

- Added Centrifuge facility for isotope separation
  - Low-risk first-stage processing
  - 40% concentrated yield, 50% waste yield
  - Support for 2 safety modules

- Added Enrichment Chamber facility
  - High-risk main enrichment process
  - 20% enriched yield, 70% waste yield
  - Containment integrity system with degradation
  - Support for 4 safety modules

- Added Waste Processor facility
  - Safe waste processing
  - 80% depleted yield, 5% recovery yield

- Added Radiation Zone system
  - Created by explosions and containment breaches
  - Damages nearby facilities over time
  - Natural decay over configurable time period
  - Remediation and cleansing support

- Added Safety Module system
  - Reduces explosion risk
  - Slows containment degradation
  - Visual indicators for installed modules

- Added full configuration support
  - Explosion risk settings
  - Radiation damage and radius settings
  - Option to disable explosions entirely

- Added BioProcessing integration hooks
  - Bio-remediation for radiation cleanup
  - Waste composting compatibility

---

## Credits

### Development

- **Certifried** - Original mod concept, design, and development

### Assistance

- **Claude Code** (Anthropic) - Development assistance, code review, and documentation

### Dependencies

- **BepInEx Team** - BepInEx modding framework
- **Equinox** - EquinoxsModUtils and EMUAdditions libraries

### Special Thanks

- The Techtonica modding community for their support and feedback
- Fire Hose Games for creating Techtonica

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

You are free to:
- **Use** - Run the mod for any purpose
- **Study** - Examine how the mod works
- **Share** - Redistribute copies of the mod
- **Modify** - Make changes and distribute modified versions

Under the following conditions:
- **Disclose Source** - Source code must be made available when distributing
- **License** - Modified versions must be licensed under GPL-3.0
- **State Changes** - Changes made to the code must be documented

For the full license text, see: https://www.gnu.org/licenses/gpl-3.0.en.html

---

## Links

### Official Resources

- **Thunderstore:** [Coming Soon]
- **GitHub Repository:** [Coming Soon]
- **Bug Reports:** [Coming Soon]

### Related Mods

- **EquinoxsModUtils:** https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/
- **EMUAdditions:** https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/
- **BioProcessing:** [If available]

### Community

- **Techtonica Discord:** https://discord.gg/techtonica
- **Techtonica Modding:** https://thunderstore.io/c/techtonica/

### Tools

- **BepInEx:** https://github.com/BepInEx/BepInEx
- **Unity Mod Development:** https://docs.bepinex.dev/

---

*Last Updated: January 2025*
