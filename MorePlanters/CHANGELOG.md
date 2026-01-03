# Changelog

All notable changes to MorePlanters will be documented in this file.

## [3.0.1] - 2025-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2025-01-02

### Changed
- **API Migration to EMU 6.1.3 nested class structure:**
  - `ModUtils.GameDefinesLoaded` → `EMU.Events.GameDefinesLoaded`
  - `ModUtils.GameLoaded` → `EMU.Events.GameLoaded`
  - `ModUtils.MachineManagerLoaded` → `EMU.Events.MachineManagerLoaded`
  - `ModUtils.GetResourceInfoByName()` → `EMU.Resources.GetResourceInfoByName()`
  - `ModUtils.GetUnlockByName()` → `EMU.Unlocks.GetUnlockByName()`
  - `ModUtils.LoadSpriteFromFile()` → `EMU.Images.LoadSpriteFromFile()`
  - `ModUtils.CloneObject()` → `EMU.CloneObject()`
  - `ModUtils.AddNewUnlock()` → `EMUAdditions.AddNewUnlock()`
- Updated `NewUnlockDetails` class references from `EquinoxsModUtils.NewUnlockDetails` to `EquinoxsModUtils.Additions.NewUnlockDetails`
- Updated `NewRecipeDetails` class references from `EquinoxsModUtils.NewRecipeDetails` to `EquinoxsModUtils.Additions.NewRecipeDetails`
- Updated `RecipeResourceInfo` class references from `EquinoxsModUtils.RecipeResourceInfo` to `EquinoxsModUtils.Additions.RecipeResourceInfo`

### Technical Details
- Planter MKII definition updated for new EMU API
- Planter MKIII definition updated for new EMU API
- Machine cloning now uses `EMU.CloneObject<T>()` helper
- All unlock and recipe registrations use EMUAdditions 2.0.0+ API
