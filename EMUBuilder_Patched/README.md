# EMUBuilder (Community Patched)

A community-maintained patch of EMUBuilder with extended machine type support for the Blueprints mod's copy/paste functionality.

## Credits & Attribution

**Original Author:** [Equinox](https://new.thunderstore.io/c/techtonica/p/Equinox/)

This mod was originally created by Equinox as part of the [CubeSuite](https://github.com/CubeSuite) collection of Techtonica mods. This is a community patch that extends the original functionality.

- **Original Thunderstore Page:** [Equinox's EMUBuilder](https://new.thunderstore.io/c/techtonica/p/Equinox/EMUBuilder/)
- **Original Source Repository:** [TTMod-EMUBuilder](https://github.com/CubeSuite/TTMod-EMUBuilder)
- **Equinox's Thunderstore Profile:** [https://new.thunderstore.io/c/techtonica/p/Equinox/](https://new.thunderstore.io/c/techtonica/p/Equinox/)

All credit for the original mod concept, design, and implementation goes to Equinox.

## What This Patch Adds

This patched version extends EMUBuilder to support additional machine types in the `SupportedMachineTypes` array and `BuildMachine` switch statement, enabling the Blueprints mod to copy/paste:

- **Water Wheels** (MachineTypeEnum 23)
- **Chests** (MachineTypeEnum 3) - for WormholeChests compatibility
- **Planters** (MachineTypeEnum 10) - for MorePlanters MKII/III compatibility
- **Power Generators** (MachineTypeEnum 11)
- **Accumulators** (MachineTypeEnum 24)
- **High Voltage Cables** (MachineTypeEnum 25)
- **Voltage Steppers** (MachineTypeEnum 26)
- **Memory Trees** (MachineTypeEnum 30) - for AtlantumReactor compatibility

## Requirements

- BepInEx 5.4.23.4+
- EquinoxsModUtils 6.1.3+
- EquinoxsDebuggingTools 2.0.0+

## Changelog

### v1.1.0 (Community Patch)
- Extended `SupportedMachineTypes` array to include 8 additional machine types
- Patched `MachineBuilder.BuildMachine` switch statement to handle new machine types via DoSimpleBuild
- Enables Blueprints copy/paste for Water Wheels, modded Planters, WormholeChests, AtlantumReactors, and power infrastructure
- Updated dependencies for EMU 6.1.3 compatibility

### v1.0.0 (Original by Equinox)
- Initial release by Equinox

## License

GPL-3.0 (GNU General Public License v3.0) - See [original repository](https://github.com/CubeSuite/TTMod-EMUBuilder) for full license.
