# Narrative Expansion

**Extended Dialogue, Lore, and NPC System for Techtonica**

Narrative Expansion is a comprehensive story-enhancement mod that adds extended dialogue for existing characters, discoverable lore entries, mysterious story threads, and interactive NPCs to the underground world of Techtonica. Dive deeper into the secrets of the facility and uncover the truth about what happened before your arrival.

---

## Table of Contents

- [Features](#features)
  - [Extended Dialogue System](#extended-dialogue-system)
  - [Hidden Lore Content](#hidden-lore-content)
  - [Mystery Story Threads](#mystery-story-threads)
  - [Interactive NPC System](#interactive-npc-system)
- [Installation](#installation)
  - [Using r2modman (Recommended)](#using-r2modman-recommended)
  - [Manual Installation](#manual-installation)
  - [Voice Synthesis (Optional)](#voice-synthesis-optional)
- [Configuration](#configuration)
  - [Content Settings](#content-settings)
  - [NPC Settings](#npc-settings)
  - [General Settings](#general-settings)
- [Requirements](#requirements)
- [Compatibility](#compatibility)
- [Known Issues](#known-issues)
- [Changelog](#changelog)
- [Credits and Attribution](#credits-and-attribution)
- [License](#license)
- [Links](#links)

---

## Features

### Extended Dialogue System

Narrative Expansion adds new dialogue for the existing characters you know and love, giving them more personality and depth:

**Sparks Extended Dialogue:**
- Idle commentary that triggers randomly during gameplay
- Reactions to player actions (damage events, power surges)
- Depth-based dialogue when exploring deep caves
- Multi-part dialogue sequences revealing hidden story elements

**Paladin Extended Dialogue:**
- Memory fragments about life before the incident
- Backstory hints about the original Groundbreaker team
- Warnings and guidance for deeper exploration
- Banter sequences with Sparks

**Custom Speakers:**
- Ancient AI - A mysterious intelligence predating the facility
- Corrupted Sparks - Glitched dialogue hinting at darker secrets
- The Groundbreaker - References to the original team members

### Hidden Lore Content

Discover the secrets of the facility through recovered data logs and archives:

- **Ancient History Logs**: Uncover what the dig team found beneath the facility
- **Facility Archives**: Learn about Project Groundbreaker and what went wrong
- **Research Notes**: Discover the true nature of the Memory Trees
- Lore entries trigger based on exploration depth and discovery
- Progressive revelation system with chained dialogue sequences

### Mystery Story Threads

Mysterious signals and cryptic messages hint at deeper secrets:

- Unknown transmissions from unidentified sources
- Corrupted data streams revealing hidden truths
- Ancient AI prophecies and revelations
- Multi-part mystery sequences leading to deeper questions
- Pattern-based hints and environmental storytelling

### Interactive NPC System

Meet five unique NPCs scattered throughout the world:

| NPC | Role | Behavior |
|-----|------|----------|
| **The Wanderer** | Mysterious traveler with cryptic wisdom | Wandering (20m radius) |
| **Old Engineer** | Technical tips and facility knowledge | Stationary |
| **The Archivist** | Lore keeper and historical records | Stationary |
| **Swift Scout** | Area hints and exploration tips | Wandering (40m radius) |
| **The Oracle** | Prophecies and cryptic messages | Stationary |

**NPC Features:**
- Visual representation with colored bodies, glowing eyes, and ambient lighting
- Name tags that display interaction prompts when in range
- Cycling dialogue with multiple unique lines per NPC
- Configurable interaction range and keybinds
- NPCs face the player when interacting
- Automatic respawning if they fall through the world

---

## Installation

### Using r2modman (Recommended)

1. Download and install [r2modman](https://thunderstore.io/package/ebkr/r2modman/)
2. Select **Techtonica** as your game
3. Search for **NarrativeExpansion** in the online mod browser
4. Click **Download** to install the mod and all dependencies automatically
5. Launch the game through r2modman

### Manual Installation

1. Ensure [BepInEx 5.4.21+](https://github.com/BepInEx/BepInEx/releases) is installed
2. Download and install all required dependencies (see [Requirements](#requirements))
3. Download the latest release of NarrativeExpansion
4. Extract `NarrativeExpansion.dll` to your `BepInEx/plugins` folder:
   ```
   Techtonica/
   └── BepInEx/
       └── plugins/
           └── NarrativeExpansion.dll
   ```
5. Launch the game

### NPC Voice System

NPCs have two voice options:

**1. Procedural Robot Voice (Default)**
- Works out of the box with no additional setup
- Uses formant synthesis for speech-like robotic sounds
- Analyzes text for natural speech patterns:
  - Syllable-based word timing
  - Rising intonation for questions
  - Punctuation-aware pausing
- Fits the sci-fi theme perfectly

**2. Piper TTS (Optional - Real Speech)**
For more natural-sounding voices, you can install Piper TTS:

1. Download [Piper TTS](https://github.com/rhasspy/piper/releases) for your platform
2. Create a `piper` folder in the mod directory:
   ```
   BepInEx/plugins/CertiFried-NarrativeExpansion/piper/
   ```
3. Extract the Piper executable:
   - **Windows**: `piper.exe`
   - **Linux**: `piper` (run `chmod +x piper` to make executable)
4. Download a voice model (`.onnx` file) from [Piper Voices](https://github.com/rhasspy/piper/blob/master/VOICES.md)
   - Recommended: `en_US-lessac-medium.onnx` (~60MB, natural English speech)
   - Smaller option: `en_US-lessac-low.onnx` (~20MB, faster but lower quality)
5. Place the `.onnx` file and its `.json` config in the `piper` folder:
   ```
   piper/
   ├── piper.exe (or piper on Linux)
   ├── en_US-lessac-medium.onnx
   └── en_US-lessac-medium.onnx.json
   ```

The mod automatically detects Piper on startup. If not found, the procedural robot voice is used.

---

## Configuration

Configuration options are available in `BepInEx/config/com.certifired.NarrativeExpansion.cfg` after running the game once with the mod installed.

### Content Settings

| Option | Default | Description |
|--------|---------|-------------|
| `Enable Extended Dialogue` | `true` | Enable additional dialogue for existing characters (Sparks, Paladin) |
| `Enable Hidden Lore` | `true` | Enable discoverable lore entries and databank content |
| `Enable Mystery Content` | `true` | Enable mysterious messages and hidden story threads |

### NPC Settings

| Option | Default | Description |
|--------|---------|-------------|
| `Enable NPCs` | `true` | Enable interactive NPCs in the world |
| `Max NPCs` | `5` | Maximum number of NPCs to spawn |
| `Interaction Range` | `5.0` | Distance at which NPCs can be interacted with (in units) |
| `Interact Key` | `E` | Key to interact with NPCs |

### General Settings

| Option | Default | Description |
|--------|---------|-------------|
| `Debug Mode` | `false` | Enable debug logging for troubleshooting |

---

## Requirements

This mod requires the following dependencies to be installed:

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| [BepInEx](https://github.com/BepInEx/BepInEx) | 5.4.21+ | Mod loader framework |
| [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/) | 6.1.3+ | Core modding utilities |
| [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/) | 2.0.0+ | Extended modding utilities |
| [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/) | 1.0.0+ | Dialogue and narrative system API |

All dependencies will be installed automatically if using r2modman.

---

## Compatibility

- **Game Version**: Compatible with current Techtonica releases
- **Multiplayer**: Not tested in multiplayer environments
- **Other Mods**: Should be compatible with most mods that don't modify the dialogue system
- **Save Games**: Safe to add to existing saves; dialogue progress is tracked per-session

---

## Known Issues

- NPCs may spawn in inaccessible locations on initial load; they will respawn if they fall through the world
- Dialogue timing may occasionally overlap with game events
- Some depth-triggered dialogue may not fire if the player moves too quickly

---

## Changelog

### [2.7.0] - Current Version
- **Authentic Game Character Voices**: 4 new NPCs that play real voiced dialogue from the game
- Sparks Terminal, Paladin Echo, Mirage Hologram, Groundbreaker Monument NPCs
- GameVoiceManager system to access existing game dialogue
- Now 9 total NPCs: 5 with procedural robot voices, 4 with game character voices

### [2.6.0]
- **Cross-platform TTS**: Replaced Windows-only System.Speech with Piper TTS
- Works on both Windows and Linux
- Automatic fallback to procedural robot beeps if Piper not installed
- Async TTS generation with audio caching for performance

### [2.3.0]
- Fixed NPCs floating in the air with continuous ground snapping
- NPCs smoothly follow terrain height

### [2.2.0]
- Real 3D robot models for all NPCs (no more primitive shapes!)
- NPCAssetLoader system for loading robot models from AssetBundles

### [2.1.0]
- Full NPC system with five unique characters
- NPC visual representation with colored bodies and glowing eyes
- NPC behavior types: Stationary, Wandering, Following

### [1.0.0] - 2025-01-05
- Initial release
- Extended dialogue system for Sparks and Paladin
- Hidden lore discovery system
- Mystery content and story threads

See [CHANGELOG.md](CHANGELOG.md) for full version history.

---

## Credits and Attribution

### Development
- **CertiFried** - Primary developer and mod author

### AI Development Assistance
- **Claude Code** (Anthropic) - AI-assisted development, code organization, and documentation

### Dependencies and Thanks
- **Equinox** - For EquinoxsModUtils and EMUAdditions
- **Fire Dev Team** - For creating Techtonica
- **BepInEx Team** - For the modding framework

### Special Thanks
- The Techtonica modding community for support and feedback
- All players who have provided bug reports and suggestions

---

## License

This mod is licensed under the **GNU General Public License v3.0 (GPL-3.0)**.

```
Copyright (C) 2025 CertiFried

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.
```

See the [LICENSE](LICENSE) file for the full license text.

---

## Links

### Mod Resources
- [Thunderstore Page](https://thunderstore.io/c/techtonica/p/CertiFried/NarrativeExpansion/)
- [Source Code (GitHub)](https://github.com/CertiFried/NarrativeExpansion)

### Dependencies
- [BepInEx](https://github.com/BepInEx/BepInEx)
- [EquinoxsModUtils](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- [EMUAdditions](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
- [TechtonicaFramework](https://thunderstore.io/c/techtonica/p/CertiFried/TechtonicaFramework/)

### Community
- [Techtonica Discord](https://discord.gg/techtonica)
- [Techtonica Modding Community](https://thunderstore.io/c/techtonica/)

### Tools
- [r2modman Mod Manager](https://thunderstore.io/package/ebkr/r2modman/)

---

*Narrative Expansion - Uncover the secrets hidden beneath the surface.*
