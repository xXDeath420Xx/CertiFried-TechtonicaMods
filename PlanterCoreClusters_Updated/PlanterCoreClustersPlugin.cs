using System;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;
using TechtonicaFramework.TechTree;

// Type aliases for nested game types
using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;

namespace PlanterCoreClusters
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class PlanterCoreClustersPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.PlanterCoreClusters";
        private const string PluginName = "PlanterCoreClusters";
        private const string VersionString = "3.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        public static string coreBoostGrowingName = "Core Boost (Growing)";
        public static ConfigEntry<float> perClusterBoost;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            perClusterBoost = Config.Bind("General", "Boost Per Cluster", 5.0f,
                new ConfigDescription("Speed boost percentage per Core Cluster (default 5%).", new AcceptableValueRange<float>(1.0f, 50.0f)));

            Harmony.PatchAll();
            Harmony.CreateAndPatchAll(typeof(PlanterInstancePatch));

            // Add new unlock using EMUAdditions (EMU 6.1.3 compatible)
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory, // Category 7 (Modded)
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 100,
                description = "Increases speed of all Planters by configurable % per Core Cluster (default 5%).",
                displayName = coreBoostGrowingName,
                requiredTier = ResearchTier.Tier6, // VICTOR zone
                treePosition = 50
            });

            // Use EMU 6.1.3 Action-based event
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void OnGameDefinesLoaded()
        {
            Unlock smeltingBoost = EMU.Unlocks.GetUnlockByName("Core Boost (Smelting)");
            Unlock threshingBoost = EMU.Unlocks.GetUnlockByName("Core Boost (Threshing)");
            Unlock planter = EMU.Unlocks.GetUnlockByName("Planter");

            EMU.Unlocks.UpdateUnlockTier(coreBoostGrowingName, smeltingBoost.requiredTier);
            EMU.Unlocks.UpdateUnlockTreePosition(coreBoostGrowingName, threshingBoost.treePosition);
            EMU.Unlocks.UpdateUnlockSprite(coreBoostGrowingName, planter.sprite);
        }
    }

    internal class PlanterInstancePatch
    {
        private static Unlock coreBoostGrowing;
        private static bool techUnlocked;

        [HarmonyPatch(typeof(PlanterInstance), "SimUpdate")]
        [HarmonyPrefix]
        private static void ApplyCoreBoost(PlanterInstance __instance)
        {
            if (coreBoostGrowing == null)
            {
                coreBoostGrowing = EMU.Unlocks.GetUnlockByName(PlanterCoreClustersPlugin.coreBoostGrowingName, false);
            }

            if (!techUnlocked)
            {
                techUnlocked = TechTreeState.instance.IsUnlockActive(coreBoostGrowing.uniqueId);
                if (!techUnlocked) return;
            }

            if (TechTreeState.instance.freeCores == 0) return;

            float boost = TechTreeState.instance.freeCores * PlanterCoreClustersPlugin.perClusterBoost.Value * 0.0001f;
            boost += 1f;

            for (int i = 0; i < __instance.plantSlots.Count(); i++)
            {
                if (__instance.plantSlots[i].growthProgress > __instance.plantSlots[i].totalGrowthDuration)
                {
                    __instance.plantSlots[i].growthProgress = __instance.plantSlots[i].totalGrowthDuration;
                }

                int plantId = __instance.plantSlots[i].plantId;
                if (plantId == -1) break;

                SaveState.GetResInfoFromId(plantId);

                if (__instance.plantSlots[i].totalGrowthDuration == 120f)
                {
                    float totalGrowthDuration = Mathf.Max(120f / boost, 1E-07f);
                    __instance.plantSlots[i].totalGrowthDuration = totalGrowthDuration;
                }
            }
        }
    }
}
