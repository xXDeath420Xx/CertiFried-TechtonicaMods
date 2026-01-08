# OmniseekerPlus

**An enhanced scanning system for Techtonica that extends your Omniseeker's capabilities with increased range, new scannable object types, and a powerful in-game interface.**

---

## Description

OmniseekerPlus supercharges your exploration experience in Techtonica by dramatically expanding what you can detect and how far you can see. Whether you're hunting for rare Atlantum deposits, tracking down hidden loot containers, or locating Memory Trees for research cores, this mod puts powerful scanning tools at your fingertips.

The mod features six distinct scan modes, configurable hotkeys for quick access, and a full in-game GUI that displays results with distance-based color coding for easy navigation.

---

## Features

### Extended Scanner Range
- **Range Multiplier**: Increase your scan range up to 10x the default (configurable from 1x to 10x)
- **Maximum Range**: Scan up to 2000 meters in any direction (configurable from 100m to 2000m)
- **Toggle Control**: Enable or disable extended range on the fly

### New Scannable Object Types
- **Hard Drives / Data Storage**: Locate data storage devices scattered throughout the world
- **Hidden Chests**: Find buried and hidden loot containers you might otherwise miss
- **Loot Containers**: Detect all types of loot crates and containers
- **Memory Trees**: Locate Memory Trees that produce research cores
- **All Ore Types**: Scan for any ore vein including common and rare varieties
- **Rare Ores**: Specifically target rare ore deposits like Atlantum

### Six Scan Modes
1. **All**: Comprehensive scan for all object types
2. **Ores Only**: Focus exclusively on ore veins
3. **Hard Drives**: Target data storage devices only
4. **Loot**: Search for chests and loot containers
5. **Memory Trees**: Locate Memory Tree machines
6. **Custom**: Use your individual config settings to create a personalized scan profile

### Quick Access Controls
- **Quick Scan Hotkey** (Default: `P`): Perform an instant area scan with one keypress
- **Mode Cycling** (Default: `O`): Quickly cycle through all six scan modes
- **GUI Toggle** (Default: `Shift+O`): Open the full in-game interface

### In-Game GUI
- Mode selection buttons for quick switching
- One-click quick scan button
- Scrollable results list showing all detected objects
- Real-time distance display for each result
- Object type categorization
- Toggle controls for extended range, HUD display, and color coding
- Current range and multiplier display

### Visual Feedback
- **Distance Color Coding**: Results are color-coded by distance:
  - Green: Close (under 50m)
  - Yellow: Medium (50-150m)
  - Orange: Far (150-300m)
  - Red: Very Far (over 300m)
- **Scan Pulse Effect**: Optional visual pulse when scanning (configurable intensity)
- **HUD Distance Display**: Show distance to scanned objects on your HUD

---

## How to Use

### Basic Scanning
1. Press `P` (default) to perform a quick scan of your surroundings
2. Results will be logged to the console and stored for GUI viewing
3. Press `Shift+O` to open the GUI and see detailed results

### Changing Scan Modes
1. Press `O` (default) to cycle through available modes
2. Alternatively, open the GUI with `Shift+O` and click mode buttons
3. The current mode affects what objects are detected during scans

### Using the GUI
1. Press `Shift+O` to open/close the interface
2. Click any mode button to switch scan modes
3. Click "Perform Quick Scan" to scan with current settings
4. Scroll through results sorted by distance
5. Use toggles at the bottom to adjust display options
6. Click "Close" or press `Shift+O` again to close

### Configuration
All settings can be adjusted in the config file located at:
`BepInEx/config/com.certifired.OmniseekerPlus.cfg`

**Scanner Range Settings:**
- `Range Multiplier`: 1.0 - 10.0 (default: 3.0)
- `Max Scan Range`: 100 - 2000 meters (default: 500)
- `Extended Range Enabled`: true/false (default: true)

