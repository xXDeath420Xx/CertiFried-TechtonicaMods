# TurretDefense

Adds buildable defensive turrets and alien invasion waves to Techtonica. Protect your factory from alien raiders with automated defense systems!

## Features

### Defensive Turrets (Buildable Machines)
All turrets are proper buildable machines that connect to the power grid:

- **Gatling Turret** - High fire rate, excellent against swarms (50kW)
- **Rocket Turret** - Explosive area damage, slow fire rate (80kW)
- **Laser Turret** - Continuous beam, perfect accuracy, no ammo needed (120kW)
- **Railgun Turret** - Extreme range and damage (200kW)
- **Lightning Turret** - Chain lightning hits multiple targets (150kW)

### Custom Defense Tab
This mod adds a **Defense** tab to the tech tree UI, separate from the existing 7 categories. All turret unlocks appear in this dedicated tab for easy access.

### Alien Invasion System
- Periodic alien ship waves attack your factory
- Multiple alien types: Fighters, Destroyers, Bio-Torpedoes
- Ground enemies: Chompers, Spitters, Mimics, Arachnids
- Alien Hives can spawn as enemy bases
- Configurable wave intervals and difficulty scaling

### Crafting & Resources
- **Turret Ammunition** - Crafted from Iron and Copper Ingots
- **Turret Upgrade Kit** - Enhance turret performance using alien materials
- **Alien Power Core** - Dropped by destroyed aliens
- **Alien Alloy** - Salvaged from alien hull plating

## Tech Tree Unlocks

| Unlock | Tier | Cores Required |
|--------|------|----------------|
| Automated Defense | Tier 6 | 200 Gold |
| Advanced Defense Systems | Tier 7 | 300 Green |

## Configuration

All settings are configurable via the BepInEx config file:

- **Enable Turrets** - Toggle turret system
- **Enable Alien Waves** - Toggle wave attacks
- **Turret Damage/Range** - Adjust turret stats
- **Wave Interval** - Time between waves (30-600 seconds)
- **Difficulty Scaling** - Wave intensity multiplier
- **Alien Damage Multipliers** - Adjust alien damage to player/buildings

### Debug Keys (configurable)
- Numpad 7: Spawn test alien
- Numpad 8: Force spawn wave
- Numpad 9: Spawn test turret

## Dependencies

- BepInEx 5.4.2100+
- EquinoxsModUtils 6.1.3+
- EMUAdditions 2.0.0+
- TechtonicaFramework 1.0.2+

## Compatibility

- Works with SurvivalElements for player health/death system
- Works with HazardousWorld for environmental hazards
- Compatible with all other CertiFried mods

## Installation

1. Install dependencies via r2modman or Thunderstore
2. Install TurretDefense
3. Launch game and research "Automated Defense" in the Defense tab

## Changelog

See [CHANGELOG.md](CHANGELOG.md) for version history.

## Credits

- **CertiFried** - Development
- **Equinox** - EMU framework
