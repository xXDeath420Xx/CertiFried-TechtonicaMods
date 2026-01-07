# Recycler

Four-tier recycling system that breaks down crafted items back into their component resources.

## Features

### Recycler Tiers

| Tier | Efficiency | Description |
|------|------------|-------------|
| MKI | 25% | Basic recycler, returns 1/4 of materials |
| MKII | 50% | Improved recycler, returns 1/2 of materials |
| MKIII | 75% | Advanced recycler, returns 3/4 of materials |
| MKIV | 100% | Perfect recycler, full material recovery |

### Organic Waste System
- Recycling organic items produces organic waste
- Waste output integrates with BioProcessing mod
- Organic waste can be composted into fertilizer

## Integration

- **BioProcessing**: Organic waste feeds into composters for fertilizer production
- Enables circular resource economy

## Requirements

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+

## Installation

1. Install BepInEx for Techtonica
2. Install EquinoxsModUtils and EMUAdditions
3. Place Recycler.dll in your BepInEx/plugins folder

## Configuration

Recycling speed can be configured in the BepInEx configuration file.

## Changelog

### [1.0.0] - 2025-01-05
- Initial release
- Four recycler tiers (25%, 50%, 75%, 100% efficiency)
- Organic waste output system
- BioProcessing integration for waste composting
