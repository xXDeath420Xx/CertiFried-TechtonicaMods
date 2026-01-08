using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;
using TechtonicaFramework.API;
using TechtonicaFramework.Equipment;
using TechtonicaFramework.Core;
using TechtonicaFramework.TechTree;
using TechtonicaFramework.BuildMenu;

namespace MobilityPlus
{
    /// <summary>
    /// MobilityPlus - Adds enhanced movement equipment and mobility features
    /// Features: Enhanced stilts, speed zones, grapple variants, jump pads
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class MobilityPlusPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.MobilityPlus";
        public const string PluginName = "MobilityPlus";
        public const string VersionString = "1.7.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static MobilityPlusPlugin Instance;

        // Configuration
        public static ConfigEntry<bool> EnableEnhancedStilts;
        public static ConfigEntry<bool> EnableSpeedZones;
        public static ConfigEntry<bool> EnableJumpPads;
        public static ConfigEntry<float> StiltsMk2Height;
        public static ConfigEntry<float> StiltsMk3Height;
        public static ConfigEntry<float> SpeedBoostMultiplier;
        public static ConfigEntry<float> JumpPadForce;
        public static ConfigEntry<bool> DebugMode;

        // Equipment names
        public const string StiltsMk2Name = "Stilts MKII";
        public const string StiltsMk3Name = "Stilts MKIII";
        public const string SpeedBootsName = "Speed Boots";
        public const string JumpPackName = "Jump Pack";
        public const string GrappleMk2Name = "Rail Runner MKII";

        // Unlock names
        public const string AdvancedMobilityUnlock = "Advanced Mobility";
        public const string ExtremeSpeedUnlock = "Extreme Speed Tech";

        // Track speed zones
        private static Dictionary<string, SpeedZoneData> speedZones = new Dictionary<string, SpeedZoneData>();

        // Vehicle system
        public const string HoverPodName = "Hover Pod";
        public const string VehicleUnlock = "Personal Vehicle Tech";
        private static HoverPodController activeVehicle = null;
        public static bool IsPlayerInVehicle => activeVehicle != null && activeVehicle.IsPlayerMounted;

        // Vehicle config
        public static ConfigEntry<bool> EnableVehicles;
        public static ConfigEntry<float> VehicleSpeed;
        public static ConfigEntry<float> VehicleHoverHeight;
        public static ConfigEntry<KeyCode> SummonVehicleKey;
        public static ConfigEntry<KeyCode> DismissVehicleKey;

        // Custom sprites cache
        private static Dictionary<string, Sprite> customSprites = new Dictionary<string, Sprite>();

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            // Load custom icons before registration
            LoadCustomIcons();

            // Register content with EMUAdditions
            RegisterUnlocks();
            RegisterEnhancedStilts();
            RegisterSpeedEquipment();
            RegisterJumpPack();
            RegisterGrappleVariant();
            RegisterSpeedZonePlaceable();
            RegisterVehicle();

