# BetterEndGame_Patched Changelog

## About This Fork
This is a patched version of **BetterEndGame** originally created by **CubeSuite/Equinox**.
- Original mod: https://thunderstore.io/c/techtonica/p/Equinox/BetterEndGame/
- Original source: https://github.com/CubeSuite/TTMod-BetterEndGame
- License: GPL-3.0

## v1.2.0-patched (CertiFried)
**Reason for patch:** Original mod uses deprecated EMU API causing game crash on load.

### Changes from original:
- Updated to use new EMU 6.x event system (`EMU.Events.GameDefinesLoaded` instead of deprecated `ModUtils.add_GameDefinesLoaded`)
- Updated `EMU.Events.SaveStateLoaded` to use proper delegate signature
- Removed Mirror.NetworkServer dependency for broader compatibility
- Added configurable settings for cost multiplier and bonus per level

### Functionality preserved:
- Infinite unlock progression system
- Miner Speed, Fuel Efficiency, Power Usage, Inventory Size, Walk Speed, MOLE Speed unlocks
- Cost scaling per level
- Endgame progression challenges
