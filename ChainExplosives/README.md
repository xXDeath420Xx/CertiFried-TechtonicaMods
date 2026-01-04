# ChainExplosives Updated

Interact with one explosive to detonate ALL placed explosives at once! Perfect for setting up large mining operations.

## Features

- Place multiple explosives around an area
- Interact with ANY ONE of them to trigger a chain detonation
- All tracked explosives detonate simultaneously
- Debug mode available for troubleshooting

## How to Use

1. Place as many explosives as you want in the area you want to clear
2. When ready, interact with just ONE explosive
3. Watch as ALL your placed explosives detonate at once!

## Configuration

Found in `BepInEx/config/com.certifired.ChainExplosives.cfg`:

- **Debug Mode** (default: false) - Enable verbose logging to track explosive placement and detonation

## Original Author

This mod was originally created by **Equinox** ([CubeSuite](https://github.com/CubeSuite)).

Original repository: https://github.com/CubeSuite/TTMod-ChainExplosives

## What Was Fixed

The original mod had a bug where explosives were tracked but never actually added to the detonation list. This has been fixed:

- Fixed: `explosiveVisuals` list was never populated (logging only, no actual add)
- Fixed: Iteration over empty list caused no chain reaction
- Improved: Now uses `ExplosiveInstance` tracking directly with proper HashSet deduplication

## License

This mod is licensed under GPL-3.0, the same license as the original mod.

## Updated By

Updated and fixed by **CertiFried**.
