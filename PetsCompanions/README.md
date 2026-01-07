# PetsCompanions

Adds companion pets that follow the player and provide passive buffs in Techtonica.

## Features

### 5 Companion Types
- **Companion Drone** - Flying drone that provides movement speed buff
- **Companion Crawler** - Spider-like ground companion that increases mining speed
- **Companion Floater** - Hovering companion with storage capabilities (inventory buff)
- **Guardian Bot** - Sturdy guardian that reduces damage taken
- **Scout Drone** - Fast scout with sensors that highlights nearby resources

### Pet Behavior
- Pets orbit around the player at configurable distance
- Each pet type has unique movement style (flying, hovering, ground)
- Smooth following with natural bobbing animations
- Pets face their movement direction

### Controls
- **Comma (,)** - Summon pet or cycle to next type
- **Period (.)** - Dismiss current pet

### Research & Crafting
All companions require unlocking **Companion Technology** (150 Purple cores) and are crafted in Assemblers.

## Configuration

| Setting | Default | Description |
|---------|---------|-------------|
| Enable Pets | true | Enable/disable pet system |
| Max Active Pets | 1 | Maximum concurrent pets (1-3) |
| Follow Distance | 3.0 | Distance pets maintain from player |
| Pet Speed | 8.0 | Pet movement speed |
| Pets Provide Buffs | true | Whether pets give passive buffs |
| Summon Key | Comma | Key to summon/cycle pets |
| Dismiss Key | Period | Key to dismiss pet |

## Dependencies

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+
- TechtonicaFramework 1.0.2+

## Changelog

### v1.0.0
- Initial release
- 5 companion pet types
- Following AI with orbit behavior
- Configurable keybinds and behavior
- Research unlock and crafting recipes
