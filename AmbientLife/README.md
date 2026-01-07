# AmbientLife

Adds ambient wildlife and creatures for atmosphere in Techtonica. Includes butterflies, birds, fireflies, and more passive creatures.

## Features

### 5 Creature Types
- **Butterflies** - Colorful butterflies that flutter around plants
- **Birds** - Small birds that fly between perches
- **Fireflies** - Glowing insects that appear in darker areas (with light emission)
- **Moths** - Nocturnal flyers attracted to light sources
- **Dragonflies** - Fast-moving insects near water/humid areas

### Behavior
- Creatures spawn naturally around the player
- Each type has unique wandering patterns and speeds
- Wing animation simulation for flying creatures
- Fireflies emit soft glow effects
- Creatures despawn when too far from player

### Performance
- Configurable creature limits to maintain performance
- Distance-based spawning and despawning
- Lightweight behavior systems

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Enable Ambient Life | true | Enable/disable creature spawning |
| Max Creatures | 30 | Maximum creatures in world |
| Spawn Radius | 30.0 | Distance from player to spawn |
| Despawn Radius | 50.0 | Distance to despawn creatures |
| Spawn Butterflies | true | Enable butterfly spawning |
| Spawn Birds | true | Enable bird spawning |
| Spawn Fireflies | true | Enable firefly spawning |
| Spawn Moths | true | Enable moth spawning |
| Spawn Dragonflies | true | Enable dragonfly spawning |

## Dependencies

- BepInEx 5.4.2100+

## Changelog

### v1.0.0
- Initial release
- 5 ambient creature types
- Wandering AI behavior
- Wing animation simulation
- Firefly glow effects
- Configurable spawn settings
