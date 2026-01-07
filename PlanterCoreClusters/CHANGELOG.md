# Changelog

All notable changes to PlanterCoreClusters will be documented in this file.

## [3.0.4] - 2026-01-03

### Fixed
- Included CHANGELOG.md in Thunderstore package for proper changelog display

## [3.0.3] - 2026-01-03

### Added
- **Configurable boost percentage** - New config option "Boost Per Cluster" (default 5%, range 1-50%)
  - Allows users to adjust how much speed boost each Core Cluster provides
  - Higher values = faster planters with same number of clusters

### Technical Details
- Boost calculation now uses configurable value instead of hardcoded 5f

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
