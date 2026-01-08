# MassProdCores_Patched Changelog

## About This Fork
This is a patched version of **MassProdCores** originally created by **Jarl**.
- Original mod: https://thunderstore.io/c/techtonica/p/Jarl/MassProdCores/
- Original license: Not specified

## v1.1.0-patched (CertiFried)
**Reason for patch:** Original mod uses deprecated EMU API (NewRecipeDetails type) causing game crash on load.

### Changes from original:
- Updated to use current EMUAdditions API
- Removed dependency on deprecated `EquinoxsModUtils.NewRecipeDetails` type
- Added configurable batch size and efficiency settings

### Functionality preserved:
- Mass production recipes for cores and coolers
- Framework for batch crafting recipes

### Note:
The original mod's exact recipes could not be fully recovered as the original
source is not available. This patched version provides a framework for mass
production that can be extended. Consider using PlanterCoreClusters for
similar functionality.
