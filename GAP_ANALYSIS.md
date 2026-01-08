# Techtonica Mods - Gap Analysis & Enhancement Opportunities

## Current State: 29 Mods, 45 Asset Bundles (Updated)

### Mods WITH Bundle Deployments (10)
| Mod | Bundles | Status |
|-----|---------|--------|
| TurretDefense | 35 | Full coverage (added enemy_robots, creatures_tortoise) |
| DroneLogistics | 7 | Full coverage |
| HazardousWorld | 4 | Full coverage |
| AmbientLife | 7 | Full coverage |
| MobilityPlus | 3 | Full coverage |
| PetsCompanions | 4 | Full coverage |
| NarrativeExpansion | 3 | Full coverage |
| SurvivalElements | 2 | Full coverage |
| BaseBuilding | 1 | NEW - scifi_modular |
| MechSuit | 1 | NEW - mech_companion |

### Mods WITHOUT Bundle Deployments (19)
| Mod | Could Use Bundles | Priority |
|-----|-------------------|----------|
| AdvancedMachines | scifi_machines | LOW - uses game assets |
| AtlantumEnrichment | icons_skymon | LOW - UI only |
| AtlantumReactor | icons_skymon | LOW - UI only |
| BioProcessing | lava_plants, mushroom_forest | MEDIUM |
| DevTools | None needed | N/A |
| EnhancedLogistics | drones_*, scifi_machines | HIGH - duplicate of DroneLogistics? |
| Recycler | scifi_machines | MEDIUM |
| WormholeChests | None needed | N/A |
| OmniseekerPlus | None needed | N/A |
| TechtonicaFramework | None - it's a library | N/A |

---

## UNUSED ASSETS - Opportunities

### 1. Sci-Fi Styled Modular Pack (100+ prefabs) - NOT USED
**Potential:** NEW MOD - "BaseBuilding" or "ModularStructures"
- Walls, floors, doors, windows
- Corridors, stairs
- Decorative pieces
- Lights and fixtures

### 2. Turrets NOT in TurretDefense Code
| Turret | Bundle Exists | In Code? | Action |
|--------|---------------|----------|--------|
| Nuke | turrets_nuke | ? | Add to TurretDefense |
| Flamethrower | turrets_flamethrower | ? | Add to TurretDefense |
| TDTK turrets (12) | turrets_tdtk | Partial | Expand variety |
| SciFi turrets (4) | turrets_scifi | ? | Add variants |

### 3. Enemy Types NOT Fully Utilized
| Enemy | Bundle | Current Use | Enhancement |
|-------|--------|-------------|-------------|
| MachineGunRobot | enemy_robots | None | Add to waves |
| CannonMachine | enemy_robots | None | Add as mini-boss |
| Ellen (character) | creatures_gamekit | None | NPC or ally? |
| Mimic | creature_mimic | TurretDefense | Expand ambush behavior |
| Arachnid Alien | creature_arachnid | TurretDefense | Expand use |

### 4. Boss Assets NOT Fully Used
| Asset | Bundle | Current Use | Enhancement |
|-------|--------|-------------|-------------|
| Tortoise Boss particles | creatures_tortoise | None? | Add to BossController |
| Tortoise Boss audio (7 SFX) | creatures_tortoise | None? | Add sound effects |
| Cthulhu Idols (2) | cthulhu_idols | None | Boss spawner structures |

### 5. Creature Animations NOT Used
| Creature | Animations | Current Use |
|----------|------------|-------------|
| Nightmare Beetle | 12 (attack×3, death, rage, etc.) | AmbientLife - partial |
| Flying Beetle | 10 (attack×3, roar, etc.) | AmbientLife - partial |
| Wolf | Full set | PetsCompanions |
| Tortoise Boss | Full set + 3 colors | Not as enemy? |

### 6. Mech/Vehicle Assets
| Asset | Bundle | Current Use | Enhancement |
|-------|--------|-------------|-------------|
| PBRCharacter | mech_companion | None | NEW: Mech suit mod |
| HPCharacter | mech_companion | None | Heavy mech variant |
| PolyartCharacter | mech_companion | None | Light mech variant |

---

## ENHANCEMENT PRIORITIES

### HIGH PRIORITY

#### 1. TurretDefense - Add Missing Turret Types
```
Add to code:
- Nuke turret (turrets_nuke bundle)
- Flamethrower turret (turrets_flamethrower bundle)
- TDTK turrets (turrets_tdtk bundle - 12 types!)
```

#### 2. TurretDefense - Add Missing Enemy Types
```
Add to GroundEnemyController:
- MachineGunRobot (enemy_robots)
- CannonMachine (enemy_robots)

Add to WaveManager:
- More enemy variety per wave
- Mini-boss spawns
```

#### 3. BossController - Use Tortoise Boss Fully
```
Current: May use primitives
Enhancement:
- Load Tortoise Boss model from creatures_tortoise
- Add particle effects (10 prefabs in bundle)
- Add audio (7 SFX files)
- Use 3 color variants for different boss phases
```

