using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;

using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;

namespace AdvancedMachines
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    public class AdvancedMachinesPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.AdvancedMachines";
        private const string PluginName = "AdvancedMachines";
        private const string VersionString = "2.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Machine names
        public const string SmelterMk4Name = "Smelter MKIV";
        public const string SmelterMk5Name = "Smelter MKV";
        public const string AssemblerMk3Name = "Assembler MKIII";
        public const string AssemblerMk4Name = "Assembler MKIV";
        public const string DrillMk3Name = "Mining Drill MKIII";
        public const string DrillMk4Name = "Mining Drill MKIV";
        public const string ThresherMk2Name = "Thresher MKII";
        public const string ThresherMk3Name = "Thresher MKIII";
        // Planter MKIV/MKV extend MorePlanters (which adds MKII/MKIII)
        public const string PlanterMk4Name = "Planter MKIV";
        public const string PlanterMk5Name = "Planter MKV";

        // Config
        public static ConfigEntry<bool> EnableHiddenVariants;
        public static ConfigEntry<bool> EnableTier4Machines;
        public static ConfigEntry<bool> EnableTier5Machines;

        // Speed multipliers (relative to Mk1)
        private const float Tier4SpeedMult = 2.0f;
        private const float Tier5SpeedMult = 3.0f;

        // Power consumption multipliers (relative to Mk1)
        private const float Tier4PowerMult = 1.5f;
        private const float Tier5PowerMult = 2.0f;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            // Config
            EnableHiddenVariants = Config.Bind("General", "Enable Hidden Variants", true,
                "Unlock all hidden machine variants");
            EnableTier4Machines = Config.Bind("General", "Enable Tier 4 Machines", true,
                "Add Tier 4 (MK IV) versions of machines");
            EnableTier5Machines = Config.Bind("General", "Enable Tier 5 Machines", true,
                "Add Tier 5 (MK V) versions of machines");

            Harmony.PatchAll();

            // Register machines
            if (EnableTier4Machines.Value)
            {
                RegisterTier4Machines();
            }

            if (EnableTier5Machines.Value)
            {
                RegisterTier5Machines();
            }

            // Hook events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void RegisterTier4Machines()
        {
            // ========== SMELTER MKIV ==========
            RegisterUnlock(SmelterMk4Name, TechCategory.Synthesis, CoreType.Purple, 500,
                "Advanced smelter with 2x processing speed.", ResearchTier.Tier2);

            SmelterDefinition smelterMk4Def = ScriptableObject.CreateInstance<SmelterDefinition>();
            smelterMk4Def.craftingEfficiency = Tier4SpeedMult;
            smelterMk4Def.usesFuel = true;
            smelterMk4Def.fuelConsumptionRate = 0.5f;

            EMUAdditions.AddNewMachine<SmelterInstance, SmelterDefinition>(smelterMk4Def, new NewResourceDetails
            {
                name = SmelterMk4Name,
                description = $"High-performance smelter with {Tier4SpeedMult}x processing speed. Requires more power but processes ores faster.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Smelters",
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = SmelterMk4Name,
                parentName = "Smelter MKIII"
            });

            RegisterRecipe(SmelterMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Smelter MKIII", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Processor", 5),
                new RecipeResourceInfo("Cooling System", 5)
            });

            // ========== ASSEMBLER MKIII ==========
            RegisterUnlock(AssemblerMk3Name, TechCategory.Synthesis, CoreType.Purple, 500,
                "Advanced assembler with 2x crafting speed.", ResearchTier.Tier2);

            AssemblerDefinition assemblerMk3Def = ScriptableObject.CreateInstance<AssemblerDefinition>();

            EMUAdditions.AddNewMachine<AssemblerInstance, AssemblerDefinition>(assemblerMk3Def, new NewResourceDetails
            {
                name = AssemblerMk3Name,
                description = $"High-performance assembler with {Tier4SpeedMult}x crafting speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Assemblers",
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = AssemblerMk3Name,
                parentName = "Assembler MKII"
            });

            RegisterRecipe(AssemblerMk3Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Assembler MKII", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Processor", 5),
                new RecipeResourceInfo("Shiverthorn Coolant", 5)
            });

            // ========== MINING DRILL MKIII ==========
            RegisterUnlock(DrillMk3Name, TechCategory.Synthesis, CoreType.Purple, 500,
                "Advanced mining drill with 2x dig speed.", ResearchTier.Tier2);

            DrillDefinition drillMk3Def = ScriptableObject.CreateInstance<DrillDefinition>();

            EMUAdditions.AddNewMachine<DrillInstance, DrillDefinition>(drillMk3Def, new NewResourceDetails
            {
                name = DrillMk3Name,
                description = $"High-performance mining drill with {Tier4SpeedMult}x dig speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Drills",
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = DrillMk3Name,
                parentName = "Mining Drill MKII"
            });

            RegisterRecipe(DrillMk3Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Mining Drill MKII", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Mechanical Components", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 5)
            });

            // ========== THRESHER MKII ==========
            RegisterUnlock(ThresherMk2Name, TechCategory.Synthesis, CoreType.Purple, 500,
                "Advanced thresher with 2x processing speed.", ResearchTier.Tier2);

            ThresherDefinition thresherMk2Def = ScriptableObject.CreateInstance<ThresherDefinition>();

            EMUAdditions.AddNewMachine<ThresherInstance, ThresherDefinition>(thresherMk2Def, new NewResourceDetails
            {
                name = ThresherMk2Name,
                description = $"High-performance thresher with {Tier4SpeedMult}x processing speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Threshers",
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = ThresherMk2Name,
                parentName = "Thresher"
            });

            RegisterRecipe(ThresherMk2Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Thresher", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Mechanical Components", 5),
                new RecipeResourceInfo("Processor", 3)
            });

            // ========== PLANTER MKIV (extends MorePlanters MKIII) ==========
            RegisterUnlock(PlanterMk4Name, TechCategory.Synthesis, CoreType.Purple, 500,
                "Advanced planter with 2.5x growth speed.", ResearchTier.Tier2);

            PlanterDefinition planterMk4Def = ScriptableObject.CreateInstance<PlanterDefinition>();

            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(planterMk4Def, new NewResourceDetails
            {
                name = PlanterMk4Name,
                description = $"High-performance planter with 2.5x growth speed. Extends MorePlanters' Planter MKIII.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Planters",
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = PlanterMk4Name,
                parentName = "Planter"  // Use base planter model
            });

            RegisterRecipe(PlanterMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Planter MKIII", 2),  // Require MorePlanters' MKIII
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Mechanical Components", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 5)
            });

            Log.LogInfo("Registered Tier 4 machines");
        }

        private void RegisterTier5Machines()
        {
            // ========== SMELTER MKV ==========
            RegisterUnlock(SmelterMk5Name, TechCategory.Synthesis, CoreType.Purple, 1000,
                "Ultimate smelter with 3x processing speed.", ResearchTier.Tier3);

            SmelterDefinition smelterMk5Def = ScriptableObject.CreateInstance<SmelterDefinition>();
            smelterMk5Def.craftingEfficiency = Tier5SpeedMult;
            smelterMk5Def.usesFuel = true;
            smelterMk5Def.fuelConsumptionRate = 0.75f;

            EMUAdditions.AddNewMachine<SmelterInstance, SmelterDefinition>(smelterMk5Def, new NewResourceDetails
            {
                name = SmelterMk5Name,
                description = $"Ultimate smelter with {Tier5SpeedMult}x processing speed. Peak performance for large-scale operations.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Smelters",
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = SmelterMk5Name,
                parentName = "Smelter MKIII"
            });

            RegisterRecipe(SmelterMk5Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(SmelterMk4Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Processor", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            // ========== ASSEMBLER MKIV ==========
            RegisterUnlock(AssemblerMk4Name, TechCategory.Synthesis, CoreType.Purple, 1000,
                "Ultimate assembler with 3x crafting speed.", ResearchTier.Tier3);

            AssemblerDefinition assemblerMk4Def = ScriptableObject.CreateInstance<AssemblerDefinition>();

            EMUAdditions.AddNewMachine<AssemblerInstance, AssemblerDefinition>(assemblerMk4Def, new NewResourceDetails
            {
                name = AssemblerMk4Name,
                description = $"Ultimate assembler with {Tier5SpeedMult}x crafting speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Assemblers",
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = AssemblerMk4Name,
                parentName = "Assembler MKII"
            });

            RegisterRecipe(AssemblerMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(AssemblerMk3Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Processor", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            // ========== MINING DRILL MKIV ==========
            RegisterUnlock(DrillMk4Name, TechCategory.Synthesis, CoreType.Purple, 1000,
                "Ultimate mining drill with 3x dig speed.", ResearchTier.Tier3);

            DrillDefinition drillMk4Def = ScriptableObject.CreateInstance<DrillDefinition>();

            EMUAdditions.AddNewMachine<DrillInstance, DrillDefinition>(drillMk4Def, new NewResourceDetails
            {
                name = DrillMk4Name,
                description = $"Ultimate mining drill with {Tier5SpeedMult}x dig speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Drills",
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = DrillMk4Name,
                parentName = "Mining Drill MKII"
            });

            RegisterRecipe(DrillMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(DrillMk3Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Mechanical Components", 20),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            // ========== THRESHER MKIII ==========
            RegisterUnlock(ThresherMk3Name, TechCategory.Synthesis, CoreType.Purple, 1000,
                "Ultimate thresher with 3x processing speed.", ResearchTier.Tier3);

            ThresherDefinition thresherMk3Def = ScriptableObject.CreateInstance<ThresherDefinition>();

            EMUAdditions.AddNewMachine<ThresherInstance, ThresherDefinition>(thresherMk3Def, new NewResourceDetails
            {
                name = ThresherMk3Name,
                description = $"Ultimate thresher with {Tier5SpeedMult}x processing speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Threshers",
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = ThresherMk3Name,
                parentName = "Thresher"
            });

            RegisterRecipe(ThresherMk3Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(ThresherMk2Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Mechanical Components", 10),
                new RecipeResourceInfo("Processor", 5)
            });

            // ========== PLANTER MKV (extends MKIV) ==========
            RegisterUnlock(PlanterMk5Name, TechCategory.Synthesis, CoreType.Purple, 1000,
                "Ultimate planter with 4x growth speed.", ResearchTier.Tier3);

            PlanterDefinition planterMk5Def = ScriptableObject.CreateInstance<PlanterDefinition>();

            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(planterMk5Def, new NewResourceDetails
            {
                name = PlanterMk5Name,
                description = $"Ultimate planter with 4x growth speed. Peak agricultural performance.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Planters",
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = PlanterMk5Name,
                parentName = "Planter"
            });

            RegisterRecipe(PlanterMk5Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(PlanterMk4Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Fertilizer", 50),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            Log.LogInfo("Registered Tier 5 machines");
        }

        private void RegisterUnlock(string name, TechCategory category, CoreType coreType, int coreCount,
            string description, ResearchTier tier)
        {
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = category,
                coreTypeNeeded = coreType,
                coreCountNeeded = coreCount,
                description = description,
                displayName = name,
                requiredTier = tier,
                treePosition = 0
            });
        }

        private void RegisterRecipe(string outputName, List<RecipeResourceInfo> ingredients)
        {
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 15f,
                unlockName = outputName,
                ingredients = ingredients,
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(outputName, 1)
                },
                sortPriority = 100
            });
        }

        private void OnGameDefinesLoaded()
        {
            // Link unlocks to resources
            LinkUnlockToResource(SmelterMk4Name);
            LinkUnlockToResource(SmelterMk5Name);
            LinkUnlockToResource(AssemblerMk3Name);
            LinkUnlockToResource(AssemblerMk4Name);
            LinkUnlockToResource(DrillMk3Name);
            LinkUnlockToResource(DrillMk4Name);
            LinkUnlockToResource(ThresherMk2Name);
            LinkUnlockToResource(ThresherMk3Name);
            LinkUnlockToResource(PlanterMk4Name);
            LinkUnlockToResource(PlanterMk5Name);

            // Configure machine tiers and speeds
            ConfigureMachine(SmelterMk4Name, 3, Tier4SpeedMult, (int)(150 * Tier4PowerMult));
            ConfigureMachine(SmelterMk5Name, 4, Tier5SpeedMult, (int)(150 * Tier5PowerMult));
            ConfigureMachine(AssemblerMk3Name, 2, Tier4SpeedMult, (int)(200 * Tier4PowerMult));
            ConfigureMachine(AssemblerMk4Name, 3, Tier5SpeedMult, (int)(200 * Tier5PowerMult));
            ConfigureMachine(DrillMk3Name, 2, Tier4SpeedMult, (int)(100 * Tier4PowerMult));
            ConfigureMachine(DrillMk4Name, 3, Tier5SpeedMult, (int)(100 * Tier5PowerMult));
            ConfigureMachine(ThresherMk2Name, 1, Tier4SpeedMult, (int)(80 * Tier4PowerMult));
            ConfigureMachine(ThresherMk3Name, 2, Tier5SpeedMult, (int)(80 * Tier5PowerMult));
            ConfigureMachine(PlanterMk4Name, 3, 2.5f, (int)(60 * Tier4PowerMult));
            ConfigureMachine(PlanterMk5Name, 4, 4.0f, (int)(60 * Tier5PowerMult));
        }

        private void LinkUnlockToResource(string name)
        {
            try
            {
                ResourceInfo info = EMU.Resources.GetResourceInfoByName(name);
                if (info != null)
                {
                    info.unlock = EMU.Unlocks.GetUnlockByName(name);
                }
            }
            catch { }
        }

        private void ConfigureMachine(string name, int tier, float speedMult, int powerKW)
        {
            try
            {
                ResourceInfo info = EMU.Resources.GetResourceInfoByName(name);
                if (info == null) return;

                // Set machine tier
                if (info is BuilderInfo builderInfo)
                {
                    builderInfo.machineTier = tier;
                }

                // Configure power consumption via reflection (MachineDefinition is generic)
                var runtimePowerField = info.GetType().GetField("runtimePowerSettings");
                if (runtimePowerField != null)
                {
                    var powerSettings = runtimePowerField.GetValue(info);
                    if (powerSettings != null)
                    {
                        var kWField = powerSettings.GetType().GetField("kWPowerConsumption");
                        if (kWField != null)
                        {
                            kWField.SetValue(powerSettings, powerKW);
                            runtimePowerField.SetValue(info, powerSettings);
                        }
                    }
                }

                Log.LogInfo($"Configured {name}: tier={tier}, speedMult={speedMult}, power={powerKW}kW");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure {name}: {ex.Message}");
            }
        }

        private void OnTechTreeStateLoaded()
        {
            // Position unlocks in tech tree
            PositionUnlockAfter(SmelterMk4Name, "Smelter MKIII");
            PositionUnlockAfter(SmelterMk5Name, SmelterMk4Name);
            PositionUnlockAfter(AssemblerMk3Name, "Assembler MKII");
            PositionUnlockAfter(AssemblerMk4Name, AssemblerMk3Name);
            PositionUnlockAfter(DrillMk3Name, "Mining Drill MKII");
            PositionUnlockAfter(DrillMk4Name, DrillMk3Name);
            PositionUnlockAfter(ThresherMk2Name, "Thresher");
            PositionUnlockAfter(ThresherMk3Name, ThresherMk2Name);
            PositionUnlockAfter(PlanterMk4Name, "Planter MKIII");  // After MorePlanters' MKIII
            PositionUnlockAfter(PlanterMk5Name, PlanterMk4Name);
        }

        private void PositionUnlockAfter(string unlockName, string afterName)
        {
            try
            {
                Unlock unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                Unlock after = EMU.Unlocks.GetUnlockByName(afterName);
                if (unlock != null && after != null)
                {
                    unlock.treePosition = after.treePosition;
                    unlock.requiredTier = after.requiredTier;
                }
            }
            catch { }
        }
    }

    /// <summary>
    /// Patches to enable hidden machine variants
    /// </summary>
    [HarmonyPatch]
    internal static class HiddenVariantsPatches
    {
        /// <summary>
        /// Patch to always return true for variant availability
        /// </summary>
        [HarmonyPatch(typeof(BuilderInfo), nameof(BuilderInfo.IsVariantAvailable))]
        [HarmonyPrefix]
        private static bool IsVariantAvailablePrefix(ref bool __result, int index)
        {
            if (!AdvancedMachinesPlugin.EnableHiddenVariants.Value)
                return true; // Run original method

            // Force all variants to be available
            __result = true;
            return false; // Skip original method
        }

        /// <summary>
        /// Patch TechTreeState variant check
        /// </summary>
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.IsVariantAvailable))]
        [HarmonyPrefix]
        private static bool TechTreeIsVariantAvailablePrefix(ref bool __result, BuilderInfo info, int variationIndex)
        {
            if (!AdvancedMachinesPlugin.EnableHiddenVariants.Value)
                return true; // Run original method

            // Force all variants to be available
            __result = true;
            return false; // Skip original method
        }
    }
}
