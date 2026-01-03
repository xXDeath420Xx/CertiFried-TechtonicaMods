# CertiFried's EMU 6.1.3 Updated Mods for Techtonica

Community-maintained updates of popular Techtonica mods for EquinoxsModUtils 6.1.3 compatibility.

## Attribution & Credits

**All mods in this repository were originally created by [Equinox](https://new.thunderstore.io/c/techtonica/p/Equinox/).**

- **Equinox's Thunderstore Profile:** https://new.thunderstore.io/c/techtonica/p/Equinox/
- **CubeSuite GitHub Organization:** https://github.com/CubeSuite

This repository contains updated versions of Equinox's mods that have been modified for compatibility with EquinoxsModUtils 6.1.3. All credit for the original mod concepts, designs, and implementations goes to Equinox.

## Included Mods

| Mod | Original | Description |
|-----|----------|-------------|
| [WormholeChests](./WormholeChests/) | [Original](https://github.com/CubeSuite/TTMod-WormholeChests) | Chests on the same channel share inventory |
| [Restock](./Restock/) | [Original](https://github.com/CubeSuite/TTMod-Restock) | Auto-restock inventory from nearby chests |
| [AtlantumReactor](./AtlantumReactor/) | [Original](https://github.com/CubeSuite/TTMod-AtlantumReactor) | End-game Atlantum power generator |
| [MorePlanters](./MorePlanters/) | [Original](https://github.com/CubeSuite/TTMod-MorePlanters) | Planter MKII and MKIII machines |
| [PlanterCoreClusters](./PlanterCoreClusters/) | [Original](https://github.com/CubeSuite/TTMod-PlanterCoreClusters) | Core Cluster speed boost for Planters |
| [EMUBuilder_Patched](./EMUBuilder_Patched/) | [Original](https://github.com/CubeSuite/TTMod-EMUBuilder) | Extended Blueprint copy/paste support |

## What Changed for EMU 6.1.3

EquinoxsModUtils 6.1.3 restructured its API from flat static methods to nested classes:

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

Additionally, `NewUnlockDetails`, `NewRecipeDetails`, and `RecipeResourceInfo` moved from `EquinoxsModUtils` namespace to `EquinoxsModUtils.Additions` namespace (in EMUAdditions assembly).

See each mod's CHANGELOG.md for detailed changes.

## Installation

These mods are available on Thunderstore under the [CertiFried](https://thunderstore.io/c/techtonica/p/CertiFried/) team:

- Install via r2modman (recommended)
- Or download from Thunderstore and extract to `BepInEx/plugins/`

## Requirements

- BepInEx 5.4.21+
- [EquinoxsModUtils 6.1.3+](https://new.thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- [EMUAdditions 2.0.0+](https://new.thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) (for mods that add content)

## Building from Source

Each mod folder contains a `.csproj` file. You'll need:
- .NET Framework 4.7.2 SDK
- References to Techtonica game assemblies (Assembly-CSharp.dll, etc.)
- References to BepInEx, Harmony, and EMU assemblies

## License

All mods are licensed under **GPL-3.0 (GNU General Public License v3.0)**, consistent with the original licensing by Equinox.

See [LICENSE](./LICENSE) for full license text.

## Contributing

If you'd like to contribute fixes or improvements:
1. Fork this repository
2. Make your changes
3. Submit a pull request

We also encourage submitting PRs to the original CubeSuite repositories if Equinox is accepting contributions.
