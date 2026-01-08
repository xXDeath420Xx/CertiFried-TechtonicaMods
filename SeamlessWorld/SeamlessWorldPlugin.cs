using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeamlessWorld
{
    /// <summary>
    /// SeamlessWorld - Removes floor barriers to create a continuous world.
    /// All floors are loaded simultaneously, allowing vertical traversal without loading screens.
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.certifired.TechtonicaFramework", BepInDependency.DependencyFlags.SoftDependency)]
    public class SeamlessWorldPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.SeamlessWorld";
        public const string PluginName = "SeamlessWorld";
        public const string VersionString = "0.1.0";

        public static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log { get; private set; }
        public static SeamlessWorldPlugin Instance { get; private set; }

        // Configuration
        public static ConfigEntry<bool> EnableSeamlessWorld;
        public static ConfigEntry<float> FloorSeparation;
        public static ConfigEntry<bool> DebugMode;
        public static ConfigEntry<int> MaxLoadedStrata;

        // Track loaded strata
        public static HashSet<byte> LoadedStrata = new HashSet<byte>();
        public static Dictionary<byte, Vector3> StrataOffsets = new Dictionary<byte, Vector3>();

        // Floor Y-offset (how far apart floors are in world space)
        public const float DEFAULT_FLOOR_SEPARATION = 350f;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} is loading...");

            InitializeConfig();

            // Apply Harmony patches
            Harmony.PatchAll();

            // Apply manual patches for methods that may not exist in all game versions
            SeamlessWorld.Patches.MachinePatches.ApplyManualPatches(Harmony);

            // Register for game events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableSeamlessWorld = Config.Bind("General", "Enable Seamless World", true,
                "Enable the seamless world system (all floors loaded simultaneously)");

            FloorSeparation = Config.Bind("General", "Floor Separation", DEFAULT_FLOOR_SEPARATION,
                "Vertical distance between floor levels in world units");

            MaxLoadedStrata = Config.Bind("Performance", "Max Loaded Strata", 4,
                "Maximum number of strata to keep loaded simultaneously (affects memory usage)");

            DebugMode = Config.Bind("Debug", "Debug Mode", false,
                "Enable verbose debug logging");
        }

        private void OnGameDefinesLoaded()
        {
            if (!EnableSeamlessWorld.Value) return;

            Log.LogInfo("Initializing seamless world system...");

            // Calculate Y-offsets for each strata
            CalculateStrataOffsets();
        }

        private void CalculateStrataOffsets()
        {
            try
            {
                // Get all strata definitions from the game
                if (FlowManager.instance == null) return;

                var levelDef = FlowManager.instance.curLevel;
                if (levelDef == null || levelDef.strataDefinitions == null) return;

                float currentOffset = 0f;
                for (int i = 0; i < levelDef.strataDefinitions.Length; i++)
                {
                    var strata = levelDef.strataDefinitions[i];
                    if (strata == null) continue;

                    StrataOffsets[(byte)i] = new Vector3(0, currentOffset, 0);
                    LogDebug($"Strata {i} ({strata.name}): Y-offset = {currentOffset}");

                    // Add separation for next floor (terrain height + gap)
                    currentOffset -= (strata.terrainBounds.y + FloorSeparation.Value);
                }

                Log.LogInfo($"Calculated offsets for {StrataOffsets.Count} strata");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to calculate strata offsets: {ex.Message}");
            }
        }

        public static void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }

        /// <summary>
        /// Get the world position offset for a given strata
        /// </summary>
        public static Vector3 GetStrataOffset(byte strataNum)
        {
            if (StrataOffsets.TryGetValue(strataNum, out Vector3 offset))
            {
                return offset;
            }
            return Vector3.zero;
        }

        /// <summary>
        /// Convert a local strata position to unified world position
        /// </summary>
        public static Vector3 LocalToWorldPosition(Vector3 localPos, byte strataNum)
        {
            return localPos + GetStrataOffset(strataNum);
        }

        /// <summary>
        /// Convert a unified world position to local strata position
        /// </summary>
        public static (Vector3 localPos, byte strataNum) WorldToLocalPosition(Vector3 worldPos)
        {
            // Find which strata this Y-position belongs to
            foreach (var kvp in StrataOffsets)
            {
                float strataTop = kvp.Value.y;
                float strataBottom = strataTop - (FlowManager.instance?.curLevel?.strataDefinitions[kvp.Key]?.terrainBounds.y ?? 300f);

                if (worldPos.y <= strataTop && worldPos.y >= strataBottom)
                {
                    Vector3 localPos = worldPos - kvp.Value;
                    return (localPos, kvp.Key);
                }
            }

            // Default to strata 0 if not found
            return (worldPos, 0);
        }
    }
}
