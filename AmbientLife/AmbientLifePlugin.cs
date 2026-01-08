using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AmbientLife
{
    /// <summary>
    /// AmbientLife - Adds passive wildlife and ambient creatures using real 3D assets
    /// Features: Spiders, beetles, wolves, dogs, cats, deer and flying insects
    /// Smart AI that avoids clipping and follows terrain properly
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class AmbientLifePlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.AmbientLife";
        public const string PluginName = "AmbientLife";
        public const string VersionString = "2.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static AmbientLifePlugin Instance;
        public static string PluginPath;

        // General Configuration
        public static ConfigEntry<bool> EnableAmbientLife;
        public static ConfigEntry<int> MaxCreatures;
        public static ConfigEntry<float> SpawnRadius;
        public static ConfigEntry<float> DespawnRadius;
        public static ConfigEntry<bool> DebugMode;

        // Flying Insects
        public static ConfigEntry<bool> EnableButterflies;
        public static ConfigEntry<bool> EnableFireflies;
        public static ConfigEntry<bool> EnableBeetles;
        public static ConfigEntry<bool> EnableNightmareBeetles;

        // Spiders (wall/ground crawlers)
        public static ConfigEntry<bool> EnableBrownSpiders;
        public static ConfigEntry<bool> EnableGreenSpiders;
        public static ConfigEntry<bool> EnableBlackSpiders;

        // Ground Animals
        public static ConfigEntry<bool> EnableDogs;
        public static ConfigEntry<bool> EnableCats;
        public static ConfigEntry<bool> EnableChickens;
        public static ConfigEntry<bool> EnableDeer;
        public static ConfigEntry<bool> EnableWolves;
                public static ConfigEntry<bool> EnablePenguins;

        // New Alien Life configs
        public static ConfigEntry<bool> EnableSpawnNests;
        public static ConfigEntry<bool> EnableAlienBugs;
        public static ConfigEntry<bool> EnableGlowWorms;
        public static ConfigEntry<bool> EnableCrystalBeetles;
        public static ConfigEntry<bool> EnableSporeClouds;
        public static ConfigEntry<bool> EnableAlienMantis;
        public static ConfigEntry<bool> EnableCaveWorms;

        // Density controls (0-1 weights)
        public static float DensityFlying = 0.4f;
        public static float DensityCrawling = 0.3f;
        public static float DensityAnimals = 0.3f;

        // Active creatures
        public static List<SmartAmbientCreature> ActiveCreatures = new List<SmartAmbientCreature>();
        private float spawnTimer = 0f;
        private const float SPAWN_INTERVAL = 1.5f;

        // Story state - tracks if anomaly/major event has occurred
        public static bool PostAnomalyState { get; private set; } = false;
        public static ConfigEntry<bool> EnableStoryBasedSpawning;
        public static ConfigEntry<float> AnomalyDepthThreshold;
        private static float deepestPlayerDepth = 0f;

        // Creature definitions
        public enum CreatureType
        {
            // Flying (Pre-anomaly organic)
            Butterfly,
            Firefly,
            FlyingBeetle,
            NightmareBeetle,

            // Wall/Ground Crawlers (Pre-anomaly organic)
            BrownSpider,
            GreenSpider,
            BlackSpider,

            // Ground Animals (Pre-anomaly organic)
            Dog,
            Cat,
            Chicken,
            Deer,
            Wolf,
            Penguin,

            // Post-anomaly mechanical/alien creatures
            ScoutDrone,       // Small hovering drone
            RepairBot,        // Ground crawler maintenance bot
            SentinelMech,     // Larger patrol unit
                        AlienGlider,      // Mysterious flying entity

            // New ambient creatures
            SpawnNest,        // Static structure that spawns creatures
            AlienBug,         // Small alien insect
            GlowWorm,         // Bioluminescent crawler
            CrystalBeetle,    // Crystalline insect
            SporeCloud,       // Floating spore organism
            AlienMantis,      // Larger predatory insect
            CaveWorm          // Underground worm creature
        }

        public static readonly Dictionary<CreatureType, CreatureDefinition> CreatureDefinitions = new Dictionary<CreatureType, CreatureDefinition>
        {
            // Flying insects
            { CreatureType.Butterfly, new CreatureDefinition("butterfly", SmartCreatureController.MovementMode.Hovering, 1.5f, 0.3f) },
            { CreatureType.Firefly, new CreatureDefinition("firefly", SmartCreatureController.MovementMode.Hovering, 0.8f, 0.1f, true) },
            { CreatureType.FlyingBeetle, new CreatureDefinition("flying beetle", SmartCreatureController.MovementMode.Flying, 3f, 0.5f) },
            { CreatureType.NightmareBeetle, new CreatureDefinition("nightmare beetle", SmartCreatureController.MovementMode.Flying, 4f, 0.8f) },

            // Spiders - wall/floor crawlers
            { CreatureType.BrownSpider, new CreatureDefinition("brown_spider", SmartCreatureController.MovementMode.WallCrawling, 2f, 0.4f) },
            { CreatureType.GreenSpider, new CreatureDefinition("green_spider", SmartCreatureController.MovementMode.WallCrawling, 1.8f, 0.35f) },
            { CreatureType.BlackSpider, new CreatureDefinition("black_spider", SmartCreatureController.MovementMode.WallCrawling, 2.5f, 0.5f) },

            // Ground animals
            { CreatureType.Dog, new CreatureDefinition("dog", SmartCreatureController.MovementMode.Crawling, 3f, 1f) },
            { CreatureType.Cat, new CreatureDefinition("kitty", SmartCreatureController.MovementMode.Crawling, 2.5f, 0.8f) },
            { CreatureType.Chicken, new CreatureDefinition("chicken", SmartCreatureController.MovementMode.Crawling, 1.5f, 0.5f) },
            { CreatureType.Deer, new CreatureDefinition("deer", SmartCreatureController.MovementMode.Crawling, 4f, 1.5f) },
            { CreatureType.Wolf, new CreatureDefinition("wolf_lrp", SmartCreatureController.MovementMode.Crawling, 5f, 1.2f) },
            { CreatureType.Penguin, new CreatureDefinition("penguin", SmartCreatureController.MovementMode.Crawling, 1.2f, 0.7f) },

            // Post-anomaly mechanical creatures (procedural models)
            { CreatureType.ScoutDrone, new CreatureDefinition("scout_drone", SmartCreatureController.MovementMode.Hovering, 3f, 0.4f, true, true) },
            { CreatureType.RepairBot, new CreatureDefinition("repair_bot", SmartCreatureController.MovementMode.Crawling, 1.5f, 0.6f, false, true) },
            { CreatureType.SentinelMech, new CreatureDefinition("sentinel_mech", SmartCreatureController.MovementMode.Crawling, 2f, 1.5f, false, true) },
                        { CreatureType.AlienGlider, new CreatureDefinition("alien_glider", SmartCreatureController.MovementMode.Flying, 4f, 0.8f, true, true) },

            // New ambient creatures
            { CreatureType.SpawnNest, new CreatureDefinition("spawn_nest", SmartCreatureController.MovementMode.Crawling, 0f, 1.5f, true, false) },
            { CreatureType.AlienBug, new CreatureDefinition("alien_bug", SmartCreatureController.MovementMode.Crawling, 3f, 0.2f, false, false) },
            { CreatureType.GlowWorm, new CreatureDefinition("glow_worm", SmartCreatureController.MovementMode.Crawling, 0.5f, 0.3f, true, false) },
            { CreatureType.CrystalBeetle, new CreatureDefinition("crystal_beetle", SmartCreatureController.MovementMode.Crawling, 2f, 0.4f, true, false) },
            { CreatureType.SporeCloud, new CreatureDefinition("spore_cloud", SmartCreatureController.MovementMode.Hovering, 0.3f, 0.6f, true, false) },
            { CreatureType.AlienMantis, new CreatureDefinition("alien_mantis", SmartCreatureController.MovementMode.Crawling, 2.5f, 0.8f, false, false) },
            { CreatureType.CaveWorm, new CreatureDefinition("cave_worm", SmartCreatureController.MovementMode.Crawling, 1f, 0.5f, false, false) }
        };

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            // Initialize asset loader
            CreatureAssetLoader.Initialize();

            // Add UI component
            gameObject.AddComponent<CreatureConfigUI>();

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
            Log.LogInfo($"Loaded {CreatureAssetLoader.LoadedPrefabCount} creature prefabs from {CreatureAssetLoader.LoadedBundleCount} bundles");
        }

        private void InitializeConfig()
        {
            // General
            EnableAmbientLife = Config.Bind("General", "Enable Ambient Life", true, "Enable ambient creature spawning");
            MaxCreatures = Config.Bind("General", "Max Creatures", 40,
                new ConfigDescription("Maximum ambient creatures at once", new AcceptableValueRange<int>(5, 150)));
            SpawnRadius = Config.Bind("Spawning", "Spawn Radius", 50f,
                new ConfigDescription("Distance from player to spawn creatures", new AcceptableValueRange<float>(20f, 150f)));
            DespawnRadius = Config.Bind("Spawning", "Despawn Radius", 80f,
                new ConfigDescription("Distance from player to despawn creatures", new AcceptableValueRange<float>(30f, 200f)));
            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");

            // Flying Insects
            EnableButterflies = Config.Bind("Flying Insects", "Enable Butterflies", true, "Spawn butterflies");
            EnableFireflies = Config.Bind("Flying Insects", "Enable Fireflies", true, "Spawn fireflies (glow)");
            EnableBeetles = Config.Bind("Flying Insects", "Enable Flying Beetles", true, "Spawn flying beetles");
            EnableNightmareBeetles = Config.Bind("Flying Insects", "Enable Nightmare Beetles", false, "Spawn nightmare beetles (larger, intimidating)");

            // Spiders
            EnableBrownSpiders = Config.Bind("Spiders", "Enable Brown Spiders", true, "Spawn brown spiders (wall crawlers)");
            EnableGreenSpiders = Config.Bind("Spiders", "Enable Green Spiders", true, "Spawn green spiders");
            EnableBlackSpiders = Config.Bind("Spiders", "Enable Black Spiders", false, "Spawn black spiders (larger)");

            // Ground Animals
            EnableDogs = Config.Bind("Ground Animals", "Enable Dogs", true, "Spawn dogs");
            EnableCats = Config.Bind("Ground Animals", "Enable Cats", true, "Spawn cats");
            EnableChickens = Config.Bind("Ground Animals", "Enable Chickens", true, "Spawn chickens");
            EnableDeer = Config.Bind("Ground Animals", "Enable Deer", true, "Spawn deer");
            EnableWolves = Config.Bind("Ground Animals", "Enable Wolves", false, "Spawn wolves");
                        EnablePenguins = Config.Bind("Ground Animals", "Enable Penguins", false, "Spawn penguins");

            // New Ambient Creatures
            EnableSpawnNests = Config.Bind("Alien Life", "Enable Spawn Nests", true, "Spawn alien spawn nests");
            EnableAlienBugs = Config.Bind("Alien Life", "Enable Alien Bugs", true, "Spawn small alien bugs");
            EnableGlowWorms = Config.Bind("Alien Life", "Enable Glow Worms", true, "Spawn bioluminescent glow worms");
            EnableCrystalBeetles = Config.Bind("Alien Life", "Enable Crystal Beetles", true, "Spawn crystalline beetles");
            EnableSporeClouds = Config.Bind("Alien Life", "Enable Spore Clouds", true, "Spawn floating spore organisms");
            EnableAlienMantis = Config.Bind("Alien Life", "Enable Alien Mantis", false, "Spawn larger alien mantis");
            EnableCaveWorms = Config.Bind("Alien Life", "Enable Cave Worms", true, "Spawn underground cave worms");

            // Story-based spawning
            EnableStoryBasedSpawning = Config.Bind("Story", "Enable Story-Based Spawning", true,
                "When enabled, organic creatures die off after reaching the anomaly depth, replaced by mechanical creatures");
            AnomalyDepthThreshold = Config.Bind("Story", "Anomaly Depth Threshold", -200f,
                new ConfigDescription("Y-coordinate depth that triggers the anomaly event (negative = deeper)",
                new AcceptableValueRange<float>(-500f, -50f)));
        }

        private void Update()
        {
            if (!EnableAmbientLife.Value) return;
            if (Player.instance == null) return;

            // Check for story state change (anomaly event)
            if (EnableStoryBasedSpawning.Value)
            {
                CheckAnomalyState();
            }

            // Update spawn timer
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SPAWN_INTERVAL)
            {
                spawnTimer = 0f;
                TrySpawnCreature();
            }

            // Update and clean up creatures
            UpdateCreatures();
        }

        /// <summary>
        /// Check if player has reached anomaly depth threshold
        /// </summary>
        private void CheckAnomalyState()
        {
            if (Player.instance == null) return;

            float playerY = Player.instance.transform.position.y;

            // Track deepest depth reached
            if (playerY < deepestPlayerDepth)
            {
                deepestPlayerDepth = playerY;
            }

            // Check if player has reached anomaly threshold
            bool shouldBePostAnomaly = deepestPlayerDepth <= AnomalyDepthThreshold.Value;

            if (shouldBePostAnomaly && !PostAnomalyState)
            {
                // Trigger anomaly event - kill all organic creatures
                TriggerAnomalyEvent();
            }

            PostAnomalyState = shouldBePostAnomaly;
        }

        /// <summary>
        /// Trigger the anomaly event - despawn all organic creatures with death effect
        /// </summary>
        private void TriggerAnomalyEvent()
        {
            Log.LogInfo("ANOMALY EVENT TRIGGERED - Organic creatures dying off!");

            // Kill all organic creatures
            List<SmartAmbientCreature> toRemove = new List<SmartAmbientCreature>();

            foreach (var creature in ActiveCreatures)
            {
                if (creature == null) continue;

                var def = CreatureDefinitions.TryGetValue(creature.Type, out var d) ? d : null;
                if (def != null && !def.IsMechanical)
                {
                    // Play death effect
                    CreateDeathEffect(creature.transform.position);
                    toRemove.Add(creature);
                }
            }

            // Destroy organic creatures
            foreach (var creature in toRemove)
            {
                if (creature != null && creature.gameObject != null)
                {
                    ActiveCreatures.Remove(creature);
                    Destroy(creature.gameObject);
                }
            }

            Log.LogInfo($"Anomaly killed {toRemove.Count} organic creatures. Mechanical entities will now spawn.");
        }

        /// <summary>
        /// Create a visual death effect for creature dying
        /// </summary>
        private void CreateDeathEffect(Vector3 position)
        {
            // Simple particle/light flash effect
            var effectObj = new GameObject("DeathEffect");
            effectObj.transform.position = position;

            var light = effectObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.5f, 0.3f, 0.1f); // Brown/orange flash
            light.range = 5f;
            light.intensity = 3f;

            // Fade out and destroy
            var fader = effectObj.AddComponent<LightFader>();
            fader.FadeTime = 0.5f;
        }

        private void TrySpawnCreature()
        {
            if (ActiveCreatures.Count >= MaxCreatures.Value) return;

            // Build list of enabled creature types with weights
            List<(CreatureType type, float weight)> enabledTypes = new List<(CreatureType, float)>();

            // Check if we're in post-anomaly state (story-based spawning)
            bool spawnMechanical = EnableStoryBasedSpawning.Value && PostAnomalyState;

            if (spawnMechanical)
            {
                // Post-anomaly: Only spawn mechanical creatures
                enabledTypes.Add((CreatureType.ScoutDrone, 0.35f));
                enabledTypes.Add((CreatureType.RepairBot, 0.25f));
                enabledTypes.Add((CreatureType.SentinelMech, 0.25f));
                enabledTypes.Add((CreatureType.AlienGlider, 0.15f));
            }
            else
            {
                // Pre-anomaly or story spawning disabled: Spawn organic creatures
                // Flying insects
                if (EnableButterflies.Value) enabledTypes.Add((CreatureType.Butterfly, DensityFlying * 0.4f));
                if (EnableFireflies.Value) enabledTypes.Add((CreatureType.Firefly, DensityFlying * 0.3f));
                if (EnableBeetles.Value) enabledTypes.Add((CreatureType.FlyingBeetle, DensityFlying * 0.2f));
                if (EnableNightmareBeetles.Value) enabledTypes.Add((CreatureType.NightmareBeetle, DensityFlying * 0.1f));

                // Spiders
                if (EnableBrownSpiders.Value) enabledTypes.Add((CreatureType.BrownSpider, DensityCrawling * 0.4f));
                if (EnableGreenSpiders.Value) enabledTypes.Add((CreatureType.GreenSpider, DensityCrawling * 0.35f));
                if (EnableBlackSpiders.Value) enabledTypes.Add((CreatureType.BlackSpider, DensityCrawling * 0.25f));

                // Ground animals
                if (EnableDogs.Value) enabledTypes.Add((CreatureType.Dog, DensityAnimals * 0.2f));
                if (EnableCats.Value) enabledTypes.Add((CreatureType.Cat, DensityAnimals * 0.2f));
                if (EnableChickens.Value) enabledTypes.Add((CreatureType.Chicken, DensityAnimals * 0.25f));
                if (EnableDeer.Value) enabledTypes.Add((CreatureType.Deer, DensityAnimals * 0.15f));
                if (EnableWolves.Value) enabledTypes.Add((CreatureType.Wolf, DensityAnimals * 0.1f));
                if (EnablePenguins.Value) enabledTypes.Add((CreatureType.Penguin, DensityAnimals * 0.1f));

                // Alien life
                if (EnableSpawnNests.Value) enabledTypes.Add((CreatureType.SpawnNest, 0.05f)); // Rare
                if (EnableAlienBugs.Value) enabledTypes.Add((CreatureType.AlienBug, 0.25f));
                if (EnableGlowWorms.Value) enabledTypes.Add((CreatureType.GlowWorm, 0.15f));
                if (EnableCrystalBeetles.Value) enabledTypes.Add((CreatureType.CrystalBeetle, 0.15f));
                if (EnableSporeClouds.Value) enabledTypes.Add((CreatureType.SporeCloud, 0.1f));
                if (EnableAlienMantis.Value) enabledTypes.Add((CreatureType.AlienMantis, 0.08f));
                if (EnableCaveWorms.Value) enabledTypes.Add((CreatureType.CaveWorm, 0.12f));
            }

            if (enabledTypes.Count == 0) return;

            // Weighted random selection
            float totalWeight = 0f;
            foreach (var item in enabledTypes) totalWeight += item.weight;

            float roll = UnityEngine.Random.value * totalWeight;
            float cumulative = 0f;
            CreatureType selectedType = enabledTypes[0].type;

            foreach (var item in enabledTypes)
            {
                cumulative += item.weight;
                if (roll <= cumulative)
                {
                    selectedType = item.type;
                    break;
                }
            }

            // Get spawn position
            Vector3 spawnPos = GetSpawnPosition(selectedType);
            if (spawnPos == Vector3.zero) return; // Invalid position

            SpawnCreature(selectedType, spawnPos);
        }

        private Vector3 GetSpawnPosition(CreatureType type)
        {
            if (Player.instance == null) return Vector3.zero;

            Vector3 playerPos = Player.instance.transform.position;
            CreatureDefinition def = CreatureDefinitions[type];

            // Random angle around player
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(SpawnRadius.Value * 0.4f, SpawnRadius.Value);

            Vector3 basePos = playerPos + new Vector3(
                Mathf.Cos(angle) * dist,
                0f,
                Mathf.Sin(angle) * dist
            );

            switch (def.MovementMode)
            {
                case SmartCreatureController.MovementMode.Flying:
                    // High altitude
                    basePos.y = playerPos.y + UnityEngine.Random.Range(5f, 15f);
                    break;

                case SmartCreatureController.MovementMode.Hovering:
                    // Low altitude hovering
                    basePos.y = playerPos.y + UnityEngine.Random.Range(0.5f, 4f);
                    break;

                case SmartCreatureController.MovementMode.Crawling:
                case SmartCreatureController.MovementMode.WallCrawling:
                    // Find ground
                    if (Physics.Raycast(basePos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                    {
                        basePos = hit.point + Vector3.up * 0.1f;
                    }
                    else
                    {
                        basePos.y = playerPos.y;
                    }
                    break;
            }

            // Verify position is valid (not inside geometry)
            if (Physics.CheckSphere(basePos, def.Scale * 0.5f))
            {
                // Try to find clear space
                for (int i = 0; i < 5; i++)
                {
                    Vector3 testPos = basePos + UnityEngine.Random.insideUnitSphere * 3f;
                    if (!Physics.CheckSphere(testPos, def.Scale * 0.5f))
                    {
                        return testPos;
                    }
                }
                return Vector3.zero; // Couldn't find valid position
            }

            return basePos;
        }

        private void SpawnCreature(CreatureType type, Vector3 position)
        {
            CreatureDefinition def = CreatureDefinitions[type];
            GameObject creatureObj = null;

            try
            {
                // Try to spawn from asset bundle first
                creatureObj = CreatureAssetLoader.SpawnCreature(def.PrefabName, position, Quaternion.identity);

                // Fallback to procedural if no prefab
                if (creatureObj == null)
                {
                    creatureObj = CreateProceduralCreature(type, position);
                }

                if (creatureObj != null)
                {
                    // Scale the creature
                    creatureObj.transform.localScale = Vector3.one * def.Scale;

                    // Add smart movement controller
                    SmartCreatureController controller = creatureObj.AddComponent<SmartCreatureController>();
                    if (controller != null)
                    {
                        controller.Initialize(def.MovementMode, def.Speed, 15f);
                    }

                    // Add creature wrapper
                    SmartAmbientCreature creature = creatureObj.AddComponent<SmartAmbientCreature>();
                    if (creature != null)
                    {
                        creature.Initialize(type, def);

                        // Add glow effect for fireflies
                        if (def.HasGlow)
                        {
                            AddGlowEffect(creatureObj);
                        }

                        ActiveCreatures.Add(creature);
                        LogDebug($"Spawned {type} at {position} (prefab: {creatureObj.name})");
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Error spawning {type}: {ex.Message}");
                // Clean up on error
                if (creatureObj != null)
                {
                    try { Destroy(creatureObj); } catch { }
                }
            }
        }

        private GameObject CreateProceduralCreature(CreatureType type, Vector3 position)
        {
            // Fallback procedural creatures when assets aren't available
            GameObject obj = new GameObject($"Ambient_{type}_Procedural");
            obj.transform.position = position;

            CreatureDefinition def = CreatureDefinitions[type];

            // Check if this is a mechanical creature
            if (def.IsMechanical)
            {
                CreateMechanicalVisual(obj, type);
                return obj;
            }

            // Handle new creature types explicitly
            switch (type)
            {
                case CreatureType.SpawnNest:
                    CreateSpawnNestVisual(obj);
                    return obj;
                case CreatureType.AlienBug:
                    CreateAlienBugVisual(obj);
                    return obj;
                case CreatureType.GlowWorm:
                    CreateGlowWormVisual(obj);
                    return obj;
                case CreatureType.CrystalBeetle:
                    CreateCrystalBeetleVisual(obj);
                    return obj;
                case CreatureType.SporeCloud:
                    CreateSporeCloudVisual(obj);
                    return obj;
                case CreatureType.AlienMantis:
                    CreateAlienMantisVisual(obj);
                    return obj;
                case CreatureType.CaveWorm:
                    CreateCaveWormVisual(obj);
                    return obj;
            }

            switch (def.MovementMode)
            {
                case SmartCreatureController.MovementMode.Flying:
                case SmartCreatureController.MovementMode.Hovering:
                    CreateFlyingInsectVisual(obj, type);
                    break;

                case SmartCreatureController.MovementMode.Crawling:
                    CreateGroundAnimalVisual(obj, type);
                    break;

                case SmartCreatureController.MovementMode.WallCrawling:
                    CreateSpiderVisual(obj, type);
                    break;
            }

            return obj;
        }

        /// <summary>
        /// Create procedural mechanical creature visuals
        /// </summary>
        private void CreateMechanicalVisual(GameObject parent, CreatureType type)
        {
            Color mechColor = new Color(0.4f, 0.5f, 0.6f); // Metallic blue-gray
            Color glowColor = new Color(0.2f, 0.8f, 1f);   // Cyan glow

            switch (type)
            {
                case CreatureType.ScoutDrone:
                    CreateDroneVisual(parent, mechColor, glowColor);
                    break;
                case CreatureType.RepairBot:
                    CreateRepairBotVisual(parent, mechColor, glowColor);
                    break;
                case CreatureType.SentinelMech:
                    CreateSentinelVisual(parent, mechColor, glowColor);
                    break;
                case CreatureType.AlienGlider:
                    CreateAlienGliderVisual(parent, mechColor, glowColor);
                    break;
            }
        }

        private void CreateDroneVisual(GameObject parent, Color bodyColor, Color glowColor)
        {
            // Central sphere body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = Vector3.one * 0.3f;
            body.GetComponent<Renderer>().material.color = bodyColor;
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Ring/disc around center
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(parent.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(0.5f, 0.02f, 0.5f);
            ring.GetComponent<Renderer>().material.color = bodyColor * 0.7f;
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            // Sensor eye
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(parent.transform);
            eye.transform.localPosition = new Vector3(0, 0, 0.12f);
            eye.transform.localScale = Vector3.one * 0.1f;
            eye.GetComponent<Renderer>().material.color = glowColor;
            eye.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            eye.GetComponent<Renderer>().material.SetColor("_EmissionColor", glowColor * 2f);
            UnityEngine.Object.Destroy(eye.GetComponent<Collider>());

            // Add hovering light
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = 0.5f;
            light.range = 3f;
        }

        private void CreateRepairBotVisual(GameObject parent, Color bodyColor, Color glowColor)
        {
            // Box body
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = new Vector3(0, 0.15f, 0);
            body.transform.localScale = new Vector3(0.3f, 0.2f, 0.4f);
            body.GetComponent<Renderer>().material.color = bodyColor;
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Sensor dome
            var dome = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dome.transform.SetParent(parent.transform);
            dome.transform.localPosition = new Vector3(0, 0.28f, 0.1f);
            dome.transform.localScale = Vector3.one * 0.12f;
            dome.GetComponent<Renderer>().material.color = glowColor;
            dome.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            dome.GetComponent<Renderer>().material.SetColor("_EmissionColor", glowColor);
            UnityEngine.Object.Destroy(dome.GetComponent<Collider>());

            // Track wheels (simplified as cylinders)
            for (int side = -1; side <= 1; side += 2)
            {
                var track = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                track.transform.SetParent(parent.transform);
                track.transform.localPosition = new Vector3(side * 0.2f, 0.05f, 0);
                track.transform.localScale = new Vector3(0.08f, 0.15f, 0.08f);
                track.transform.localRotation = Quaternion.Euler(0, 0, 90);
                track.GetComponent<Renderer>().material.color = bodyColor * 0.5f;
                UnityEngine.Object.Destroy(track.GetComponent<Collider>());
            }

            // Tool arm
            var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            arm.transform.SetParent(parent.transform);
            arm.transform.localPosition = new Vector3(0, 0.15f, 0.25f);
            arm.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
            arm.transform.localRotation = Quaternion.Euler(70, 0, 0);
            arm.GetComponent<Renderer>().material.color = new Color(0.8f, 0.6f, 0.2f);
            UnityEngine.Object.Destroy(arm.GetComponent<Collider>());
        }

        private void CreateSentinelVisual(GameObject parent, Color bodyColor, Color glowColor)
        {
            // Tall bipedal mech body
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.transform.SetParent(parent.transform);
            torso.transform.localPosition = new Vector3(0, 0.5f, 0);
            torso.transform.localScale = new Vector3(0.35f, 0.4f, 0.25f);
            torso.GetComponent<Renderer>().material.color = bodyColor;
            UnityEngine.Object.Destroy(torso.GetComponent<Collider>());

            // Head/sensor array
            var head = GameObject.CreatePrimitive(PrimitiveType.Cube);
            head.transform.SetParent(parent.transform);
            head.transform.localPosition = new Vector3(0, 0.8f, 0);
            head.transform.localScale = new Vector3(0.2f, 0.15f, 0.18f);
            head.GetComponent<Renderer>().material.color = bodyColor * 0.8f;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            // Visor
            var visor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            visor.transform.SetParent(parent.transform);
            visor.transform.localPosition = new Vector3(0, 0.78f, 0.08f);
            visor.transform.localScale = new Vector3(0.18f, 0.05f, 0.03f);
            visor.GetComponent<Renderer>().material.color = glowColor;
            visor.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            visor.GetComponent<Renderer>().material.SetColor("_EmissionColor", glowColor * 1.5f);
            UnityEngine.Object.Destroy(visor.GetComponent<Collider>());

            // Legs
            for (int side = -1; side <= 1; side += 2)
            {
                var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.transform.SetParent(parent.transform);
                leg.transform.localPosition = new Vector3(side * 0.12f, 0.15f, 0);
                leg.transform.localScale = new Vector3(0.1f, 0.2f, 0.1f);
                leg.GetComponent<Renderer>().material.color = bodyColor * 0.7f;
                UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
            }
        }

        private void CreateAlienGliderVisual(GameObject parent, Color bodyColor, Color glowColor)
        {
            // Elongated crystalline body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.15f, 0.3f, 0.15f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.GetComponent<Renderer>().material.color = new Color(0.3f, 0.2f, 0.5f); // Purple alien
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Membrane wings
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wing.transform.SetParent(parent.transform);
                wing.transform.localPosition = new Vector3(side * 0.3f, 0, 0);
                wing.transform.localScale = new Vector3(0.4f, 0.3f, 1f);
                wing.transform.localRotation = Quaternion.Euler(0, 90, side * 15f);
                wing.GetComponent<Renderer>().material.color = new Color(0.5f, 0.3f, 0.8f, 0.5f);
                UnityEngine.Object.Destroy(wing.GetComponent<Collider>());
            }

            // Glowing core
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(parent.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * 0.1f;
            core.GetComponent<Renderer>().material.color = new Color(1f, 0.5f, 1f);
            core.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            core.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(1f, 0.5f, 1f) * 3f);
            UnityEngine.Object.Destroy(core.GetComponent<Collider>());

            // Trail light
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1f, 0.5f, 1f);
            light.intensity = 1f;
            light.range = 5f;
        }

        private void CreateFlyingInsectVisual(GameObject parent, CreatureType type)
        {
            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Wings
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wing.transform.SetParent(parent.transform);
                wing.transform.localPosition = new Vector3(i * 0.08f, 0, 0);
                wing.transform.localScale = new Vector3(0.15f, 0.1f, 1f);
                wing.transform.localRotation = Quaternion.Euler(0, 90, 0);
                UnityEngine.Object.Destroy(wing.GetComponent<Collider>());

                var renderer = wing.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color wingColor = type switch
                    {
                        CreatureType.Butterfly => new Color(UnityEngine.Random.Range(0.5f, 1f),
                            UnityEngine.Random.Range(0.3f, 0.8f), UnityEngine.Random.Range(0.5f, 1f), 0.9f),
                        CreatureType.FlyingBeetle => new Color(0.2f, 0.15f, 0.1f, 1f),
                        CreatureType.NightmareBeetle => new Color(0.3f, 0.1f, 0.1f, 1f),
                        _ => Color.white
                    };
                    renderer.material.color = wingColor;
                }
            }

            // Color body
            var bodyRenderer = body.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                Color bodyColor = type switch
                {
                    CreatureType.Butterfly => new Color(0.2f, 0.2f, 0.2f),
                    CreatureType.Firefly => new Color(0.1f, 0.1f, 0.1f),
                    CreatureType.FlyingBeetle => new Color(0.3f, 0.2f, 0.1f),
                    CreatureType.NightmareBeetle => new Color(0.4f, 0.1f, 0.1f),
                    _ => Color.gray
                };
                bodyRenderer.material.color = bodyColor;
            }
        }

        private void CreateGroundAnimalVisual(GameObject parent, CreatureType type)
        {
            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.3f, 0.2f, 0.5f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Head
            GameObject head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent.transform);
            head.transform.localPosition = new Vector3(0, 0.1f, 0.3f);
            head.transform.localScale = Vector3.one * 0.2f;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            // Legs (4)
            for (int x = -1; x <= 1; x += 2)
            {
                for (int z = -1; z <= 1; z += 2)
                {
                    GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    leg.transform.SetParent(parent.transform);
                    leg.transform.localPosition = new Vector3(x * 0.15f, -0.1f, z * 0.15f);
                    leg.transform.localScale = new Vector3(0.05f, 0.1f, 0.05f);
                    UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
                }
            }

            // Color based on type
            Color color = type switch
            {
                CreatureType.Dog => new Color(0.6f, 0.4f, 0.2f),
                CreatureType.Cat => new Color(0.8f, 0.6f, 0.4f),
                CreatureType.Chicken => new Color(0.9f, 0.85f, 0.7f),
                CreatureType.Deer => new Color(0.5f, 0.35f, 0.2f),
                CreatureType.Wolf => new Color(0.4f, 0.4f, 0.4f),
                CreatureType.Penguin => new Color(0.1f, 0.1f, 0.1f),
                _ => Color.gray
            };

            foreach (var renderer in parent.GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = color;
            }
        }

        private void CreateSpiderVisual(GameObject parent, CreatureType type)
        {
            // Body (abdomen)
            GameObject abdomen = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            abdomen.transform.SetParent(parent.transform);
            abdomen.transform.localPosition = new Vector3(0, 0, -0.1f);
            abdomen.transform.localScale = new Vector3(0.15f, 0.1f, 0.2f);
            UnityEngine.Object.Destroy(abdomen.GetComponent<Collider>());

            // Cephalothorax
            GameObject ceph = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            ceph.transform.SetParent(parent.transform);
            ceph.transform.localPosition = new Vector3(0, 0, 0.1f);
            ceph.transform.localScale = new Vector3(0.1f, 0.08f, 0.1f);
            UnityEngine.Object.Destroy(ceph.GetComponent<Collider>());

            // Legs (8)
            for (int i = 0; i < 8; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float zOffset = (i / 2 - 1.5f) * 0.05f;

                GameObject leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.transform.SetParent(parent.transform);
                leg.transform.localPosition = new Vector3(side * 0.12f, -0.02f, zOffset);
                leg.transform.localScale = new Vector3(0.01f, 0.08f, 0.01f);
                leg.transform.localRotation = Quaternion.Euler(0, 0, side * 45f);
                UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
            }

            // Color based on type
            Color color = type switch
            {
                CreatureType.BrownSpider => new Color(0.4f, 0.25f, 0.15f),
                CreatureType.GreenSpider => new Color(0.2f, 0.5f, 0.2f),
                CreatureType.BlackSpider => new Color(0.1f, 0.1f, 0.1f),
                _ => Color.gray
            };

            foreach (var renderer in parent.GetComponentsInChildren<Renderer>())
            {
                renderer.material.color = color;
            }
        }

        
        /// <summary>
        /// Create alien spawn nest - a static structure that visually represents creature spawning
        /// </summary>
        private void CreateSpawnNestVisual(GameObject parent)
        {
            // Base mound
            var mound = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            mound.transform.SetParent(parent.transform);
            mound.transform.localPosition = new Vector3(0, -0.2f, 0);
            mound.transform.localScale = new Vector3(1.2f, 0.6f, 1.2f);
            mound.GetComponent<Renderer>().material.color = new Color(0.3f, 0.25f, 0.2f);
            UnityEngine.Object.Destroy(mound.GetComponent<Collider>());

            // Egg sacs
            Color eggColor = new Color(0.4f, 0.6f, 0.3f, 0.8f);
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 60f * Mathf.Deg2Rad;
                var egg = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                egg.transform.SetParent(parent.transform);
                egg.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.4f,
                    0.1f + UnityEngine.Random.Range(0f, 0.1f),
                    Mathf.Sin(angle) * 0.4f
                );
                egg.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.15f, 0.25f);
                egg.GetComponent<Renderer>().material.color = eggColor;
                egg.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                egg.GetComponent<Renderer>().material.SetColor("_EmissionColor", eggColor * 0.5f);
                UnityEngine.Object.Destroy(egg.GetComponent<Collider>());
            }

            // Central opening with glow
            var opening = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            opening.transform.SetParent(parent.transform);
            opening.transform.localPosition = new Vector3(0, 0.1f, 0);
            opening.transform.localScale = new Vector3(0.3f, 0.1f, 0.3f);
            opening.GetComponent<Renderer>().material.color = new Color(0.6f, 0.2f, 0.8f);
            opening.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            opening.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.6f, 0.2f, 0.8f) * 2f);
            UnityEngine.Object.Destroy(opening.GetComponent<Collider>());

            // Add pulsing light
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.2f, 0.8f);
            light.intensity = 1.5f;
            light.range = 5f;

            // Add nest controller for spawning
            parent.AddComponent<SpawnNestController>();
        }

        /// <summary>
        /// Create small alien bug - scuttling insect
        /// </summary>
        private void CreateAlienBugVisual(GameObject parent)
        {
            Color bugColor = new Color(0.2f, 0.4f, 0.3f);

            // Segmented body
            for (int i = 0; i < 3; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                segment.transform.SetParent(parent.transform);
                segment.transform.localPosition = new Vector3(0, 0, i * 0.08f - 0.08f);
                float size = i == 1 ? 0.08f : 0.06f;
                segment.transform.localScale = Vector3.one * size;
                segment.GetComponent<Renderer>().material.color = bugColor;
                UnityEngine.Object.Destroy(segment.GetComponent<Collider>());
            }

            // Legs (6)
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float zOffset = (i / 2 - 1) * 0.05f;

                var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = "Leg_" + i;
                leg.transform.SetParent(parent.transform);
                leg.transform.localPosition = new Vector3(side * 0.06f, -0.02f, zOffset);
                leg.transform.localScale = new Vector3(0.01f, 0.04f, 0.01f);
                leg.transform.localRotation = Quaternion.Euler(0, 0, side * 50f);
                leg.GetComponent<Renderer>().material.color = bugColor * 0.8f;
                UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
            }

            // Antennae
            for (int side = -1; side <= 1; side += 2)
            {
                var antenna = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                antenna.transform.SetParent(parent.transform);
                antenna.transform.localPosition = new Vector3(side * 0.02f, 0.02f, -0.1f);
                antenna.transform.localScale = new Vector3(0.005f, 0.03f, 0.005f);
                antenna.transform.localRotation = Quaternion.Euler(-30, side * 20, 0);
                antenna.GetComponent<Renderer>().material.color = bugColor;
                UnityEngine.Object.Destroy(antenna.GetComponent<Collider>());
            }
        }

        /// <summary>
        /// Create bioluminescent glow worm
        /// </summary>
        private void CreateGlowWormVisual(GameObject parent)
        {
            Color wormColor = new Color(0.2f, 0.8f, 0.4f);

            // Segmented glowing body
            for (int i = 0; i < 8; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                segment.transform.SetParent(parent.transform);
                segment.transform.localPosition = new Vector3(0, 0, i * 0.04f - 0.14f);
                float size = 0.025f + Mathf.Sin(i * 0.5f) * 0.008f;
                segment.transform.localScale = Vector3.one * size;

                var renderer = segment.GetComponent<Renderer>();
                renderer.material.color = wormColor;
                renderer.material.EnableKeyword("_EMISSION");
                float glow = 0.5f + (i / 8f) * 0.5f; // Brighter towards tail
                renderer.material.SetColor("_EmissionColor", wormColor * glow * 2f);
                UnityEngine.Object.Destroy(segment.GetComponent<Collider>());
            }

            // Add light at tail
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = wormColor;
            light.intensity = 0.8f;
            light.range = 3f;
        }

        /// <summary>
        /// Create crystalline beetle
        /// </summary>
        private void CreateCrystalBeetleVisual(GameObject parent)
        {
            Color crystalColor = new Color(0.4f, 0.7f, 1f, 0.8f);

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.12f, 0.08f, 0.15f);
            body.GetComponent<Renderer>().material.color = crystalColor;
            body.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            body.GetComponent<Renderer>().material.SetColor("_EmissionColor", crystalColor * 0.8f);
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Crystal spikes on back
            for (int i = 0; i < 4; i++)
            {
                var crystal = GameObject.CreatePrimitive(PrimitiveType.Cube);
                crystal.transform.SetParent(parent.transform);
                float angle = (i * 45f + 22.5f) * Mathf.Deg2Rad;
                crystal.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 0.04f,
                    0.06f,
                    Mathf.Sin(angle) * 0.05f
                );
                crystal.transform.localScale = new Vector3(0.015f, 0.05f + UnityEngine.Random.Range(0f, 0.03f), 0.015f);
                crystal.transform.localRotation = Quaternion.Euler(UnityEngine.Random.Range(-15f, 15f), 0, UnityEngine.Random.Range(-15f, 15f));
                crystal.GetComponent<Renderer>().material.color = crystalColor;
                crystal.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                crystal.GetComponent<Renderer>().material.SetColor("_EmissionColor", crystalColor * 1.5f);
                UnityEngine.Object.Destroy(crystal.GetComponent<Collider>());
            }

            // Legs
            for (int i = 0; i < 6; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float zOffset = (i / 2 - 1) * 0.06f;

                var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = "Leg_" + i;
                leg.transform.SetParent(parent.transform);
                leg.transform.localPosition = new Vector3(side * 0.08f, -0.02f, zOffset);
                leg.transform.localScale = new Vector3(0.012f, 0.04f, 0.012f);
                leg.transform.localRotation = Quaternion.Euler(0, 0, side * 45f);
                leg.GetComponent<Renderer>().material.color = crystalColor * 0.6f;
                UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
            }

            // Inner glow light
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = crystalColor;
            light.intensity = 0.6f;
            light.range = 2f;
        }

        /// <summary>
        /// Create floating spore cloud organism
        /// </summary>
        private void CreateSporeCloudVisual(GameObject parent)
        {
            Color sporeColor = new Color(0.8f, 0.9f, 0.3f, 0.6f);

            // Central mass
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(parent.transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = Vector3.one * 0.15f;
            core.GetComponent<Renderer>().material.color = sporeColor;
            core.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
            core.GetComponent<Renderer>().material.SetColor("_EmissionColor", sporeColor * 0.5f);
            UnityEngine.Object.Destroy(core.GetComponent<Collider>());

            // Floating spore particles around core
            for (int i = 0; i < 12; i++)
            {
                var spore = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                spore.transform.SetParent(parent.transform);
                spore.transform.localPosition = UnityEngine.Random.insideUnitSphere * 0.25f;
                spore.transform.localScale = Vector3.one * UnityEngine.Random.Range(0.02f, 0.05f);
                spore.GetComponent<Renderer>().material.color = sporeColor;
                spore.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                spore.GetComponent<Renderer>().material.SetColor("_EmissionColor", sporeColor * UnityEngine.Random.Range(0.3f, 0.8f));
                UnityEngine.Object.Destroy(spore.GetComponent<Collider>());

                // Add floating animation component
                var floater = spore.AddComponent<SporeParticle>();
                floater.floatSpeed = UnityEngine.Random.Range(0.5f, 1.5f);
                floater.floatRadius = UnityEngine.Random.Range(0.05f, 0.15f);
            }

            // Ambient light
            var light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = sporeColor;
            light.intensity = 0.4f;
            light.range = 2.5f;
        }

        /// <summary>
        /// Create alien mantis - larger predatory insect
        /// </summary>
        private void CreateAlienMantisVisual(GameObject parent)
        {
            Color mantisColor = new Color(0.5f, 0.3f, 0.6f);

            // Elongated body
            var abdomen = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            abdomen.transform.SetParent(parent.transform);
            abdomen.transform.localPosition = new Vector3(0, 0.1f, -0.1f);
            abdomen.transform.localScale = new Vector3(0.08f, 0.12f, 0.08f);
            abdomen.transform.localRotation = Quaternion.Euler(70, 0, 0);
            abdomen.GetComponent<Renderer>().material.color = mantisColor;
            UnityEngine.Object.Destroy(abdomen.GetComponent<Collider>());

            // Thorax
            var thorax = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            thorax.transform.SetParent(parent.transform);
            thorax.transform.localPosition = new Vector3(0, 0.15f, 0.05f);
            thorax.transform.localScale = new Vector3(0.06f, 0.08f, 0.06f);
            thorax.transform.localRotation = Quaternion.Euler(80, 0, 0);
            thorax.GetComponent<Renderer>().material.color = mantisColor;
            UnityEngine.Object.Destroy(thorax.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent.transform);
            head.transform.localPosition = new Vector3(0, 0.2f, 0.15f);
            head.transform.localScale = new Vector3(0.06f, 0.05f, 0.05f);
            head.GetComponent<Renderer>().material.color = mantisColor;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            // Eyes (glowing)
            Color eyeColor = new Color(1f, 0.3f, 0.3f);
            for (int side = -1; side <= 1; side += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.transform.SetParent(parent.transform);
                eye.transform.localPosition = new Vector3(side * 0.025f, 0.21f, 0.17f);
                eye.transform.localScale = Vector3.one * 0.02f;
                eye.GetComponent<Renderer>().material.color = eyeColor;
                eye.GetComponent<Renderer>().material.EnableKeyword("_EMISSION");
                eye.GetComponent<Renderer>().material.SetColor("_EmissionColor", eyeColor * 2f);
                UnityEngine.Object.Destroy(eye.GetComponent<Collider>());
            }

            // Front raptorial legs (folded)
            for (int side = -1; side <= 1; side += 2)
            {
                // Upper arm
                var upper = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                upper.transform.SetParent(parent.transform);
                upper.transform.localPosition = new Vector3(side * 0.04f, 0.12f, 0.08f);
                upper.transform.localScale = new Vector3(0.015f, 0.05f, 0.015f);
                upper.transform.localRotation = Quaternion.Euler(45, side * 20, 0);
                upper.GetComponent<Renderer>().material.color = mantisColor * 0.9f;
                UnityEngine.Object.Destroy(upper.GetComponent<Collider>());

                // Lower arm (claw)
                var lower = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                lower.transform.SetParent(parent.transform);
                lower.transform.localPosition = new Vector3(side * 0.06f, 0.08f, 0.12f);
                lower.transform.localScale = new Vector3(0.012f, 0.04f, 0.012f);
                lower.transform.localRotation = Quaternion.Euler(-30, side * 10, 0);
                lower.GetComponent<Renderer>().material.color = mantisColor * 0.9f;
                UnityEngine.Object.Destroy(lower.GetComponent<Collider>());
            }

            // Walking legs (4)
            for (int i = 0; i < 4; i++)
            {
                float side = (i % 2 == 0) ? -1f : 1f;
                float zOffset = (i / 2) * 0.08f - 0.06f;

                var leg = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                leg.name = "Leg_" + i;
                leg.transform.SetParent(parent.transform);
                leg.transform.localPosition = new Vector3(side * 0.06f, 0.02f, zOffset);
                leg.transform.localScale = new Vector3(0.012f, 0.06f, 0.012f);
                leg.transform.localRotation = Quaternion.Euler(0, 0, side * 50f);
                leg.GetComponent<Renderer>().material.color = mantisColor * 0.8f;
                UnityEngine.Object.Destroy(leg.GetComponent<Collider>());
            }
        }

        /// <summary>
        /// Create cave worm - segmented underground creature
        /// </summary>
        private void CreateCaveWormVisual(GameObject parent)
        {
            Color wormColor = new Color(0.5f, 0.35f, 0.25f);

            // Segmented body - 10 segments
            for (int i = 0; i < 10; i++)
            {
                var segment = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                segment.transform.SetParent(parent.transform);
                segment.transform.localPosition = new Vector3(0, 0, i * 0.05f - 0.225f);
                float size = 0.04f - Mathf.Abs(i - 4.5f) * 0.003f; // Thicker in middle
                segment.transform.localScale = new Vector3(size, size * 0.8f, size);
                segment.GetComponent<Renderer>().material.color = wormColor;
                UnityEngine.Object.Destroy(segment.GetComponent<Collider>());
            }

            // Head segment with mouth
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(parent.transform);
            head.transform.localPosition = new Vector3(0, 0, 0.28f);
            head.transform.localScale = new Vector3(0.05f, 0.04f, 0.06f);
            head.GetComponent<Renderer>().material.color = wormColor * 0.8f;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            // Mandibles
            for (int side = -1; side <= 1; side += 2)
            {
                var mandible = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                mandible.transform.SetParent(parent.transform);
                mandible.transform.localPosition = new Vector3(side * 0.02f, -0.01f, 0.32f);
                mandible.transform.localScale = new Vector3(0.008f, 0.02f, 0.008f);
                mandible.transform.localRotation = Quaternion.Euler(30, side * 30, 0);
                mandible.GetComponent<Renderer>().material.color = wormColor * 0.6f;
                UnityEngine.Object.Destroy(mandible.GetComponent<Collider>());
            }
        }

        private void AddGlowEffect(GameObject obj)
        {
            // Add point light for glow
            Light light = obj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.8f, 1f, 0.3f);
            light.intensity = 0.8f;
            light.range = 4f;

            // Emissive material on visible parts
            foreach (var renderer in obj.GetComponentsInChildren<Renderer>())
            {
                if (renderer.material != null)
                {
                    renderer.material.EnableKeyword("_EMISSION");
                    renderer.material.SetColor("_EmissionColor", new Color(0.5f, 0.8f, 0.2f) * 2f);
                }
            }
        }

        private void UpdateCreatures()
        {
            if (Player.instance == null) return;
            Vector3 playerPos = Player.instance.transform.position;

            for (int i = ActiveCreatures.Count - 1; i >= 0; i--)
            {
                var creature = ActiveCreatures[i];
                if (creature == null || creature.gameObject == null)
                {
                    ActiveCreatures.RemoveAt(i);
                    continue;
                }

                // Despawn if too far
                float dist = Vector3.Distance(creature.transform.position, playerPos);
                if (dist > DespawnRadius.Value)
                {
                    ActiveCreatures.RemoveAt(i);
                    Destroy(creature.gameObject);
                }
            }
        }

        public void ClearAllCreatures()
        {
            foreach (var creature in ActiveCreatures)
            {
                if (creature != null && creature.gameObject != null)
                {
                    Destroy(creature.gameObject);
                }
            }
            ActiveCreatures.Clear();
            Log.LogInfo("Cleared all ambient creatures");
        }

        private void OnDestroy()
        {
            ClearAllCreatures();
            CreatureAssetLoader.Unload();
        }

        public static void LogDebug(string message)
        {
            if (DebugMode.Value)
                Log.LogInfo($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// Definition for creature behavior and appearance
    /// </summary>
    public class CreatureDefinition
    {
        public string PrefabName { get; }
        public SmartCreatureController.MovementMode MovementMode { get; }
        public float Speed { get; }
        public float Scale { get; }
        public bool HasGlow { get; }
        public bool IsMechanical { get; }

        public CreatureDefinition(string prefabName, SmartCreatureController.MovementMode mode,
            float speed, float scale, bool hasGlow = false, bool isMechanical = false)
        {
            PrefabName = prefabName;
            MovementMode = mode;
            Speed = speed;
            Scale = scale;
            HasGlow = hasGlow;
            IsMechanical = isMechanical;
        }
    }

    /// <summary>
    /// Wrapper component for ambient creatures with type-specific behavior
    /// </summary>
    public class SmartAmbientCreature : MonoBehaviour
    {
        public AmbientLifePlugin.CreatureType Type { get; private set; }
        public CreatureDefinition Definition { get; private set; }

        private SmartCreatureController controller;
        private Animator animator;
        private float animationPhase;

        public void Initialize(AmbientLifePlugin.CreatureType type, CreatureDefinition definition)
        {
            Type = type;
            Definition = definition;
            controller = GetComponent<SmartCreatureController>();
            animator = GetComponent<Animator>();
            animationPhase = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            // Type-specific visual updates
            UpdateVisualEffects();

            // Flee from player if too close (for shy creatures)
            CheckPlayerProximity();
        }

        private void UpdateVisualEffects()
        {
            animationPhase += Time.deltaTime;

            switch (Definition.MovementMode)
            {
                case SmartCreatureController.MovementMode.Flying:
                case SmartCreatureController.MovementMode.Hovering:
                    AnimateWings();
                    break;

                case SmartCreatureController.MovementMode.Crawling:
                    AnimateLegs();
                    break;
            }

            // Firefly glow pulsing
            if (Definition.HasGlow)
            {
                AnimateGlow();
            }
        }

        private void AnimateWings()
        {
            if (animator != null) return; // Let animator handle it

            float flapSpeed = Definition.MovementMode == SmartCreatureController.MovementMode.Hovering ? 20f : 12f;
            float flapAngle = Mathf.Sin(animationPhase * flapSpeed) * 40f;

            foreach (Transform child in transform)
            {
                if (child.name.Contains("Quad") || child.name.ToLower().Contains("wing"))
                {
                    float sign = child.localPosition.x > 0 ? 1f : -1f;
                    child.localRotation = Quaternion.Euler(flapAngle * sign, 90, 0);
                }
            }
        }

        private void AnimateLegs()
        {
            if (animator != null) return;

            // Simple leg movement for procedural creatures
            float walkPhase = animationPhase * 8f;
            int legIndex = 0;
            foreach (Transform child in transform)
            {
                if (child.name.Contains("Capsule") && child.localPosition.y < 0)
                {
                    float offset = (legIndex % 2 == 0) ? 0 : Mathf.PI;
                    float swing = Mathf.Sin(walkPhase + offset) * 15f;
                    child.localRotation = Quaternion.Euler(swing, 0, child.localRotation.eulerAngles.z);
                    legIndex++;
                }
            }
        }

        private void AnimateGlow()
        {
            Light light = GetComponent<Light>();
            if (light != null)
            {
                float pulse = (Mathf.Sin(animationPhase * 2f) + 1f) * 0.5f;
                light.intensity = 0.3f + pulse * 0.7f;
            }
        }

        private void CheckPlayerProximity()
        {
            if (controller == null || Player.instance == null) return;

            float distToPlayer = Vector3.Distance(transform.position, Player.instance.transform.position);

            // Different flee distances for different creature types
            float fleeDistance = Type switch
            {
                AmbientLifePlugin.CreatureType.Deer => 10f,
                AmbientLifePlugin.CreatureType.Chicken => 5f,
                AmbientLifePlugin.CreatureType.BrownSpider => 3f,
                AmbientLifePlugin.CreatureType.GreenSpider => 3f,
                AmbientLifePlugin.CreatureType.BlackSpider => 4f,
                AmbientLifePlugin.CreatureType.Butterfly => 2f,
                _ => 0f // No fleeing for dogs, cats, wolves, etc.
            };

            if (fleeDistance > 0 && distToPlayer < fleeDistance)
            {
                controller.Flee(Player.instance.transform.position, fleeDistance * 2f);
            }
        }
    }

    /// <summary>
    /// Helper component for fading out lights over time
    /// </summary>
    public class LightFader : MonoBehaviour
    {
        public float FadeTime { get; set; } = 1f;

        private Light targetLight;
        private float startIntensity;
        private float elapsed;

        private void Awake()
        {
            targetLight = GetComponent<Light>();
            if (targetLight != null)
            {
                startIntensity = targetLight.intensity;
            }
        }

        private void Update()
        {
            elapsed += Time.deltaTime;

            if (targetLight != null)
            {
                float t = elapsed / FadeTime;
                targetLight.intensity = Mathf.Lerp(startIntensity, 0f, t);
            }

            if (elapsed >= FadeTime)
            {
                Destroy(gameObject);
            }
        }
    }
    /// <summary>
    /// Controller for spawn nests that periodically spawns creatures
    /// </summary>
    public class SpawnNestController : MonoBehaviour
    {
        private float spawnTimer = 0f;
        private float spawnInterval = 15f; // Spawn every 15 seconds
        private int maxSpawns = 3;
        private int currentSpawns = 0;

        private void Update()
        {
            if (currentSpawns >= maxSpawns) return;

            spawnTimer += Time.deltaTime;
            if (spawnTimer >= spawnInterval)
            {
                spawnTimer = 0f;
                SpawnCreature();
            }
        }

        private void SpawnCreature()
        {
            // Spawn a small creature near the nest
            Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
            spawnPos.y = transform.position.y + 0.5f;

            // Pick a small creature type to spawn
            AmbientLifePlugin.CreatureType[] spawnableTypes = new[]
            {
                AmbientLifePlugin.CreatureType.AlienBug,
                AmbientLifePlugin.CreatureType.GlowWorm,
                AmbientLifePlugin.CreatureType.CrystalBeetle
            };

            var type = spawnableTypes[UnityEngine.Random.Range(0, spawnableTypes.Length)];

            // Create visual effect
            CreateSpawnEffect(spawnPos);

            currentSpawns++;
            AmbientLifePlugin.LogDebug($"Nest spawned {type} (#{currentSpawns}/{maxSpawns})");
        }

        private void CreateSpawnEffect(Vector3 position)
        {
            var effectObj = new GameObject("NestSpawnEffect");
            effectObj.transform.position = position;

            var light = effectObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.2f, 0.8f);
            light.intensity = 3f;
            light.range = 4f;

            var fader = effectObj.AddComponent<LightFader>();
            fader.FadeTime = 1f;
        }
    }

    /// <summary>
    /// Makes spore particles float around their parent
    /// </summary>
    public class SporeParticle : MonoBehaviour
    {
        public float floatSpeed = 1f;
        public float floatRadius = 0.1f;

        private Vector3 startLocalPos;
        private float timeOffset;

        private void Start()
        {
            startLocalPos = transform.localPosition;
            timeOffset = UnityEngine.Random.value * Mathf.PI * 2f;
        }

        private void Update()
        {
            float t = Time.time * floatSpeed + timeOffset;
            Vector3 offset = new Vector3(
                Mathf.Sin(t) * floatRadius,
                Mathf.Sin(t * 1.3f) * floatRadius * 0.5f,
                Mathf.Cos(t * 0.7f) * floatRadius
            );
            transform.localPosition = startLocalPos + offset;
        }
    }
}