# Techtonica Mod Ecosystem Design

## Overview

An interconnected system of mods where each component enhances and depends on others, creating emergent gameplay depth.

---

## Core Systems

### 1. DroneLogistics
**Purpose**: Automated transport using drones and cargo crates

**Components**:
- **Cargo Crates**: Packaged resources (2 stacks per crate)
- **Packing Station**: Machine that packs loose items → crates
- **Unpacking Station**: Machine that unpacks crates → items
- **Drone Pad**: Landing/charging station for drones
- **Drone Controller**: Assigns routes and priorities

**Drone Types**:
| Type | Capacity | Speed | Range | Power |
|------|----------|-------|-------|-------|
| Scout Drone | 1 crate | Fast | Short | Low |
| Cargo Drone | 4 crates | Medium | Medium | Medium |
| Heavy Lifter | 8 crates | Slow | Long | High |
| Bio-Drone | 2 crates | Medium | Medium | Biofuel |

**Integration Points**:
- BioProcessing → Biofuel for Bio-Drones
- TurretDefense → Drones can be attacked by enemies
- Recycler → Damaged drones can be recycled

---

### 2. Recycler
**Purpose**: Disassemble items back to components

**Machine Tiers**:
| Tier | Efficiency | Can Process | Power |
|------|------------|-------------|-------|
| Basic Recycler | 25% | Simple items | 50 kW |
| Advanced Recycler | 50% | Complex items | 150 kW |
| Quantum Recycler | 75% | All items | 500 kW |
| Perfect Recycler | 100% | All items | 2 MW + Atlantum |

**Outputs**:
- Raw materials (ores, ingots)
- Scrap (unusable waste)
- Rare components (random bonus chance)

**Integration Points**:
- BioProcessing → Organic scrap → compost → fertilizer
- AtlantumEnrichment → Radioactive waste recycling
- DroneLogistics → Recycle damaged drones

---

### 3. BioProcessing
**Purpose**: Biological resource chains and renewable materials

**Production Chains**:

```
Algae Vat (water + light)
    └── Raw Algae
        ├── Algae Press → Bio-Oil → Biofuel
        ├── Algae Dryer → Dried Algae → Fiber
        └── Algae Composter → Fertilizer

Mushroom Farm (dark + fertilizer)
    └── Mushrooms
        ├── Spore Extractor → Spores (planting)
        ├── Mushroom Press → Mushroom Oil
        └── Bio-Reactor → Biogas → Power

Lava Plant Farm (heat + minerals)
    └── Lava Plants
        ├── Heat Extractor → Thermal Energy
        ├── Mineral Extractor → Rare Minerals
        └── Bio-Armor → Heat-resistant equipment
```

**Integration Points**:
- DroneLogistics → Biofuel powers Bio-Drones
- Recycler → Organic waste → compost
- HazardousWorld → Bio-remediation of toxic zones
- AtlantumEnrichment → Bio-shielding from radiation

---

### 4. AtlantumEnrichment
**Purpose**: Advanced Atlantum processing with risk/reward

**Process Chain**:
```
Raw Atlantum Ore
    ↓ (Crusher)
Atlantum Dust
    ↓ (Centrifuge) [10% explosion risk]
Refined Atlantum + Radioactive Waste
    ↓ (Enrichment Chamber) [25% explosion risk without safety]
Enriched Atlantum + High-Level Waste
    ↓ (Quantum Stabilizer) [50% explosion risk without safety]
Stable Quantum Atlantum
```

**Safety System**:
- Safety Module I: -10% risk per module (max 4)
- Safety Module II: -15% risk per module
- Safety Module III: -25% risk per module
- Emergency Shutdown: Automatic if risk > threshold

**Waste Management**:
- Radioactive Waste → Vitrification → Glass Blocks (safe storage)
- High-Level Waste → Deep Bore Injection (permanent disposal)
- Leaked Waste → Radiation Zone (HazardousWorld)

**Integration Points**:
- HazardousWorld → Radiation zones from accidents
- Recycler → Quantum Recycler requires Enriched Atlantum
- VoidExtractor → Powered by Stable Quantum Atlantum
- BioProcessing → Bio-remediation of contaminated zones

---

### 5. ArtilleryDefense (TurretDefense Extension)
**Purpose**: Long-range area denial weapons

**New Turret Types**:
| Type | Damage | Range | Fire Rate | Special |
|------|--------|-------|-----------|---------|
| Light Cannon | 150 | 80m | 0.5/s | Armor pierce |
| Medium Cannon | 400 | 120m | 0.25/s | Explosive |
| Heavy Cannon | 1000 | 200m | 0.1/s | Massive AOE |
| Auto-Cannon | 75 | 60m | 4/s | Rapid fire |
| Flare Launcher | 0 | 150m | 0.2/s | Reveals enemies |

**Ammo System**:
- Cannons require ammunition (not just power)
- Ammo crafted from metal + explosives
- DroneLogistics can resupply turrets automatically

**Integration Points**:
- DroneLogistics → Ammo resupply drones
- Recycler → Salvage destroyed enemy parts
- BioProcessing → Bio-explosive compounds

---

### 6. VoidExtractor (End-Game)
**Purpose**: Generate resources from quantum vacuum

**Requirements**:
- Stable Quantum Atlantum (fuel)
- Massive power (10 MW continuous)
- Void Containment Field (prevents reality tears)

**Outputs** (configurable):
- Common ores (iron, copper, limestone)
- Rare materials (kindlevine, plantmatter)
- Exotic matter (new resource for ultimate items)

**Risks**:
- Void Breach: Spawns void creatures (new enemy type)
- Reality Tear: Creates unstable zone
- Power Surge: Damages nearby machines

