# Changelog

All notable changes to MorePlanters will be documented in this file.

## [3.0.9] - 2026-01-07

### Fixed
- Minor stability improvements
- Compatibility updates

## [3.0.4] - 2026-01-03

### Fixed
- Included CHANGELOG.md in Thunderstore package for proper changelog display

## [3.0.3] - 2026-01-03

### Added
- **Configurable speed multiplier** - New config option "Speed Multiplier" (default 2.0x, range 1-10x)
  - Allows users to tune planter speed boost to their preference
  - Affects both Planter MKII and MKIII

### Technical Details
- Speed calculation now uses `120f / speedMultiplier.Value` instead of hardcoded 60f

## [3.0.2] - 2026-01-03

### Changed
- Published to Thunderstore with proper packaging and metadata
- Verified compatibility with latest EMU 6.1.3

## [3.0.1] - 2026-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2026-01-02

### Changed
- **API Migration to EMU 6.1.3 nested class structure**
- Updated all EMU API calls to new nested class format
- All unlock and recipe registrations use EMUAdditions 2.0.0+ API