**Scannable Types (for Custom mode):**
- `Scan Hard Drives`: true/false (default: true)
- `Scan Hidden Chests`: true/false (default: true)
- `Scan Rare Ores`: true/false (default: true)
- `Scan All Ore Types`: true/false (default: true)
- `Scan Loot Containers`: true/false (default: true)
- `Scan Memory Trees`: true/false (default: true)

**Visual Settings:**
- `Show Distance on HUD`: true/false (default: true)
- `Color Code by Distance`: true/false (default: true)
- `Show Scan Pulse Effect`: true/false (default: true)
- `Pulse Intensity`: 0.5 - 3.0 (default: 1.5)

**Hotkeys:**
- `Cycle Mode Key`: Any KeyCode (default: O)
- `Quick Scan Key`: Any KeyCode (default: P)

---

## Installation

### Using r2modman (Recommended)
1. Open r2modman and select Techtonica
2. Search for "OmniseekerPlus" in the online mods
3. Click "Download" and the mod will be installed automatically
4. Launch the game through r2modman

### Manual Installation
1. Ensure BepInEx 5.4.21 or later is installed
2. Ensure EquinoxsModUtils 6.1.3 or later is installed
3. Ensure EMUAdditions 2.0.0 or later is installed
4. Download the latest release of OmniseekerPlus
5. Extract `OmniseekerPlus.dll` to your `BepInEx/plugins` folder
6. Launch the game

---

## Requirements

| Dependency | Minimum Version | Purpose |
|------------|-----------------|---------|
| BepInEx | 5.4.21+ | Mod loading framework |
| EquinoxsModUtils | 6.1.3+ | Core modding utilities |
| EMUAdditions | 2.0.0+ | Extended modding utilities |

---

## Compatibility

- **Game Version**: Compatible with current Techtonica versions
- **Multiplayer**: Works in both single-player and multiplayer sessions
- **Other Mods**: Should be compatible with most other mods

---

## Changelog

### [1.0.7] - Current
- Latest stable release

### [1.0.5] - 2025-01-05
- Updated mod icon

### [1.0.0] - 2025-01-04
- Initial release
- Extended scanner range functionality
- New scannable object types
- Six scan modes (All, Ores, Hard Drives, Loot, Memory Trees, Custom)
- Quick scan and mode cycling hotkeys
- Full in-game GUI with results display
- Distance-based color coding

---

## Known Issues

- Scan results are based on active GameObjects and may not detect objects that haven't been loaded yet
- Very large scan ranges may cause brief performance impacts on lower-end systems

---

## Credits

- **Author**: CertiFried
- **Development Assistance**: Claude Code (Anthropic AI)
- **Dependencies**: Built using EquinoxsModUtils and EMUAdditions by Equinox

---

## License

This mod is licensed under the **GNU General Public License v3.0** (GPL-3.0).

You are free to:
- Use this mod for any purpose
- Modify the source code
- Distribute copies
- Distribute modified versions

Under the conditions that:
- You include the original license
- You state any changes made
- You distribute under the same license
- You make source code available

Full license text: https://www.gnu.org/licenses/gpl-3.0.en.html

---

## Links

- **Thunderstore**: [OmniseekerPlus on Thunderstore](https://thunderstore.io/c/techtonica/p/CertiFried/OmniseekerPlus/)
- **GitHub**: [Source Repository](https://github.com/CertiFried/TechtonicaMods)
- **EquinoxsModUtils**: [EMU on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EquinoxsModUtils/)
- **EMUAdditions**: [EMUAdditions on Thunderstore](https://thunderstore.io/c/techtonica/p/Equinox/EMUAdditions/)
- **BepInEx**: [BepInEx Documentation](https://docs.bepinex.dev/)

---

## Support

If you encounter any issues or have feature requests:
1. Check the [Known Issues](#known-issues) section above
2. Ensure all dependencies are up to date
3. Check the BepInEx log file for error messages
4. Report issues on the GitHub repository or Thunderstore comments
