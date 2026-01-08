using System;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace CoreComposerPlus
{
    /// <summary>
    /// CoreComposerPlus - Enhances the Memory Tree (Core Composer) with batch processing and speed options.
    /// Allows processing multiple cores at once and/or faster processing speed.
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class CoreComposerPlusPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.CoreComposerPlus";
        public const string PluginName = "CoreComposerPlus";
        public const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Configuration
        public static ConfigEntry<int> BatchSize;
        public static ConfigEntry<float> SpeedMultiplier;
        public static ConfigEntry<bool> InstantMode;
        public static ConfigEntry<bool> DebugMode;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} is loading...");

            // Initialize configuration
            BatchSize = Config.Bind("Processing", "Batch Size", 1,
                new ConfigDescription("Number of cores to process per cycle (1 = vanilla behavior)",
                new AcceptableValueRange<int>(1, 100)));

            SpeedMultiplier = Config.Bind("Processing", "Speed Multiplier", 1f,
                new ConfigDescription("Processing speed multiplier (1.0 = vanilla, 2.0 = 2x faster)",
                new AcceptableValueRange<float>(0.1f, 10f)));

            InstantMode = Config.Bind("Processing", "Instant Mode", false,
                "Process all queued cores instantly (ignores batch size and speed)");

            DebugMode = Config.Bind("General", "Debug Mode", false,
                "Enable verbose debug logging");

            // Apply Harmony patches
            Harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{VersionString} loaded successfully!");
            Log.LogInfo($"Settings: Batch={BatchSize.Value}, Speed={SpeedMultiplier.Value}x, Instant={InstantMode.Value}");
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
    /// Patches to enhance Memory Tree (Core Composer) processing
    /// </summary>
    [HarmonyPatch]
    internal static class MemoryTreePatches
    {
        /// <summary>
        /// Override the SimUpdate method to enable batch processing
        /// </summary>
        [HarmonyPatch(typeof(MemoryTreeInstance), nameof(MemoryTreeInstance.SimUpdate))]
        [HarmonyPrefix]
        private static bool EnhancedSimUpdate(ref MemoryTreeInstance __instance, float dt)
        {
            try
            {
                // Handle power network refresh
                if (__instance.powerInfo.shouldRefreshPowerNetwork)
                {
                    __instance.powerInfo.RefreshPowerNetwork(__instance.gridInfo.neighbors, __instance.CreateRef());
                }

                int stackIndex;
                byte coreType = __instance.FindNextCoreTypeInInventory(out stackIndex);

                __instance.isOn = coreType != byte.MaxValue &&
                                  __instance.GetCurCoreCount() < __instance.GetMaxNumCores();

                __instance.powerInfo.UpdateSimplePowerUsage(__instance.isOn);

                if (!__instance.isOn)
                {
                    __instance.coreBuildTimer = __instance.myDef.coreBuildInterval;
                    return false; // Skip original method
                }

                if (__instance.powerInfo.showPowerWarning)
                {
                    return false; // Skip original method
                }

                // Instant mode - process all available cores immediately
                if (CoreComposerPlusPlugin.InstantMode.Value)
                {
                    ProcessAllCores(ref __instance);
                    return false; // Skip original method
                }

                // Apply speed multiplier to timer
                float speedMult = CoreComposerPlusPlugin.SpeedMultiplier.Value;
                __instance.coreBuildTimer -= dt * __instance.powerInfo.powerSatisfaction * speedMult;

                if (__instance.coreBuildTimer <= 0f)
                {
                    // Process batch of cores
                    int batchSize = CoreComposerPlusPlugin.BatchSize.Value;
                    int processed = ProcessCoresBatch(ref __instance, batchSize);

                    if (processed > 0)
                    {
                        CoreComposerPlusPlugin.LogDebug($"Processed {processed} cores in batch");
                    }

                    __instance.coreBuildTimer = __instance.myDef.coreBuildInterval;
                }

                return false; // Skip original method
            }
            catch (Exception ex)
            {
                CoreComposerPlusPlugin.Log.LogWarning($"Error in enhanced SimUpdate: {ex.Message}");
                return true; // Fall back to original method on error
            }
        }

        /// <summary>
        /// Process a batch of cores from inventory
        /// </summary>
        private static int ProcessCoresBatch(ref MemoryTreeInstance instance, int maxCount)
        {
            int processed = 0;
            int maxCores = instance.GetMaxNumCores();

            while (processed < maxCount)
            {
                // Check if we can add more cores
                if (instance.GetCurCoreCount() >= maxCores)
                    break;

                // Find next core in inventory
                int stackIndex;
                byte coreType = instance.FindNextCoreTypeInInventory(out stackIndex);

                if (coreType == byte.MaxValue)
                    break; // No more cores in inventory

                // Remove from inventory and add to memory tree
                instance.GetInputInventory().RemoveResourcesFromSlot(stackIndex, out var _, 1);
                instance.AddCore(in coreType);
                processed++;
            }

            return processed;
        }

        /// <summary>
        /// Process all available cores instantly
        /// </summary>
        private static void ProcessAllCores(ref MemoryTreeInstance instance)
        {
            int processed = 0;
            int maxCores = instance.GetMaxNumCores();

            while (true)
            {
                // Check if we can add more cores
                if (instance.GetCurCoreCount() >= maxCores)
                    break;

                // Find next core in inventory
                int stackIndex;
                byte coreType = instance.FindNextCoreTypeInInventory(out stackIndex);

                if (coreType == byte.MaxValue)
                    break; // No more cores in inventory

                // Remove from inventory and add to memory tree
                instance.GetInputInventory().RemoveResourcesFromSlot(stackIndex, out var _, 1);
                instance.AddCore(in coreType);
                processed++;
            }

            if (processed > 0)
            {
                CoreComposerPlusPlugin.LogDebug($"Instant processed {processed} cores");
            }

            // Reset timer
            instance.coreBuildTimer = instance.myDef.coreBuildInterval;
        }
    }

    /// <summary>
    /// Patches to increase input inventory capacity for queuing
    /// </summary>
    [HarmonyPatch]
    internal static class MemoryTreeInventoryPatches
    {
        /// <summary>
        /// Increase the input inventory stack size for better queuing
        /// </summary>
        [HarmonyPatch(typeof(MemoryTreeDefinition), nameof(MemoryTreeDefinition.InitInstance))]
        [HarmonyPostfix]
        private static void IncreaseInventoryCapacity(ref MemoryTreeInstance newInstance)
        {
            try
            {
                // The vanilla limit is 8000, but the input inventory may have smaller stack limits
                // Ensure we can queue a good amount of cores
                ref var inventory = ref newInstance.GetInputInventory();

                // Set a generous stack size to allow queuing many cores
                inventory.SetFixedMaxStackSize(8000);

                CoreComposerPlusPlugin.LogDebug("Initialized Memory Tree with enhanced queue capacity");
            }
            catch (Exception ex)
            {
                CoreComposerPlusPlugin.Log.LogWarning($"Error setting inventory capacity: {ex.Message}");
            }
        }
    }
}
