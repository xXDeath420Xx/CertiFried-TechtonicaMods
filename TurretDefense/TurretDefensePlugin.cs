using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using TechtonicaFramework.TechTree;
using TechtonicaFramework.BuildMenu;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// TurretDefense v2.1 - Full Combat System with proper machine integration
    /// Features: Buildable turrets, alien waves, damage integration, building targeting
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class TurretDefensePlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.TurretDefense";
        public const string PluginName = "TurretDefense";
        public const string VersionString = "4.5.4";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static TurretDefensePlugin Instance;
        public static string PluginPath;

        // ========== CONFIGURATION ==========
        public static ConfigEntry<bool> EnableTurrets;
        public static ConfigEntry<bool> EnableAlienWaves;
        public static ConfigEntry<float> TurretDamage;
        public static ConfigEntry<float> TurretRange;
        public static ConfigEntry<float> WaveInterval;
        public static ConfigEntry<bool> WaveAutoStart;
        public static ConfigEntry<float> InitialWaveDelay;
        public static ConfigEntry<int> BaseAliensPerWave;
        public static ConfigEntry<float> DifficultyScaling;
        public static ConfigEntry<bool> ShowHealthBars;
        public static ConfigEntry<bool> ShowDamageNumbers;
        public static ConfigEntry<bool> EnableLootDrops;
        public static ConfigEntry<KeyCode> TestSpawnKey;
        public static ConfigEntry<KeyCode> SpawnWaveKey;
        public static ConfigEntry<KeyCode> SpawnTurretKey;
        public static ConfigEntry<bool> DebugMode;
        public static ConfigEntry<float> AlienDamageToPlayer;
        public static ConfigEntry<float> AlienDamageToBuildings;

        // ========== ASSET MANAGEMENT ==========
        public static AssetBundleLoader AssetLoader;
        public static Material DefaultMaterial;
        public static Material ParticleMaterial;
        public static Material LineMaterial;
        public static Shader CapturedLitShader;
        public static Shader CapturedUnlitShader;

        // ========== MACHINE NAMES (for buildable turrets) ==========
        public const string GatlingTurretMachine = "Gatling Turret";
        public const string RocketTurretMachine = "Rocket Turret";
        public const string LaserTurretMachine = "Laser Turret";
        public const string RailgunTurretMachine = "Railgun Turret";
        public const string LightningTurretMachine = "Lightning Turret";

        // Track machine definition IDs
        public static int GatlingDefId = -1;
        public static int RocketDefId = -1;
        public static int LaserDefId = -1;
        public static int RailgunDefId = -1;
        public static int LightningDefId = -1;

        // ========== ITEM NAMES ==========
        public const string TurretAmmo = "Turret Ammunition";
        public const string TurretUpgradeKit = "Turret Upgrade Kit";
        public const string TurretUnlock = "Automated Defense";
        public const string AdvancedTurretUnlock = "Advanced Defense Systems";
        public const string AlienCoreName = "Alien Power Core";
        public const string AlienAlloyName = "Alien Alloy";

        // ========== ACTIVE ENTITIES ==========
        public static List<TurretController> ActiveTurrets = new List<TurretController>();
        public static List<ArtilleryController> ActiveArtillery = new List<ArtilleryController>();
        public static List<AlienShipController> ActiveAliens = new List<AlienShipController>();
        public static List<GroundEnemyController> ActiveGroundEnemies = new List<GroundEnemyController>();
        public static List<AlienHiveController> ActiveHives = new List<AlienHiveController>();
        public static List<BossController> ActiveBosses = new List<BossController>();
        public static List<FloatingHealthBar> ActiveHealthBars = new List<FloatingHealthBar>();
        public static List<DamageNumber> ActiveDamageNumbers = new List<DamageNumber>();

        // ========== WAVE SYSTEM ==========
        public static WaveManager WaveSystem;
        public static int TotalKills = 0;
        public static int CurrentScore = 0;

        // ========== SPAWN POINTS ==========
        private static List<Vector3> spawnPointOffsets = new List<Vector3>();

        // ========== CUSTOM ICONS ==========
        private static Dictionary<string, Sprite> customIcons = new Dictionary<string, Sprite>();

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            InitializeSpawnPoints();
            Harmony.PatchAll();

            AssetLoader = new AssetBundleLoader(PluginPath);
            LoadCustomIcons();

            RegisterContent();

            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.GameLoaded += OnGameLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableTurrets = Config.Bind("General", "Enable Turrets", true, "Enable defensive turret machines");
            EnableAlienWaves = Config.Bind("Aliens", "Enable Alien Waves", true, "Enable periodic alien ship attacks");

            TurretDamage = Config.Bind("Turrets", "Base Damage", 25f,
                new ConfigDescription("Base damage dealt by turrets", new AcceptableValueRange<float>(1f, 500f)));
            TurretRange = Config.Bind("Turrets", "Base Range", 35f,
                new ConfigDescription("Base targeting range for turrets", new AcceptableValueRange<float>(5f, 100f)));

            WaveInterval = Config.Bind("Waves", "Wave Interval (seconds)", 180f,
                new ConfigDescription("Time between alien waves", new AcceptableValueRange<float>(30f, 600f)));
            WaveAutoStart = Config.Bind("Waves", "Auto Start Waves", false,
                "Automatically start alien waves when game loads. If false, use Numpad8 to manually trigger first wave.");
            InitialWaveDelay = Config.Bind("Waves", "Initial Wave Delay (seconds)", 300f,
                new ConfigDescription("Delay before first wave when auto-start is enabled", new AcceptableValueRange<float>(30f, 900f)));
            BaseAliensPerWave = Config.Bind("Waves", "Base Aliens Per Wave", 5,
                new ConfigDescription("Starting number of aliens per wave", new AcceptableValueRange<int>(1, 30)));
            DifficultyScaling = Config.Bind("Waves", "Difficulty Scaling", 1.2f,
                new ConfigDescription("Multiplier for each wave", new AcceptableValueRange<float>(1f, 2f)));

            ShowHealthBars = Config.Bind("UI", "Show Health Bars", true, "Show floating health bars above enemies");
            ShowDamageNumbers = Config.Bind("UI", "Show Damage Numbers", true, "Show floating damage numbers");
            EnableLootDrops = Config.Bind("Loot", "Enable Loot Drops", true, "Aliens drop resources on death");

            AlienDamageToPlayer = Config.Bind("Combat", "Alien Damage to Player", 1.0f,
                new ConfigDescription("Multiplier for alien damage to player", new AcceptableValueRange<float>(0f, 5f)));
            AlienDamageToBuildings = Config.Bind("Combat", "Alien Damage to Buildings", 1.0f,
                new ConfigDescription("Multiplier for alien damage to buildings", new AcceptableValueRange<float>(0f, 5f)));

            TestSpawnKey = Config.Bind("Debug", "Test Spawn Key", KeyCode.Keypad7, "Spawn a single alien (Numpad 7)");
            SpawnWaveKey = Config.Bind("Debug", "Spawn Wave Key", KeyCode.Keypad8, "Force spawn a wave (Numpad 8)");
            SpawnTurretKey = Config.Bind("Debug", "Spawn Turret Key", KeyCode.Keypad9, "Spawn a test turret (Numpad 9)");
            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void InitializeSpawnPoints()
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                spawnPointOffsets.Add(new Vector3(Mathf.Cos(angle), 0.3f, Mathf.Sin(angle)));
            }
        }

        /// <summary>
        /// Load sprite from embedded resource PNG file
        /// </summary>
        private static Sprite LoadEmbeddedSprite(string resourceName, int width = 256, int height = 256)
        {
            try
            {
                var assembly = Assembly.GetExecutingAssembly();
                string fullName = assembly.GetManifestResourceNames()
                    .FirstOrDefault(n => n.EndsWith(resourceName));

                if (string.IsNullOrEmpty(fullName))
                {
                    LogDebug($"Embedded resource not found: {resourceName}");
                    return null;
                }

                using (var stream = assembly.GetManifestResourceStream(fullName))
                {
                    if (stream == null) return null;

                    byte[] data = new byte[stream.Length];
                    stream.Read(data, 0, data.Length);

                    Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
                    tex.filterMode = FilterMode.Bilinear;

                    // Use LoadImage (extension method from UnityEngine)
                    if (tex.LoadImage(data))
                    {
                        tex.Apply();
                        Sprite sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f);
                        LogDebug($"Loaded embedded sprite: {resourceName} ({tex.width}x{tex.height})");
                        return sprite;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to load embedded sprite {resourceName}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Load all custom icons from embedded resources
        /// </summary>
        private void LoadCustomIcons()
        {
            // Turret icons - mapped to our turret machine names (embedded resources first)
            customIcons["Gatling Turret"] = LoadEmbeddedSprite("Icon_MGTower.png");
            customIcons["Rocket Turret"] = LoadEmbeddedSprite("Icon_MissileTower.png");
            customIcons["Laser Turret"] = LoadEmbeddedSprite("Icon_LaserTower.png");
            customIcons["Railgun Turret"] = LoadEmbeddedSprite("Icon_CannonTower.png");
            customIcons["Lightning Turret"] = LoadEmbeddedSprite("Icon_AOETower.png");

            // Alien drops - use alien texture thumbnails if available
            customIcons["Alien Power Core"] = LoadEmbeddedSprite("AlienDestroyer_AlbedoTransparency.png");
            customIcons["Alien Alloy"] = LoadEmbeddedSprite("AlienFighter_AlbedoTransparency.png");

            int loaded = customIcons.Values.Count(s => s != null);
            Log.LogInfo($"Loaded {loaded}/{customIcons.Count} custom icons from embedded resources");
        }

        /// <summary>
        /// Load icons from asset bundles (called after bundles are loaded)
        /// </summary>
        private void LoadBundleIcons()
        {
            int loaded = 0;

            // Ammo icon from Navidtbt bullet icons
            var ammoSprite = AssetLoader.GetSprite("icons_ammo", "Bullet1");
            if (ammoSprite != null)
            {
                customIcons[TurretAmmo] = ammoSprite;
                loaded++;
            }

            // High-quality turret icons from PBR texture bundles
            // Each bundle contains L1, L2, L3 variants - use L2 as default (middle tier)
            var turretIconMappings = new Dictionary<string, (string bundle, string texture)>
            {
                // Gatling uses PBRTurrets GatlingGun textures
                { "Gatling Turret", ("icons_turret_gatling", "GatlingGun_L2_Low_ColorChanging_AlbedoTransparency") },
                // Rocket uses RocketLauncher textures
                { "Rocket Turret", ("icons_turret_rocket", "RocketLauncher_L2_Low_ColorChanging_AlbedoTransparency") },
                // Laser uses Flamethrower textures (similar energy weapon aesthetic)
                { "Laser Turret", ("icons_turret_laser", "FlameThrower_L2_Low_ColorChanging_AlbedoTransparency") },
                // Railgun uses RailGun textures
                { "Railgun Turret", ("icons_turret_railgun", "RailGun_L2_Low_ColorChanging_AlbedoTransparency") },
                // Lightning uses LighteningGun textures
                { "Lightning Turret", ("icons_turret_lightning", "LighteningGun_Base_L2_Low_ColorChanging_AlbedoTransparency") }
            };

            foreach (var mapping in turretIconMappings)
            {
                var sprite = AssetLoader.GetSprite(mapping.Value.bundle, mapping.Value.texture);
                if (sprite != null)
                {
                    customIcons[mapping.Key] = sprite;
                    loaded++;
                    LogDebug($"Loaded PBR turret icon for {mapping.Key}: {mapping.Value.texture}");
                }
                else
                {
                    // Fallback to Skymon icons if PBR bundle fails
                    var fallbackMappings = new Dictionary<string, string>
                    {
                        { "Gatling Turret", "tower" },
                        { "Rocket Turret", "rocket" },
                        { "Laser Turret", "bolt" },
                        { "Railgun Turret", "meteor" },
                        { "Lightning Turret", "lightning" }
                    };

                    if (fallbackMappings.TryGetValue(mapping.Key, out var fallbackName))
                    {
                        var fallbackSprite = AssetLoader.GetSprite("icons_skymon", fallbackName);
                        if (fallbackSprite != null)
                        {
                            customIcons[mapping.Key] = fallbackSprite;
                            loaded++;
                            LogDebug($"Loaded fallback icon for {mapping.Key}: {fallbackName}");
                        }
                    }
                }
            }

            Log.LogInfo($"Loaded {loaded} turret icons from asset bundles");
        }

        /// <summary>
        /// Apply custom icons to resource definitions using reflection
        /// </summary>
        private void ApplyCustomIcons()
        {
            // Try multiple possible field names for sprites
            var possibleFields = new[] { "rawSprite", "_sprite", "sprite", "<sprite>k__BackingField" };
            FieldInfo spriteField = null;

            foreach (var fieldName in possibleFields)
            {
                spriteField = typeof(ResourceInfo).GetField(fieldName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                if (spriteField != null)
                {
                    LogDebug($"Found sprite field: {fieldName}");
                    break;
                }
            }

            // Fallback sprites from existing game resources
            var fallbackSprites = new Dictionary<string, string>
            {
                { "Gatling Turret", "Sentry" },
                { "Rocket Turret", "Mining Charge" },
                { "Laser Turret", "High Voltage Cable" },
                { "Railgun Turret", "Railrunner" },
                { "Lightning Turret", "Shiverthorn Coolant" },
                { TurretAmmo, "Iron Ingot" },
                { TurretUpgradeKit, "Iron Components" },
                { AlienCoreName, "Research Core (Blue)" },
                { AlienAlloyName, "Steel Ingot" }
            };

            foreach (var kvp in customIcons)
            {
                var resourceInfo = EMU.Resources.GetResourceInfoByName(kvp.Key);
                if (resourceInfo == null) continue;

                Sprite spriteToApply = kvp.Value;

                // If custom icon is null, try to clone from a game resource
                if (spriteToApply == null && fallbackSprites.TryGetValue(kvp.Key, out string fallbackName))
                {
                    var fallbackResource = EMU.Resources.GetResourceInfoByName(fallbackName);
                    if (fallbackResource != null && fallbackResource.sprite != null)
                    {
                        spriteToApply = fallbackResource.sprite;
                        Log.LogInfo($"Using fallback sprite from {fallbackName} for {kvp.Key}");
                    }
                }

                if (spriteToApply == null) continue;

                // Try to set the sprite field
                if (spriteField != null)
                {
                    try
                    {
                        spriteField.SetValue(resourceInfo, spriteToApply);
                        LogDebug($"Applied custom icon to {kvp.Key}");
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Failed to set sprite for {kvp.Key}: {ex.Message}");
                    }
                }
            }

            // Also apply fallback sprites for resources that didn't have custom icons registered
            foreach (var kvp in fallbackSprites)
            {
                if (customIcons.ContainsKey(kvp.Key) && customIcons[kvp.Key] != null) continue;

                var resourceInfo = EMU.Resources.GetResourceInfoByName(kvp.Key);
                if (resourceInfo == null || resourceInfo.sprite != null) continue;

                var fallbackResource = EMU.Resources.GetResourceInfoByName(kvp.Value);
                if (fallbackResource?.sprite != null && spriteField != null)
                {
                    try
                    {
                        spriteField.SetValue(resourceInfo, fallbackResource.sprite);
                        Log.LogInfo($"Applied fallback sprite from {kvp.Value} to {kvp.Key}");
                    }
                    catch { }
                }
            }
        }

        private void RegisterContent()
        {
            if (!EnableTurrets.Value) return;

            // ========== UNLOCKS ==========
            // Use Modded category (index 7) - provided by TechtonicaFramework
            // TESTING MODE: Set to 0 cores for free unlocks
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 0, // FREE FOR TESTING
                description = "Alien ships have been detected in orbit. Research automated defense turrets to protect your factory from alien raiders. Warning: Deploying turrets may attract more hostile attention.",
                displayName = TurretUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier0,
                treePosition = 10
            });

            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 0, // FREE FOR TESTING
                description = "Advanced energy-based defense systems utilizing recovered alien technology. Higher power draw but devastating effectiveness against armored targets.",
                displayName = AdvancedTurretUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier0,
                treePosition = 30
            });

            // ========== ALIEN DROPS (Resources) ==========
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = AlienCoreName,
                description = "A pulsing energy core salvaged from destroyed alien ships. Contains unknown power generation technology that PALADIN is eager to study.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 100,
                sortPriority = 250,
                unlockName = TurretUnlock,
                parentName = "Shiverthorn Extract"
            });

            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = AlienAlloyName,
                description = "Strange metallic alloy from alien hull plating. Incredibly durable and lightweight. Could revolutionize construction techniques.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 200,
                sortPriority = 251,
                unlockName = TurretUnlock,
                parentName = "Steel Mixture"
            });

            // ========== TURRET AMMUNITION ==========
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = TurretAmmo,
                description = "High-velocity ammunition for ballistic turrets. Each Gatling and Rocket turret consumes ammo while firing.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 500,
                sortPriority = 200,
                unlockName = TurretUnlock,
                parentName = "Iron Ingot"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_ammo",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 5f,
                unlockName = TurretUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Iron Ingot", 5),
                    new RecipeResourceInfo("Copper Ingot", 2)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(TurretAmmo, 50)
                },
                sortPriority = 200
            });

            // ========== TURRET UPGRADE KIT ==========
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = TurretUpgradeKit,
                description = "Upgrade kit to enhance turret damage, range, and fire rate. Apply to any turret for improved performance.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 20,
                sortPriority = 205,
                unlockName = AdvancedTurretUnlock,
                parentName = "Processor Unit"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_upgrade",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 30f,
                unlockName = AdvancedTurretUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    // NOTE: Using vanilla resources to avoid IndexOutOfRangeException
                    // Modded resources as ingredients cause crafting UI crashes
                    new RecipeResourceInfo("Shiverthorn Extract", 10),
                    new RecipeResourceInfo("Steel Mixture", 15),
                    new RecipeResourceInfo("Processor Unit", 3)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(TurretUpgradeKit, 1)
                },
                sortPriority = 205
            });

            // ========== TURRET MACHINES (proper buildable machines with power) ==========
            RegisterTurretMachine(GatlingTurretMachine,
                "High fire rate defense turret. Excellent against swarms. Connects to power grid (50kW).",
                TurretUnlock, 210, 50, new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Limestone", 1)  // FREE FOR TESTING
                });

            RegisterTurretMachine(RocketTurretMachine,
                "Explosive rocket turret with area damage. High damage, slow fire rate. Connects to power grid (80kW).",
                TurretUnlock, 211, 80, new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Limestone", 1)  // FREE FOR TESTING
                });

            RegisterTurretMachine(LaserTurretMachine,
                "Continuous beam laser turret. Perfect accuracy, no ammo required. Connects to power grid (120kW).",
                AdvancedTurretUnlock, 220, 120, new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Limestone", 1)  // FREE FOR TESTING
                });

            RegisterTurretMachine(RailgunTurretMachine,
                "Electromagnetic railgun turret. Extreme range and damage. Connects to power grid (200kW).",
                AdvancedTurretUnlock, 221, 200, new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Limestone", 1)  // FREE FOR TESTING
                });

            RegisterTurretMachine(LightningTurretMachine,
                "Tesla coil turret. Chain lightning hits multiple targets. Connects to power grid (150kW).",
                AdvancedTurretUnlock, 222, 150, new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Limestone", 1)  // FREE FOR TESTING
                });

            Log.LogInfo("All turret machines registered as proper buildings!");
        }

        private void RegisterTurretMachine(string name, string desc, string unlock, int priority, int powerKW, List<RecipeResourceInfo> ingredients)
        {
            // Create a PowerGeneratorDefinition for the turret (we'll make it consume power via patch)
            var turretDef = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();
            turretDef.usesFuel = false;
            turretDef.isCrankDriven = false;
            turretDef.powerDuration = 0f;
            turretDef.torqueDemand = 0;

            // Register as a proper machine using Crank Generator as visual base
            // This makes turrets appear in the Power/Energy building tab
            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(turretDef, new NewResourceDetails
            {
                name = name,
                description = $"{desc}\n\nPower Draw: {powerKW} kW",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",      // Main category header
                maxStackCount = 10,
                sortPriority = priority,
                unlockName = unlock,
                parentName = "Crank Generator"        // Use Crank Generator as visual base for Power tab placement
            });

            // Add crafting recipe
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_" + name.Replace(" ", "").ToLower(),
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 45f,
                unlockName = unlock,
                ingredients = ingredients,
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(name, 1)
                },
                sortPriority = priority
            });

            LogDebug($"Registered turret machine: {name} ({powerKW}kW)");
        }

        private void OnGameDefinesLoaded()
        {
            LoadAssetBundles();
            LoadBundleIcons();
            InitializeMaterials();
            ApplyCustomIcons();

            // Track turret machine definition IDs
            var gatlingInfo = EMU.Resources.GetResourceInfoByName(GatlingTurretMachine);
            if (gatlingInfo != null)
            {
                GatlingDefId = gatlingInfo.uniqueId;
                gatlingInfo.unlock = EMU.Unlocks.GetUnlockByName(TurretUnlock);
                LogDebug($"Gatling Turret registered with ID: {GatlingDefId}");
            }

            var rocketInfo = EMU.Resources.GetResourceInfoByName(RocketTurretMachine);
            if (rocketInfo != null)
            {
                RocketDefId = rocketInfo.uniqueId;
                rocketInfo.unlock = EMU.Unlocks.GetUnlockByName(TurretUnlock);
                LogDebug($"Rocket Turret registered with ID: {RocketDefId}");
            }

            var laserInfo = EMU.Resources.GetResourceInfoByName(LaserTurretMachine);
            if (laserInfo != null)
            {
                LaserDefId = laserInfo.uniqueId;
                laserInfo.unlock = EMU.Unlocks.GetUnlockByName(AdvancedTurretUnlock);
                LogDebug($"Laser Turret registered with ID: {LaserDefId}");
            }

            var railgunInfo = EMU.Resources.GetResourceInfoByName(RailgunTurretMachine);
            if (railgunInfo != null)
            {
                RailgunDefId = railgunInfo.uniqueId;
                railgunInfo.unlock = EMU.Unlocks.GetUnlockByName(AdvancedTurretUnlock);
                LogDebug($"Railgun Turret registered with ID: {RailgunDefId}");
            }

            var lightningInfo = EMU.Resources.GetResourceInfoByName(LightningTurretMachine);
            if (lightningInfo != null)
            {
                LightningDefId = lightningInfo.uniqueId;
                lightningInfo.unlock = EMU.Unlocks.GetUnlockByName(AdvancedTurretUnlock);
                LogDebug($"Lightning Turret registered with ID: {LightningDefId}");
            }
        }

        private void OnTechTreeStateLoaded()
        {
            // TESTING MODE: Keep at Tier0 for free unlocks
            // Production tiers: Turret Systems = Tier13 (XRAY), Advanced Defense = Tier15 (XRAY)
            ConfigureUnlock(TurretUnlock, GatlingTurretMachine, TechTreeState.ResearchTier.Tier0, 10);
            ConfigureUnlock(AdvancedTurretUnlock, RailgunTurretMachine, TechTreeState.ResearchTier.Tier0, 30);

            Log.LogInfo("TurretDefense: TESTING MODE - Unlocks at Tier0 (free)");
        }

        private void ConfigureUnlock(string unlockName, string spriteSource, TechTreeState.ResearchTier tier, int position)
        {
            try
            {
                Unlock unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                if (unlock == null) return;
                unlock.requiredTier = tier;
                unlock.treePosition = position;
                if (unlock.sprite == null)
                {
                    // Try the specified sprite source first
                    var sourceRes = EMU.Resources.GetResourceInfoByName(spriteSource);
                    if (sourceRes?.sprite != null)
                    {
                        unlock.sprite = sourceRes.sprite;
                    }
                    else
                    {
                        // Fallback to reliable game resources based on unlock type
                        string[] fallbackSources = unlockName == TurretUnlock
                            ? new[] { "Sentry", "Mining Charge", "Iron Frame", "Steel Frame" }
                            : new[] { "High Voltage Cable", "Railrunner", "Processor Unit", "Steel Frame" };

                        foreach (var fallback in fallbackSources)
                        {
                            var fallbackRes = EMU.Resources.GetResourceInfoByName(fallback);
                            if (fallbackRes?.sprite != null)
                            {
                                unlock.sprite = fallbackRes.sprite;
                                Log.LogInfo($"Applied fallback sprite from {fallback} to unlock {unlockName}");
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure unlock {unlockName}: {ex.Message}");
            }
        }

        private void OnGameLoaded()
        {
            // Initialize wave manager
            var waveObj = new GameObject("WaveManager");
            UnityEngine.Object.DontDestroyOnLoad(waveObj);
            WaveSystem = waveObj.AddComponent<WaveManager>();

            if (EnableAlienWaves.Value && WaveAutoStart.Value)
            {
                WaveSystem.StartWaveSystem(InitialWaveDelay.Value);
                Log.LogInfo($"Wave system auto-started. First wave in {InitialWaveDelay.Value} seconds.");
            }
            else if (EnableAlienWaves.Value)
            {
                Log.LogInfo("Wave system ready. Press Numpad8 to start first wave manually.");
            }

            // Hook into turret machines being placed
            SetupTurretMachineHooks();

            Log.LogInfo("TurretDefense: Game loaded, defense systems active");
        }

        private void SetupTurretMachineHooks()
        {
            // This will be called when machines are placed
            // We'll use Harmony patches to detect when turret machines are built
            LogDebug("Turret machine hooks initialized");
        }

        private void LoadAssetBundles()
        {
            string bundlesPath = Path.Combine(PluginPath, "Bundles");
            if (!Directory.Exists(bundlesPath))
            {
                Log.LogWarning($"Bundles directory not found at {bundlesPath}");
                return;
            }

            // Get list of already loaded bundles (from other mods like EnhancedLogistics)
            var loadedBundles = new HashSet<string>();
            foreach (var bundle in AssetBundle.GetAllLoadedAssetBundles())
            {
                if (bundle != null)
                    loadedBundles.Add(bundle.name.ToLower());
            }

            string[] bundleNames = {
                // Turrets
                "autarca_ships", "laser_cannon",
                "turrets_gatling", "turrets_rocket", "turrets_railgun", "turrets_lightning",
                // Enemies
                "drones_scifi",           // Robot enemies from Sci_fi_Drones pack (may be loaded by EnhancedLogistics)
                "enemy_robots",           // MachineGunRobot, CannonMachine from Enemy Robots pack
                "creatures_gamekit",      // Chomper, Spitter, Grenadier, Gunner from 3D Game Kit
                "creatures_tortoise",     // Tortoise Boss models + particles + audio (3 color variants)
                "creature_mimic",         // Mimic ambush enemy
                "creature_arachnid",      // Arachnid alien spider
                // Hive structures
                "alien_buildings",        // Birther, Brain, Stomach, Terraformer, Claw, Crystal
                "cthulhu_idols",          // Boss spawner structures
                // Hazards
                "mushroom_forest",        // Hostile flora
                "lava_plants",            // Hazardous plants
                // UI Icons
                "icons_ammo",             // Bullet, grenade, heavy gun icons from Navidtbt
                "icons_skymon",           // Game-themed icons (tower, rocket, shield, etc.)
                "icons_heathen"           // Flat UI icons
            };
            foreach (var bundleName in bundleNames)
            {
                // Skip if already loaded by another mod
                if (loadedBundles.Contains(bundleName.ToLower()))
                {
                    LogDebug($"Asset bundle already loaded (by another mod): {bundleName}");
                    continue;
                }

                if (AssetLoader.LoadBundle(bundleName))
                    LogDebug($"Loaded asset bundle: {bundleName}");
            }
        }

        private void InitializeMaterials()
        {
            // Capture shaders from the actual game materials - this is the ONLY way to guarantee URP compatibility
            try
            {
                Log.LogInfo("Initializing materials - searching for game shaders...");

                // Search for a lit shader from game renderers
                var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
                        continue;

                    var shader = renderer.sharedMaterial.shader;
                    string shaderName = shader.name;

                    // Skip error/hidden shaders
                    if (shaderName.Contains("Error") || shaderName.Contains("Hidden") || shaderName.Contains("Internal"))
                        continue;

                    // Capture lit shader (for solid objects)
                    if (CapturedLitShader == null && (shaderName.Contains("Lit") || shaderName.Contains("Standard") || shaderName.Contains("URP")))
                    {
                        CapturedLitShader = shader;
                        DefaultMaterial = new Material(renderer.sharedMaterial);
                        DefaultMaterial.name = "TurretDefense_Default";
                        Log.LogInfo($"Captured LIT shader: {shaderName}");
                    }

                    // Capture unlit shader (for particles/effects)
                    if (CapturedUnlitShader == null && (shaderName.Contains("Unlit") || shaderName.Contains("Particle") || shaderName.Contains("Sprite")))
                    {
                        CapturedUnlitShader = shader;
                        ParticleMaterial = new Material(renderer.sharedMaterial);
                        ParticleMaterial.name = "TurretDefense_Particle";
                        Log.LogInfo($"Captured UNLIT shader: {shaderName}");
                    }

                    if (CapturedLitShader != null && CapturedUnlitShader != null)
                        break;
                }

                // Fallback: if we didn't find a proper lit shader, use the first working one
                if (CapturedLitShader == null)
                {
                    foreach (var renderer in renderers)
                    {
                        if (renderer == null || renderer.sharedMaterial == null || renderer.sharedMaterial.shader == null)
                            continue;

                        var shader = renderer.sharedMaterial.shader;
                        if (!shader.name.Contains("Error") && !shader.name.Contains("Hidden"))
                        {
                            CapturedLitShader = shader;
                            DefaultMaterial = new Material(renderer.sharedMaterial);
                            DefaultMaterial.name = "TurretDefense_Default";
                            Log.LogInfo($"Captured FALLBACK shader: {shader.name}");
                            break;
                        }
                    }
                }

                // Create line material using the captured shader
                if (CapturedUnlitShader != null)
                {
                    LineMaterial = new Material(CapturedUnlitShader);
                    LineMaterial.name = "TurretDefense_Line";
                }
                else if (CapturedLitShader != null)
                {
                    LineMaterial = new Material(CapturedLitShader);
                    LineMaterial.name = "TurretDefense_Line";
                }

                // Last resort: create basic materials
                if (DefaultMaterial == null)
                {
                    Log.LogWarning("Could not capture game shaders - using fallback (may be pink!)");
                    DefaultMaterial = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
                    DefaultMaterial.name = "TurretDefense_Default_Fallback";
                }

                if (ParticleMaterial == null && DefaultMaterial != null)
                {
                    ParticleMaterial = new Material(DefaultMaterial);
                    ParticleMaterial.name = "TurretDefense_Particle_Fallback";
                }

                if (LineMaterial == null && DefaultMaterial != null)
                {
                    LineMaterial = new Material(DefaultMaterial);
                    LineMaterial.name = "TurretDefense_Line_Fallback";
                }

                Log.LogInfo($"Material init complete. LitShader: {CapturedLitShader?.name ?? "null"}, UnlitShader: {CapturedUnlitShader?.name ?? "null"}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to initialize materials: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a URP-compatible material with the specified color
        /// Uses captured game shader to guarantee compatibility
        /// </summary>
        public static Material GetColoredMaterial(Color color)
        {
            Material mat;

            // Priority: use captured game material
            if (DefaultMaterial != null)
            {
                mat = new Material(DefaultMaterial);
            }
            else if (CapturedLitShader != null)
            {
                mat = new Material(CapturedLitShader);
            }
            else
            {
                // Last resort fallback - will likely be pink but at least won't crash
                Log.LogWarning($"GetColoredMaterial: No shader available, color {color} may appear pink");
                mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            }

            // Set color - try multiple property names for compatibility
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color); // URP
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color); // Standard
            mat.color = color;

            return mat;
        }

        /// <summary>
        /// Get a material suitable for particles/effects
        /// Uses captured game shader for compatibility
        /// </summary>
        public static Material GetEffectMaterial(Color color)
        {
            Material mat;

            // Priority: use captured particle material
            if (ParticleMaterial != null)
            {
                mat = new Material(ParticleMaterial);
            }
            else if (CapturedUnlitShader != null)
            {
                mat = new Material(CapturedUnlitShader);
            }
            else if (DefaultMaterial != null)
            {
                // Use lit material as fallback for effects
                mat = new Material(DefaultMaterial);
            }
            else
            {
                Log.LogWarning($"GetEffectMaterial: No shader available, color {color} may appear pink");
                mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            }

            // Set color
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);
            mat.color = color;

            return mat;
        }

        private int testSpawnIndex = 0;

        private void Update()
        {
            if (Input.GetKeyDown(TestSpawnKey.Value))
            {
                TestSpawnAlien();
            }
            if (Input.GetKeyDown(SpawnWaveKey.Value))
            {
                WaveSystem?.ForceSpawnWave();
            }
            if (Input.GetKeyDown(SpawnTurretKey.Value))
            {
                TestSpawnTurret();
            }

            // Cleanup lists
            ActiveAliens.RemoveAll(a => a == null || !a.IsAlive);
            ActiveGroundEnemies.RemoveAll(e => e == null || !e.IsAlive);
            ActiveHives.RemoveAll(h => h == null || !h.IsAlive);
            ActiveBosses.RemoveAll(b => b == null || !b.IsAlive);
            ActiveTurrets.RemoveAll(t => t == null);
            ActiveHealthBars.RemoveAll(h => h == null);
            ActiveDamageNumbers.RemoveAll(d => d == null);
        }

        private void TestSpawnAlien()
        {
            var player = Player.instance;
            if (player == null) return;

            string[] alienTypes = {
    // Original alien ships
    "AlienFighter", "AlienFighterGreen", "AlienFighterWhite",
    "AlienDestroyer", "AlienDestroyerGreen", "AlienDestroyerWhite",
    "BioTorpedo", "BioTorpedoGreen", "BioTorpedoWhite",
    // Robot enemies (from Sci_fi_Drones)
    "Robot_Scout", "Robot_Scout_HyperX", "Robot_Scout_HyperX_Red", "Robot_Scout_Rockie",
    "Robot_Invader", "Robot_Collector", "Robot_Guardian"
};
            string alienType = alienTypes[testSpawnIndex % alienTypes.Length];
            testSpawnIndex++;

            Vector3 spawnPos = player.transform.position + player.transform.forward * 20f + Vector3.up * 15f;
            Log.LogInfo($"TEST: Spawning {alienType}");
            SpawnAlien(alienType, spawnPos, 1);
        }

        private void TestSpawnTurret()
        {
            var player = Player.instance;
            if (player == null) return;

            string[] turretTypes = { "Gatling", "Rocket", "Laser", "Railgun", "Lightning" };
            string turretType = turretTypes[testSpawnIndex % turretTypes.Length];

            Vector3 spawnPos = player.transform.position + player.transform.forward * 5f;
            Log.LogInfo($"TEST: Spawning {turretType} turret");
            SpawnTurret(turretType, spawnPos, Quaternion.identity);
        }

        public static Vector3 GetSpawnPosition(int index, float distance)
        {
            var player = Player.instance;
            if (player == null) return Vector3.zero;

            Vector3 offset = spawnPointOffsets[index % spawnPointOffsets.Count];
            Vector3 basePos = player.transform.position + offset * distance;
            basePos.y += UnityEngine.Random.Range(10f, 25f);
            basePos += UnityEngine.Random.insideUnitSphere * 10f;

            return basePos;
        }

        public static Vector3 GetGroundSpawnPosition(int index)
        {
            var player = Player.instance;
            if (player == null) return Vector3.zero;

            // Spawn ground enemies in a ring around the player at ground level
            float distance = UnityEngine.Random.Range(40f, 70f);
            float angle = (index * 37f) + UnityEngine.Random.Range(-15f, 15f); // Golden angle spread
            angle *= Mathf.Deg2Rad;

            Vector3 offset = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
            Vector3 basePos = player.transform.position + offset * distance;

            // Raycast down to find ground level
            if (Physics.Raycast(basePos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
            {
                basePos = hit.point + Vector3.up * 0.5f; // Slightly above ground
            }
            else
            {
                // Fallback to player's Y position
                basePos.y = player.transform.position.y;
            }

            return basePos;
        }

        public static GameObject SpawnAlien(string alienType, Vector3 position, int waveNumber)
        {
            // Determine which bundle to load from based on alien type
            string bundleName = alienType.StartsWith("Robot_") ? "drones_scifi" : "autarca_ships";
            GameObject prefab = AssetLoader?.GetPrefab(bundleName, alienType);
            GameObject instance;

            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);

                // Scale based on enemy type (Robot_Guardian is a boss, should be larger)
                float scale = 3f;
                if (alienType.Contains("Robot_Guardian"))
                    scale = 5f; // Boss-sized
                else if (alienType.Contains("Robot_Invader"))
                    scale = 4f; // Elite-sized
                else if (alienType.StartsWith("Robot_"))
                    scale = 2.5f; // Robot drones are smaller

                instance.transform.localScale = Vector3.one * scale;

                // Fix materials on the loaded prefab
                FixPrefabMaterials(instance);
            }
            else
            {
                // Fallback: create primitive alien with proper materials
                instance = CreatePrimitiveAlien(alienType);
                instance.transform.position = position;
            }

            var controller = instance.AddComponent<AlienShipController>();
            controller.Initialize(alienType, waveNumber);

            ActiveAliens.Add(controller);

            if (ShowHealthBars.Value)
            {
                var healthBar = instance.AddComponent<FloatingHealthBar>();
                healthBar.Initialize(controller);
                ActiveHealthBars.Add(healthBar);
            }

            LogDebug($"Spawned {alienType} at {position}");
            return instance;
        }

        /// <summary>
        /// Fix materials on imported prefabs to be URP compatible
        /// AGGRESSIVELY replaces ALL materials with captured game shaders
        /// Preserves albedo, normal, metallic, emission, and other texture maps
        /// </summary>
        public static void FixPrefabMaterials(GameObject obj)
        {
            var renderers = obj.GetComponentsInChildren<Renderer>(true); // Include inactive
            int fixedCount = 0;

            foreach (var renderer in renderers)
            {
                if (renderer.sharedMaterials == null || renderer.sharedMaterials.Length == 0)
                    continue;

                var newMaterials = new Material[renderer.sharedMaterials.Length];

                for (int i = 0; i < renderer.sharedMaterials.Length; i++)
                {
                    var oldMat = renderer.sharedMaterials[i];

                    // ALWAYS replace materials from asset bundles - they're built with Standard pipeline
                    // which doesn't work in URP regardless of whether they "look" broken
                    Color originalColor = Color.white;
                    Texture mainTex = null;
                    Texture normalMap = null;
                    Texture metallicMap = null;
                    Texture emissionMap = null;
                    Texture occlusionMap = null;
                    Color emissionColor = Color.black;
                    float metallic = 0f;
                    float smoothness = 0.5f;
                    float normalScale = 1f;

                    if (oldMat != null)
                    {
                        // Try to preserve original color
                        if (oldMat.HasProperty("_Color"))
                            originalColor = oldMat.GetColor("_Color");
                        else if (oldMat.HasProperty("_BaseColor"))
                            originalColor = oldMat.GetColor("_BaseColor");
                        else
                            originalColor = oldMat.color;

                        // Try to preserve main texture (albedo)
                        if (oldMat.HasProperty("_MainTex"))
                            mainTex = oldMat.GetTexture("_MainTex");
                        else if (oldMat.HasProperty("_BaseMap"))
                            mainTex = oldMat.GetTexture("_BaseMap");
                        else if (oldMat.HasProperty("_Albedo"))
                            mainTex = oldMat.GetTexture("_Albedo");

                        // Try to preserve normal map
                        if (oldMat.HasProperty("_BumpMap"))
                            normalMap = oldMat.GetTexture("_BumpMap");
                        else if (oldMat.HasProperty("_NormalMap"))
                            normalMap = oldMat.GetTexture("_NormalMap");

                        // Get normal scale
                        if (oldMat.HasProperty("_BumpScale"))
                            normalScale = oldMat.GetFloat("_BumpScale");

                        // Try to preserve metallic/smoothness
                        if (oldMat.HasProperty("_MetallicGlossMap"))
                            metallicMap = oldMat.GetTexture("_MetallicGlossMap");
                        else if (oldMat.HasProperty("_MetallicMap"))
                            metallicMap = oldMat.GetTexture("_MetallicMap");

                        if (oldMat.HasProperty("_Metallic"))
                            metallic = oldMat.GetFloat("_Metallic");
                        if (oldMat.HasProperty("_Glossiness"))
                            smoothness = oldMat.GetFloat("_Glossiness");
                        else if (oldMat.HasProperty("_Smoothness"))
                            smoothness = oldMat.GetFloat("_Smoothness");

                        // Try to preserve emission
                        if (oldMat.HasProperty("_EmissionMap"))
                            emissionMap = oldMat.GetTexture("_EmissionMap");
                        if (oldMat.HasProperty("_EmissionColor"))
                            emissionColor = oldMat.GetColor("_EmissionColor");

                        // Try to preserve occlusion map
                        if (oldMat.HasProperty("_OcclusionMap"))
                            occlusionMap = oldMat.GetTexture("_OcclusionMap");
                    }

                    // Create new material with captured game shader
                    Material newMat = GetColoredMaterial(originalColor);

                    // Apply albedo texture
                    if (mainTex != null)
                    {
                        if (newMat.HasProperty("_MainTex"))
                            newMat.SetTexture("_MainTex", mainTex);
                        if (newMat.HasProperty("_BaseMap"))
                            newMat.SetTexture("_BaseMap", mainTex);
                    }

                    // Apply normal map
                    if (normalMap != null)
                    {
                        if (newMat.HasProperty("_BumpMap"))
                        {
                            newMat.SetTexture("_BumpMap", normalMap);
                            newMat.SetFloat("_BumpScale", normalScale);
                            newMat.EnableKeyword("_NORMALMAP");
                        }
                    }

                    // Apply metallic/smoothness
                    if (metallicMap != null)
                    {
                        if (newMat.HasProperty("_MetallicGlossMap"))
                        {
                            newMat.SetTexture("_MetallicGlossMap", metallicMap);
                            newMat.EnableKeyword("_METALLICGLOSSMAP");
                        }
                    }
                    if (newMat.HasProperty("_Metallic"))
                        newMat.SetFloat("_Metallic", metallic);
                    if (newMat.HasProperty("_Smoothness"))
                        newMat.SetFloat("_Smoothness", smoothness);
                    if (newMat.HasProperty("_Glossiness"))
                        newMat.SetFloat("_Glossiness", smoothness);

                    // Apply emission
                    if (emissionMap != null || emissionColor != Color.black)
                    {
                        if (newMat.HasProperty("_EmissionMap") && emissionMap != null)
                            newMat.SetTexture("_EmissionMap", emissionMap);
                        if (newMat.HasProperty("_EmissionColor"))
                        {
                            newMat.SetColor("_EmissionColor", emissionColor);
                            if (emissionColor != Color.black || emissionMap != null)
                                newMat.EnableKeyword("_EMISSION");
                        }
                    }

                    // Apply occlusion
                    if (occlusionMap != null && newMat.HasProperty("_OcclusionMap"))
                    {
                        newMat.SetTexture("_OcclusionMap", occlusionMap);
                        newMat.EnableKeyword("_OCCLUSIONMAP");
                    }

                    newMaterials[i] = newMat;
                    fixedCount++;

                    // Log what we preserved for debugging
                    if (mainTex != null || normalMap != null || metallicMap != null)
                    {
                        LogDebug($"  Material {i}: albedo={mainTex?.name ?? "none"}, normal={normalMap?.name ?? "none"}, metallic={metallicMap?.name ?? "none"}");
                    }
                }

                renderer.sharedMaterials = newMaterials;
            }

            // Also fix particle systems
            var particleSystems = obj.GetComponentsInChildren<ParticleSystemRenderer>(true);
            foreach (var psr in particleSystems)
            {
                if (psr.sharedMaterial != null)
                {
                    Color color = psr.sharedMaterial.color;
                    psr.sharedMaterial = GetEffectMaterial(color);
                    fixedCount++;
                }
            }

            LogDebug($"FixPrefabMaterials: Fixed {fixedCount} materials on {obj.name}");
        }

        private static bool IsMaterialBroken(Material mat)
        {
            // Check if material has the pink "missing shader" look
            if (mat == null) return true;
            if (mat.shader == null) return true;
            string shaderName = mat.shader.name;
            if (shaderName.Contains("Hidden/InternalErrorShader")) return true;
            if (shaderName.Contains("Error")) return true;
            if (shaderName.Contains("Hidden/")) return true;
            // Asset bundle materials with Standard shader will also be broken in URP
            if (shaderName == "Standard") return true;
            return false;
        }

        private static GameObject CreatePrimitiveAlien(string alienType)
        {
            GameObject alien = new GameObject($"Alien_{alienType}");

            Color bodyColor;
            Color accentColor;

            if (alienType.Contains("Green"))
            {
                bodyColor = new Color(0.2f, 0.6f, 0.3f);
                accentColor = new Color(0.3f, 0.8f, 0.4f);
            }
            else
            {
                bodyColor = new Color(0.5f, 0.2f, 0.5f);
                accentColor = new Color(0.7f, 0.3f, 0.7f);
            }

            if (alienType.Contains("Destroyer"))
            {
                // Large destroyer
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(alien.transform);
                body.transform.localScale = new Vector3(1.5f, 0.5f, 3f);
                body.transform.localRotation = Quaternion.Euler(90, 0, 0);
                body.GetComponent<Renderer>().material = GetColoredMaterial(bodyColor);

                for (int i = -1; i <= 1; i += 2)
                {
                    var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wing.transform.SetParent(alien.transform);
                    wing.transform.localPosition = new Vector3(i * 1.5f, 0, 0);
                    wing.transform.localScale = new Vector3(2f, 0.1f, 1f);
                    wing.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.3f, 0.3f, 0.4f));
                }
            }
            else if (alienType.Contains("Torpedo"))
            {
                var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.transform.SetParent(alien.transform);
                body.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
                body.transform.localRotation = Quaternion.Euler(90, 0, 0);
                body.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.8f, 0.4f, 0.1f));

                var tip = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                tip.transform.SetParent(alien.transform);
                tip.transform.localPosition = Vector3.forward * 1.2f;
                tip.transform.localScale = Vector3.one * 0.5f;
                tip.GetComponent<Renderer>().material = GetColoredMaterial(new Color(1f, 0.8f, 0.2f));
                UnityEngine.Object.Destroy(tip.GetComponent<Collider>());
            }
            else
            {
                // Fighter
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.SetParent(alien.transform);
                body.transform.localScale = new Vector3(1f, 0.4f, 1.5f);
                body.GetComponent<Renderer>().material = GetColoredMaterial(bodyColor);

                for (int i = -1; i <= 1; i += 2)
                {
                    var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    wing.transform.SetParent(alien.transform);
                    wing.transform.localPosition = new Vector3(i * 0.8f, 0, -0.2f);
                    wing.transform.localScale = new Vector3(1f, 0.05f, 0.5f);
                    wing.transform.localRotation = Quaternion.Euler(0, 0, i * 15);
                    wing.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.4f, 0.4f, 0.5f));
                }
            }

            // Engine glow
            var engine = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            engine.transform.SetParent(alien.transform);
            engine.transform.localPosition = Vector3.back * 1f;
            engine.transform.localScale = Vector3.one * 0.4f;
            engine.GetComponent<Renderer>().material = GetColoredMaterial(accentColor);
            UnityEngine.Object.Destroy(engine.GetComponent<Collider>());

            return alien;
        }

        public static GameObject SpawnTurret(string turretType, Vector3 position, Quaternion rotation)
        {
            string bundleName = GetBundleForTurret(turretType);
            GameObject prefab = AssetLoader?.GetPrefab(bundleName, GetPrefabNameForTurret(turretType));

            GameObject instance;
            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
                FixPrefabMaterials(instance);
            }
            else
            {
                instance = CreatePrimitiveTurret(turretType);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }

            var controller = instance.AddComponent<TurretController>();
            controller.Initialize(turretType);

            ActiveTurrets.Add(controller);
            Log.LogInfo($"Deployed {turretType} turret at {position}");
            return instance;
        }

        private static GameObject CreatePrimitiveTurret(string turretType)
        {
            GameObject turret = new GameObject($"Turret_{turretType}");
            Color turretColor = GetTurretColor(turretType);

            // Base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.transform.SetParent(turret.transform);
            baseObj.transform.localPosition = Vector3.up * 0.25f;
            baseObj.transform.localScale = new Vector3(2f, 0.5f, 2f);
            baseObj.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.3f, 0.3f, 0.35f));

            // Turret body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.name = "TurretBody";
            body.transform.SetParent(turret.transform);
            body.transform.localPosition = Vector3.up * 1f;
            body.transform.localScale = new Vector3(1.2f, 0.8f, 1.2f);
            body.GetComponent<Renderer>().material = GetColoredMaterial(turretColor);

            // Barrel(s)
            GameObject barrelRoot = new GameObject("BarrelRoot");
            barrelRoot.transform.SetParent(body.transform);
            barrelRoot.transform.localPosition = Vector3.zero;

            if (turretType == "Gatling")
            {
                for (int i = 0; i < 4; i++)
                {
                    var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                    barrel.name = $"Barrel_{i}";
                    barrel.transform.SetParent(barrelRoot.transform);
                    float angle = (i / 4f) * Mathf.PI * 2f;
                    barrel.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.15f, Mathf.Sin(angle) * 0.15f, 0.8f);
                    barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);
                    barrel.transform.localScale = new Vector3(0.1f, 0.5f, 0.1f);
                    barrel.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.2f, 0.2f, 0.25f));
                    UnityEngine.Object.Destroy(barrel.GetComponent<Collider>());
                }
            }
            else
            {
                var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                barrel.name = "Barrel";
                barrel.transform.SetParent(barrelRoot.transform);
                barrel.transform.localPosition = Vector3.forward * 0.8f;
                barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);

                if (turretType == "Railgun")
                    barrel.transform.localScale = new Vector3(0.15f, 1f, 0.15f);
                else if (turretType == "Rocket")
                    barrel.transform.localScale = new Vector3(0.25f, 0.6f, 0.25f);
                else
                    barrel.transform.localScale = new Vector3(0.12f, 0.7f, 0.12f);

                barrel.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.25f, 0.25f, 0.3f));
                UnityEngine.Object.Destroy(barrel.GetComponent<Collider>());
            }

            GameObject muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(barrelRoot.transform);
            muzzle.transform.localPosition = Vector3.forward * 1.5f;

            return turret;
        }

        private static Color GetTurretColor(string type)
        {
            return type switch
            {
                "Gatling" => new Color(0.4f, 0.5f, 0.4f),
                "Rocket" => new Color(0.5f, 0.35f, 0.3f),
                "Laser" => new Color(0.3f, 0.4f, 0.6f),
                "Railgun" => new Color(0.35f, 0.35f, 0.5f),
                "Lightning" => new Color(0.3f, 0.5f, 0.6f),
                _ => new Color(0.4f, 0.4f, 0.4f)
            };
        }

        private static string GetBundleForTurret(string turretType)
        {
            return turretType.ToLower() switch
            {
                // PBR Turrets (high quality models)
                "gatling" => "turrets_gatling",
                "rocket" => "turrets_rocket",
                "railgun" => "turrets_railgun",
                "laser" => "laser_cannon",
                "lightning" => "turrets_lightning",
                "nuke" => "turrets_nuke",
                "flamethrower" => "turrets_flamethrower",
                // TDTK Turrets (cartoon style but functional - 12 total)
                "cannon" => "turrets_tdtk",
                "cannon2" => "turrets_tdtk",
                "mg" => "turrets_tdtk",
                "mg2" => "turrets_tdtk",
                "missile" => "turrets_tdtk",
                "missile2" => "turrets_tdtk",
                "beamlaser" => "turrets_tdtk",
                "beamlaser2" => "turrets_tdtk",
                "aoe" => "turrets_tdtk",
                "aoe2" => "turrets_tdtk",
                "support" => "turrets_tdtk",
                "support2" => "turrets_tdtk",
                // SciFi Turrets (simple models)
                "scifi1" => "turrets_scifi",
                "scifi2" => "turrets_scifi",
                "scifi3" => "turrets_scifi",
                "scifi4" => "turrets_scifi",
                _ => "turrets_gatling"
            };
        }

        private static string GetPrefabNameForTurret(string turretType)
        {
            // Map turret types to actual prefab names in the asset bundles
            return turretType.ToLower() switch
            {
                // PBR Turrets (ColorChanging variants for customization)
                "gatling" => "GatelingGun_ColorChanging_L1",
                "rocket" => "RocketLauncher_ColorChanging_L1",
                "railgun" => "RailGun_ColorChanging_L1",
                "laser" => "Laser Cannon_fbx_prefab",
                "lightning" => "LighteningGun_ColorChanging_L1",
                "nuke" => "Nuke_Base_ColorChanging",
                "flamethrower" => "Flamethrower_ColorChanging_L1",
                // TDTK Turrets (12 types available)
                "cannon" => "TowerCanon",
                "cannon2" => "TowerCanon2",
                "mg" => "TowerMG",
                "mg2" => "TowerMG2",
                "missile" => "TowerMissile",
                "missile2" => "TowerMissile2",
                "beamlaser" => "TowerBeamLaser",
                "beamlaser2" => "TowerBeamLaser2",
                "aoe" => "TowerAOE",
                "aoe2" => "TowerAOE2",
                "support" => "TowerSupport",
                "support2" => "TowerSupport2",
                // SciFi Turrets (4 variants)
                "scifi1" => "Turret 1a",
                "scifi2" => "Turret 1b",
                "scifi3" => "Turret 1c",
                "scifi4" => "Turret 1d",
                _ => turretType
            };
        }

        /// <summary>
        /// Spawn an artillery turret (long range, high damage, requires ammo)
        /// </summary>
        public static ArtilleryController SpawnArtillery(string artilleryType, Vector3 position, Quaternion rotation)
        {
            // Try to load from asset bundles
            string bundleName = artilleryType == "Auto" ? "artillery_outpost" : "artillery_cannons";
            string prefabName = GetArtilleryPrefabName(artilleryType);

            GameObject prefab = AssetLoader?.GetPrefab(bundleName, prefabName);
            GameObject instance;

            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
                FixPrefabMaterials(instance);
            }
            else
            {
                // Create primitive artillery
                instance = CreatePrimitiveArtillery(artilleryType);
                instance.transform.position = position;
                instance.transform.rotation = rotation;
            }

            var controller = instance.AddComponent<ArtilleryController>();
            controller.Initialize(artilleryType);

            ActiveArtillery.Add(controller);
            Log.LogInfo($"Deployed {artilleryType} artillery at {position}");

            return controller;
        }

        private static string GetArtilleryPrefabName(string type)
        {
            switch (type)
            {
                case "Light": return "cannon_small";
                case "Medium": return "cannon_medium";
                case "Heavy": return "cannon_large";
                case "Multi": return "cannon_set";  // Multi-barrel artillery
                case "Auto": return "AutoCannon";
                default: return "cannon_medium";
            }
        }

        private static GameObject CreatePrimitiveArtillery(string type)
        {
            GameObject artillery = new GameObject($"Artillery_{type}");

            // Base platform
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(artillery.transform);
            baseObj.transform.localPosition = Vector3.up * 0.2f;
            baseObj.transform.localScale = new Vector3(3f, 0.4f, 3f);
            baseObj.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.3f, 0.35f, 0.3f));

            // Turret housing
            var housing = GameObject.CreatePrimitive(PrimitiveType.Cube);
            housing.name = "Housing";
            housing.transform.SetParent(artillery.transform);
            housing.transform.localPosition = Vector3.up * 0.8f;
            housing.transform.localScale = new Vector3(1.5f, 1f, 2f);
            housing.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.4f, 0.45f, 0.35f));

            // Barrel
            var barrel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            barrel.name = "Barrel";
            barrel.transform.SetParent(housing.transform);
            barrel.transform.localPosition = new Vector3(0, 0.2f, 1.5f);
            barrel.transform.localRotation = Quaternion.Euler(90, 0, 0);

            float barrelLength = type == "Heavy" ? 3f : (type == "Medium" ? 2f : 1.5f);
            float barrelWidth = type == "Heavy" ? 0.4f : (type == "Medium" ? 0.3f : 0.2f);
            barrel.transform.localScale = new Vector3(barrelWidth, barrelLength, barrelWidth);
            barrel.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.2f, 0.2f, 0.25f));

            // Muzzle point
            var muzzle = new GameObject("MuzzlePoint");
            muzzle.transform.SetParent(barrel.transform);
            muzzle.transform.localPosition = Vector3.up * 0.5f;

            return artillery;
        }

        public static void SpawnDamageNumber(Vector3 position, float damage, bool isCritical = false)
        {
            if (!ShowDamageNumbers.Value) return;

            var dmgObj = new GameObject("DamageNumber");
            dmgObj.transform.position = position + Vector3.up * 2f;
            var dmgNum = dmgObj.AddComponent<DamageNumber>();
            dmgNum.Initialize(damage, isCritical);
            ActiveDamageNumbers.Add(dmgNum);
        }

        /// <summary>
        /// Apply damage to the player - integrates with SurvivalElements if available
        /// </summary>
        public static void DamagePlayer(float damage, string source)
        {
            float finalDamage = damage * AlienDamageToPlayer.Value;
            if (finalDamage <= 0) return;

            var player = Player.instance;
            if (player == null)
            {
                LogDebug($"DamagePlayer: No player instance");
                return;
            }

            // Try SurvivalElements first (our mod's player health system)
            if (TryDamageViaSurvivalElements(finalDamage, source))
            {
                return;
            }

            // Fallback: Try native Damageable component
            var damageable = player.GetComponent<Damageable>();
            if (damageable == null)
            {
                damageable = player.GetComponentInChildren<Damageable>();
            }

            if (damageable != null)
            {
                var damageMessage = new Damageable.DamageMessage
                {
                    amount = Mathf.RoundToInt(finalDamage),
                    damager = Instance,
                    direction = Vector3.up,
                    damageSource = player.transform.position,
                    stopCamera = false
                };
                damageable.ApplyDamage(damageMessage);
                Log.LogInfo($"Player took {finalDamage:F0} damage from {source} (via Damageable)");
            }
            else
            {
                Log.LogWarning($"[Combat] Player took {finalDamage:F0} damage from {source} - no health system active!");
            }
        }

        /// <summary>
        /// Try to apply damage via SurvivalElements mod (if loaded)
        /// </summary>
        private static bool TryDamageViaSurvivalElements(float damage, string source)
        {
            try
            {
                // Look for SurvivalElements assembly
                var survivalAssembly = AppDomain.CurrentDomain.GetAssemblies()
                    .FirstOrDefault(a => a.GetName().Name == "SurvivalElements");

                if (survivalAssembly == null) return false;

                var pluginType = survivalAssembly.GetType("SurvivalElements.SurvivalElementsPlugin");
                if (pluginType == null) return false;

                // Call the static DamagePlayer method
                var damageMethod = pluginType.GetMethod("DamagePlayer",
                    System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);

                if (damageMethod != null)
                {
                    damageMethod.Invoke(null, new object[] { damage, source });
                    LogDebug($"Player took {damage:F0} damage from {source} (via SurvivalElements)");
                    return true;
                }
            }
            catch (Exception ex)
            {
                LogDebug($"SurvivalElements integration error: {ex.Message}");
            }
            return false;
        }

        /// <summary>
        /// Find nearby machines that can be targeted by aliens
        /// Returns a list of machine transforms within range
        /// </summary>
        public static List<Transform> GetNearbyMachines(Vector3 position, float radius)
        {
            var machines = new List<Transform>();

            // Find placed machines by tag or layer (buildings typically use specific tags/layers)
            // For now, return empty - full implementation requires deeper game integration
            // This is a stub for future machine-targeting functionality
            LogDebug($"GetNearbyMachines called at {position} with radius {radius}");

            return machines;
        }

        public static void OnAlienKilled(AlienShipController alien)
        {
            TotalKills++;
            CurrentScore += alien.ScoreValue;
            LogDebug($"Alien killed! Total: {TotalKills}, Score: {CurrentScore}");
        }

        public static void OnGroundEnemyKilled(GroundEnemyController enemy)
        {
            TotalKills++;
            CurrentScore += enemy.ScoreValue;
            LogDebug($"Ground enemy killed! Total: {TotalKills}, Score: {CurrentScore}");
        }

        public static void OnHiveDestroyed(AlienHiveController hive)
        {
            CurrentScore += hive.ScoreValue;
            Log.LogInfo($"HIVE DESTROYED: {hive.HiveName}! Score: +{hive.ScoreValue}");
        }

        /// <summary>
        /// Spawn a ground-based enemy (Chomper, Spitter, Mimic, Arachnid, etc.)
        /// </summary>
        public static GameObject SpawnGroundEnemy(GroundEnemyType type, Vector3 position, int waveNumber)
        {
            string bundleName;
            string prefabName;

            switch (type)
            {
                case GroundEnemyType.Chomper:
                    bundleName = "creatures_gamekit";
                    prefabName = "Chomper";
                    break;
                case GroundEnemyType.Spitter:
                    bundleName = "creatures_gamekit";
                    prefabName = "Spitter";
                    break;
                case GroundEnemyType.Grenadier:
                    bundleName = "creatures_gamekit";
                    prefabName = "Grenadier";
                    break;
                case GroundEnemyType.Gunner:
                    bundleName = "creatures_gamekit";
                    prefabName = "Gunner";
                    break;
                case GroundEnemyType.Mimic:
                    bundleName = "creature_mimic";
                    prefabName = "Mimic";
                    break;
                case GroundEnemyType.Arachnid:
                    bundleName = "creature_arachnid";
                    prefabName = "SKM_ArachnidAlienHead_Lite";
                    break;
                case GroundEnemyType.MachineGunRobot:
                    bundleName = "enemy_robots";
                    prefabName = "MachineGunRobot";
                    break;
                case GroundEnemyType.CannonMachine:
                    bundleName = "enemy_robots";
                    prefabName = "CannonMachine";
                    break;
                default:
                    bundleName = "creatures_gamekit";
                    prefabName = "Chomper";
                    break;
            }

            GameObject prefab = AssetLoader?.GetPrefab(bundleName, prefabName);
            GameObject instance;

            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                // Scale based on enemy type
                float scale = type switch
                {
                    GroundEnemyType.Arachnid => 1.5f,
                    GroundEnemyType.MachineGunRobot => 3f,  // Heavy mech
                    GroundEnemyType.CannonMachine => 4f,    // Artillery mech (bigger)
                    _ => 2f
                };
                instance.transform.localScale = Vector3.one * scale;
                FixPrefabMaterials(instance);
            }
            else
            {
                // Fallback: create primitive
                instance = CreatePrimitiveGroundEnemy(type);
                instance.transform.position = position;
            }

            var controller = instance.AddComponent<GroundEnemyController>();
            controller.Initialize(prefabName, type, waveNumber);

            ActiveGroundEnemies.Add(controller);

            if (ShowHealthBars.Value)
            {
                var healthBar = instance.AddComponent<FloatingHealthBar>();
                healthBar.Initialize(controller);
                ActiveHealthBars.Add(healthBar);
            }

            LogDebug($"Spawned ground enemy {type} at {position}");
            return instance;
        }

        private static GameObject CreatePrimitiveGroundEnemy(GroundEnemyType type)
        {
            GameObject enemy = new GameObject($"Enemy_{type}");
            Color bodyColor;

            switch (type)
            {
                case GroundEnemyType.Chomper:
                    bodyColor = new Color(0.4f, 0.2f, 0.4f);
                    break;
                case GroundEnemyType.Spitter:
                    bodyColor = new Color(0.3f, 0.6f, 0.2f);
                    break;
                case GroundEnemyType.Mimic:
                    bodyColor = new Color(0.3f, 0.25f, 0.2f);
                    break;
                case GroundEnemyType.Arachnid:
                    bodyColor = new Color(0.2f, 0.2f, 0.3f);
                    break;
                case GroundEnemyType.MachineGunRobot:
                    bodyColor = new Color(0.4f, 0.4f, 0.45f); // Metallic gray
                    break;
                case GroundEnemyType.CannonMachine:
                    bodyColor = new Color(0.3f, 0.35f, 0.4f); // Dark steel
                    break;
                default:
                    bodyColor = new Color(0.5f, 0.3f, 0.3f);
                    break;
            }

            // Simple body
            var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            body.transform.SetParent(enemy.transform);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(1f, 0.8f, 1.2f);
            body.GetComponent<Renderer>().material = GetColoredMaterial(bodyColor);

            return enemy;
        }

        /// <summary>
        /// Spawn an alien hive/spawner structure
        /// </summary>
        public static GameObject SpawnHive(HiveType type, Vector3 position, int threatLevel)
        {
            string bundleName;
            string prefabName;
            float scale = 3f;

            switch (type)
            {
                case HiveType.Birther:
                    bundleName = "alien_buildings";
                    prefabName = "birther";
                    break;
                case HiveType.Brain:
                    bundleName = "alien_buildings";
                    prefabName = "brain";
                    break;
                case HiveType.Stomach:
                    bundleName = "alien_buildings";
                    prefabName = "stomach";
                    break;
                case HiveType.Terraformer:
                    bundleName = "alien_buildings";
                    prefabName = "terraformer";
                    break;
                case HiveType.Claw:
                    bundleName = "alien_buildings";
                    prefabName = "claw";
                    break;
                case HiveType.Crystal:
                    bundleName = "alien_buildings";
                    prefabName = "Crystals";
                    break;
                case HiveType.Idol:
                    // Boss spawner uses Cthulhu Idol visuals
                    bundleName = "cthulhu_idols";
                    // Alternate between the two idol variants based on threat level
                    prefabName = threatLevel % 2 == 0 ? "CthulhuIdolPBR_V1" : "CthulhuIdolPBR_V2";
                    scale = 5f; // Larger for boss spawner
                    break;
                default:
                    bundleName = "alien_buildings";
                    prefabName = "birther";
                    break;
            }

            GameObject prefab = AssetLoader?.GetPrefab(bundleName, prefabName);
            GameObject instance;

            if (prefab != null)
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                instance.transform.localScale = Vector3.one * scale;
                FixPrefabMaterials(instance);
            }
            else
            {
                // Fallback: create primitive hive
                instance = CreatePrimitiveHive(type);
                instance.transform.position = position;
            }

            var controller = instance.AddComponent<AlienHiveController>();
            controller.Initialize(prefabName, type, threatLevel);

            ActiveHives.Add(controller);

            Log.LogInfo($"Spawned {type} hive at {position} (Threat Level {threatLevel})");
            return instance;
        }

        /// <summary>
        /// Spawn an epic boss encounter
        /// </summary>
        public static BossController SpawnBoss(BossType type, Vector3 position, int difficulty = 1)
        {
            Log.LogError($"==============================================");
            Log.LogError($"=== EPIC BOSS ENCOUNTER: {type} ===");
            Log.LogError($"==============================================");

            GameObject bossObj = new GameObject($"Boss_{type}");
            bossObj.transform.position = position;

            var controller = bossObj.AddComponent<BossController>();
            controller.Initialize(type, difficulty);

            ActiveBosses.Add(controller);

            // Add health bar for boss
            if (ShowHealthBars.Value)
            {
                var healthBar = bossObj.AddComponent<FloatingHealthBar>();
                healthBar.Initialize(controller);
                ActiveHealthBars.Add(healthBar);
            }

            return controller;
        }

        /// <summary>
        /// Called when a boss is defeated
        /// </summary>
        public static void OnBossKilled(BossController boss)
        {
            Log.LogError($"==============================================");
            Log.LogError($"=== BOSS DEFEATED: {boss.BossName} ===");
            Log.LogError($"==============================================");
            CurrentScore += boss.ScoreValue;
            TotalKills++;
        }

        private static GameObject CreatePrimitiveHive(HiveType type)
        {
            GameObject hive = new GameObject($"Hive_{type}");
            Color hiveColor;

            switch (type)
            {
                case HiveType.Birther:
                    hiveColor = new Color(0.8f, 0.3f, 0.4f);
                    break;
                case HiveType.Brain:
                    hiveColor = new Color(0.6f, 0.2f, 0.8f);
                    break;
                case HiveType.Stomach:
                    hiveColor = new Color(0.3f, 0.7f, 0.3f);
                    break;
                case HiveType.Crystal:
                    hiveColor = new Color(0.3f, 0.5f, 0.9f);
                    break;
                case HiveType.Idol:
                    hiveColor = new Color(0.1f, 0.5f, 0.4f); // Eldritch teal
                    break;
                default:
                    hiveColor = new Color(0.5f, 0.3f, 0.5f);
                    break;
            }

            // Create visual based on type
            if (type == HiveType.Idol)
            {
                // Idol structure - tall pillar with tentacle-like protrusions
                var pillar = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pillar.transform.SetParent(hive.transform);
                pillar.transform.localPosition = Vector3.up * 3f;
                pillar.transform.localScale = new Vector3(1.5f, 6f, 1.5f);
                pillar.GetComponent<Renderer>().material = GetColoredMaterial(hiveColor);

                // Add tentacle-like protrusions
                for (int i = 0; i < 4; i++)
                {
                    var tentacle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    tentacle.transform.SetParent(hive.transform);
                    float angle = i * 90f * Mathf.Deg2Rad;
                    tentacle.transform.localPosition = new Vector3(Mathf.Cos(angle) * 2f, 4f, Mathf.Sin(angle) * 2f);
                    tentacle.transform.localRotation = Quaternion.Euler(30f, i * 90f, 0);
                    tentacle.transform.localScale = new Vector3(0.5f, 2f, 0.5f);
                    tentacle.GetComponent<Renderer>().material = GetColoredMaterial(hiveColor * 0.8f);
                }

                // Add glowing eye/orb at top
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.transform.SetParent(hive.transform);
                eye.transform.localPosition = Vector3.up * 7f;
                eye.transform.localScale = Vector3.one * 1.2f;
                var eyeMat = GetColoredMaterial(new Color(0.3f, 1f, 0.8f));
                eyeMat.SetFloat("_Metallic", 0);
                eyeMat.EnableKeyword("_EMISSION");
                eyeMat.SetColor("_EmissionColor", new Color(0.3f, 1f, 0.8f) * 2f);
                eye.GetComponent<Renderer>().material = eyeMat;
            }
            else
            {
                // Standard organic-looking structure
                var body = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                body.transform.SetParent(hive.transform);
                body.transform.localPosition = Vector3.up * 2f;
                body.transform.localScale = new Vector3(3f, 4f, 3f);
                body.GetComponent<Renderer>().material = GetColoredMaterial(hiveColor);
            }

            return hive;
        }

        /// <summary>
        /// Spawn a hazard zone (used by Terraformer hives)
        /// </summary>
        public static void SpawnHazardZone(Vector3 position, float radius, float duration)
        {
            var hazardObj = new GameObject("HazardZone");
            hazardObj.transform.position = position;

            // Visual indicator
            var visual = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            visual.transform.SetParent(hazardObj.transform);
            visual.transform.localPosition = Vector3.up * 0.1f;
            visual.transform.localScale = new Vector3(radius * 2f, 0.1f, radius * 2f);
            visual.GetComponent<Renderer>().material = GetColoredMaterial(new Color(0.5f, 0.2f, 0.1f, 0.5f));
            UnityEngine.Object.Destroy(visual.GetComponent<Collider>());

            // Particles
            var particles = hazardObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.5f, 0.3f, 0.1f, 0.6f);

            var emission = particles.emission;
            emission.rateOverTime = radius * 5f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = radius;

            var renderer = hazardObj.GetComponent<ParticleSystemRenderer>();
            if (renderer != null)
                renderer.material = GetEffectMaterial(Color.white);

            // Auto-destroy
            UnityEngine.Object.Destroy(hazardObj, duration);

            LogDebug($"Spawned hazard zone at {position}, radius {radius}, duration {duration}s");
        }

        /// <summary>
        /// Get all enemies (ships and ground) near a position
        /// </summary>
        public static List<GameObject> GetNearbyEnemies(Vector3 position, float radius)
        {
            var enemies = new List<GameObject>();

            foreach (var alien in ActiveAliens)
            {
                if (alien != null && alien.IsAlive)
                {
                    if (Vector3.Distance(position, alien.transform.position) <= radius)
                        enemies.Add(alien.gameObject);
                }
            }

            foreach (var ground in ActiveGroundEnemies)
            {
                if (ground != null && ground.IsAlive)
                {
                    if (Vector3.Distance(position, ground.transform.position) <= radius)
                        enemies.Add(ground.gameObject);
                }
            }

            return enemies;
        }

        /// <summary>
        /// Get all hives near a position
        /// </summary>
        public static List<AlienHiveController> GetNearbyHives(Vector3 position, float radius)
        {
            var hives = new List<AlienHiveController>();

            foreach (var hive in ActiveHives)
            {
                if (hive != null && hive.IsAlive)
                {
                    if (Vector3.Distance(position, hive.transform.position) <= radius)
                        hives.Add(hive);
                }
            }

            return hives;
        }

        public static void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
                Log.LogInfo($"[DEBUG] {message}");
        }
    }

    #region Asset Bundle Loader

    public class AssetBundleLoader
    {
        private string basePath;
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, Dictionary<string, GameObject>> cachedPrefabs = new Dictionary<string, Dictionary<string, GameObject>>();

        public AssetBundleLoader(string pluginPath)
        {
            basePath = pluginPath;
        }

        public bool LoadBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName)) return true;

            string[] possiblePaths = {
                Path.Combine(basePath, "Bundles", bundleName),
                Path.Combine(basePath, bundleName),
                Path.Combine(basePath, "Bundles", bundleName + ".bundle"),
                Path.Combine(basePath, bundleName + ".bundle")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        AssetBundle bundle = AssetBundle.LoadFromFile(path);
                        if (bundle != null)
                        {
                            loadedBundles[bundleName] = bundle;
                            cachedPrefabs[bundleName] = new Dictionary<string, GameObject>();
                            TurretDefensePlugin.LogDebug($"Loaded bundle: {path}");

                            // Log available assets
                            var names = bundle.GetAllAssetNames();
                            TurretDefensePlugin.LogDebug($"Bundle contains {names.Length} assets");
                            foreach (var name in names.Take(10))
                            {
                                TurretDefensePlugin.LogDebug($"  - {name}");
                            }

                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        TurretDefensePlugin.Log.LogError($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
            }
            return false;
        }

        public GameObject GetPrefab(string bundleName, string prefabName)
        {
            if (cachedPrefabs.TryGetValue(bundleName, out var cache))
            {
                if (cache.TryGetValue(prefabName, out var cached))
                    return cached;
            }

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
                return null;

            // Try exact name
            GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);

            // Try with .prefab extension
            if (prefab == null) prefab = bundle.LoadAsset<GameObject>(prefabName + ".prefab");

            // Search for partial match
            if (prefab == null)
            {
                foreach (var assetName in bundle.GetAllAssetNames())
                {
                    if (assetName.ToLower().Contains(prefabName.ToLower()) && assetName.EndsWith(".prefab"))
                    {
                        prefab = bundle.LoadAsset<GameObject>(assetName);
                        if (prefab != null)
                        {
                            TurretDefensePlugin.LogDebug($"Found prefab {prefabName} as {assetName}");
                            break;
                        }
                    }
                }
            }

            if (prefab != null && cachedPrefabs.ContainsKey(bundleName))
                cachedPrefabs[bundleName][prefabName] = prefab;

            return prefab;
        }

        public string[] GetAssetNames(string bundleName)
        {
            if (loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                return bundle.GetAllAssetNames();
            }
            return new string[0];
        }

        /// <summary>
        /// Get a sprite/texture from an icon bundle
        /// </summary>
        public Sprite GetSprite(string bundleName, string spriteName)
        {
            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                if (!LoadBundle(bundleName))
                    return null;
                bundle = loadedBundles[bundleName];
            }

            // Try loading as Sprite first
            Sprite sprite = bundle.LoadAsset<Sprite>(spriteName);
            if (sprite != null) return sprite;

            // Try with .png extension
            sprite = bundle.LoadAsset<Sprite>(spriteName + ".png");
            if (sprite != null) return sprite;

            // Try loading as Texture2D and convert to Sprite
            Texture2D tex = bundle.LoadAsset<Texture2D>(spriteName);
            if (tex == null) tex = bundle.LoadAsset<Texture2D>(spriteName + ".png");

            // Search for partial match
            if (tex == null)
            {
                foreach (var assetName in bundle.GetAllAssetNames())
                {
                    if (assetName.ToLower().Contains(spriteName.ToLower()) && assetName.EndsWith(".png"))
                    {
                        tex = bundle.LoadAsset<Texture2D>(assetName);
                        if (tex != null)
                        {
                            TurretDefensePlugin.LogDebug($"Found sprite {spriteName} as {assetName}");
                            break;
                        }
                    }
                }
            }

            if (tex != null)
            {
                // Convert texture to sprite
                sprite = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f));
                return sprite;
            }

            return null;
        }
    }

    #endregion

    #region Turret Machine Patches

    /// <summary>
    /// Harmony patches to make turret machines consume power and have turret behavior
    /// </summary>
    [HarmonyPatch]
    internal static class TurretMachinePatches
    {
        // Track turret controllers attached to machine instances
        private static readonly Dictionary<uint, TurretController> turretControllers = new Dictionary<uint, TurretController>();

        // Power consumption values per turret type (in kW)
        private static readonly Dictionary<string, int> turretPowerConsumption = new Dictionary<string, int>
        {
            { TurretDefensePlugin.GatlingTurretMachine, 50 },
            { TurretDefensePlugin.RocketTurretMachine, 80 },
            { TurretDefensePlugin.LaserTurretMachine, 120 },
            { TurretDefensePlugin.RailgunTurretMachine, 200 },
            { TurretDefensePlugin.LightningTurretMachine, 150 }
        };

        /// <summary>
        /// Check if this is one of our turret machines
        /// </summary>
        private static bool IsTurretMachine(ref PowerGeneratorInstance instance)
        {
            if (instance.myDef == null) return false;
            string name = instance.myDef.displayName;
            return name == TurretDefensePlugin.GatlingTurretMachine ||
                   name == TurretDefensePlugin.RocketTurretMachine ||
                   name == TurretDefensePlugin.LaserTurretMachine ||
                   name == TurretDefensePlugin.RailgunTurretMachine ||
                   name == TurretDefensePlugin.LightningTurretMachine;
        }

        /// <summary>
        /// Make turret machines consume power and run turret logic
        /// </summary>
        [HarmonyPatch(typeof(PowerGeneratorInstance), nameof(PowerGeneratorInstance.SimUpdate))]
        [HarmonyPostfix]
        private static void TurretSimUpdate(ref PowerGeneratorInstance __instance)
        {
            try
            {
                if (!IsTurretMachine(ref __instance)) return;

                string turretName = __instance.myDef.displayName;
                uint instanceId = __instance.commonInfo.instanceId;

                // Set power consumption (positive = consuming, negative = generating)
                if (turretPowerConsumption.TryGetValue(turretName, out int powerKW))
                {
                    ref var powerInfo = ref __instance.powerInfo;
                    powerInfo.curPowerConsumption = powerKW;
                    powerInfo.isGenerator = false;
                }

                // Initialize turret controller if needed
                var visual = __instance.commonInfo.refGameObj;
                if (visual != null && !turretControllers.ContainsKey(instanceId))
                {
                    // Add turret controller
                    var controller = visual.GetComponent<TurretController>();
                    if (controller == null)
                    {
                        controller = visual.AddComponent<TurretController>();
                        string turretType = turretName.Replace(" Turret", "");
                        controller.Initialize(turretType);
                        TurretDefensePlugin.ActiveTurrets.Add(controller);
                        TurretDefensePlugin.Log.LogInfo($"Turret controller initialized for {turretName}");
                    }
                    turretControllers[instanceId] = controller;

                    // Apply visual customization
                    ApplyTurretVisuals(visual, turretName);
                }
            }
            catch (Exception ex)
            {
                TurretDefensePlugin.Log.LogError($"TurretSimUpdate error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up when turret is deconstructed
        /// </summary>
        [HarmonyPatch(typeof(GridManager), "RemoveObj", new Type[] { typeof(GenericMachineInstanceRef) })]
        [HarmonyPrefix]
        private static void OnMachineRemoved(GenericMachineInstanceRef machineRef)
        {
            try
            {
                if (machineRef.IsValid() && turretControllers.TryGetValue(machineRef.instanceId, out var controller))
                {
                    TurretDefensePlugin.ActiveTurrets.Remove(controller);
                    turretControllers.Remove(machineRef.instanceId);
                    TurretDefensePlugin.LogDebug($"Turret {machineRef.instanceId} removed");
                }
            }
            catch { }
        }

        /// <summary>
        /// Apply color tint based on turret type
        /// </summary>
        private static void ApplyTurretVisuals(GameObject visual, string turretName)
        {
            Color turretColor = turretName switch
            {
                TurretDefensePlugin.GatlingTurretMachine => new Color(0.7f, 0.7f, 0.8f),     // Steel gray
                TurretDefensePlugin.RocketTurretMachine => new Color(0.9f, 0.4f, 0.2f),      // Orange-red
                TurretDefensePlugin.LaserTurretMachine => new Color(1f, 0.2f, 0.2f),         // Red
                TurretDefensePlugin.RailgunTurretMachine => new Color(0.3f, 0.5f, 1f),       // Blue
                TurretDefensePlugin.LightningTurretMachine => new Color(0.5f, 0.8f, 1f),     // Electric blue
                _ => Color.white
            };

            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_Color", turretColor);
                propBlock.SetColor("_BaseColor", turretColor);
                renderer.SetPropertyBlock(propBlock);
            }
        }
    }

    #endregion
}



