using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;

namespace ChainExplosives
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ChainExplosivesPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.ChainExplosives";
        private const string PluginName = "ChainExplosives";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Track all placed explosives by their instance ID
        public static HashSet<uint> trackedExplosiveIds = new HashSet<uint>();
        public static List<ExplosiveInstance> explosives = new List<ExplosiveInstance>();

        // Config options
        public static ConfigEntry<bool> debugMode;

        private void Awake()
        {
            Log = Logger;

            debugMode = Config.Bind("Debug", "Debug Mode", false,
                "Enable debug logging for troubleshooting");

            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");
            Harmony.PatchAll(typeof(ExplosiveDefinitionPatch));
            Harmony.PatchAll(typeof(ExplosiveInteractionPatch));
            Harmony.PatchAll(typeof(ExplosiveInstancePatch));
            Logger.LogInfo($"{PluginName} v{VersionString} loaded - Interact with any explosive to detonate ALL placed explosives!");
        }

        public static void LogDebug(string message)
        {
            if (debugMode.Value)
            {
                Log.LogInfo($"[Debug] {message}");
            }
        }
    }

    /// <summary>
    /// Tracks explosives when they are placed/initialized
    /// </summary>
    [HarmonyPatch(typeof(ExplosiveDefinition))]
    public static class ExplosiveDefinitionPatch
    {
        [HarmonyPatch("InitInstance")]
        [HarmonyPostfix]
        static void TrackExplosive(ExplosiveDefinition __instance, ref ExplosiveInstance newInstance)
        {
            // ExplosiveInstance is a struct, check for valid instanceId
            if (newInstance.commonInfo.instanceId == 0) return;

            uint instanceId = newInstance.commonInfo.instanceId;

            // Only add if not already tracked
            if (!ChainExplosivesPlugin.trackedExplosiveIds.Contains(instanceId))
            {
                ChainExplosivesPlugin.trackedExplosiveIds.Add(instanceId);
                ChainExplosivesPlugin.explosives.Add(newInstance);
                ChainExplosivesPlugin.LogDebug($"Tracking explosive #{instanceId} (Total: {ChainExplosivesPlugin.explosives.Count})");
            }
        }
    }

    /// <summary>
    /// Triggers chain detonation when any explosive is interacted with
    /// </summary>
    [HarmonyPatch(typeof(ExplosiveInteration))]
    public static class ExplosiveInteractionPatch
    {
        [HarmonyPatch("Interact")]
        [HarmonyPostfix]
        static void DetonateAllExplosives(ExplosiveInteration __instance)
        {
            ChainExplosivesPlugin.Log.LogInfo($"Chain detonation triggered! Detonating {ChainExplosivesPlugin.explosives.Count} explosives...");

            // Create a copy to avoid modification during iteration
            var explosivesToDetonate = new List<ExplosiveInstance>(ChainExplosivesPlugin.explosives);

            int detonated = 0;
            foreach (var explosive in explosivesToDetonate)
            {
                // ExplosiveInstance is a struct - check if still valid
                if (explosive.commonInfo.instanceId == 0) continue;

                try
                {
                    ChainExplosivesPlugin.LogDebug($"Detonating explosive #{explosive.commonInfo.instanceId}");
                    explosive.Detonate();
                    detonated++;
                }
                catch (System.Exception ex)
                {
                    ChainExplosivesPlugin.Log.LogWarning($"Failed to detonate explosive: {ex.Message}");
                }
            }

            ChainExplosivesPlugin.Log.LogInfo($"Chain detonation complete! Detonated {detonated} explosives.");

            // Clear tracking after chain detonation
            ChainExplosivesPlugin.explosives.Clear();
            ChainExplosivesPlugin.trackedExplosiveIds.Clear();
        }
    }

    /// <summary>
    /// Cleans up tracking when an explosive is detonated individually
    /// </summary>
    [HarmonyPatch(typeof(ExplosiveInstance))]
    public static class ExplosiveInstancePatch
    {
        [HarmonyPatch("Detonate")]
        [HarmonyPrefix]
        static void RemoveFromTracking(ExplosiveInstance __instance)
        {
            // ExplosiveInstance is a struct, check for valid ID
            if (__instance.commonInfo.instanceId == 0) return;

            uint instanceId = __instance.commonInfo.instanceId;

            if (ChainExplosivesPlugin.trackedExplosiveIds.Contains(instanceId))
            {
                ChainExplosivesPlugin.trackedExplosiveIds.Remove(instanceId);
                ChainExplosivesPlugin.explosives.RemoveAll(e => e.commonInfo.instanceId == instanceId);
                ChainExplosivesPlugin.LogDebug($"Removed explosive #{instanceId} from tracking");
            }
        }
    }
}
