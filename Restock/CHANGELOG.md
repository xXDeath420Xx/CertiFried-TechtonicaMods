# Changelog

All notable changes to Restock will be documented in this file.

## [3.0.1] - 2025-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2025-01-02

### Fixed
- **Critical:** Fixed `ArgumentException: An item with the same key has already been added` error on startup
  - Root cause: `EMU.Names.Resources.SafeResources` list contains duplicate resource names
  - Solution: Added duplicate key check before adding to `stacksDictionary`
  
### Changed
- **API Migration to EMU 6.1.3 nested class structure:**
  - `ResourceNames.SafeResources` → `EMU.Names.Resources.SafeResources`
  - Resource name constants now accessed via `EMU.Names.Resources.X` (e.g., `EMU.Names.Resources.Biobrick`, `EMU.Names.Resources.PowerFloor`)

### Technical Details
- Modified `Awake()` method to include duplicate check:
  ```csharp
  foreach (string name in EMU.Names.Resources.SafeResources) {
      if (stacksDictionary.ContainsKey(name)) continue;  // Skip duplicates
      // ... rest of config binding
  }
  ```
- Modified `IsItemBuilding()` method to use new EMU.Names.Resources path for resource name lookups
