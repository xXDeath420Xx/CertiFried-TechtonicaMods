using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace NarrativeExpansion
{
    /// <summary>
    /// NPC model types available from AssetBundles
    /// </summary>
    public enum NPCModelType
    {
        Primitive,          // Default fallback - capsule + sphere
        MechWarrior,        // SciFiWarrior HPCharacter
        MechPBR,            // SciFiWarrior PBRCharacter
        MechPolyart,        // SciFiWarrior PolyartCharacter
        RobotSphere,        // Floating sphere robot
        RobotMetallic       // Metallic humanoid robot
    }

    /// <summary>
    /// Loads NPC robot models from AssetBundles
    /// </summary>
    public static class NPCAssetLoader
    {
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> loadedPrefabs = new Dictionary<string, GameObject>();
        private static string bundlesPath;
        private static bool initialized = false;

        // Bundle to prefab name mappings
        private static readonly Dictionary<NPCModelType, (string bundle, string prefab)> modelMappings = new Dictionary<NPCModelType, (string, string)>
        {
            { NPCModelType.MechWarrior, ("mech_companion", "HPCharacter") },
            { NPCModelType.MechPBR, ("mech_companion", "PBRCharacter") },
            { NPCModelType.MechPolyart, ("mech_companion", "PolyartCharacter") },
            { NPCModelType.RobotSphere, ("robot_sphere", "robotSphere") },
            { NPCModelType.RobotMetallic, ("robot_metallic", "robot_metallic") }
        };

        /// <summary>
        /// Initialize the asset loader with the plugin path
        /// </summary>
        public static void Initialize(string pluginPath)
        {
            bundlesPath = Path.Combine(pluginPath, "Bundles");
            NarrativeExpansionPlugin.Log.LogInfo($"NPCAssetLoader: Looking for bundles in {bundlesPath}");

            if (!Directory.Exists(bundlesPath))
            {
                NarrativeExpansionPlugin.Log.LogWarning($"NPCAssetLoader: Bundles folder not found at {bundlesPath}");
                Directory.CreateDirectory(bundlesPath);
                NarrativeExpansionPlugin.Log.LogInfo("NPCAssetLoader: Created Bundles folder - add robot bundles for 3D NPC models");
            }

            LoadAllBundles();
            initialized = true;
        }

        /// <summary>
        /// Load all available bundles
        /// </summary>
        private static void LoadAllBundles()
        {
            if (!Directory.Exists(bundlesPath))
            {
                NarrativeExpansionPlugin.Log.LogWarning("NPCAssetLoader: Bundles path doesn't exist");
                return;
            }

            string[] bundleNames = { "mech_companion", "robot_sphere", "robot_metallic" };
            int loadedCount = 0;

            foreach (var bundleName in bundleNames)
            {
                string bundlePath = Path.Combine(bundlesPath, bundleName);

                if (!File.Exists(bundlePath))
                {
                    NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Bundle not found: {bundleName}");
                    continue;
                }

                try
                {
                    var bundle = AssetBundle.LoadFromFile(bundlePath);
                    if (bundle != null)
                    {
                        loadedBundles[bundleName] = bundle;
                        loadedCount++;

                        // Pre-load prefabs from this bundle
                        LoadPrefabsFromBundle(bundleName, bundle);

                        NarrativeExpansionPlugin.Log.LogInfo($"NPCAssetLoader: Loaded bundle '{bundleName}'");
                    }
                }
                catch (Exception ex)
                {
                    NarrativeExpansionPlugin.Log.LogError($"NPCAssetLoader: Failed to load bundle '{bundleName}': {ex.Message}");
                }
            }

            NarrativeExpansionPlugin.Log.LogInfo($"NPCAssetLoader: Loaded {loadedCount} bundles, {loadedPrefabs.Count} prefabs");
        }

        /// <summary>
        /// Load prefabs from a bundle
        /// </summary>
        private static void LoadPrefabsFromBundle(string bundleName, AssetBundle bundle)
        {
            try
            {
                var allAssets = bundle.GetAllAssetNames();
                foreach (var assetPath in allAssets)
                {
                    if (assetPath.EndsWith(".prefab"))
                    {
                        try
                        {
                            var prefab = bundle.LoadAsset<GameObject>(assetPath);
                            if (prefab != null)
                            {
                                string prefabName = Path.GetFileNameWithoutExtension(assetPath);
                                string key = $"{bundleName}:{prefabName}";
                                loadedPrefabs[key] = prefab;
                                NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Loaded prefab '{prefabName}' from '{bundleName}'");
                            }
                        }
                        catch (Exception ex)
                        {
                            NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Failed to load prefab {assetPath}: {ex.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.Log.LogError($"NPCAssetLoader: Error loading prefabs from {bundleName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get a prefab for the specified model type
        /// </summary>
        public static GameObject GetPrefab(NPCModelType modelType)
        {
            if (modelType == NPCModelType.Primitive)
                return null;

            if (!modelMappings.TryGetValue(modelType, out var mapping))
                return null;

            string key = $"{mapping.bundle}:{mapping.prefab}";

            if (loadedPrefabs.TryGetValue(key, out var prefab))
                return prefab;

            // Try case-insensitive search
            foreach (var kvp in loadedPrefabs)
            {
                if (kvp.Key.ToLower().Contains(mapping.prefab.ToLower()))
                {
                    return kvp.Value;
                }
            }

            NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Prefab not found for {modelType} (key: {key})");
            return null;
        }

        /// <summary>
        /// Instantiate an NPC model at the specified position
        /// </summary>
        public static GameObject InstantiateModel(NPCModelType modelType, Vector3 position, Quaternion rotation, Transform parent = null)
        {
            var prefab = GetPrefab(modelType);

            if (prefab == null)
            {
                NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: No prefab for {modelType}, using primitive fallback");
                return null;
            }

            try
            {
                var instance = UnityEngine.Object.Instantiate(prefab, position, rotation, parent);

                if (instance != null)
                {
                    // Clean up any scripts that might cause issues
                    CleanupInstance(instance);

                    NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Instantiated {modelType} model");
                    return instance;
                }
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.Log.LogError($"NPCAssetLoader: Failed to instantiate {modelType}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Clean up an instantiated model (remove unwanted scripts, etc.)
        /// </summary>
        private static void CleanupInstance(GameObject instance)
        {
            try
            {
                // Remove any Animator components that might cause issues without proper setup
                // Keep them for now as they might have idle animations

                // Remove any AI/movement scripts from the asset
                var monoBehaviours = instance.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var mb in monoBehaviours)
                {
                    if (mb == null) continue;

                    string typeName = mb.GetType().FullName ?? "";

                    // Remove demo/sample scripts but keep Animator
                    if (typeName.Contains("Demo") ||
                        typeName.Contains("Sample") ||
                        typeName.Contains("Controller") && !typeName.Contains("Animator"))
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(mb);
                        }
                        catch { }
                    }
                }

                // Remove colliders from children (we'll add our own trigger collider)
                var colliders = instance.GetComponentsInChildren<Collider>(true);
                foreach (var col in colliders)
                {
                    if (col != null)
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(col);
                        }
                        catch { }
                    }
                }

                // Remove Rigidbodies
                var rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
                foreach (var rb in rigidbodies)
                {
                    if (rb != null)
                    {
                        try
                        {
                            UnityEngine.Object.Destroy(rb);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.LogDebug($"NPCAssetLoader: Cleanup error: {ex.Message}");
            }
        }

        /// <summary>
        /// Check if bundles are loaded and models are available
        /// </summary>
        public static bool HasLoadedModels => loadedPrefabs.Count > 0;

        /// <summary>
        /// Get count of loaded prefabs
        /// </summary>
        public static int LoadedPrefabCount => loadedPrefabs.Count;

        /// <summary>
        /// Unload all bundles (call on plugin unload)
        /// </summary>
        public static void Cleanup()
        {
            foreach (var bundle in loadedBundles.Values)
            {
                try
                {
                    bundle?.Unload(false);
                }
                catch { }
            }
            loadedBundles.Clear();
            loadedPrefabs.Clear();
            initialized = false;
        }
    }
}
