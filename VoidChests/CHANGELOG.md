# VoidChests_Patched Changelog

## About This Fork
This is a patched version of **VoidChests** originally created by **CubeSuite/Equinox**.
- Original mod: https://thunderstore.io/c/techtonica/p/Equinox/VoidChests/
- Original source: https://github.com/CubeSuite/TTMod-VoidChests
- License: GPL-3.0

## v2.2.0-patched (CertiFried)
**Reason for patch:** Original mod uses deprecated EMU API causing game crash on load.

### Changes from original:
- Updated to use new EMU 6.x event system (`EMU.Events.GameDefinesLoaded` instead of deprecated `ModUtils.add_GameDefinesLoaded`)
- Fixed MachineInstanceList iteration to use `.myArray` property
- Added null safety checks throughout
- Removed Mirror.NetworkServer dependency for single-player compatibility

### Functionality preserved:
- Void Chest automatically deletes any items placed inside
- Works with existing Void Chest machines from original mod
