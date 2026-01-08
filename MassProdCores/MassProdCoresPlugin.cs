using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;

namespace MassProdCores
{
    /// <summary>
    /// MassProdCores_Patched - Adds mass production recipes for cores and coolers.
    ///
    /// ORIGINAL AUTHOR: Jarl
    /// ORIGINAL MOD: https://thunderstore.io/c/techtonica/p/Jarl/MassProdCores/
    /// LICENSE: Not specified (original)
    ///
    /// PATCHED BY: CertiFried
    /// PATCH REASON: Updated to EMU 6.x API (original uses deprecated NewRecipeDetails type)
    /// CHANGES: See CHANGELOG.md for full list of changes
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    public class MassProdCoresPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.MassProdCores_Patched";
        private const string PluginName = "MassProdCores_Patched";
        private const string VersionString = "1.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Configuration
        public static ConfigEntry<int> BatchSize;
        public static ConfigEntry<float> EfficiencyBonus;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"PluginName: {PluginName}, {VersionString} is loading...");

            BatchSize = Config.Bind("General", "Batch Size", 10,
                "Number of items produced per batch in mass production recipes");

            EfficiencyBonus = Config.Bind("General", "Efficiency Bonus", 0.9f,
                "Resource efficiency multiplier for mass production (0.9 = 90% of normal cost)");

            // Register with EMU events using the NEW API
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Harmony.PatchAll();

            Logger.LogInfo($"PluginName: {PluginName}, {VersionString} is loaded.");
        }

        private void OnGameDefinesLoaded()
        {
            try
            {
                Log.LogInfo("Registering mass production recipes...");

                RegisterCoreMassProductionRecipes();
                RegisterCoolerMassProductionRecipes();
                RegisterProcessorMassProductionRecipes();

                Log.LogInfo("Mass production recipes registered!");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"OnGameDefinesLoaded error: {ex.Message}");
            }
        }

        private void RegisterCoreMassProductionRecipes()
        {
            try
            {
                int batch = BatchSize.Value;
                float efficiency = EfficiencyBonus.Value;

                // Mass Kindlevine Extract Production (batch processing)
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Thresher,
                    craftTierRequired = 0,
                    duration = 30f,  // Longer duration for batch
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Kindlevine Stems", (int)(batch * 2 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Kindlevine Extract", batch)
                    },
                    sortPriority = 150
                });

                // Mass Shiverthorn Extract Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Thresher,
                    craftTierRequired = 0,
                    duration = 30f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Shiverthorn Buds", (int)(batch * 2 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Shiverthorn Extract", batch)
                    },
                    sortPriority = 151
                });

                // Mass Plantmatter Fiber Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Thresher,
                    craftTierRequired = 0,
                    duration = 20f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Plantmatter", (int)(batch * 3 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Plantmatter Fiber", batch)
                    },
                    sortPriority = 152
                });

                Log.LogInfo($"Core mass production recipes registered (batch size: {batch})");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"RegisterCoreMassProductionRecipes error: {ex.Message}");
            }
        }

        private void RegisterCoolerMassProductionRecipes()
        {
            try
            {
                int batch = BatchSize.Value;
                float efficiency = EfficiencyBonus.Value;

                // Mass Shiverthorn Coolant Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 45f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Shiverthorn Extract", (int)(batch * 2 * efficiency)),
                        new RecipeResourceInfo("Iron Ingot", (int)(batch * 1 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Shiverthorn Coolant", batch)
                    },
                    sortPriority = 160
                });

                // Mass Cooling System Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 60f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Shiverthorn Coolant", (int)(batch * 2 * efficiency)),
                        new RecipeResourceInfo("Copper Wire", (int)(batch * 4 * efficiency)),
                        new RecipeResourceInfo("Iron Frame", (int)(batch * 1 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Cooling System", batch)
                    },
                    sortPriority = 161
                });

                Log.LogInfo($"Cooler mass production recipes registered (batch size: {batch})");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"RegisterCoolerMassProductionRecipes error: {ex.Message}");
            }
        }

        private void RegisterProcessorMassProductionRecipes()
        {
            try
            {
                int batch = BatchSize.Value;
                float efficiency = EfficiencyBonus.Value;

                // Mass Processor Unit Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 90f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Copper Wire", (int)(batch * 6 * efficiency)),
                        new RecipeResourceInfo("Iron Ingot", (int)(batch * 2 * efficiency)),
                        new RecipeResourceInfo("Kindlevine Extract", (int)(batch * 1 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", batch)
                    },
                    sortPriority = 170
                });

                // Mass Mechanical Components Production
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 60f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Iron Ingot", (int)(batch * 3 * efficiency)),
                        new RecipeResourceInfo("Copper Ingot", (int)(batch * 1 * efficiency))
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Mechanical Components", batch)
                    },
                    sortPriority = 171
                });

                Log.LogInfo($"Processor mass production recipes registered (batch size: {batch})");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"RegisterProcessorMassProductionRecipes error: {ex.Message}");
            }
        }
    }
}