#### 4. Artillery System Expansion
```
Bundle: artillery_cannons has 4 models
- cannon_small
- cannon_medium
- cannon_large
- cannon_set

Enhancement: Add all variants to ArtilleryController
```

### MEDIUM PRIORITY

#### 5. NEW MOD: BaseBuilding / ModularStructures
```
Use: Sci-Fi Styled Modular Pack
Features:
- Decorative walls/floors
- Defensive barriers
- Lighting fixtures
- Storage rooms
- Command centers
```

#### 6. NEW MOD: MechSuit
```
Use: mech_companion bundle
Features:
- Player-wearable mech suit
- Enhanced strength
- Damage resistance
- Special abilities
```

#### 7. BioProcessing - Add Visual Assets
```
Add bundles:
- mushroom_forest (for bio-reactors visual)
- lava_plants (for exotic bio-matter)
```

#### 8. Recycler - Add Machine Visuals
```
Add bundles:
- scifi_machines (for recycler machine model)
```

### LOW PRIORITY

#### 9. EnhancedLogistics - Merge or Differentiate
```
Currently overlaps with DroneLogistics
Options:
- Merge into single mod
- Differentiate features
- Remove duplicate
```

#### 10. Icon Bundle Deployment
```
Deploy to more mods:
- icons_skymon → AtlantumReactor, AtlantumEnrichment
- icons_heathen → DevTools, UI mods
- icons_ammo → Any combat mod
```

---

## CODE ENHANCEMENTS NEEDED

### TurretDefense Additions

```csharp
// Add to GetBundleForTurret()
"nuke" => "turrets_nuke",
"flamethrower" => "turrets_flamethrower",

// Add to GetPrefabNameForTurret()
"nuke" => "Nuke_Base_ColorChanging",
"flamethrower" => "Flamethrower_ColorChanging_L1",

// Add enemy types to SpawnGroundEnemy()
"machinegunrobot" => bundle: "enemy_robots", prefab: "MachineGunRobot",
"cannonmachine" => bundle: "enemy_robots", prefab: "CannonMachine",
```

### BossController - Tortoise Boss Integration

```csharp
// Load actual Tortoise Boss model
string[] bossColors = { "Standard", "Blue", "Violet" };
// Use particles: Attack_*, Smoke_*, Ground_Shake_*, Acid_Splash_*
// Use audio from bundle
```

### New Turret Types to Add

| Type | Bundle | Prefab | Damage Type |
|------|--------|--------|-------------|
| Nuke | turrets_nuke | Nuke_Base_ColorChanging | Massive AOE |
| Flamethrower | turrets_flamethrower | Flamethrower_ColorChanging_L1 | DOT Fire |
| Canon (TDTK) | turrets_tdtk | TowerCanon | High single |
| MG (TDTK) | turrets_tdtk | TowerMG | Rapid fire |
| Missile (TDTK) | turrets_tdtk | TowerMissile | Homing |
| BeamLaser (TDTK) | turrets_tdtk | TowerBeamLaser | Continuous |
| AOE (TDTK) | turrets_tdtk | TowerAOE | Area denial |
| Support (TDTK) | turrets_tdtk | TowerSupport | Buff nearby |

---

## SUMMARY

### Fully Utilized
- Alien Ships (autarca_ships) ✓
- Robot Drones (drones_scifi) ✓
- Simple Drones (drones_simple, drones_voodooplay) ✓
- Basic Turrets (gatling, rocket, railgun, laser, lightning) ✓
- Mushroom Forest ✓
- Basic Creatures (beetles, spiders, animals) ✓

### Partially Utilized
- Tortoise Boss (model yes, particles/audio no)
- TDTK Turrets (12 types, only some used)
- Creature Animations (loaded but not all played)
- Artillery (AutoCannon used, 4 cannons not)

### NOW Utilized (Previously Unused)
- Sci-Fi Modular Pack (100+ prefabs) - NOW IN BaseBuilding mod
- Mech Companion (3 variants) - NOW IN MechSuit mod
- Nuke Turret - NOW IN TurretDefense
- Flamethrower Turret - NOW IN TurretDefense
- Enemy Robots (MachineGunRobot, CannonMachine) - NOW IN TurretDefense
- Tortoise Boss (model + particles + audio) - NOW IN TurretDefense BossController

### Still Unused
- Cthulhu Idols (boss spawners) - could be used for hive spawner visuals

### Recommended Next Steps - COMPLETED
1. ~~Add Nuke + Flamethrower turrets to TurretDefense~~ DONE
2. ~~Add MachineGunRobot + CannonMachine enemies~~ DONE
3. ~~Integrate Tortoise Boss particles/audio~~ DONE
4. ~~Create BaseBuilding mod with Modular Pack~~ DONE
5. ~~Create MechSuit mod~~ DONE

### Future Enhancements
1. Add more TDTK turret types (12 available, ~6 implemented)
2. Add SciFi turret variants (4 available)
3. Expand WaveManager with robot enemy variety
4. Add more decorative structures to BaseBuilding
5. Add mech suit crafting recipes and tech tree integration