**Integration Points**:
- AtlantumEnrichment → Fuel source
- TurretDefense → Defend against void creatures
- HazardousWorld → Void zones as new hazard type

---

### 7. QuantumBeacon
**Purpose**: Enhance nearby machines

**Effects** (configurable per beacon):
- Speed Boost: +50% to +200% production speed
- Efficiency: -25% to -75% power consumption
- Quality: +10% to +50% rare output chance
- Range: Affects machines within 15-50m radius

**Module Slots**: 8 slots for effect modules

**Power**: 500 kW base + 100 kW per active module

**Integration Points**:
- AtlantumEnrichment → Powered by Enriched Atlantum
- All production mods → Benefits from beacon effects

---

## Resource Flow Diagram

```
                    ┌─────────────────┐
                    │   Raw Ores      │
                    └────────┬────────┘
                             │
              ┌──────────────┼──────────────┐
              ▼              ▼              ▼
        ┌──────────┐  ┌──────────┐  ┌──────────────┐
        │ Smelting │  │ Atlantum │  │ BioProcessing│
        └────┬─────┘  │Enrichment│  └──────┬───────┘
             │        └────┬─────┘         │
             │             │               │
             ▼             ▼               ▼
        ┌──────────┐  ┌──────────┐  ┌──────────────┐
        │ Crafting │  │ Power &  │  │   Biofuel    │
        └────┬─────┘  │ Quantum  │  └──────┬───────┘
             │        └────┬─────┘         │
             │             │               │
             └──────┬──────┴───────┬───────┘
                    ▼              ▼
              ┌──────────┐  ┌──────────────┐
              │ Packing  │  │DroneLogistics│◄────┐
              │ Station  │  └──────┬───────┘     │
              └────┬─────┘         │             │
                   │               │             │
                   ▼               ▼             │
              ┌──────────┐  ┌──────────────┐     │
              │  Crates  │──►   Drones     │     │
              └──────────┘  └──────┬───────┘     │
                                   │             │
                    ┌──────────────┼─────────────┤
                    ▼              ▼             │
              ┌──────────┐  ┌──────────────┐     │
              │ Delivery │  │   Combat     │     │
              │  Points  │  │  (enemies)   │     │
              └────┬─────┘  └──────┬───────┘     │
                   │               │             │
                   ▼               ▼             │
              ┌──────────┐  ┌──────────────┐     │
              │Unpacking │  │   Salvage    │─────┘
              └────┬─────┘  └──────┬───────┘
                   │               │
                   ▼               ▼
              ┌──────────┐  ┌──────────────┐
              │  Output  │  │   Recycler   │
              └──────────┘  └──────────────┘
```

---

## Implementation Order

### Phase 1: Foundation
1. **Asset Bundles** - Build Outpost, Cannon set bundles
2. **DroneLogistics Core** - Cargo crates, packing/unpacking
3. **Recycler** - Basic recycling mechanics

### Phase 2: Combat Extension
4. **ArtilleryDefense** - New turret types
5. **Ammo System** - Craftable ammunition

### Phase 3: Production Chains
6. **BioProcessing** - Algae, mushrooms, lava plants
7. **AtlantumEnrichment** - Advanced processing

### Phase 4: End-Game
8. **VoidExtractor** - Resource generation
9. **QuantumBeacon** - Machine enhancement

### Phase 5: Polish
10. **UI/Visualization** - Production planner overlay
11. **Balance Pass** - Adjust costs, outputs, risks
12. **Integration Testing** - All systems working together

---

## New Resources

| Resource | Source | Used By |
|----------|--------|---------|
| Cargo Crate | Packing Station | DroneLogistics |
| Biofuel | BioProcessing | Bio-Drones |
| Bio-Oil | Algae Press | Biofuel, Lubricant |
| Fertilizer | Composter | Farms |
| Spores | Mushroom Farm | Planting |
| Enriched Atlantum | Enrichment | VoidExtractor, QuantumBeacon |
| Radioactive Waste | Enrichment | Disposal, Weapons |
| Exotic Matter | VoidExtractor | Ultimate items |
| Artillery Shell | Crafting | Cannons |
| Explosive Compound | BioProcessing + Chemistry | Shells, Mining |

---

## File Structure

```
TechtonicaMods/NewMods/
├── TechtonicaFramework/     (shared systems)
├── TurretDefense/           (existing)
├── DroneLogistics/          (NEW)
│   ├── DroneLogisticsPlugin.cs
│   ├── Controllers/
│   │   ├── DroneController.cs
│   │   ├── DronePadController.cs
│   │   └── PackingStationController.cs
│   ├── Systems/
│   │   ├── CargoSystem.cs
│   │   └── RouteManager.cs
│   └── Data/
│       └── CrateDefinitions.cs
├── Recycler/                (NEW)
│   ├── RecyclerPlugin.cs
│   └── RecyclerMachine.cs
├── BioProcessing/           (NEW)
│   ├── BioProcessingPlugin.cs
│   ├── Machines/
│   │   ├── AlgaeVat.cs
│   │   ├── MushroomFarm.cs
│   │   └── BioReactor.cs
│   └── Data/
│       └── BioRecipes.cs
├── AtlantumEnrichment/      (NEW)
│   ├── AtlantumEnrichmentPlugin.cs
│   ├── Machines/
│   │   ├── Centrifuge.cs
│   │   ├── EnrichmentChamber.cs
│   │   └── WasteProcessor.cs
│   └── Systems/
│       ├── RadiationSystem.cs
│       └── SafetySystem.cs
└── ArtilleryDefense/        (NEW - or extend TurretDefense)
    ├── Turrets/
    │   ├── LightCannon.cs
    │   ├── MediumCannon.cs
    │   └── HeavyCannon.cs
    └── Systems/
        └── AmmoSystem.cs
```
