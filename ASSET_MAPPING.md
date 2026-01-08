# Unity Asset to Mod Feature Mapping

## Available Quality 3D Assets

### Creatures/Enemies (with animations)
| Asset | Animations | Textures | Best For |
|-------|-----------|----------|----------|
| Nightmare Beetle | run, walk, idle×2, attack×3, jump, death, rage, gethit (12) | PBR | Ground enemies |
| Flying Beetle | flying, attack×3, idle×2, death, gethit, roar (10) | PBR | Flying enemies |
| Tortoise Boss | Animations included | 3 colors, 7 audio, particles | Bosses |
| Spider Infestation | Spider Controller | 12 color variants + eggs | Swarm enemies |
| Alien Ships | N/A (static) | PBR, 3 colors each | Flying enemies |

### Turrets/Weapons
| Asset | Features | Textures | Best For |
|-------|----------|----------|----------|
| Simple SciFi Gun Turret Set | 4 variants | PBR | Gatling, Rocket turrets |
| Outpost AutoCannon | 20+ audio SFX, muzzle flash | PBR | Heavy turret |
| Laser Cannon | Emission maps | PBR | Laser turret |
| Guardian Cannon | Animation controller | PBR | Heavy/boss turret |

### Drones/Vehicles
| Asset | Variants | Textures | Best For |
|-------|----------|----------|----------|
| scifi-drone | 1 | Full PBR (7 maps) | Hover Pod, Combat drone |
| Drone (simple) | 6 colors | Basic | Scout drones |
| VoodooPlay Drone | 1 | Full PBR | Cargo drones |
| RobotSphere | 1 + animation | PBR | Heavy lifter |

### Flora (Hazards/Decoration)
| Asset | Types | Textures | Best For |
|-------|-------|----------|----------|
| Mushroom Forest | 7 types | PBR | Toxic mushrooms, decoration |

### Structures
| Asset | Count | Features | Best For |
|-------|-------|----------|----------|
| Sci-Fi Modular Pack | 100+ prefabs | PBR, doors animate | Base building |
| AlienBuildings | 5 types | Animated (Claw, Brain, etc.) | Alien hives |

---

## TurretDefense Mod - Required Upgrades

### GroundEnemyController.cs - Replace Primitives

| Enemy Type | Current | Replacement Asset | Notes |
|------------|---------|-------------------|-------|
| Chomper | CreatePrimitive(Sphere) | Nightmare Beetle | Use attack1/attack2/attack3 |
| Spitter | CreatePrimitive(Cube) | Spider Infestation (light color) | Ranged, use gethit |
| Mimic | CreatePrimitive(Cylinder) | Nightmare Beetle (dark variant) | Stealth, use rage |
| Arachnid | CreatePrimitive(Sphere) | Spider Infestation (dark color) | Fast, use run animation |
| Grenadier | CreatePrimitive(Capsule) | Nightmare Beetle (scaled 1.5x) | Heavy, use jump |
| Gunner | CreatePrimitive(Cylinder) | Spider Infestation (armored) | Ranged, eggs for grenades |

### AlienShipController.cs - Replace Primitives

| Ship Type | Current | Replacement Asset | Notes |
|-----------|---------|-------------------|-------|
| Fighter | CreatePrimitive(Cube) | AlienFighter | Use AlienFighterGreen for scouts |
| Destroyer | CreatePrimitive(Sphere) | AlienDestroyer | Slower, more HP |
| Torpedo | CreatePrimitive(Capsule) | BioTorpedo | Fast, kamikaze |
| Bomber | CreatePrimitive(Cylinder) | AlienDestroyerWhite | Area attacks |
| Scout | CreatePrimitive(Cube) | AlienFighterWhite | Fast, low HP |
| Robot | CreatePrimitive(Sphere) | RobotSphere | Use animation |

### BossController.cs - Replace Primitives

| Boss Type | Current | Replacement Asset | Scale | Notes |
|-----------|---------|-------------------|-------|-------|
| Overlord | CreateOverlordModel() | Tortoise Boss (red) | 3x | Main boss, uses audio |
| Behemoth | CreateBehemothModel() | Tortoise Boss (green) | 5x | Tank boss |
| Hivemind | CreateHivemindModel() | AlienBuildings/Brain | 2x | Summons minions |
| Dreadnought | CreateDreadnoughtModel() | AlienDestroyer (scaled) | 4x | Flying boss |
| Harvester | CreateHarvesterModel() | AlienBuildings/Claw | 2.5x | Collector boss |

### TurretController.cs - Replace Primitives

| Turret Type | Current | Replacement Asset | Notes |
|-------------|---------|-------------------|-------|
| Gatling | Cylinder + Cube | Outpost AutoCannon | Use all 20+ SFX |
| Rocket | Cylinders | SciFi Gun Turret (rocket variant) | Add particle trails |
| Laser | Cube | Laser Cannon | Use emission for beam |
| Railgun | Cylinder | SciFi Gun Turret (heavy variant) | Charge-up effect |
| Lightning | Sphere | Guardian Cannon | Arc effects |

