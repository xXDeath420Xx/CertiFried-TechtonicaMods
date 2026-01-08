# Changelog

All notable changes to NarrativeExpansion will be documented in this file.

## [2.7.0] - 2026-01-07

### Added
- **GameVoiceManager** - Access to the game's existing voiced dialogue system
- **4 new NPCs with authentic game character voices**:
  - Sparks Terminal - Plays real Sparks voice lines from the game
  - Paladin Echo - Plays real Paladin voice lines from the game
  - Mirage Hologram - Plays real Mirage voice lines from the game
  - Groundbreaker Monument - Plays real The Groundbreaker voice lines from the game
- NPCVoiceType enum (Procedural, GameCharacter, Silent)
- NPCDefinition voice settings (VoiceType, GameSpeaker, UseGameDialogueOnly)

### Changed
- NPCs can now use authentic pre-recorded game character voices
- Game character NPCs trigger the game's dialogue popup with real voiced lines
- Now 9 total NPCs: 5 with procedural robot voices, 4 with game character voices

## [2.6.0] - 2026-01-07

### Changed
- **Cross-platform TTS**: Replaced Windows-only System.Speech with Piper TTS
- NPCs now use Piper for voice synthesis (works on Windows and Linux)
- **Improved procedural robot voice**: Now uses formant synthesis with speech-like patterns
  - 12 unique syllable types with vowel/consonant characteristics
  - Natural intonation (rising pitch for questions, falling for statements)
  - Syllable detection from text for realistic word timing
  - Punctuation-aware pausing (sentence, clause, word gaps)
- Automatic fallback to procedural voice if Piper is not installed

### Technical
- Added PiperTTS.cs with full cross-platform support
- Background thread processing for non-blocking TTS generation
- WAV file loader supporting 8/16/24/32-bit PCM formats
- Formant-based audio synthesis with ADSR envelopes
- Removed System.Speech dependency (Windows-only)

## [2.5.0] - 2026-01-07

### Added
- Windows Text-to-Speech support for NPC voices (deprecated in 2.6.0)
- Procedural robot voice synthesis fallback

## [2.4.0] - 2026-01-07

### Added
- Audio system for NPCs with 3D spatial sound
- Procedural robot voice clip generation
- Voice playback during NPC dialogue

## [2.3.0] - 2026-01-07

### Fixed
- NPCs no longer float in the air - continuous ground snapping implemented
- NPCs now raycast to terrain and smoothly follow ground height
- Fall-through-world protection with automatic respawn at home position

### Improved
- NPCs snap to ground on spawn for immediate correct positioning
- Smooth lerp for height changes prevents jittering on uneven terrain

## [2.2.0] - 2026-01-07

### Added
- Real 3D robot models for all NPCs (no more primitive shapes!)
- NPCAssetLoader system for loading robot models from AssetBundles
- Bundles folder with mech_companion, robot_sphere, robot_metallic assets

### Changed
- Wanderer now uses MechPolyart model (Sci-Fi warrior style)
- Engineer now uses RobotMetallic model (humanoid robot)
- Archivist now uses MechPBR model (high-detail warrior)
- Scout now uses RobotSphere model (floating sphere drone)
- Oracle now uses MechWarrior model (hero-style character)
- Each NPC has unique scale for visual variety

## [2.1.0] - 2026-01-07

### Changed
- Major narrative system improvements
- Updated dependency on TechtonicaFramework 1.2.0
- Better integration with dialogue systems

## [1.0.0] - 2025-01-05

### Added
- Initial release
- Extended dialogue system framework
- Quest chain support
- Story content expansion hooks
- Integration with TechtonicaFramework narrative system
