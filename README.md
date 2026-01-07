# CertiFried's Techtonica Mod Suite

A comprehensive collection of mods for [Techtonica](https://store.steampowered.com/app/1457320/Techtonica/), including community-maintained updates of Equinox's original mods and new original content.

## Attribution & Credits

Many mods in this repository were **originally created by [Equinox](https://new.thunderstore.io/c/techtonica/p/Equinox/)**:
- **Equinox's Thunderstore Profile:** https://new.thunderstore.io/c/techtonica/p/Equinox/
- **CubeSuite GitHub Organization:** https://github.com/CubeSuite

Original mods have been updated for compatibility with EquinoxsModUtils 6.1.3. All credit for original mod concepts, designs, and implementations goes to Equinox.

New mods marked with **(NEW)** are original creations by CertiFried.

---

## Included Mods

### Core Framework
| Mod | Description |
|-----|-------------|
| [TechtonicaFramework](./TechtonicaFramework/) **(NEW)** | Core framework providing shared systems (health, narrative, equipment, environment) |

### Machine & Production Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [AdvancedMachines](./AdvancedMachines/) | [Original](https://github.com/CubeSuite/TTMod-AdvancedMachines) | MK4-MK5 machine tiers with increased speed/efficiency |
| [AtlantumReactor](./AtlantumReactor/) | [Original](https://github.com/CubeSuite/AtlantumReactor) | End-game Atlantum power generator |
| [AtlantumEnrichment](./AtlantumEnrichment/) **(NEW)** | - | Atlantum enrichment and processing systems |
| [MorePlanters](./MorePlanters/) | [Original](https://github.com/CubeSuite/TTMod-MorePlanters) | Planter MKII and MKIII machines |
| [PlanterCoreClusters](./PlanterCoreClusters/) | [Original](https://github.com/CubeSuite/TTMod-PlanterCoreClusters) | Core Cluster speed boost for Planters |
| [BioProcessing](./BioProcessing/) **(NEW)** | - | Biological processing and organic resources |
| [Recycler](./Recycler/) **(NEW)** | - | Machine recycling and resource recovery |

### Logistics & Storage Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [WormholeChests](./WormholeChests/) | [Original](https://github.com/CubeSuite/TTMod-WormholeChests) | Chests on the same channel share inventory |
| [Restock](./Restock/) | [Original](https://github.com/CubeSuite/TTMod-Restock) | Auto-restock inventory from nearby chests |
| [EnhancedLogistics](./EnhancedLogistics/) **(NEW)** | - | Advanced inventory search and drone management |
| [DroneLogistics](./DroneLogistics/) **(NEW)** | - | Automated drone delivery systems |

### Equipment & Mobility Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [MobilityPlus](./MobilityPlus/) **(NEW)** | - | Enhanced mobility equipment (Stilts MKII/III, Speed Boots, Jump Pack) |
| [BeltImmunity](./BeltImmunity/) | [Original](https://github.com/CubeSuite/TTMod-BeltImmunity) | Immune to conveyor belt movement |
| [OmniseekerPlus](./OmniseekerPlus/) **(NEW)** | - | Enhanced Omniseeker scanner functionality |

### Survival & Combat Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [SurvivalElements](./SurvivalElements/) **(NEW)** | - | Machine health, repair tools, damage systems |
| [HazardousWorld](./HazardousWorld/) **(NEW)** | - | Environmental hazards and protective equipment |
| [TurretDefense](./TurretDefense/) **(NEW)** | - | Automated defense turrets against alien threats |

### Utility & QoL Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [ChainExplosives](./ChainExplosives/) | [Original](https://github.com/CubeSuite/TTMod-ChainExplosives) | Detonate all explosives at once |
| [DevTools](./DevTools/) | [Original](https://github.com/CubeSuite/TTMod-DevTools) | Developer tools and debugging utilities |

### Content & Ambiance Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [NarrativeExpansion](./NarrativeExpansion/) **(NEW)** | - | Expanded dialogue and quest systems |
| [AmbientLife](./AmbientLife/) **(NEW)** | - | Ambient creatures and environmental ambiance |
| [PetsCompanions](./PetsCompanions/) **(NEW)** | - | Pet companions that follow the player |

### Library Mods
| Mod | Original | Description |
|-----|----------|-------------|
| [EMUBuilder_Patched](./EMUBuilder_Patched/) | [Original](https://github.com/CubeSuite/TTMod-EMUBuilder) | Extended Blueprint copy/paste support |

---

## New Features in 2025.01.06

### Resolution-Independent UI
All mods now feature resolution-independent UI positioning that works on any screen resolution including ultrawide (3440x1440) and 4K displays:
- Percentage-based positioning
- Automatic UI scaling based on screen height
- Screen clamping to prevent windows going offscreen

### Modded Tech Tree Tab
TechtonicaFramework adds a dedicated "Modded" category (Tab 7) to the tech tree, providing a clean organized location for all mod unlocks.

---

## Installation

### Via r2modman (Recommended)
1. Install [r2modman](https://thunderstore.io/package/ebkr/r2modman/)
2. Search for mods by "CertiFried" in the mod browser
3. Install desired mods - dependencies will be handled automatically

### Manual Installation
1. Install [BepInEx 5.4.21+](https://github.com/BepInEx/BepInEx/releases)
2. Install [EquinoxsModUtils 6.1.3+](https://new.thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
3. Install [EMUAdditions 2.0.0+](https://new.thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
4. Download mods from [Thunderstore](https://thunderstore.io/c/techtonica/p/CertiFried/)
5. Extract to `BepInEx/plugins/`

---

## Requirements

- **BepInEx 5.4.21+**
- **[EquinoxsModUtils 6.1.3+](https://new.thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)**
- **[EMUAdditions 2.0.0+](https://new.thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)** (for mods that add content)
- Some mods require **[TechtonicaFramework](./TechtonicaFramework/)** as a dependency

---

## API Changes for EMU 6.1.3

EquinoxsModUtils 6.1.3 restructured its API:

```csharp
// Old API (EMU 5.x / 6.0.x)
ModUtils.GameLoaded += OnGameLoaded;
ModUtils.GetUnlockByName("MyUnlock");
ResourceNames.SafeResources;

// New API (EMU 6.1.3+)
EMU.Events.GameLoaded += OnGameLoaded;
EMU.Unlocks.GetUnlockByName("MyUnlock");
EMU.Names.Resources.SafeResources;
```

Additionally, `NewUnlockDetails`, `NewRecipeDetails`, and `RecipeResourceInfo` moved from `EquinoxsModUtils` namespace to `EquinoxsModUtils.Additions` namespace.

---

## Building from Source

Each mod folder contains a `.csproj` file. Requirements:
- .NET Framework 4.7.2 SDK
- References to Techtonica game assemblies
- References to BepInEx, Harmony, and EMU assemblies

---

## Development

These mods were developed with assistance from **Claude Code** (Anthropic's AI coding assistant).
All code has been reviewed and tested by the mod author (CertiFried / xXDeath420Xx).

---

## Links

- **Thunderstore:** https://thunderstore.io/c/techtonica/p/CertiFried/
- **GitHub:** https://github.com/xXDeath420Xx/CertiFried-EMU613-Mods
- **Bug Reports:** https://github.com/xXDeath420Xx/CertiFried-EMU613-Mods/issues

---

## License

All mods are licensed under **GPL-3.0 (GNU General Public License v3.0)**.

See [LICENSE](./LICENSE) for full license text.

---

## Contributing

Contributions welcome! Please:
1. Fork this repository
2. Make your changes
3. Submit a pull request

For original Equinox mods, consider contributing to the [CubeSuite repositories](https://github.com/CubeSuite) as well.
