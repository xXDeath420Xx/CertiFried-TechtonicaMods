# Changelog

All notable changes to WormholeChests will be documented in this file.

## [3.0.1] - 2025-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2025-01-02

### Added
- Global `isUnlockActive` flag to completely disable mod functionality when "Wormhole Chests" tech is not researched
- `TechTreeStateLoaded` event subscription to properly check unlock status after game loads
- Comprehensive null checks throughout all Harmony patches
- Try-catch exception handling around critical patch methods to prevent game crashes
- Safe dictionary lookups using `TryGetValue` instead of direct indexing

### Changed
- **API Migration to EMU 6.1.3 nested class structure:**
  - `ModUtils.GameLoaded` → `EMU.Events.GameLoaded`
  - `ModUtils.SaveStateLoaded` → `EMU.Events.SaveStateLoaded`
  - `ModUtils.TechTreeStateLoaded` → `EMU.Events.TechTreeStateLoaded`
  - `ModUtils.GetUnlockByName()` → `EMU.Unlocks.GetUnlockByName()`
  - `ModUtils.AddNewUnlock()` → `EMUAdditions.AddNewUnlock()`
- All Harmony patches now check `isUnlockActive` flag before executing any mod logic
- When unlock is not researched, chests function as normal vanilla chests

### Fixed
- Potential NullReferenceException when opening chests before game fully loads
- Potential crashes when chest machine reference is invalid
- Dictionary key not found exceptions when looking up wormhole channels
- Game freeze issues caused by patches running before proper initialization

### Technical Details
- Modified patches: `GetWormholeInsteadOfInventory`, `DoSetChestChannel`, `DoDestroyChest`, `GetGUIWormhole`, `DoLoadWormholes`, `DoSaveWormholes`
- Added safety wrapper to `Wormhole.GetWormhole()` static method
- Added validation for `GenericMachineInstanceRef.IsValid()` before accessing chest data
