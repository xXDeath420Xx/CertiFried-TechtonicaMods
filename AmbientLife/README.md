# AmbientLife

[![Version](https://img.shields.io/badge/version-2.0.1-blue.svg)](https://github.com/certifired/AmbientLife)
[![License](https://img.shields.io/badge/license-GPL--3.0-green.svg)](LICENSE)
[![BepInEx](https://img.shields.io/badge/BepInEx-5.4.2100+-orange.svg)](https://github.com/BepInEx/BepInEx)

**AmbientLife** is a Techtonica mod that brings your factory world to life with intelligent ambient creatures. From spiders crawling on walls and ceilings to flying beetles soaring overhead, dogs following you around, and deer grazing in the distance - this mod creates an immersive ecosystem using real 3D creature models with smart AI that properly navigates terrain without clipping.

---

## Table of Contents

- [Features](#features)
  - [Creature Categories](#creature-categories)
  - [Smart AI System](#smart-ai-system)
  - [In-Game Configuration UI](#in-game-configuration-ui)
  - [3D Asset Integration](#3d-asset-integration)
- [Installation](#installation)
- [Configuration](#configuration)
  - [General Settings](#general-settings)
  - [Flying Insects](#flying-insects)
  - [Spiders](#spiders)
  - [Ground Animals](#ground-animals)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Changelog](#changelog)
- [Credits](#credits)
- [License](#license)

---

## Features

### Creature Categories

AmbientLife v2.0 features **13 unique creature types** across 4 categories:

#### Flying Insects
| Creature | Speed | Behavior | Notes |
|----------|-------|----------|-------|
| **Butterflies** | 1.5 m/s | Hovering | Colorful, flutters at low altitude |
| **Fireflies** | 0.8 m/s | Hovering | Pulsing glow effect, atmospheric lighting |
| **Flying Beetles** | 3 m/s | Flying | Soars at medium altitude |
| **Nightmare Beetles** | 4 m/s | Flying | Larger, intimidating, high altitude |

#### Spiders (Wall Crawlers)
| Creature | Speed | Behavior | Notes |
|----------|-------|----------|-------|
| **Brown Spiders** | 2 m/s | Wall Crawling | Climbs walls, floors, and ceilings |
| **Green Spiders** | 1.8 m/s | Wall Crawling | Camouflaged variant |
| **Black Spiders** | 2.5 m/s | Wall Crawling | Larger, faster |

#### Ground Animals
| Creature | Speed | Behavior | Notes |
|----------|-------|----------|-------|
| **Dogs** | 3 m/s | Ground | Friendly, follows terrain |
| **Cats** | 2.5 m/s | Ground | Curious wanderers |
| **Chickens** | 1.5 m/s | Ground | Flees from player |
| **Deer** | 4 m/s | Ground | Skittish, runs when approached |
| **Wolves** | 5 m/s | Ground | Pack wanderers |
| **Penguins** | 1.2 m/s | Ground | Waddles around |

### Smart AI System

Every creature features intelligent movement with advanced pathfinding:

- **Terrain Following**: Ground creatures properly follow slopes, stairs, and uneven terrain
- **Wall Crawling**: Spiders can climb walls and transition to ceilings seamlessly
- **Obstacle Avoidance**: 360-degree collision detection prevents clipping through buildings
- **Height Management**: Flying creatures maintain safe altitudes and avoid structures
- **Dynamic Fleeing**: Shy creatures (deer, chickens, spiders) flee when player approaches
- **Weighted Spawning**: Configurable density per creature category

### In-Game Configuration UI

Press **F8** to open the configuration panel:

- **Master Controls**: Enable/disable all creatures, set max count
- **Per-Creature Toggles**: Turn individual creature types on/off
- **Density Sliders**: Adjust spawn weights for flying, crawling, and ground animals
- **Status Display**: See active creature count and loaded prefab count
- **Quick Actions**: Reload assets, clear all creatures

### 3D Asset Integration

- Uses real 3D models from AssetBundles when available
- Automatic fallback to procedural geometry if bundles missing
- Supports animated models with proper Animator components
- Disables default AI controllers to use custom smart movement

---

## Installation

### Mod Manager Installation (Recommended)

1. Open r2modman or Thunderstore Mod Manager
2. Search for "AmbientLife"
3. Click Install
4. Launch the game through the mod manager

### Manual Installation

1. **Install BepInEx 5.4.2100+** and **TechtonicaFramework**
2. Download the latest release
3. Extract to `BepInEx/plugins/CertiFried-AmbientLife/`
4. Ensure the `Bundles` folder is present for 3D creature assets
5. Launch the game

---

## Configuration

Configuration file: `BepInEx/config/com.certifired.AmbientLife.cfg`

Or use the **in-game UI** by pressing **F8**.

### General Settings

| Setting | Default | Range | Description |
|---------|---------|-------|-------------|
| `Enable Ambient Life` | `true` | - | Master toggle for all spawning |
| `Max Creatures` | `40` | 5-150 | Maximum creatures in world |
| `Spawn Radius` | `50` | 20-150m | Distance from player to spawn |
| `Despawn Radius` | `80` | 30-200m | Distance to despawn creatures |
| `Debug Mode` | `false` | - | Enable spawn logging |

### Flying Insects

| Setting | Default | Description |
|---------|---------|-------------|
| `Enable Butterflies` | `true` | Colorful low-altitude flyers |
| `Enable Fireflies` | `true` | Glowing ambient insects |
| `Enable Flying Beetles` | `true` | Standard flying beetles |
| `Enable Nightmare Beetles` | `false` | Large intimidating beetles |

### Spiders

| Setting | Default | Description |
|---------|---------|-------------|
| `Enable Brown Spiders` | `true` | Common wall crawlers |
| `Enable Green Spiders` | `true` | Camouflaged spiders |
| `Enable Black Spiders` | `false` | Larger black spiders |

### Ground Animals

| Setting | Default | Description |
|---------|---------|-------------|
| `Enable Dogs` | `true` | Friendly companions |
| `Enable Cats` | `true` | Curious wanderers |
| `Enable Chickens` | `true` | Flighty birds |
| `Enable Deer` | `true` | Skittish grazers |
| `Enable Wolves` | `false` | Pack animals |
| `Enable Penguins` | `false` | Cold climate friends |

---

## Requirements

| Dependency | Version | Required |
|------------|---------|----------|
| Techtonica | Latest | Yes |
| BepInEx | 5.4.2100+ | Yes |
| TechtonicaFramework | 1.2.0+ | Yes |

---

## Compatibility

- **Save Safe**: Add or remove mid-save without issues
- **Multiplayer**: Client-side only, creatures won't sync
- **Other Mods**: No conflicts with standard mods

---

## Changelog

### v2.0.0 - Major Overhaul
- **Complete rewrite** with smart AI system
- Added **13 creature types** across 4 categories
- Implemented **wall crawling** for spiders (climbs walls and ceilings)
- Added **terrain following** for ground animals
- Implemented **obstacle avoidance** with 360-degree collision detection
- Added **in-game configuration UI** (press F8)
- Integrated **AssetBundle loading** for real 3D creature models
- Added **per-creature toggles** and density controls
- Creatures now **flee from player** based on type
- Added **glow effects** for fireflies with pulsing animation
- Performance optimizations with configurable limits

### v1.0.2
- Added CHANGELOG.md
- Icon and metadata updates

### v1.0.0 - Initial Release
- Basic ambient creature system
- Procedural creature visuals
- Simple wandering behavior

---

## Credits

### Development
- **Certifired** - Lead Developer
- **Claude Code (Anthropic)** - AI-assisted development

### 3D Assets
- Spider models from Tower Defense asset packs
- Animal models from ithappy Animals pack
- Beetle models from Flying Beetle and Nightmare Beetle packs

### Tools & Frameworks
- **BepInEx** - Modding framework
- **Harmony** - Runtime patching
- **Unity** - Game engine

---

## License

GNU General Public License v3.0 (GPL-3.0)

```
AmbientLife - Intelligent Ambient Wildlife for Techtonica
Copyright (C) 2024-2025 Certifired

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
```

---

*Made with care for the Techtonica community*
