# DroneLogistics

Automated drone transport system for Techtonica. Features multiple drone types, packing stations, and intelligent route management.

## Features

### Drone Types
- **Scout Drone**: Fast reconnaissance, 10 unit capacity
- **Cargo Drone**: Standard transport, 50 unit capacity
- **Heavy Lifter**: Large cargo transport, 100 unit capacity
- **Combat Drone**: Armed transport with turret integration, 25 unit capacity

### Facilities
- **Drone Pad**: Home base for drones, handles spawning and queuing
- **Packing Station**: Converts items into cargo crates for transport
- **Unpacking Station**: Extracts items from cargo crates

### Operating Modes
- **Standalone Mode**: Drones transport items directly from inventories
- **Enhanced Mode**: Drones transport packed cargo crates (more efficient)

### Route Management
- Automatic route optimization
- Priority-based request processing
- Multi-stop route support

## Integration

- **BioProcessing**: Combat drones can use biofuel
- **TurretDefense**: Combat drones integrate with turret targeting

## Requirements

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+

## Installation

1. Install BepInEx for Techtonica
2. Install EquinoxsModUtils
3. Place DroneLogistics.dll in your BepInEx/plugins folder

## Configuration

Drone speeds, capacities, and behavior can be configured in the BepInEx configuration file.

## Changelog

### [1.0.0] - 2025-01-05
- Initial release
- 4 drone types with unique capabilities
- Drone pads with spawning and queue management
- Packing/unpacking stations for cargo crate mode
- Route optimization system
- Standalone and enhanced operating modes
