# Changelog

All notable changes to PlanterCoreClusters will be documented in this file.

## [3.0.1] - 2025-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2025-01-02

### Changed
- **API Migration to EMU 6.1.3 nested class structure:**
  - `ModUtils.GameDefinesLoaded` → `EMU.Events.GameDefinesLoaded`
  - `ModUtils.TechTreeStateLoaded` → `EMU.Events.TechTreeStateLoaded`
  - `ModUtils.GetUnlockByName()` → `EMU.Unlocks.GetUnlockByName()`
  - `ModUtils.UpdateUnlockSprite()` → `EMU.Unlocks.UpdateUnlockSprite()`
  - `ModUtils.LoadSpriteFromFile()` → `EMU.Images.LoadSpriteFromFile()`
  - `ModUtils.AddNewUnlock()` → `EMUAdditions.AddNewUnlock()`
- Updated `NewUnlockDetails` class references from `EquinoxsModUtils.NewUnlockDetails` to `EquinoxsModUtils.Additions.NewUnlockDetails`

### Technical Details
- "Planter Core Boost" unlock registration updated for new EMU API
- Core Cluster speed calculation logic unchanged
- Harmony patches for Planter speed modification remain compatible
