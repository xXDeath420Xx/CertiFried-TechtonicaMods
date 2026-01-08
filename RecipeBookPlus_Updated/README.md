# RecipeBookPlus

**Collapsible Recipe Book replacement for Techtonica**

RecipeBookPlus is a modern, streamlined replacement for the original RecipeBook mod. It features collapsible panels, a clean interface, and no external dependencies beyond the core modding utilities.

---

## Features

### Collapsible Panels

The main feature of RecipeBookPlus is the ability to collapse panels you're not using:

- **Items Panel** (Left side): Shows all items in the game
- **Recipes Panel** (Right side): Shows recipes for selected item

Each panel has a collapse/expand tab (`<` or `>`) on its edge that you can click to toggle visibility.

### Clean Interface

- Resolution-independent UI that scales with your screen
- Fast, real-time filtering as you type
- Clear item selection with visual feedback
- Scrollable lists for both items and recipes
- Recipe details showing ingredients and quantities

### Keyboard Shortcut

Press **F3** to toggle the Recipe Book window on/off.

---

## How to Use

1. Press **F3** to open the Recipe Book
2. Type in the search box to filter items by name
3. Click on an item to see its recipe(s)
4. Click `<` or `>` tabs to collapse/expand panels
5. Press **F3** again or click outside to close

---

## Configuration

All settings can be adjusted in the config file:
`BepInEx/config/com.certifired.RecipeBookPlus.cfg`

| Option | Default | Description |
|--------|---------|-------------|
| `Toggle Key` | `F3` | Key to open/close the Recipe Book |

---

## Installation

### Using r2modman (Recommended)

1. Open r2modman and select Techtonica
2. Search for "RecipeBookPlus"
3. Click Download

### Manual Installation

1. Ensure BepInEx 5.4.21+ is installed
2. Ensure EquinoxsModUtils is installed
3. Place `RecipeBookPlus.dll` in `BepInEx/plugins`

---

## Requirements

| Dependency | Minimum Version |
|------------|-----------------|
| BepInEx | 5.4.21+ |
| EquinoxsModUtils | 6.1.1+ |

---

## Compatibility

- **Game Version**: Compatible with current Techtonica releases
- **Original RecipeBook**: Disable the original RecipeBook mod when using this one
- **Other Mods**: Compatible with most mods

---

## Why RecipeBookPlus?

This mod was created as a lightweight replacement for the original RecipeBook mod:

- **No CaspuinoxGUI dependency** - Uses built-in Unity IMGUI
- **Collapsible panels** - Only show what you need
- **Smaller footprint** - Single DLL, no extra dependencies
- **Modern code** - Built on latest modding practices

---

## Changelog

### [1.0.0] - Initial Release
- Collapsible Items and Recipes panels
- F3 toggle keybind
- Real-time search filtering
- Resolution-independent UI
- Clean, modern interface

---

## Credits

- **Author**: CertiFried
- **Development Assistance**: Claude Code (Anthropic AI)
- **Inspired by**: Original RecipeBook by Equinox

---

## License

This mod is licensed under the **GNU General Public License v3.0** (GPL-3.0).

---

## Links

- **Thunderstore**: [RecipeBookPlus](https://thunderstore.io/c/techtonica/p/CertiFried/RecipeBookPlus/)
- **GitHub**: [Source Repository](https://github.com/CertiFried/TechtonicaMods)

---

*RecipeBookPlus v1.0.0 - A better way to browse recipes*