---

## Implementation Priority

### Phase 1: TurretDefense Core (High Impact)
1. Add AssetBundle loading to TurretDefensePlugin.cs
2. Update TurretController to use SciFi turrets
3. Update AlienShipController to use Alien Ships prefabs
4. Update BossController to use Tortoise Boss

### Phase 2: Ground Combat (Medium Impact)
1. Create beetle bundle from Nightmare Beetle assets
2. Create spider bundle from Spider Infestation
3. Update GroundEnemyController with animated models
4. Add attack/death animations

### Phase 3: DroneLogistics Integration
1. Already uses VoodooPlay/SciFi drones (DONE)
2. Add RobotSphere for HeavyLifter variant
3. Combat drone uses scifi-drone

### Phase 4: HazardousWorld
1. Load Mushroom Forest assets
2. Create toxic mushroom variants with particle effects
3. Add alien building hazards (Stomach = acid pool)

### Phase 5: AmbientLife Enhancement
1. Already loads creature bundles (DONE)
2. Add more creature variety
3. Add animations where missing

---

## Asset Bundle Creation Required

| Bundle Name | Contents | For Mod |
|-------------|----------|---------|
| turrets_scifi | SciFi Gun Turret Set, AutoCannon, Laser Cannon, Guardian Cannon | TurretDefense |
| enemies_beetles | Nightmare Beetle, Flying Beetle | TurretDefense |
| enemies_spiders | Spider Infestation (all colors) | TurretDefense |
| enemies_aliens | Alien Ships (all 9 prefabs) | TurretDefense |
| bosses | Tortoise Boss (3 colors), AlienBuildings | TurretDefense |
| flora_mushrooms | Mushroom Forest (all 7 types) | HazardousWorld |
| vehicles_hover | scifi-drone, pilot_seat | MobilityPlus |

---

## Keybind Audit (Avoid Conflicts)

### Techtonica Default Keys (DO NOT USE)
- WASD - Movement
- E - Interact
- Q - Equipment/Tool switch
- R - Rotate/Reload
- T - Scanner
- F - Flashlight
- C - Crouch
- Space - Jump
- Ctrl - Modifier (crouch hold)
- Shift - Sprint
- Tab - Inventory
- M - Map
- I - Inventory alternate
- Escape - Menu
- 1-9 - Hotbar

### Mod Keybinds (Verified Safe - No Game Conflicts)
| Mod | Action | Key | Status |
|-----|--------|-----|--------|
| MobilityPlus | Summon Vehicle | Home | SAFE |
| MobilityPlus | Dismiss Vehicle | End | SAFE |
| MobilityPlus | Altitude Up | PageUp | SAFE |
| MobilityPlus | Altitude Down | PageDown | SAFE |
| TurretDefense | Test Spawn Alien | Numpad7 | SAFE |
| TurretDefense | Force Wave | Numpad8 | SAFE |
| TurretDefense | Spawn Turret | Numpad9 | SAFE |
| AmbientLife | Creature Config UI | F7 | SAFE (was F8) |
| DevTools | GUI Toggle | Insert/F8 | SAFE |
| NarrativeExpansion | NPC Interact | K | SAFE (was E - game conflict!) |
| EnhancedLogistics | Search UI | Ctrl+L | SAFE (was Ctrl+F - flashlight!) |
| EnhancedLogistics | Drone Menu | J | SAFE |
| OmniseekerPlus | Cycle Mode | O | SAFE |
| OmniseekerPlus | Quick Scan | P | SAFE |
| PetsCompanions | Summon Pet | Comma (,) | SAFE |
| PetsCompanions | Dismiss Pet | Period (.) | SAFE |
| HazardousWorld | Spawn Spore (Debug) | F10 | SAFE |
| HazardousWorld | Spawn Flora (Debug) | F11 | SAFE |

---

## Code Changes Summary

### TurretDefensePlugin.cs
```csharp
// Add at class level
private static Dictionary<string, AssetBundle> loadedBundles = new();
private static Dictionary<string, GameObject> prefabCache = new();

// Add in Awake()
LoadAssetBundles();

// Add method
private void LoadAssetBundles() {
    string bundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Bundles");
    string[] bundles = { "turrets_scifi", "enemies_aliens", "enemies_beetles", "bosses" };
    // Load each bundle...
}

// Add method
public static GameObject GetPrefab(string bundle, string prefab) {
    // Cached prefab loading with URP material fix...
}
```

### Controllers Updates
Each controller needs:
1. Remove CreatePrimitive() calls
2. Add GetPrefab() calls for real models
3. Add FixMaterialsForURP() after instantiation
4. Set up animation controllers where available
5. Scale appropriately

