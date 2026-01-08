# Changelog

All notable changes to Restock will be documented in this file.

## [3.0.4] - 2026-01-03

### Fixed
- Included CHANGELOG.md in Thunderstore package for proper changelog display

## [3.0.3] - 2026-01-03

### Added
- **Configurable restock cooldown** - New config option "Restock Cooldown" (default 0.5s, range 0.1-5s)
  - Reduces CPU usage by throttling how often the mod scans for chests
  - Default 0.5 seconds = 2 scans per second instead of 50

### Performance
- Significantly reduced CPU overhead from constant FixedUpdate scanning
- Configurable for users who want faster/slower restock response

## [3.0.2] - 2026-01-03

### Changed
- Published to Thunderstore with proper packaging and metadata
- Verified compatibility with latest EMU 6.1.3

## [3.0.1] - 2026-01-03

### Changed
- Updated README with proper attribution and links to original author Equinox

## [3.0.0] - 2026-01-02

### Fixed
- **Critical:** Fixed duplicate key error from SafeResources list

### Changed
- **API Migration to EMU 6.1.3 nested class structure**
