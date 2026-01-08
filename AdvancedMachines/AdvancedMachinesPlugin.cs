using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;
using TechtonicaFramework.TechTree;
using TechtonicaFramework.BuildMenu;

using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;

namespace AdvancedMachines
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class AdvancedMachinesPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.AdvancedMachines";
        private const string PluginName = "AdvancedMachines";
        private const string VersionString = "2.4.2";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Machine names
        public const string SmelterMk4Name = "Smelter MKIV";
        public const string SmelterMk5Name = "Smelter MKV";
        public const string AssemblerMk3Name = "Assembler MKIII";
        public const string AssemblerMk4Name = "Assembler MKIV";
        public const string DrillMk3Name = "Mining Drill MKIII";
        public const string DrillMk4Name = "Mining Drill MKIV";
        public const string ThresherMk3Name = "Thresher MKIII";
        public const string ThresherMk4Name = "Thresher MKIV";
        public const string ThresherMk5Name = "Thresher MKV";
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
            // Note: Removed legacy "Thresher MKII" unlock - it conflicts with the base game's
            // Thresher MKII and creates broken tech tree entries. Save compatibility is handled
            // by the game's own system.

            // ========== SMELTER MKIV ==========
            RegisterUnlock(SmelterMk4Name, TechCategory.Synthesis, CoreType.Gold, 500,
                "Advanced smelter with 2x processing speed.", ResearchTier.Tier14);

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
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = SmelterMk4Name,
                parentName = "Smelter MKIII"
            });

            RegisterRecipe(SmelterMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Smelter MKIII", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Processor Unit", 5),
                new RecipeResourceInfo("Cooling System", 5)
            });

            // ========== ASSEMBLER MKIII ==========
            RegisterUnlock(AssemblerMk3Name, TechCategory.Synthesis, CoreType.Gold, 500,
                "Advanced assembler with 2x crafting speed.", ResearchTier.Tier10);

            AssemblerDefinition assemblerMk3Def = ScriptableObject.CreateInstance<AssemblerDefinition>();

            EMUAdditions.AddNewMachine<AssemblerInstance, AssemblerDefinition>(assemblerMk3Def, new NewResourceDetails
            {
                name = AssemblerMk3Name,
                description = $"High-performance assembler with {Tier4SpeedMult}x crafting speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = AssemblerMk3Name,
                parentName = "Assembler MKII"
            });

            RegisterRecipe(AssemblerMk3Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Assembler MKII", 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Processor Unit", 5),
                new RecipeResourceInfo("Shiverthorn Coolant", 5)
            });

            // ========== MINING DRILL MKIII ==========
            RegisterUnlock(DrillMk3Name, TechCategory.Synthesis, CoreType.Gold, 500,
                "Advanced mining drill with 2x dig speed.", ResearchTier.Tier10);

            DrillDefinition drillMk3Def = ScriptableObject.CreateInstance<DrillDefinition>();

            EMUAdditions.AddNewMachine<DrillInstance, DrillDefinition>(drillMk3Def, new NewResourceDetails
            {
                name = DrillMk3Name,
                description = $"High-performance mining drill with {Tier4SpeedMult}x dig speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
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

            // ========== THRESHER MKIII ==========
            RegisterUnlock(ThresherMk3Name, TechCategory.Synthesis, CoreType.Gold, 300,
                "Improved thresher with 1.5x processing speed.", ResearchTier.Tier10);

            ThresherDefinition thresherMk3Def = ScriptableObject.CreateInstance<ThresherDefinition>();

            EMUAdditions.AddNewMachine<ThresherInstance, ThresherDefinition>(thresherMk3Def, new NewResourceDetails
            {
                name = ThresherMk3Name,
                description = "Improved thresher with 1.5x processing speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 99,
                unlockName = ThresherMk3Name,
                parentName = "Thresher"
            });

            RegisterRecipe(ThresherMk3Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Thresher", 2),
                new RecipeResourceInfo("Steel Frame", 5),
                new RecipeResourceInfo("Mechanical Components", 3),
                new RecipeResourceInfo("Processor Unit", 2)
            });

            // ========== Thresher MKIV ==========
            RegisterUnlock(ThresherMk4Name, TechCategory.Synthesis, CoreType.Gold, 500,
                "Advanced thresher with 2x processing speed.", ResearchTier.Tier14);

            ThresherDefinition thresherMk4Def = ScriptableObject.CreateInstance<ThresherDefinition>();

            EMUAdditions.AddNewMachine<ThresherInstance, ThresherDefinition>(thresherMk4Def, new NewResourceDetails
            {
                name = ThresherMk4Name,
                description = $"High-performance thresher with {Tier4SpeedMult}x processing speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = ThresherMk4Name,
                parentName = ThresherMk3Name
            });

            RegisterRecipe(ThresherMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(ThresherMk3Name, 2),
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Mechanical Components", 5),
                new RecipeResourceInfo("Processor Unit", 3)
            });

            // ========== PLANTER MKIV (extends MorePlanters MKIII) ==========
            RegisterUnlock(PlanterMk4Name, TechCategory.Synthesis, CoreType.Gold, 500,
                "Advanced planter with 2.5x growth speed.", ResearchTier.Tier14);

            PlanterDefinition planterMk4Def = ScriptableObject.CreateInstance<PlanterDefinition>();

            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(planterMk4Def, new NewResourceDetails
            {
                name = PlanterMk4Name,
                description = $"High-performance planter with 2.5x growth speed. Extends MorePlanters' Planter MKIII.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
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
            RegisterUnlock(SmelterMk5Name, TechCategory.Synthesis, CoreType.Green, 1000,
                "Ultimate smelter with 3x processing speed.", ResearchTier.Tier18);

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
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = SmelterMk5Name,
                parentName = "Smelter MKIII"
            });

            RegisterRecipe(SmelterMk5Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(SmelterMk4Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Processor Unit", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            // ========== ASSEMBLER MKIV ==========
            RegisterUnlock(AssemblerMk4Name, TechCategory.Synthesis, CoreType.Green, 1000,
                "Ultimate assembler with 3x crafting speed.", ResearchTier.Tier14);

            AssemblerDefinition assemblerMk4Def = ScriptableObject.CreateInstance<AssemblerDefinition>();

            EMUAdditions.AddNewMachine<AssemblerInstance, AssemblerDefinition>(assemblerMk4Def, new NewResourceDetails
            {
                name = AssemblerMk4Name,
                description = $"Ultimate assembler with {Tier5SpeedMult}x crafting speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = AssemblerMk4Name,
                parentName = "Assembler MKII"
            });

            RegisterRecipe(AssemblerMk4Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(AssemblerMk3Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Processor Unit", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            // ========== MINING DRILL MKIV ==========
            RegisterUnlock(DrillMk4Name, TechCategory.Synthesis, CoreType.Green, 1000,
                "Ultimate mining drill with 3x dig speed.", ResearchTier.Tier14);

            DrillDefinition drillMk4Def = ScriptableObject.CreateInstance<DrillDefinition>();

            EMUAdditions.AddNewMachine<DrillInstance, DrillDefinition>(drillMk4Def, new NewResourceDetails
            {
                name = DrillMk4Name,
                description = $"Ultimate mining drill with {Tier5SpeedMult}x dig speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
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

            // ========== THRESHER MKV ==========
            RegisterUnlock(ThresherMk5Name, TechCategory.Synthesis, CoreType.Green, 1000,
                "Ultimate thresher with 3x processing speed.", ResearchTier.Tier18);

            ThresherDefinition thresherMk5Def = ScriptableObject.CreateInstance<ThresherDefinition>();

            EMUAdditions.AddNewMachine<ThresherInstance, ThresherDefinition>(thresherMk5Def, new NewResourceDetails
            {
                name = ThresherMk5Name,
                description = $"Ultimate thresher with {Tier5SpeedMult}x processing speed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = ThresherMk5Name,
                parentName = ThresherMk3Name
            });

            RegisterRecipe(ThresherMk5Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(ThresherMk4Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Mechanical Components", 10),
                new RecipeResourceInfo("Processor Unit", 5)
            });

            // ========== PLANTER MKV (extends MKIV) ==========
            RegisterUnlock(PlanterMk5Name, TechCategory.Synthesis, CoreType.Green, 1000,
                "Ultimate planter with 4x growth speed.", ResearchTier.Tier18);

            PlanterDefinition planterMk5Def = ScriptableObject.CreateInstance<PlanterDefinition>();

            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(planterMk5Def, new NewResourceDetails
            {
                name = PlanterMk5Name,
                description = $"Ultimate planter with 4x growth speed. Peak agricultural performance.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                // subHeaderTitle inherited from parent
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = PlanterMk5Name,
                parentName = "Planter"
            });

            RegisterRecipe(PlanterMk5Name, new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo(PlanterMk4Name, 2),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Kindlevine Extract", 50),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            Log.LogInfo("Registered Tier 5 machines");
        }

        private void RegisterUnlock(string name, Unlock.TechCategory category, CoreType coreType, int coreCount,
            string description, ResearchTier tier)
        {
            // All AdvancedMachines unlocks go to the Modded category
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = coreType,
                coreCountNeeded = coreCount,
                description = description,
                displayName = name,
                requiredTier = tier,
                treePosition = 50
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
            LinkUnlockToResource(ThresherMk3Name);
            LinkUnlockToResource(ThresherMk4Name);
            LinkUnlockToResource(ThresherMk5Name);
            LinkUnlockToResource(PlanterMk4Name);
            LinkUnlockToResource(PlanterMk5Name);

            // Configure machine tiers and speeds
            ConfigureMachine(SmelterMk4Name, 3, Tier4SpeedMult, (int)(150 * Tier4PowerMult));
            ConfigureMachine(SmelterMk5Name, 4, Tier5SpeedMult, (int)(150 * Tier5PowerMult));
            ConfigureMachine(AssemblerMk3Name, 2, Tier4SpeedMult, (int)(200 * Tier4PowerMult));
            ConfigureMachine(AssemblerMk4Name, 3, Tier5SpeedMult, (int)(200 * Tier5PowerMult));
            ConfigureMachine(DrillMk3Name, 2, Tier4SpeedMult, (int)(100 * Tier4PowerMult));
            ConfigureMachine(DrillMk4Name, 3, Tier5SpeedMult, (int)(100 * Tier5PowerMult));
            ConfigureMachine(ThresherMk3Name, 2, 1.5f, (int)(80 * 1.5f));
            ConfigureMachine(ThresherMk4Name, 3, Tier4SpeedMult, (int)(80 * Tier4PowerMult));
            ConfigureMachine(ThresherMk5Name, 4, Tier5SpeedMult, (int)(80 * Tier5PowerMult));
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
            // Configure unlocks with CORRECT tiers matching ForcedTiers dictionary
            // Tech Tree Layout (4 floors):
            // - Floor 1: Tiers 1-4 (LIMA)
            // - Floor 2: Tiers 5-10 (VICTOR) - MKIII = Tier10 (late floor 2)
            // - Floor 3: Tiers 11-17 (XRAY) - MKIV = Tier14 (mid floor 3)
            // - Floor 4: Tiers 17-24 (SIERRA) - MKV = Tier18 (early-mid floor 4)

            // MKIII machines - Tier10 (late floor 2 / VICTOR)
            ConfigureUnlock(AssemblerMk3Name, "Assembler", ResearchTier.Tier10, 10);
            ConfigureUnlock(DrillMk3Name, "Mining Drill", ResearchTier.Tier10, 30);
            ConfigureUnlock(ThresherMk3Name, "Thresher", ResearchTier.Tier10, 50);

            // MKIV machines - Tier14 (mid floor 3 / XRAY)
            ConfigureUnlock(SmelterMk4Name, "Smelter", ResearchTier.Tier14, 10);
            ConfigureUnlock(AssemblerMk4Name, "Assembler", ResearchTier.Tier14, 30);
            ConfigureUnlock(DrillMk4Name, "Mining Drill", ResearchTier.Tier14, 50);
            ConfigureUnlock(ThresherMk4Name, "Thresher", ResearchTier.Tier14, 70);
            ConfigureUnlock(PlanterMk4Name, "Planter", ResearchTier.Tier14, 90);

            // MKV machines - Tier18 (early-mid floor 4 / SIERRA)
            ConfigureUnlock(SmelterMk5Name, "Smelter", ResearchTier.Tier18, 10);
            ConfigureUnlock(ThresherMk5Name, "Thresher", ResearchTier.Tier18, 30);
            ConfigureUnlock(PlanterMk5Name, "Planter", ResearchTier.Tier18, 50);

            Log.LogInfo("Configured unlock tiers and sprites");
        }

        private void ConfigureUnlock(string unlockName, string spriteSourceName, ResearchTier tier, int position)
        {
            try
            {
                Unlock unlock = EMU.Unlocks.GetUnlockByName(unlockName);

                if (unlock == null)
                {
                    Log.LogWarning($"Could not find unlock: {unlockName}");
                    return;
                }

                // Set the correct tier explicitly
                unlock.requiredTier = tier;

                // Set explicit position - no more parent-relative positioning!
                unlock.treePosition = position;

                // Copy sprite from base machine for proper icon display
                // Always try to set sprite even if one exists (may be placeholder)
                Sprite sourceSprite = null;

                // Try to get sprite from the base machine resource
                ResourceInfo sourceResource = EMU.Resources.GetResourceInfoByName(spriteSourceName);
                if (sourceResource != null && sourceResource.sprite != null)
                {
                    sourceSprite = sourceResource.sprite;
                }
                else
                {
                    // Try unlock with that name
                    Unlock sourceUnlock = EMU.Unlocks.GetUnlockByName(spriteSourceName);
                    if (sourceUnlock != null && sourceUnlock.sprite != null)
                    {
                        sourceSprite = sourceUnlock.sprite;
                    }
                }

                if (sourceSprite != null)
                {
                    unlock.sprite = sourceSprite;
                }

                Log.LogInfo($"Configured {unlockName}: tier={tier}, pos={position}, sprite={(unlock.sprite != null ? "SET" : "NULL")}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure unlock {unlockName}: {ex.Message}");
            }
        }

        private string GetBaseMachineName(string machineName)
        {
            if (machineName.Contains("Smelter")) return "Smelter";
            if (machineName.Contains("Assembler")) return "Assembler";
            if (machineName.Contains("Mining Drill") || machineName.Contains("Drill")) return "Mining Drill";
            if (machineName.Contains("Thresher")) return "Thresher";
            if (machineName.Contains("Planter")) return "Planter";
            return null;
        }
    }

    /// <summary>
    /// Patches to FORCE correct tech tree tiers regardless of save data
    /// </summary>
    [HarmonyPatch]
    internal static class ForceTierPatches
    {
        // Map of unlock names to their FORCED tiers - save data CANNOT override these
        // Tech Tree Layout (4 floors):
        // - Floor 1: Tiers 1-4
        // - Floor 2: Tiers 5-10 (MKIII = late floor 2 = Tier10)
        // - Floor 3: Tiers 11-17 (MKIV = mid floor 3 = Tier14)
        // - Floor 4: Tiers 17-24 (MKV = early-mid floor 4 = Tier18)
        private static readonly Dictionary<string, TechTreeState.ResearchTier> ForcedTiers = new Dictionary<string, TechTreeState.ResearchTier>
        {
            // MKIII machines - Tier10 (late floor 2)
            { "Assembler MKIII", TechTreeState.ResearchTier.Tier10 },
            { "Mining Drill MKIII", TechTreeState.ResearchTier.Tier10 },
            { "Thresher MKIII", TechTreeState.ResearchTier.Tier10 },
            // MKIV machines - Tier14 (mid floor 3)
            { "Smelter MKIV", TechTreeState.ResearchTier.Tier14 },
            { "Assembler MKIV", TechTreeState.ResearchTier.Tier14 },
            { "Mining Drill MKIV", TechTreeState.ResearchTier.Tier14 },
            { "Thresher MKIV", TechTreeState.ResearchTier.Tier14 },
            { "Planter MKIV", TechTreeState.ResearchTier.Tier14 },
            // MKV machines - Tier18 (early-mid floor 4)
            { "Smelter MKV", TechTreeState.ResearchTier.Tier18 },
            { "Thresher MKV", TechTreeState.ResearchTier.Tier18 },
            { "Planter MKV", TechTreeState.ResearchTier.Tier18 },
        };

        /// <summary>
        /// Force tier when TechTreeState checks if unlock is available for a tier
        /// </summary>
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.IsUnlockActive))]
        [HarmonyPrefix]
        private static void ForceUnlockTier(int unlockId)
        {
            if (unlockId < 0 || TechTreeState.instance == null) return;

            var unlockStates = TechTreeState.instance.unlockStates;
            if (unlockId >= unlockStates.Length) return;

            Unlock unlock = unlockStates[unlockId].unlockRef;
            if (unlock != null && !string.IsNullOrEmpty(unlock.displayName))
            {
                if (ForcedTiers.TryGetValue(unlock.displayName, out var forcedTier))
                {
                    if (unlock.requiredTier != forcedTier)
                    {
                        unlock.requiredTier = forcedTier;
                    }
                }
            }
        }

        /// <summary>
        /// Force tier when TechTreeState initializes
        /// </summary>
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.Init))]
        [HarmonyPostfix]
        private static void ForceAllTiersAfterInit()
        {
            ForceAllTiers("Init");
        }

        /// <summary>
        /// Force tier AFTER save data is read - this is critical!
        /// </summary>
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.ReadSaveState))]
        [HarmonyPostfix]
        private static void ForceAllTiersAfterSaveLoad()
        {
            ForceAllTiers("ReadSaveState");
        }

        public static void ForceAllTiers(string source)
        {
            if (GameDefines.instance?.unlocks == null) return;

            bool anyChanged = false;
            foreach (var unlock in GameDefines.instance.unlocks)
            {
                if (unlock != null && !string.IsNullOrEmpty(unlock.displayName))
                {
                    if (ForcedTiers.TryGetValue(unlock.displayName, out var forcedTier))
                    {
                        if (unlock.requiredTier != forcedTier)
                        {
                            unlock.requiredTier = forcedTier;
                            anyChanged = true;
                        }
                    }
                }
            }

            if (anyChanged && source != null)
            {
                AdvancedMachinesPlugin.Log.LogInfo($"Forced MKIII/MKIV/MKV unlocks to correct tiers ({source})");
            }
        }
    }

    /// <summary>
    /// Patches to fix toolbar loading crashes from renamed machines
    /// </summary>
    [HarmonyPatch]
    internal static class ToolbarFixPatches
    {
        /// <summary>
        /// Catch exceptions in ToolbarSlotUI.Refresh to prevent crashes
        /// </summary>
        [HarmonyPatch(typeof(ToolbarSlotUI), nameof(ToolbarSlotUI.Refresh))]
        [HarmonyFinalizer]
        private static Exception CatchRefreshErrors(Exception __exception)
        {
            if (__exception != null)
            {
                AdvancedMachinesPlugin.Log.LogWarning($"Toolbar slot refresh error (caught): {__exception.Message}");
                return null; // Suppress the exception
            }
            return null;
        }

        /// <summary>
        /// Catch exceptions in RefreshToolbars to prevent game freeze
        /// </summary>
        [HarmonyPatch(typeof(PlayerToolbar), nameof(PlayerToolbar.RefreshToolbars))]
        [HarmonyFinalizer]
        private static Exception CatchToolbarRefreshErrors(Exception __exception)
        {
            if (__exception != null)
            {
                AdvancedMachinesPlugin.Log.LogWarning($"Toolbar refresh error (caught): {__exception.Message}");
                return null; // Suppress the exception
            }
            return null;
        }

        /// <summary>
        /// Catch exceptions in LoadToolbarData to prevent game freeze
        /// </summary>
        [HarmonyPatch(typeof(PlayerToolbar), nameof(PlayerToolbar.LoadToolbarData))]
        [HarmonyFinalizer]
        private static Exception CatchToolbarLoadErrors(Exception __exception)
        {
            if (__exception != null)
            {
                AdvancedMachinesPlugin.Log.LogWarning($"Toolbar load error (caught): {__exception.Message}");
                return null; // Suppress the exception
            }
            return null;
        }
    }

    /// <summary>
    /// Patches to enable hidden machine variants
    /// </summary>
    [HarmonyPatch]
    internal static class HiddenVariantsPatches
    {
        /// <summary>
        /// Patch to enable hidden variants, but only for valid indices
        /// </summary>
        [HarmonyPatch(typeof(BuilderInfo), nameof(BuilderInfo.IsVariantAvailable))]
        [HarmonyPrefix]
        private static bool IsVariantAvailablePrefix(ref bool __result, BuilderInfo __instance, int index)
        {
            if (!AdvancedMachinesPlugin.EnableHiddenVariants.Value)
                return true; // Run original method

            // Only mark as available if index is within bounds
            if (__instance.variationData != null && index >= 0 && index < __instance.variationData.Length)
            {
                __result = true;
                return false; // Skip original method
            }

            // Invalid index - return false
            __result = false;
            return false; // Skip original method
        }

        /// <summary>
        /// Patch TechTreeState variant check, but only for valid indices
        /// </summary>
        [HarmonyPatch(typeof(TechTreeState), nameof(TechTreeState.IsVariantAvailable))]
        [HarmonyPrefix]
        private static bool TechTreeIsVariantAvailablePrefix(ref bool __result, BuilderInfo info, int variationIndex)
        {
            if (!AdvancedMachinesPlugin.EnableHiddenVariants.Value)
                return true; // Run original method

            // Only mark as available if index is within bounds
            if (info != null && info.variationData != null && variationIndex >= 0 && variationIndex < info.variationData.Length)
            {
                __result = true;
                return false; // Skip original method
            }

            // Invalid index - return false
            __result = false;
            return false; // Skip original method
        }
    }
}







