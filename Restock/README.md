# Restock - Updated for EMU 6.1.3

Automatically restocks your inventory from nearby chests.

## Features

- Automatically pulls items from nearby chests to maintain configured stack counts
- Configure desired quantity per item type in the config file
- Adjustable scan radius (0-10 blocks)
- Buildings default to 1 stack, items default to 0 (disabled)

## Configuration

After first run, edit the config file to set desired stack counts for each item type:

- **Restock Radius**: The radius around the player to scan for chests (0-10)
- **Per-item settings**: Number of stacks to maintain for each resource

## Changelog

### v3.0.0
- Updated for EquinoxsModUtils 6.1.3 compatibility
- Fixed API changes (ResourceNames -> EMU.Names.Resources)
- Fixed duplicate key handling in configuration

## Credits & Attribution

**Original Author:** [Equinox](https://new.thunderstore.io/c/techtonica/p/Equinox/)

This mod was originally created by Equinox as part of the [CubeSuite](https://github.com/CubeSuite) collection of Techtonica mods. This version has been updated for EMU 6.1.3 compatibility.

- **Original Thunderstore Page:** [Equinox's Restock](https://new.thunderstore.io/c/techtonica/p/Equinox/Restock/)
- **Original Source Repository:** [TTMod-Restock](https://github.com/CubeSuite/TTMod-Restock)
- **Equinox's Thunderstore Profile:** [https://new.thunderstore.io/c/techtonica/p/Equinox/](https://new.thunderstore.io/c/techtonica/p/Equinox/)

All credit for the original mod concept, design, and implementation goes to Equinox.

## License

GPL-3.0 (GNU General Public License v3.0) - See [original repository](https://github.com/CubeSuite/TTMod-Restock) for full license.
