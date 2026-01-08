using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ChainExplosives
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class ChainExplosivesPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.ChainExplosives";
        private const string PluginName = "ChainExplosives";
        private const string VersionString = "1.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Track all placed explosives by their instance ID
        public static HashSet<uint> trackedExplosiveIds = new HashSet<uint>();
        public static List<ExplosiveInstance> explosives = new List<ExplosiveInstance>();

        // Config options
        public static ConfigEntry<bool> debugMode;

        // Bigger Explosives config (from Equinox-BiggerExplosives)
        public static ConfigEntry<int> ExplosionWidth;
        public static ConfigEntry<int> ExplosionDepth;
        public static ConfigEntry<bool> BiggerExplosivesEnabled;

        private void Awake()
        {
            Log = Logger;

            debugMode = Config.Bind("Debug", "Debug Mode", false,
                "Enable debug logging for troubleshooting");

            // Bigger Explosives settings
            BiggerExplosivesEnabled = Config.Bind("Bigger Explosives", "Enabled", true,
                "Enable configurable explosion size (like Equinox-BiggerExplosives)");
            ExplosionWidth = Config.Bind("Bigger Explosives", "Explosion Width", 11,
                new ConfigDescription("Width of the resulting tunnel (0-15)", new AcceptableValueRange<int>(0, 15)));
            ExplosionDepth = Config.Bind("Bigger Explosives", "Explosion Depth", 20,
                new ConfigDescription("Distance from the explosive to dig (1-30)", new AcceptableValueRange<int>(1, 30)));

            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");
            Harmony.PatchAll(typeof(ExplosiveDefinitionPatch));
            Harmony.PatchAll(typeof(ExplosiveInteractionPatch));
            Harmony.PatchAll(typeof(ExplosiveInstancePatch));
            Harmony.PatchAll(typeof(BiggerExplosivesPatch));
            Logger.LogInfo($"{PluginName} v{VersionString} loaded!");
            Logger.LogInfo($"  Chain Detonation: Interact with any explosive to detonate ALL");
            Logger.LogInfo($"  Bigger Explosives: Width={ExplosionWidth.Value}, Depth={ExplosionDepth.Value}");
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

    /// <summary>
    /// Bigger Explosives - Modifies explosion width and depth
    /// Based on Equinox-BiggerExplosives functionality
    /// </summary>
    [HarmonyPatch(typeof(ExplosiveDefinition))]
    public static class BiggerExplosivesPatch
    {
        /// <summary>
        /// Modify explosion parameters before detonation
        /// ExplosiveDefinition contains the blast parameters
        /// </summary>
        [HarmonyPatch("Detonate")]
        [HarmonyPrefix]
        static void ModifyExplosionSize(ExplosiveDefinition __instance, ref ExplosiveInstance explosive)
        {
            if (!ChainExplosivesPlugin.BiggerExplosivesEnabled.Value) return;

            try
            {
                // ExplosiveDefinition has blast parameters we can modify
                // blastWidth controls the width of the tunnel
                // blastDistance controls how far the explosion digs

                int configWidth = ChainExplosivesPlugin.ExplosionWidth.Value;
                int configDepth = ChainExplosivesPlugin.ExplosionDepth.Value;

                // Store original values for logging
                int origWidth = __instance.blastWidth;
                int origDist = __instance.blastDistance;

                // Apply configured values
                __instance.blastWidth = configWidth;
                __instance.blastDistance = configDepth;

                ChainExplosivesPlugin.LogDebug($"Modified explosion: Width {origWidth}->{configWidth}, Depth {origDist}->{configDepth}");
            }
            catch (System.Exception ex)
            {
                ChainExplosivesPlugin.Log.LogWarning($"Failed to modify explosion size: {ex.Message}");
            }
        }

        /// <summary>
        /// Alternative patch for VoxelBlaster if ExplosiveDefinition.Detonate doesn't have the params
        /// VoxelBlaster.BlastDirectional is called with width/distance parameters
        /// </summary>
        [HarmonyPatch(typeof(VoxelBlaster), "BlastDirectional")]
        [HarmonyPrefix]
        static void ModifyBlastDirectional(ref int width, ref int distance)
        {
            if (!ChainExplosivesPlugin.BiggerExplosivesEnabled.Value) return;

            int origWidth = width;
            int origDist = distance;

            width = ChainExplosivesPlugin.ExplosionWidth.Value;
            distance = ChainExplosivesPlugin.ExplosionDepth.Value;

            ChainExplosivesPlugin.LogDebug($"BlastDirectional modified: Width {origWidth}->{width}, Distance {origDist}->{distance}");
        }
    }
}
