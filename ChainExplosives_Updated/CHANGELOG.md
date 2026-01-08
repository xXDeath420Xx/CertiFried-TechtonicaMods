# Changelog

All notable changes to ChainExplosives will be documented in this file.

## [1.0.0] - 2026-01-03

### Fixed
- **Critical bug fix**: Original mod never added explosives to the detonation list
  - `addToExplosiveVisuals` postfix only logged but never called `.Add()`
  - `detonateAll()` iterated over `explosiveVisuals` which was always empty
- Explosives are now properly tracked and detonated in chain reactions

### Changed
- Simplified tracking to use `ExplosiveInstance` directly instead of `ExplosiveVisuals`
- Added `HashSet<uint>` for O(1) duplicate detection
- Improved detonation loop with proper exception handling
- Added configurable debug mode for troubleshooting

### Technical
- Modernized to SDK-style .csproj
- Fixed struct null comparison errors (ExplosiveInstance is a value type)
- Removed unused `chainDelay` config (instant detonation works better)
- Cleaned up Harmony patch registration

### Credits
- Original mod concept by Equinox (https://github.com/CubeSuite/TTMod-ChainExplosives)
- Bug fixes and update by CertiFried
