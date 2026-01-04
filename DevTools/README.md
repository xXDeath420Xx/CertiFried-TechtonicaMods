# DevTools

In-game developer tools with full GUI for Techtonica. Now includes integrated CasperProtections features!

## Features

- **In-Game GUI**: Press F8 to toggle the developer tools panel
- **Tabbed Interface**: Organized into Player, Game Mode, Machines, Power, Protection, and Special tabs
- **Live Value Updates**: All values update in real-time as you modify them

### Player Tab
- Infinite Crafting - Build without consuming resources
- Max Power - All machines at full power
- Ultra Pickaxe - Enhanced mining
- Faster MOLE - 4x MOLE speed
- Disable Encumbrance - No weight limits
- Show Debug Coordinates
- Hide Machine Lights/Particles (performance)
- Simulation Speed slider (0.1x - 10x)
- Free Camera Mode selection

### Game Mode Tab
- All Doors Unlocked
- MOLE Bits Always Available
- Infinite Ore - Veins never deplete
- Player Speed percentage (50% - 1000%)
- MOLE Speed percentage (50% - 1000%)

### Machines Tab
- Smelter Speed percentage
- Assembler Speed percentage
- Thresher Speed percentage
- Planter Speed percentage
- Inserter Base Stack Size

### Power Tab
- Fuel Consumption percentage
- Power Consumption percentage
- Power Generation percentage

### Protection Tab (NEW - CasperProtections Integration)
- **Disable Protection Zones** - Dig/build anywhere, ignore protected areas
- **Disable Safe Sprint** - Allow sprinting inside buildings
- **Disable Prebuild Protections** - Allow erasing prebuilt/developer objects
- **Sprint Hold Mode** - Only sprint while holding the sprint key

### Special Tab
- Cat Sounds - Replace machine sounds with cat meowing (FMOD Cat Cheat)
- Rainbow Cores - Memory Tree cores cycle through rainbow colors
- Placeholder Voice - Enable developer placeholder voice lines
- Status display for active cheats

## Installation

1. Install BepInEx for Techtonica
2. Place the `DevTools.dll` in your `BepInEx/plugins` folder
3. Launch the game and press F8 to open the developer tools

## Hotkeys

- **F8**: Toggle DevTools GUI (configurable)

## Configuration

All settings can be configured via the in-game GUI or the BepInEx configuration file located at `BepInEx/config/com.certifired.DevTools.cfg`

## Credits

- CasperProtections features originally by Casper_Dev - integrated with permission

## Changelog

### v2.1.0
- Added Protection tab with CasperProtections features integration
- Disable Protection Zones (dig anywhere)
- Disable Safe Sprint (run in buildings)
- Disable Prebuild Protections (erase prebuilt objects)
- Sprint Hold Mode (only run while holding key)

### v2.0.1
- Added custom icon
- Minor bug fixes

### v2.0.0
- Initial release with full GUI