            // Hook events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.GameLoaded += OnGameLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableEnhancedStilts = Config.Bind("Stilts", "Enable Enhanced Stilts", true,
                "Enable Stilts MKII and MKIII variants");

            StiltsMk2Height = Config.Bind("Stilts", "Stilts MKII Height", 4f,
                new ConfigDescription("Height increase for Stilts MKII", new AcceptableValueRange<float>(2f, 10f)));

            StiltsMk3Height = Config.Bind("Stilts", "Stilts MKIII Height", 6f,
                new ConfigDescription("Height increase for Stilts MKIII", new AcceptableValueRange<float>(3f, 15f)));

            EnableSpeedZones = Config.Bind("Speed", "Enable Speed Zones", true,
                "Enable placeable speed boost zones");

            SpeedBoostMultiplier = Config.Bind("Speed", "Speed Boost Multiplier", 1.5f,
                new ConfigDescription("Speed multiplier in speed zones", new AcceptableValueRange<float>(1.1f, 3f)));

            EnableJumpPads = Config.Bind("Jump", "Enable Jump Pads", true,
                "Enable placeable jump pads");

            JumpPadForce = Config.Bind("Jump", "Jump Pad Force", 20f,
                new ConfigDescription("Upward force from jump pads", new AcceptableValueRange<float>(5f, 50f)));

            // Vehicle settings
            EnableVehicles = Config.Bind("Vehicle", "Enable Vehicles", true,
                "Enable personal hover vehicle");
            VehicleSpeed = Config.Bind("Vehicle", "Vehicle Speed", 15f,
                new ConfigDescription("Base vehicle movement speed", new AcceptableValueRange<float>(5f, 30f)));
            VehicleHoverHeight = Config.Bind("Vehicle", "Hover Height", 1.5f,
                new ConfigDescription("Height vehicle hovers above ground", new AcceptableValueRange<float>(0.5f, 5f)));
            SummonVehicleKey = Config.Bind("Vehicle", "Summon Vehicle Key", KeyCode.Home,
                "Key to summon personal vehicle (Home key - avoids game keybind conflicts)");
            DismissVehicleKey = Config.Bind("Vehicle", "Dismiss Vehicle Key", KeyCode.End,
                "Key to dismiss/exit vehicle (End key - avoids game keybind conflicts)");

            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void RegisterUnlocks()
        {
            // Use Modded category from TechtonicaFramework
            // Advanced Mobility unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Green,
                coreCountNeeded = 150,
                description = "Research enhanced mobility equipment including improved stilts and speed technology.",
                displayName = AdvancedMobilityUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier5,
                treePosition = 80  // High position to avoid collisions
            });

            // Extreme Speed unlock (later tier)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 300,
                description = "Push the boundaries of movement with experimental speed and jump technology.",
                displayName = ExtremeSpeedUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier6,
                treePosition = 81,
                dependencyNames = new List<string> { AdvancedMobilityUnlock }
            });

            // Personal Vehicle unlock (standalone)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 500,
                description = "Research personal vehicle technology for faster long-distance travel.",
                displayName = VehicleUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier7,
                treePosition = 82
            });

            Log.LogInfo("Unlocks registered to Equipment category");
        }

        private void RegisterEnhancedStilts()
        {
            if (!EnableEnhancedStilts.Value) return;

            // Stilts MKII - Higher and faster
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = StiltsMk2Name,
                description = $"Enhanced stilts that extend to {StiltsMk2Height.Value}m height. Faster extension speed and more stable.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 200,
                unlockName = AdvancedMobilityUnlock,
                parentName = "Hover Pack" // Use base Hover Pack as visual
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_stilts2",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 15f,
                unlockName = AdvancedMobilityUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Hover Pack", 1),
                    new RecipeResourceInfo("Steel Frame", 5),
                    new RecipeResourceInfo("Iron Components", 10)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(StiltsMk2Name, 1)
                },
                sortPriority = 200
            });

            // Stilts MKIII - Even higher with stability assist
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = StiltsMk3Name,
                description = $"Maximum height stilts extending to {StiltsMk3Height.Value}m. Includes gyroscopic stabilization for rough terrain.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 201,
                unlockName = ExtremeSpeedUnlock,
                parentName = "Hover Pack"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_stilts3",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 25f,
                unlockName = ExtremeSpeedUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    // NOTE: Using vanilla Hover Pack instead of modded StiltsMk2 to avoid IndexOutOfRangeException
                    // Modded resources as ingredients cause crafting UI crashes
                    new RecipeResourceInfo("Hover Pack", 1),
                    new RecipeResourceInfo("Processor Unit", 8),
                    new RecipeResourceInfo("Electric Motor", 5),
                    new RecipeResourceInfo("Steel Frame", 15)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(StiltsMk3Name, 1)
                },
                sortPriority = 201
            });

            LogDebug("Enhanced Stilts registered");
        }

        private void RegisterSpeedEquipment()
        {
            // Speed Boots - Passive speed increase
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = SpeedBootsName,
                description = "Motorized boots that increase base movement speed by 25%. Requires battery power.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 210,
                unlockName = AdvancedMobilityUnlock,
                parentName = "Iron Components" // Visual placeholder
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_speedboots",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 20f,
                unlockName = AdvancedMobilityUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Iron Frame", 5),
                    new RecipeResourceInfo("Electric Motor", 2),
                    new RecipeResourceInfo("Copper Wire", 15),
                    new RecipeResourceInfo("Plantmatter Fiber", 10)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(SpeedBootsName, 1)
                },
                sortPriority = 210
            });

            LogDebug("Speed equipment registered");
        }

        private void RegisterJumpPack()
        {
            // Jump Pack - Enhanced jumping
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = JumpPackName,
                description = "Compressed air-powered jump assist. Double-tap jump for a powerful boost. Limited charges, recharges over time.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 220,
                unlockName = ExtremeSpeedUnlock,
                parentName = "Iron Frame"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_jumppack",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 30f,
                unlockName = ExtremeSpeedUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Steel Frame", 10),
                    new RecipeResourceInfo("Mechanical Components", 5),
                    new RecipeResourceInfo("Processor Unit", 2),
                    new RecipeResourceInfo("Electric Motor", 2)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(JumpPackName, 1)
                },
                sortPriority = 220
            });

            LogDebug("Jump Pack registered");
        }

        private void RegisterGrappleVariant()
        {
            // Rail Runner MKII - Faster and longer range
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = GrappleMk2Name,
                description = "Enhanced grappling hook with 50% longer range and faster retraction. Improved grip for heavy loads.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 230,
                unlockName = ExtremeSpeedUnlock,
                parentName = "Railrunner" // Use base Railrunner as visual
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_grapple2",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 25f,
                unlockName = ExtremeSpeedUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Railrunner", 1),
                    new RecipeResourceInfo("Steel Frame", 5),
                    new RecipeResourceInfo("Electric Motor", 3),
                    new RecipeResourceInfo("Copper Wire", 20)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(GrappleMk2Name, 1)
                },
                sortPriority = 230
            });

            LogDebug("Grapple variant registered");
        }

        private void RegisterSpeedZonePlaceable()
        {
            if (!EnableSpeedZones.Value) return;

            // Speed Pad - Placeable speed zone
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = "Speed Pad",
                description = $"A placeable pad that boosts movement speed by {SpeedBoostMultiplier.Value}x when walked over. Perfect for factory floors.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 20,
                sortPriority = 240,
                unlockName = AdvancedMobilityUnlock,
                parentName = "Conveyor Belt" // Visual placeholder
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_speedpad",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 5f,
                unlockName = AdvancedMobilityUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Iron Frame", 2),
                    new RecipeResourceInfo("Electric Motor", 1),
                    new RecipeResourceInfo("Copper Wire", 5)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Speed Pad", 2)
                },
                sortPriority = 240
            });

            if (EnableJumpPads.Value)
            {
                // Jump Pad - Placeable jump boost
                EMUAdditions.AddNewResource(new NewResourceDetails
                {
                    name = "Jump Pad",
                    description = $"A placeable pad that launches the player upward when stepped on. Great for vertical navigation.",
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    headerTitle = "Modded",
                    // subHeaderTitle inherited from parent
                    maxStackCount = 20,
                    sortPriority = 241,
                    unlockName = ExtremeSpeedUnlock,
                    parentName = "Conveyor Belt"
                });

                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    GUID = MyGUID + "_jumppad",
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 8f,
                    unlockName = ExtremeSpeedUnlock,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Steel Frame", 3),
                        new RecipeResourceInfo("Electric Motor", 2),
                        new RecipeResourceInfo("Mechanical Components", 3)
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Jump Pad", 2)
                    },
                    sortPriority = 241
                });
            }

            LogDebug("Speed/Jump pads registered");
        }

        private void RegisterVehicle()
        {
            if (!EnableVehicles.Value) return;

            // Hover Pod - Personal vehicle
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = HoverPodName,
                description = $"A personal hover vehicle for fast travel. Press V to summon, B to dismiss. Speed: {VehicleSpeed.Value}m/s.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 1,
                sortPriority = 250,
                unlockName = VehicleUnlock,
                parentName = "Hover Pack"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_hoverpod",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 60f,
                unlockName = VehicleUnlock,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Hover Pack", 2),
                    new RecipeResourceInfo("Steel Frame", 20),
                    new RecipeResourceInfo("Electric Motor", 5),
                    new RecipeResourceInfo("Processor Unit", 5),
                    new RecipeResourceInfo("Copper Wire", 30)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(HoverPodName, 1)
                },
                sortPriority = 250
            });

            LogDebug("Vehicle registered");
        }

        private void OnGameDefinesLoaded()
        {
            // Link unlocks to resources - CRITICAL for crafting to work
            LinkUnlockToResource(StiltsMk2Name, AdvancedMobilityUnlock);
            LinkUnlockToResource(StiltsMk3Name, ExtremeSpeedUnlock);
            LinkUnlockToResource(SpeedBootsName, AdvancedMobilityUnlock);
            LinkUnlockToResource(JumpPackName, ExtremeSpeedUnlock);
            LinkUnlockToResource(GrappleMk2Name, ExtremeSpeedUnlock);
            LinkUnlockToResource("Speed Pad", AdvancedMobilityUnlock);
            LinkUnlockToResource("Jump Pad", ExtremeSpeedUnlock);
            LinkUnlockToResource(HoverPodName, VehicleUnlock);

            // Apply custom sprites to resources
            ApplyCustomSprites();

            Log.LogInfo("Linked unlocks to resources");
        }

        private void LinkUnlockToResource(string resourceName, string unlockName)
        {
            try
            {
                ResourceInfo info = EMU.Resources.GetResourceInfoByName(resourceName);
                if (info != null)
                {
                    info.unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                    LogDebug($"Linked {resourceName} to unlock {unlockName}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to link {resourceName} to {unlockName}: {ex.Message}");
            }
        }

        private void OnGameLoaded()
        {
            // Register equipment effects with framework
            RegisterEquipmentEffects();
        }

        private void OnTechTreeStateLoaded()
        {
            // PT level tier mapping: LIMA=1-4, VICTOR=5-11, XRAY=12-16, SIERRA=17-24
            // Advanced Mobility: VICTOR (Tier6), position 50
            // Extreme Speed: VICTOR (Tier9), position 50
            // Personal Vehicle: XRAY (Tier13), position 50
            ConfigureUnlock(AdvancedMobilityUnlock, "Hover Pack", TechTreeState.ResearchTier.Tier6, 50);
            ConfigureUnlock(ExtremeSpeedUnlock, "Hover Pack", TechTreeState.ResearchTier.Tier9, 50);
            ConfigureUnlock(VehicleUnlock, "Hover Pack", TechTreeState.ResearchTier.Tier13, 50);
            Log.LogInfo("Configured MobilityPlus unlock tiers");
        }

        private void ConfigureUnlock(string unlockName, string spriteSourceName, TechTreeState.ResearchTier tier, int position)
        {
            try
            {
                Unlock unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                if (unlock == null) return;

                // Set correct tier explicitly
                unlock.requiredTier = tier;

                // Set explicit position to avoid collisions
                unlock.treePosition = position;

                // Copy sprite from source
                if (unlock.sprite == null)
                {
                    ResourceInfo sourceRes = EMU.Resources.GetResourceInfoByName(spriteSourceName);
                    if (sourceRes != null && sourceRes.sprite != null)
                    {
                        unlock.sprite = sourceRes.sprite;
                    }
                    else
                    {
                        Unlock sourceUnlock = EMU.Unlocks.GetUnlockByName(spriteSourceName);
                        if (sourceUnlock != null && sourceUnlock.sprite != null)
                        {
                            unlock.sprite = sourceUnlock.sprite;
                        }
                    }
                }

                Log.LogInfo($"Configured {unlockName}: tier={tier}, position={position}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure unlock {unlockName}: {ex.Message}");
            }
        }

        private void RegisterEquipmentEffects()
        {
            try
            {
                // Speed Boots effect
                FrameworkAPI.RegisterEquipment(
                    "speed_boots",
                    SpeedBootsName,
                    "Motorized speed boots",
                    EquipmentSlot.Feet,
                    onEquip: (player) =>
                    {
                        try { FrameworkAPI.ApplyMovementModifier("speed_boots", 1.25f, 1f, -1f); }
                        catch { }
                        LogDebug("Speed Boots equipped - 25% speed boost active");
                    },
                    onUnequip: (player) =>
                    {
                        try { FrameworkAPI.RemoveMovementModifier("speed_boots"); }
                        catch { }
                        LogDebug("Speed Boots unequipped");
                    }
                );

                // Jump Pack effect
                FrameworkAPI.RegisterEquipment(
                    "jump_pack",
                    JumpPackName,
                    "Compressed air jump assist",
                    EquipmentSlot.Body,
                    onEquip: (player) =>
                    {
                        try { FrameworkAPI.ApplyMovementModifier("jump_pack", 1f, 1.5f, -1f); }
                        catch { }
                        LogDebug("Jump Pack equipped - 50% jump boost active");
                    },
                    onUnequip: (player) =>
                    {
                        try { FrameworkAPI.RemoveMovementModifier("jump_pack"); }
                        catch { }
                        LogDebug("Jump Pack unequipped");
                    }
                );
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to register equipment effects (TechtonicaFramework may not be ready): {ex.Message}");
            }
        }

        private void Update()
        {
            // Process speed zones
            ProcessSpeedZones();

            // Vehicle input handling
            if (EnableVehicles.Value)
            {
                HandleVehicleInput();
            }
        }

        private void HandleVehicleInput()
        {
            var player = Player.instance;
            if (player == null) return;

            // Summon vehicle
            if (Input.GetKeyDown(SummonVehicleKey.Value))
            {
                if (activeVehicle == null)
                {
                    SummonVehicle(player);
                }
                else if (!activeVehicle.IsPlayerMounted)
                {
                    // Mount if nearby
                    float dist = Vector3.Distance(player.transform.position, activeVehicle.transform.position);
                    if (dist < 5f)
                    {
                        activeVehicle.Mount(player);
                    }
                    else
                    {
                        // Recall vehicle to player
                        activeVehicle.RecallToPlayer(player);
                    }
                }
            }

            // Dismiss/Exit vehicle
            if (Input.GetKeyDown(DismissVehicleKey.Value))
            {
                if (activeVehicle != null)
                {
                    if (activeVehicle.IsPlayerMounted)
                    {
                        activeVehicle.Dismount();
                    }
                    else
                    {
                        DismissVehicle();
                    }
                }
            }
        }

        private void SummonVehicle(Player player)
        {
            // TODO: Check if player has Hover Pod in inventory

            Vector3 spawnPos = player.transform.position + player.transform.forward * 3f;
            spawnPos.y += VehicleHoverHeight.Value;

            GameObject vehicleObj = CreateHoverPodVisual(spawnPos);
            activeVehicle = vehicleObj.AddComponent<HoverPodController>();
            activeVehicle.speed = VehicleSpeed.Value;
            activeVehicle.hoverHeight = VehicleHoverHeight.Value;

            Log.LogInfo("Hover Pod summoned! Press Home near it to mount, End to dismiss.");
        }

        private void DismissVehicle()
        {
            if (activeVehicle != null)
            {
                UnityEngine.Object.Destroy(activeVehicle.gameObject);
                activeVehicle = null;
                Log.LogInfo("Hover Pod dismissed.");
            }
        }

        // Asset bundle for hover pod model
        private static AssetBundle vehicleBundle;
        private static GameObject vehiclePrefab;
        private static Material cachedURPMaterial;

        private void LoadVehicleAssets()
        {
            if (vehicleBundle != null) return;

            string bundlePath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(Info.Location), "Bundles");
            string[] bundleNames = { "drones_scifi", "drones_voodooplay", "hoverbuggy" };

            foreach (var bundleName in bundleNames)
            {
                string fullPath = System.IO.Path.Combine(bundlePath, bundleName);
                if (System.IO.File.Exists(fullPath))
                {
                    try
                    {
                        vehicleBundle = AssetBundle.LoadFromFile(fullPath);
                        if (vehicleBundle != null)
                        {
                            Log.LogInfo($"Loaded vehicle bundle: {bundleName}");
                            // Try to find a suitable hover pod prefab
                            foreach (var assetName in vehicleBundle.GetAllAssetNames())
                            {
                                if (assetName.Contains("drone") && assetName.EndsWith(".prefab"))
                                {
                                    vehiclePrefab = vehicleBundle.LoadAsset<GameObject>(assetName);
                                    if (vehiclePrefab != null)
                                    {
                                        Log.LogInfo($"Loaded vehicle prefab: {assetName}");
                                        break;
                                    }
                                }
                            }
                            if (vehiclePrefab != null) break;
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogWarning($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
            }
        }

        private static void FixMaterialsForURP(GameObject obj)
        {
            if (cachedURPMaterial == null)
            {
                var gameRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                foreach (var r in gameRenderers)
                {
                    if (r.material != null && r.material.shader != null &&
                        r.material.shader.name.Contains("Universal"))
                    {
                        cachedURPMaterial = r.material;
                        break;
                    }
                }
            }

            if (cachedURPMaterial == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var oldMat = materials[i];
                    if (oldMat != null && oldMat.shader != null &&
                        !oldMat.shader.name.Contains("Universal") && !oldMat.shader.name.Contains("URP"))
                    {
                        // Preserve textures when converting to URP
                        Color originalColor = oldMat.HasProperty("_Color") ? oldMat.GetColor("_Color") : oldMat.color;
                        Texture mainTex = oldMat.HasProperty("_MainTex") ? oldMat.GetTexture("_MainTex") : null;

                        var newMat = new Material(cachedURPMaterial);
                        newMat.color = originalColor;
                        if (newMat.HasProperty("_BaseColor")) newMat.SetColor("_BaseColor", originalColor);
                        if (mainTex != null)
                        {
                            if (newMat.HasProperty("_MainTex")) newMat.SetTexture("_MainTex", mainTex);
                            if (newMat.HasProperty("_BaseMap")) newMat.SetTexture("_BaseMap", mainTex);
                        }
                        materials[i] = newMat;
                    }
                }
                renderer.materials = materials;
            }
        }

        private GameObject CreateHoverPodVisual(Vector3 position)
        {
            GameObject pod = null;

            // Try to load real 3D model first
            LoadVehicleAssets();
            if (vehiclePrefab != null)
            {
                pod = UnityEngine.Object.Instantiate(vehiclePrefab, position, Quaternion.identity);
                pod.name = "HoverPod";
                FixMaterialsForURP(pod);

                // Scale up to vehicle size (drones are usually small)
                pod.transform.localScale = Vector3.one * 2.5f;

                // Remove any existing colliders from prefab
                foreach (var col in pod.GetComponentsInChildren<Collider>())
                {
                    UnityEngine.Object.Destroy(col);
                }

                Log.LogInfo("Created Hover Pod from 3D asset");
            }
            else
            {
                // Fallback: create a better-looking sci-fi pod from primitives
                pod = CreateFallbackHoverPod(position);
                Log.LogWarning("Using fallback primitive Hover Pod - bundle not found");
            }

            // Add physics components
            var collider = pod.AddComponent<BoxCollider>();
            collider.size = new Vector3(2f, 1f, 3f);
            collider.center = Vector3.zero;

            var rb = pod.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.mass = 100f; // Heavier for stability
            rb.drag = 8f; // Much higher drag to reduce drift
            rb.angularDrag = 10f; // High angular drag for rotation stability
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.interpolation = RigidbodyInterpolation.Interpolate; // Smooth movement

            return pod;
        }

        private GameObject CreateFallbackHoverPod(Vector3 position)
        {
            GameObject pod = new GameObject("HoverPod");
            pod.transform.position = position;

            Color bodyColor = new Color(0.15f, 0.2f, 0.3f);
            Color accentColor = new Color(0.3f, 0.6f, 0.9f);
            Color glowColor = new Color(0.2f, 0.7f, 1f);

            // Main body - sleek ellipsoid
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(pod.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            body.transform.localScale = new Vector3(1.2f, 1.8f, 0.8f);
            body.GetComponent<Renderer>().material.color = bodyColor;
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Cockpit canopy
            GameObject cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cockpit.transform.SetParent(pod.transform);
            cockpit.transform.localPosition = new Vector3(0, 0.35f, 0.4f);
            cockpit.transform.localScale = new Vector3(0.9f, 0.45f, 0.7f);
            var cockpitMat = cockpit.GetComponent<Renderer>().material;
            cockpitMat.color = new Color(0.4f, 0.7f, 0.95f, 0.6f);
            UnityEngine.Object.Destroy(cockpit.GetComponent<Collider>());

            // Thruster housings (4 corners, lower profile)
            Vector3[] thrusterPositions = {
                new Vector3(-0.9f, -0.25f, 0.6f),
                new Vector3(0.9f, -0.25f, 0.6f),
                new Vector3(-0.9f, -0.25f, -0.6f),
                new Vector3(0.9f, -0.25f, -0.6f)
            };

            foreach (var thrusterPos in thrusterPositions)
            {
                GameObject thruster = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                thruster.transform.SetParent(pod.transform);
                thruster.transform.localPosition = thrusterPos;
                thruster.transform.localScale = new Vector3(0.25f, 0.1f, 0.25f);
                thruster.GetComponent<Renderer>().material.color = accentColor;
                UnityEngine.Object.Destroy(thruster.GetComponent<Collider>());

                // Thruster glow
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.transform.SetParent(thruster.transform);
                glow.transform.localPosition = Vector3.down * 0.3f;
                glow.transform.localScale = new Vector3(0.8f, 0.4f, 0.8f);
                glow.GetComponent<Renderer>().material.color = glowColor;
                UnityEngine.Object.Destroy(glow.GetComponent<Collider>());
            }

            // Add a point light for glow effect
            var lightObj = new GameObject("HoverLight");
            lightObj.transform.SetParent(pod.transform);
            lightObj.transform.localPosition = Vector3.down * 0.5f;
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = glowColor;
            light.intensity = 1.5f;
            light.range = 4f;

            return pod;
        }

        private void ProcessSpeedZones()
        {
            var player = Player.instance;
            if (player == null) return;

            Vector3 playerPos = player.transform.position;

            foreach (var zone in speedZones.Values)
            {
                float distance = Vector3.Distance(playerPos, zone.Position);
                bool isInside = distance <= zone.Radius;

                if (isInside && !zone.PlayerInside)
                {
                    // Entered zone
                    zone.PlayerInside = true;
                    try { FrameworkAPI.ApplyMovementModifier($"speedzone_{zone.Id}", zone.SpeedMultiplier, 1f, -1f); }
                    catch { }
                    LogDebug($"Entered speed zone {zone.Id}");
                }
                else if (!isInside && zone.PlayerInside)
                {
                    // Exited zone
                    zone.PlayerInside = false;
                    try { FrameworkAPI.RemoveMovementModifier($"speedzone_{zone.Id}"); }
                    catch { }
                    LogDebug($"Exited speed zone {zone.Id}");
                }
            }
        }

        /// <summary>
        /// Create a speed zone at a position
        /// </summary>
        public static void CreateSpeedZone(string id, Vector3 position, float radius, float multiplier)
        {
            speedZones[id] = new SpeedZoneData
            {
                Id = id,
                Position = position,
                Radius = radius,
                SpeedMultiplier = multiplier,
                PlayerInside = false
            };
            LogDebug($"Created speed zone {id} at {position}");
        }

        /// <summary>
        /// Remove a speed zone
        /// </summary>
        public static void RemoveSpeedZone(string id)
        {
            if (speedZones.TryGetValue(id, out var zone))
            {
                if (zone.PlayerInside)
                {
                    FrameworkAPI.RemoveMovementModifier($"speedzone_{id}");
                }
                speedZones.Remove(id);
            }
        }

        public static void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }

        /// <summary>
        /// Clone a sprite from an existing game resource to match Techtonica's icon style
        /// </summary>
        private static Sprite CloneGameSprite(string sourceResourceName)
        {
            try
            {
                ResourceInfo sourceResource = EMU.Resources.GetResourceInfoByName(sourceResourceName);
                if (sourceResource != null && sourceResource.sprite != null)
                {
                    Log.LogInfo($"Cloned sprite from {sourceResourceName}");
                    return sourceResource.sprite;
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to clone sprite from {sourceResourceName}: {ex.Message}");
            }
            return null;
        }

        /// <summary>
        /// Load all custom icons by cloning from existing game resources
        /// This ensures icons match Techtonica's visual style
        /// </summary>
        private void LoadCustomIcons()
        {
            // Clone sprites from existing similar items to match game aesthetic
            // Stilts MKII/MKIII - use base Stilts sprite
            Sprite stiltsSprite = CloneGameSprite("Stilts");
            if (stiltsSprite != null)
            {
                customSprites[StiltsMk2Name] = stiltsSprite;
                customSprites[StiltsMk3Name] = stiltsSprite;
            }

            // Speed Boots - use Stilts sprite (leg equipment)
            customSprites[SpeedBootsName] = CloneGameSprite("Stilts");

            // Jump Pack - use M.O.L.E. sprite (mobility equipment)
            customSprites[JumpPackName] = CloneGameSprite("M.O.L.E.");

            // Hover Pod - use M.O.L.E. sprite (vehicle)
            customSprites[HoverPodName] = CloneGameSprite("M.O.L.E.");

            // Rail Runner MKII - use Rail Runner sprite
            customSprites[GrappleMk2Name] = CloneGameSprite("Rail Runner");

            int loaded = customSprites.Values.Count(s => s != null);
            Log.LogInfo($"Cloned {loaded}/{customSprites.Count} sprites from game assets");
        }

        /// <summary>
        /// Apply custom sprites to resources by name
        /// </summary>
        private void ApplyCustomSprites()
        {
            foreach (var kvp in customSprites)
            {
                string resourceName = kvp.Key;
                Sprite sprite = kvp.Value;

                try
                {
                    ResourceInfo info = EMU.Resources.GetResourceInfoByName(resourceName);
                    if (info != null && sprite != null)
                    {
                        // Use reflection to set the rawSprite field
                        var spriteField = typeof(ResourceInfo).GetField("rawSprite", BindingFlags.Public | BindingFlags.Instance);
                        if (spriteField != null)
                        {
                            spriteField.SetValue(info, sprite);
                            LogDebug($"Applied custom sprite to {resourceName}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"Failed to apply sprite to {resourceName}: {ex.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Speed zone data
    /// </summary>
    public class SpeedZoneData
    {
        public string Id;
        public Vector3 Position;
        public float Radius;
        public float SpeedMultiplier;
        public bool PlayerInside;
    }

    /// <summary>
    /// Hover Pod vehicle controller - handles mounting, movement, and stable hover physics
    /// Uses PID-style control for smooth, wobble-free hovering
    /// </summary>
    public class HoverPodController : MonoBehaviour
    {
        public float speed = 15f;
        public float hoverHeight = 1.5f;
        public float rotationSpeed = 120f;
        public float maxTiltAngle = 8f;

        private Rigidbody rb;
        private Player mountedPlayer;
        private bool playerMounted = false;

        // PID controller values for stable hover
        private float hoverProportional = 80f;  // Spring strength
        private float hoverDamping = 25f;       // Velocity damping
        private float hoverIntegral = 5f;       // Accumulated error correction
        private float integralError = 0f;
        private float maxIntegralError = 5f;

        // Smoothed values for stability
        private float smoothedTargetHeight;
        private Quaternion targetRotation;

        public bool IsPlayerMounted => playerMounted;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.mass = 100f;
                rb.drag = 8f;
                rb.angularDrag = 10f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
            }

            smoothedTargetHeight = hoverHeight;
            targetRotation = transform.rotation;
        }

        private void Update()
        {
            if (playerMounted && mountedPlayer != null)
            {
                HandleMountedInput();
            }
        }

        private void FixedUpdate()
        {
            ApplyStableHover();

            if (playerMounted && mountedPlayer != null)
            {
                ApplyMovement();
                UpdatePlayerPosition();
            }
            else
            {
                // When idle, slowly stabilize rotation
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.Euler(0, transform.eulerAngles.y, 0), Time.fixedDeltaTime * 2f);
            }
        }

        private void ApplyStableHover()
        {
            // Smooth height target changes
            smoothedTargetHeight = Mathf.Lerp(smoothedTargetHeight, hoverHeight, Time.fixedDeltaTime * 3f);

            // Find ground height using multiple raycasts for stability
            float groundHeight = GetGroundHeight();

            if (groundHeight < float.MaxValue)
            {
                float currentHeight = transform.position.y - groundHeight;
                float heightError = smoothedTargetHeight - currentHeight;

                // Accumulate integral error (with limits to prevent windup)
                integralError = Mathf.Clamp(integralError + heightError * Time.fixedDeltaTime,
                    -maxIntegralError, maxIntegralError);

                // PID control: P (proportional) + I (integral) + D (derivative/damping)
                float verticalForce =
                    heightError * hoverProportional +           // Spring force
                    integralError * hoverIntegral -             // Accumulated error correction
                    rb.velocity.y * hoverDamping;               // Velocity damping

                rb.AddForce(Vector3.up * verticalForce, ForceMode.Acceleration);
            }
            else
            {
                // No ground detected - gentle descent
                rb.AddForce(Vector3.down * 10f, ForceMode.Acceleration);
                integralError = 0f;
            }
        }

        private float GetGroundHeight()
        {
            // Use multiple raycasts for stability on uneven terrain
            Vector3[] offsets = {
                Vector3.zero,
                Vector3.forward * 0.5f,
                Vector3.back * 0.5f,
                Vector3.left * 0.5f,
                Vector3.right * 0.5f
            };

            float minHeight = float.MaxValue;
            int hits = 0;
            float totalHeight = 0f;

            foreach (var offset in offsets)
            {
                Vector3 rayStart = transform.position + transform.TransformDirection(offset);
                if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, hoverHeight * 4f))
                {
                    totalHeight += hit.point.y;
                    hits++;
                    if (hit.point.y < minHeight) minHeight = hit.point.y;
                }
            }

            // Return average ground height for stability
            return hits > 0 ? totalHeight / hits : float.MaxValue;
        }

        private void HandleMountedInput()
        {
            // Height adjustment with PageUp/PageDown (avoids Space/Ctrl conflicts with game jump/crouch)
            if (Input.GetKey(KeyCode.PageUp))
            {
                hoverHeight = Mathf.Min(hoverHeight + Time.deltaTime * 4f, 12f);
            }
            else if (Input.GetKey(KeyCode.PageDown))
            {
                hoverHeight = Mathf.Max(hoverHeight - Time.deltaTime * 4f, 0.5f);
            }
        }

        private void ApplyMovement()
        {
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Get camera-relative direction
            Vector3 moveDirection = Vector3.zero;
            if (Camera.main != null)
            {
                Vector3 camForward = Camera.main.transform.forward;
                Vector3 camRight = Camera.main.transform.right;
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();
                moveDirection = (camForward * vertical + camRight * horizontal);
            }
            else
            {
                moveDirection = (transform.forward * vertical + transform.right * horizontal);
            }

            // Apply horizontal movement force
            if (moveDirection.sqrMagnitude > 0.01f)
            {
                moveDirection = moveDirection.normalized;

                // Smooth acceleration
                Vector3 targetVel = moveDirection * speed;
                Vector3 currentHorizontalVel = new Vector3(rb.velocity.x, 0, rb.velocity.z);
                Vector3 velDiff = targetVel - currentHorizontalVel;

                rb.AddForce(velDiff * 5f, ForceMode.Acceleration);

                // Smooth rotation towards movement direction
                targetRotation = Quaternion.LookRotation(moveDirection);
            }

            // Apply smooth rotation (Y axis only)
            float currentY = transform.eulerAngles.y;
            float targetY = targetRotation.eulerAngles.y;
            float newY = Mathf.LerpAngle(currentY, targetY, Time.fixedDeltaTime * 5f);

            // Calculate tilt based on velocity (purely visual, non-cumulative)
            Vector3 localVel = transform.InverseTransformDirection(rb.velocity);
            float forwardTilt = Mathf.Clamp(-localVel.z / speed * maxTiltAngle, -maxTiltAngle, maxTiltAngle);
            float sideTilt = Mathf.Clamp(localVel.x / speed * maxTiltAngle * 0.5f, -maxTiltAngle * 0.5f, maxTiltAngle * 0.5f);

            // Apply rotation directly (not additively) to prevent accumulation
            transform.rotation = Quaternion.Euler(forwardTilt, newY, -sideTilt);

            // Speed cap
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (horizontalVelocity.magnitude > speed)
            {
                horizontalVelocity = horizontalVelocity.normalized * speed;
                rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
            }
        }

        private void UpdatePlayerPosition()
        {
            if (mountedPlayer != null)
            {
                // Player stays attached via parenting, no need for manual position updates
            }
        }

        public void Mount(Player player)
        {
            if (playerMounted) return;

            mountedPlayer = player;
            playerMounted = true;

            // Disable player's normal movement
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Parent player to vehicle
            player.transform.SetParent(transform);
            player.transform.localPosition = Vector3.up * 0.6f;
            player.transform.localRotation = Quaternion.identity;

            // Reset integral error for fresh hover
            integralError = 0f;

            MobilityPlusPlugin.Log.LogInfo("Mounted Hover Pod! WASD to move, PageUp/PageDown for altitude, End to dismount.");
        }

        public void Dismount()
        {
            if (!playerMounted || mountedPlayer == null) return;

            // Unparent player
            mountedPlayer.transform.SetParent(null);

            // Position player beside vehicle
            Vector3 dismountPos = transform.position + transform.right * 2.5f;
            if (Physics.Raycast(dismountPos + Vector3.up * 5f, Vector3.down, out RaycastHit hit, 20f))
            {
                dismountPos = hit.point + Vector3.up * 0.1f;
            }
            mountedPlayer.transform.position = dismountPos;

            // Re-enable player movement
            var controller = mountedPlayer.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = true;
            }

            mountedPlayer = null;
            playerMounted = false;

            MobilityPlusPlugin.Log.LogInfo("Dismounted from Hover Pod.");
        }

        public void RecallToPlayer(Player player)
        {
            if (playerMounted) return;

            Vector3 targetPos = player.transform.position + player.transform.forward * 3f;
            targetPos.y = player.transform.position.y + hoverHeight;

            transform.position = targetPos;
            rb.velocity = Vector3.zero;
            integralError = 0f;

            MobilityPlusPlugin.Log.LogInfo("Hover Pod recalled! Press Home to mount.");
        }

        private void OnDestroy()
        {
            if (playerMounted && mountedPlayer != null)
            {
                Dismount();
            }
        }
    }

    // NOTE: Harmony patches disabled until correct class/method signatures are verified
    // The Stilts and RailRunner classes may have different field names in the actual game
    /*
    [HarmonyPatch]
    public static class MobilityPatches
    {
        [HarmonyPatch(typeof(Stilts), "OnUpdate")]
        [HarmonyPostfix]
        public static void ModifyStiltsHeight(Stilts __instance) { }

        [HarmonyPatch(typeof(RailRunner), "OnUpdate")]
        [HarmonyPostfix]
        public static void ModifyRailRunnerRange(RailRunner __instance) { }
    }
    */
}



