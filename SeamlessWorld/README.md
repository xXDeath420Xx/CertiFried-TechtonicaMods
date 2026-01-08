# SeamlessWorld

Removes floor barriers to create a continuous, unified world in Techtonica. All floors are loaded simultaneously, allowing seamless vertical traversal without loading screens.

## Features

- **No Loading Screens** - Travel between floors without interruption
- **Continuous World** - All floors exist in unified 3D space
- **Vertical Factories** - Build conveyors and power lines spanning multiple levels
- **Free Fall** - Drop down elevator shafts (carefully!)
- **Flight Ready** - Perfect for jetpack/flight mods
- **Performance Options** - Configure how many floors stay loaded

## How It Works

Techtonica normally loads one floor (strata) at a time, unloading others. This mod:

1. Prevents floor unloading during elevator transitions
2. Repositions each floor at different Y-coordinates
3. Allows player movement between floors without teleportation
4. Keeps machines and power networks active across all loaded floors

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Enable Seamless World | true | Master toggle for the mod |
| Floor Separation | 350 | Vertical distance between floors |
| Max Loaded Strata | 4 | Memory management - max simultaneous floors |
| Debug Mode | false | Verbose logging |

## Performance Notes

Loading multiple floors simultaneously increases memory usage significantly:
- 2 floors: ~1.5x memory
- 3 floors: ~2x memory
- 4 floors: ~2.5x memory

Adjust "Max Loaded Strata" based on your system's RAM.

## Known Limitations

- **Work In Progress** - This is an experimental mod
- Elevator UI may behave unexpectedly
- Some machines may not render at extreme distances
- Multiplayer compatibility untested

## Compatibility

- Requires TechtonicaFramework 1.4.0+
- Works with MobilityPlus for enhanced vertical movement
- May conflict with other mods that modify FlowManager or VoxelManager

## Changelog

### 0.1.0
- Initial release
- Basic seamless floor loading
- Player position-based strata detection
- Configurable floor separation and memory limits
