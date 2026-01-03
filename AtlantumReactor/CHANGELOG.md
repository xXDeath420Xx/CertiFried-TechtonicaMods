# Changelog

All notable changes to AtlantumReactor will be documented in this file.

## [3.0.1] - 2025-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2025-01-02

### Changed
- **API Migration to EMU 6.1.3 nested class structure:**
  - `ModUtils.GameDefinesLoaded` → `EMU.Events.GameDefinesLoaded`
  - `ModUtils.GameLoaded` → `EMU.Events.GameLoaded`
  - `ModUtils.SaveStateLoaded` → `EMU.Events.SaveStateLoaded`
  - `ModUtils.GetResourceInfoByName()` → `EMU.Resources.GetResourceInfoByName()`
  - `ModUtils.GetUnlockByName()` → `EMU.Unlocks.GetUnlockByName()`
  - `ModUtils.LoadSpriteFromFile()` → `EMU.Images.LoadSpriteFromFile()`
  - `ModUtils.AddNewUnlock()` → `EMUAdditions.AddNewUnlock()`
- Updated `NewUnlockDetails` class references from `EquinoxsModUtils.NewUnlockDetails` to `EquinoxsModUtils.Additions.NewUnlockDetails`
- Updated `NewRecipeDetails` class references from `EquinoxsModUtils.NewRecipeDetails` to `EquinoxsModUtils.Additions.NewRecipeDetails`
- Updated `RecipeResourceInfo` class references from `EquinoxsModUtils.RecipeResourceInfo` to `EquinoxsModUtils.Additions.RecipeResourceInfo`

### Technical Details
- All resource registration now uses EMUAdditions 2.0.0+ API
- Custom machine and recipe definitions updated for new EMU structure
- Atlantum Mixture Brick and Shiverthorn Coolant resource lookups updated
