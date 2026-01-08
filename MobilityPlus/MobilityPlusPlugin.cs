using System;
using System.Collections.Generic;
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

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

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
            SummonVehicleKey = Config.Bind("Vehicle", "Summon Vehicle Key", KeyCode.V,
                "Key to summon personal vehicle");
            DismissVehicleKey = Config.Bind("Vehicle", "Dismiss Vehicle Key", KeyCode.B,
                "Key to dismiss/exit vehicle");

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

            Log.LogInfo("Hover Pod summoned! Press V near it to mount, B to dismiss.");
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

        private GameObject CreateHoverPodVisual(Vector3 position)
        {
            GameObject pod = new GameObject("HoverPod");
            pod.transform.position = position;

            Color bodyColor = new Color(0.2f, 0.3f, 0.5f); // Blue-gray
            Color accentColor = new Color(0.4f, 0.7f, 0.9f); // Light blue
            Color glowColor = new Color(0.3f, 0.8f, 1f); // Cyan glow

            // Main body (flattened capsule)
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(pod.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90f, 0, 0);
            body.transform.localScale = new Vector3(1.5f, 2f, 1f);
            body.GetComponent<Renderer>().material.color = bodyColor;
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Cockpit dome
            GameObject cockpit = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            cockpit.transform.SetParent(pod.transform);
            cockpit.transform.localPosition = Vector3.up * 0.4f + Vector3.forward * 0.3f;
            cockpit.transform.localScale = new Vector3(0.8f, 0.5f, 0.8f);
            cockpit.GetComponent<Renderer>().material.color = new Color(0.5f, 0.8f, 1f, 0.5f);
            UnityEngine.Object.Destroy(cockpit.GetComponent<Collider>());

            // Hover engines (4 corners)
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f + Mathf.PI / 4f;
                Vector3 enginePos = new Vector3(Mathf.Cos(angle) * 1.2f, -0.3f, Mathf.Sin(angle) * 0.8f);

                GameObject engine = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                engine.transform.SetParent(pod.transform);
                engine.transform.localPosition = enginePos;
                engine.transform.localScale = new Vector3(0.3f, 0.15f, 0.3f);
                engine.GetComponent<Renderer>().material.color = accentColor;
                UnityEngine.Object.Destroy(engine.GetComponent<Collider>());

                // Engine glow
                GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                glow.transform.SetParent(engine.transform);
                glow.transform.localPosition = Vector3.down * 0.5f;
                glow.transform.localScale = new Vector3(0.7f, 0.3f, 0.7f);
                glow.GetComponent<Renderer>().material.color = glowColor;
                UnityEngine.Object.Destroy(glow.GetComponent<Collider>());
            }

            // Main collider for the vehicle
            var collider = pod.AddComponent<BoxCollider>();
            collider.size = new Vector3(3f, 1f, 2f);
            collider.center = Vector3.zero;

            var rb = pod.AddComponent<Rigidbody>();
            rb.useGravity = false;
            rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            rb.drag = 3f;

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
    /// Hover Pod vehicle controller - handles mounting, movement, and hover physics
    /// </summary>
    public class HoverPodController : MonoBehaviour
    {
        public float speed = 15f;
        public float hoverHeight = 1.5f;
        public float hoverForce = 50f;
        public float rotationSpeed = 100f;
        public float tiltAmount = 15f;

        private Rigidbody rb;
        private Player mountedPlayer;
        private Vector3 originalPlayerPosition;
        private bool playerMounted = false;
        private float engineHum = 0f;
        private float bobOffset = 0f;

        public bool IsPlayerMounted => playerMounted;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            if (rb == null)
            {
                rb = gameObject.AddComponent<Rigidbody>();
                rb.useGravity = false;
                rb.drag = 3f;
                rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
            }
        }

        private void Update()
        {
            // Idle bobbing animation
            bobOffset += Time.deltaTime * 2f;

            if (playerMounted && mountedPlayer != null)
            {
                HandleMountedMovement();
                UpdatePlayerPosition();
            }
            else
            {
                // Idle hover bob
                ApplyIdleHover();
            }
        }

        private void FixedUpdate()
        {
            ApplyHoverPhysics();
        }

        private void ApplyHoverPhysics()
        {
            // Raycast down to maintain hover height
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, hoverHeight * 3f))
            {
                float currentHeight = hit.distance;
                float heightError = hoverHeight - currentHeight;

                // Apply upward force to maintain hover height
                Vector3 hoverForceVec = Vector3.up * heightError * hoverForce;
                rb.AddForce(hoverForceVec, ForceMode.Acceleration);
            }
            else
            {
                // No ground below - gentle descent
                rb.AddForce(Vector3.down * 5f, ForceMode.Acceleration);
            }

            // Dampen vertical oscillation
            if (Mathf.Abs(rb.velocity.y) > 0.1f)
            {
                rb.velocity = new Vector3(rb.velocity.x, rb.velocity.y * 0.95f, rb.velocity.z);
            }
        }

        private void ApplyIdleHover()
        {
            // Gentle bobbing when idle
            float bob = Mathf.Sin(bobOffset) * 0.05f;
            transform.position = new Vector3(
                transform.position.x,
                transform.position.y + bob * Time.deltaTime,
                transform.position.z
            );
        }

        private void HandleMountedMovement()
        {
            // Get input
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            // Calculate movement direction based on camera/player facing
            Vector3 moveDirection = Vector3.zero;

            if (Camera.main != null)
            {
                Vector3 camForward = Camera.main.transform.forward;
                Vector3 camRight = Camera.main.transform.right;

                // Flatten to horizontal plane
                camForward.y = 0;
                camRight.y = 0;
                camForward.Normalize();
                camRight.Normalize();

                moveDirection = (camForward * vertical + camRight * horizontal).normalized;
            }
            else
            {
                moveDirection = (transform.forward * vertical + transform.right * horizontal).normalized;
            }

            // Apply movement
            if (moveDirection.magnitude > 0.1f)
            {
                rb.AddForce(moveDirection * speed, ForceMode.Acceleration);

                // Rotate to face movement direction
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.RotateTowards(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

                // Apply forward tilt when moving
                float currentSpeed = rb.velocity.magnitude;
                float tilt = Mathf.Clamp(currentSpeed / speed, 0, 1) * tiltAmount;
                transform.localRotation *= Quaternion.Euler(tilt, 0, 0);
            }

            // Speed cap
            Vector3 horizontalVelocity = new Vector3(rb.velocity.x, 0, rb.velocity.z);
            if (horizontalVelocity.magnitude > speed)
            {
                horizontalVelocity = horizontalVelocity.normalized * speed;
                rb.velocity = new Vector3(horizontalVelocity.x, rb.velocity.y, horizontalVelocity.z);
            }

            // Ascend/Descend with Space/Ctrl
            if (Input.GetKey(KeyCode.Space))
            {
                hoverHeight = Mathf.Min(hoverHeight + Time.deltaTime * 3f, 10f);
            }
            else if (Input.GetKey(KeyCode.LeftControl))
            {
                hoverHeight = Mathf.Max(hoverHeight - Time.deltaTime * 3f, 0.5f);
            }

            // Engine sound simulation (visual only - particles)
            engineHum = Mathf.Lerp(engineHum, moveDirection.magnitude > 0.1f ? 1f : 0.3f, Time.deltaTime * 3f);
        }

        private void UpdatePlayerPosition()
        {
            if (mountedPlayer != null)
            {
                // Keep player attached to vehicle
                Vector3 seatPosition = transform.position + Vector3.up * 0.5f;
                mountedPlayer.transform.position = seatPosition;
            }
        }

        /// <summary>
        /// Mount a player onto the vehicle
        /// </summary>
        public void Mount(Player player)
        {
            if (playerMounted) return;

            mountedPlayer = player;
            playerMounted = true;
            originalPlayerPosition = player.transform.position;

            // Disable player's normal movement
            var controller = player.GetComponent<CharacterController>();
            if (controller != null)
            {
                controller.enabled = false;
            }

            // Parent player to vehicle for smooth following
            player.transform.SetParent(transform);
            player.transform.localPosition = Vector3.up * 0.5f;

            MobilityPlusPlugin.Log.LogInfo("Mounted Hover Pod! Use WASD to move, Space/Ctrl for altitude, B to dismount.");
        }

        /// <summary>
        /// Dismount the player from the vehicle
        /// </summary>
        public void Dismount()
        {
            if (!playerMounted || mountedPlayer == null) return;

            // Unparent player
            mountedPlayer.transform.SetParent(null);

            // Position player beside vehicle
            Vector3 dismountPos = transform.position + transform.right * 2f;

            // Ensure ground contact
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

        /// <summary>
        /// Recall the vehicle to the player's position
        /// </summary>
        public void RecallToPlayer(Player player)
        {
            if (playerMounted) return;

            Vector3 targetPos = player.transform.position + player.transform.forward * 3f;
            targetPos.y = player.transform.position.y + hoverHeight;

            // Teleport vehicle to player
            transform.position = targetPos;
            rb.velocity = Vector3.zero;

            MobilityPlusPlugin.Log.LogInfo("Hover Pod recalled! Press V to mount.");
        }

        private void OnDestroy()
        {
            // Ensure player is dismounted if vehicle is destroyed
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



