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
        public const string VersionString = "1.0.0";

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

            // Hook events
            EMU.Events.GameLoaded += OnGameLoaded;

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

            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void RegisterUnlocks()
        {
            // Advanced Mobility unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = Unlock.TechCategory.Logistics,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Green,
                coreCountNeeded = 150,
                description = "Research enhanced mobility equipment including improved stilts and speed technology.",
                displayName = AdvancedMobilityUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier0,
                treePosition = 0
            });

            // Extreme Speed unlock (later tier)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = Unlock.TechCategory.Logistics,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                coreCountNeeded = 300,
                description = "Push the boundaries of movement with experimental speed and jump technology.",
                displayName = ExtremeSpeedUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier1,
                treePosition = 0,
                dependencyNames = new List<string> { AdvancedMobilityUnlock }
            });

            LogDebug("Unlocks registered");
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
                headerTitle = "Equipment",
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
                headerTitle = "Equipment",
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
                    new RecipeResourceInfo(StiltsMk2Name, 1),
                    new RecipeResourceInfo("Processor Unit", 5),
                    new RecipeResourceInfo("Electric Motor", 3),
                    new RecipeResourceInfo("Steel Frame", 10)
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
                headerTitle = "Equipment",
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
                headerTitle = "Equipment",
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
                headerTitle = "Equipment",
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
                headerTitle = "Logistics",
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
                    headerTitle = "Logistics",
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

        private void OnGameLoaded()
        {
            // Register equipment effects with framework
            RegisterEquipmentEffects();
        }

        private void RegisterEquipmentEffects()
        {
            // Speed Boots effect
            FrameworkAPI.RegisterEquipment(
                "speed_boots",
                SpeedBootsName,
                "Motorized speed boots",
                EquipmentSlot.Feet,
                onEquip: (player) =>
                {
                    FrameworkAPI.ApplyMovementModifier("speed_boots", 1.25f, 1f, -1f);
                    LogDebug("Speed Boots equipped - 25% speed boost active");
                },
                onUnequip: (player) =>
                {
                    FrameworkAPI.RemoveMovementModifier("speed_boots");
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
                    FrameworkAPI.ApplyMovementModifier("jump_pack", 1f, 1.5f, -1f);
                    LogDebug("Jump Pack equipped - 50% jump boost active");
                },
                onUnequip: (player) =>
                {
                    FrameworkAPI.RemoveMovementModifier("jump_pack");
                    LogDebug("Jump Pack unequipped");
                }
            );
        }

        private void Update()
        {
            // Process speed zones
            ProcessSpeedZones();
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
                    FrameworkAPI.ApplyMovementModifier($"speedzone_{zone.Id}", zone.SpeedMultiplier, 1f, -1f);
                    LogDebug($"Entered speed zone {zone.Id}");
                }
                else if (!isInside && zone.PlayerInside)
                {
                    // Exited zone
                    zone.PlayerInside = false;
                    FrameworkAPI.RemoveMovementModifier($"speedzone_{zone.Id}");
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
