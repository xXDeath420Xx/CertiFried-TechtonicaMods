# Chain Explosives

When one explosive detonates, all nearby explosives chain react! Perfect for massive demolition projects.

## Features

- Chain reaction explosions
- All explosives in range detonate together
- Satisfying demolition gameplay

## Requirements

- BepInEx 5.4.21+
- EquinoxsModUtils 6.1.3+

## Installation

Install via r2modman or manually place the DLL in your BepInEx/plugins folder.

## Credits

- Original mod concept by Equinox (https://github.com/CubeSuite/TTMod-ChainExplosives)
- Bug fixes and update by CertiFried

## Changelog

### [1.0.2] - 2025-01-05
- Updated icon

### [1.0.0] - 2025-01-03
- Critical bug fix: Original mod never added explosives to detonation list
- Explosives now properly tracked and detonated in chain reactions
- Simplified tracking with HashSet for O(1) duplicate detection
- Added configurable debug mode
