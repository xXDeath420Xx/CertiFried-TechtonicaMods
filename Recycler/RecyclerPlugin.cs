using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using EquinoxsModUtils;

namespace Recycler
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.equinox.EquinoxsModUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class RecyclerPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.certifried.recycler";
        public const string NAME = "Recycler";
        public const string VERSION = "1.0.0";

        private static RecyclerPlugin instance;
        private Harmony harmony;

        // Configuration
        public static ConfigEntry<float> BasicEfficiency;
        public static ConfigEntry<float> AdvancedEfficiency;
        public static ConfigEntry<float> QuantumEfficiency;
        public static ConfigEntry<float> RecycleTime;
        public static ConfigEntry<bool> AllowOrganic;
        public static ConfigEntry<float> BonusChance;

        // Cached recipe data for recycling
        private static Dictionary<string, RecycleRecipe> recycleRecipes = new Dictionary<string, RecycleRecipe>();
        private static bool recipesBuilt = false;

        // Statistics
        public static int TotalItemsRecycled = 0;
        public static int TotalResourcesRecovered = 0;

        void Awake()
        {
            instance = this;
            Logger.LogInfo($"{NAME} v{VERSION} loading...");

            InitializeConfig();

            harmony = new Harmony(GUID);
            harmony.PatchAll();

            // Register with EMU for game loaded event
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Logger.LogInfo($"{NAME} loaded successfully!");
        }

        private void InitializeConfig()
        {
            BasicEfficiency = Config.Bind("Efficiency", "BasicRecycler", 0.25f,
                new ConfigDescription("Resource recovery rate for Basic Recycler (0.25 = 25%)",
                    new AcceptableValueRange<float>(0.1f, 1f)));

            AdvancedEfficiency = Config.Bind("Efficiency", "AdvancedRecycler", 0.50f,
                new ConfigDescription("Resource recovery rate for Advanced Recycler",
                    new AcceptableValueRange<float>(0.1f, 1f)));

            QuantumEfficiency = Config.Bind("Efficiency", "QuantumRecycler", 0.75f,
                new ConfigDescription("Resource recovery rate for Quantum Recycler",
                    new AcceptableValueRange<float>(0.1f, 1f)));

            RecycleTime = Config.Bind("Processing", "RecycleTime", 5f,
                new ConfigDescription("Base time in seconds to recycle one item",
                    new AcceptableValueRange<float>(1f, 30f)));

            AllowOrganic = Config.Bind("Processing", "AllowOrganic", true,
                "Allow recycling of organic materials (produces compost for BioProcessing)");

            BonusChance = Config.Bind("Processing", "BonusChance", 0.1f,
                new ConfigDescription("Chance to recover rare components as bonus",
                    new AcceptableValueRange<float>(0f, 0.5f)));
        }

        private void OnGameDefinesLoaded()
        {
            if (recipesBuilt) return;

            Logger.LogInfo("Building recycling recipe database...");
            BuildRecycleDatabase();
            Logger.LogInfo($"Created {recycleRecipes.Count} recycling recipes");
            recipesBuilt = true;
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();
            EMU.Events.GameDefinesLoaded -= OnGameDefinesLoaded;
        }

        #region Recipe Database

        private void BuildRecycleDatabase()
        {
            // Get all resource infos and build recycle recipes based on them
            // Since we can't access recipes directly, we'll use predefined recycle rates
            try
            {
                // Add some predefined recycle recipes for common items
                AddDefaultRecipes();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Error building recycle database: {ex.Message}");
            }
        }

        private void AddDefaultRecipes()
        {
            // These are example recipes - in full implementation, would scan actual game recipes
            // For now, provide a framework that can be expanded

            // Machines recycle to components
            AddRecipe("Inserter", new[] { ("Iron Ingot", 2), ("Copper Ingot", 1) });
            AddRecipe("Conveyor Belt", new[] { ("Iron Ingot", 1) });
            AddRecipe("Smelter", new[] { ("Iron Ingot", 5), ("Copper Ingot", 2) });
            AddRecipe("Assembler", new[] { ("Iron Ingot", 8), ("Copper Ingot", 4) });
        }

        private void AddRecipe(string inputName, (string resource, int quantity)[] outputs)
        {
            var recipe = new RecycleRecipe
            {
                InputResourceName = inputName,
                InputQuantity = 1,
                Outputs = new List<RecycleOutput>(),
                IsOrganic = IsOrganicMaterial(inputName)
            };

            foreach (var (resource, quantity) in outputs)
            {
                recipe.Outputs.Add(new RecycleOutput
                {
                    ResourceName = resource,
                    Quantity = quantity,
                    IsGuaranteed = true
                });
            }

            recycleRecipes[inputName] = recipe;
        }

        private bool IsOrganicMaterial(string name)
        {
            if (string.IsNullOrEmpty(name)) return false;

            string lower = name.ToLower();
            return lower.Contains("plant") || lower.Contains("seed") ||
                   lower.Contains("kindlevine") || lower.Contains("shiverthorn") ||
                   lower.Contains("organic") || lower.Contains("fiber");
        }

        #endregion

        #region Recycling Logic

        public static bool CanRecycle(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return false;

            if (!recycleRecipes.ContainsKey(resourceName)) return false;

            var recipe = recycleRecipes[resourceName];

            // Check organic setting
            if (recipe.IsOrganic && !AllowOrganic.Value)
                return false;

            return true;
        }

        public static RecycleResult Recycle(string resourceName, int quantity, RecyclerTier tier)
        {
            if (!CanRecycle(resourceName))
                return null;

            var recipe = recycleRecipes[resourceName];
            float efficiency = GetEfficiency(tier);

            var result = new RecycleResult
            {
                InputResourceName = resourceName,
                InputQuantity = quantity,
                Outputs = new List<RecycleOutput>(),
                Tier = tier
            };

            // Calculate outputs
            foreach (var output in recipe.Outputs)
            {
                // Base quantity scaled by efficiency
                int baseQty = Mathf.RoundToInt(output.Quantity * quantity * efficiency / recipe.InputQuantity);

                if (baseQty > 0)
                {
                    result.Outputs.Add(new RecycleOutput
                    {
                        ResourceName = output.ResourceName,
                        Quantity = baseQty,
                        IsGuaranteed = true
                    });

                    TotalResourcesRecovered += baseQty;
                }
            }

            // Bonus chance for rare components
            if (UnityEngine.Random.value < BonusChance.Value)
            {
                result.BonusReceived = true;
            }

            // Organic waste for BioProcessing integration
            if (recipe.IsOrganic)
            {
                result.ProducesCompost = true;
                result.CompostQuantity = Mathf.Max(1, quantity / 4);
            }

            // Produce scrap from non-100% efficiency
            float wastePercent = 1f - efficiency;
            if (wastePercent > 0)
            {
                result.ScrapQuantity = Mathf.Max(1, Mathf.RoundToInt(quantity * wastePercent));
            }

            TotalItemsRecycled += quantity;

            return result;
        }

        public static float GetEfficiency(RecyclerTier tier)
        {
            switch (tier)
            {
                case RecyclerTier.Basic: return BasicEfficiency.Value;
                case RecyclerTier.Advanced: return AdvancedEfficiency.Value;
                case RecyclerTier.Quantum: return QuantumEfficiency.Value;
                case RecyclerTier.Perfect: return 1f;
                default: return 0.25f;
            }
        }

        public static float GetProcessingTime(RecyclerTier tier)
        {
            float baseTime = RecycleTime.Value;

            switch (tier)
            {
                case RecyclerTier.Basic: return baseTime * 1.5f;
                case RecyclerTier.Advanced: return baseTime;
                case RecyclerTier.Quantum: return baseTime * 0.75f;
                case RecyclerTier.Perfect: return baseTime * 0.5f;
                default: return baseTime;
            }
        }

        #endregion

        #region Utility

        public static void Log(string message)
        {
            instance?.Logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            instance?.Logger.LogWarning(message);
        }

        public static RecycleRecipe GetRecipe(string resourceName)
        {
            if (string.IsNullOrEmpty(resourceName)) return null;
            return recycleRecipes.TryGetValue(resourceName, out var recipe) ? recipe : null;
        }

        public static IEnumerable<RecycleRecipe> GetAllRecipes()
        {
            return recycleRecipes.Values;
        }

        #endregion
    }

    #region Data Structures

    public enum RecyclerTier
    {
        Basic,      // 25% recovery, slow
        Advanced,   // 50% recovery, medium speed
        Quantum,    // 75% recovery, fast
        Perfect     // 100% recovery, requires Atlantum power
    }

    public class RecycleRecipe
    {
        public string InputResourceName;
        public int InputQuantity;
        public List<RecycleOutput> Outputs;
        public bool IsOrganic;
    }

    public class RecycleOutput
    {
        public string ResourceName;
        public int Quantity;
        public bool IsGuaranteed;
        public float BonusChance;
    }

    public class RecycleResult
    {
        public string InputResourceName;
        public int InputQuantity;
        public List<RecycleOutput> Outputs;
        public RecyclerTier Tier;
        public int ScrapQuantity;
        public bool BonusReceived;
        public bool ProducesCompost;
        public int CompostQuantity;
    }

    #endregion
}
