using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace AmbientLife
{
    /// <summary>
    /// Loads creature prefabs from AssetBundles
    /// </summary>
    public static class CreatureAssetLoader
    {
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> creaturePrefabs = new Dictionary<string, GameObject>();
        private static bool isInitialized = false;

        // Bundle names
        private const string BUNDLE_SPIDERS = "creatures_spiders";
        private const string BUNDLE_BEETLES = "creatures_beetles";
        private const string BUNDLE_WOLF = "creatures_wolf";
        private const string BUNDLE_TORTOISE = "creatures_tortoise";
        private const string BUNDLE_ANIMALS = "creatures_animals";

        // Creature categories and their prefabs
        public static readonly Dictionary<CreatureCategory, List<string>> CategoryPrefabs = new Dictionary<CreatureCategory, List<string>>
        {
            { CreatureCategory.Spiders, new List<string> { "spider", "spider_egg_1", "spider_egg_2", "spider_egg_3", "brown_spider", "green_spider", "black_spider" } },
            { CreatureCategory.Beetles, new List<string> { "Flying Beetle", "Nightmare Beetle" } },
            { CreatureCategory.Wolves, new List<string> { "Wolf_LRP", "Wolf_URP", "Wolf_HDRP" } },
            { CreatureCategory.SmallAnimals, new List<string> { "Dog", "Kitty", "Chicken", "Penguin", "Deer", "Tiger", "Horse" } },
            { CreatureCategory.Ambient, new List<string> { "Tortoise Boss" } }
        };

        public enum CreatureCategory
        {
            Spiders,
            Beetles,
            Wolves,
            SmallAnimals,
            Ambient
        }

        public static void Initialize()
        {
            if (isInitialized) return;

            string bundlePath = Path.Combine(AmbientLifePlugin.PluginPath, "Bundles");
            AmbientLifePlugin.Log.LogInfo($"Loading creature bundles from: {bundlePath}");

            if (!Directory.Exists(bundlePath))
            {
                AmbientLifePlugin.Log.LogWarning("Bundles folder not found, creating...");
                Directory.CreateDirectory(bundlePath);
                return;
            }

            // Load all creature bundles
            LoadBundle(bundlePath, BUNDLE_SPIDERS);
            LoadBundle(bundlePath, BUNDLE_BEETLES);
            LoadBundle(bundlePath, BUNDLE_WOLF);
            LoadBundle(bundlePath, BUNDLE_TORTOISE);
            LoadBundle(bundlePath, BUNDLE_ANIMALS);

            // Cache all prefabs
            CachePrefabs();

            isInitialized = true;
            AmbientLifePlugin.Log.LogInfo($"Loaded {creaturePrefabs.Count} creature prefabs");
        }

        private static void LoadBundle(string basePath, string bundleName)
        {
            string fullPath = Path.Combine(basePath, bundleName);
            if (!File.Exists(fullPath))
            {
                AmbientLifePlugin.Log.LogWarning($"Bundle not found: {bundleName}");
                return;
            }

            try
            {
                AssetBundle bundle = AssetBundle.LoadFromFile(fullPath);
                if (bundle != null)
                {
                    loadedBundles[bundleName] = bundle;
                    AmbientLifePlugin.Log.LogInfo($"Loaded bundle: {bundleName}");

                    // Log available assets
                    string[] assetNames = bundle.GetAllAssetNames();
                    foreach (string name in assetNames)
                    {
                        AmbientLifePlugin.LogDebug($"  Asset: {name}");
                    }
                }
                else
                {
                    AmbientLifePlugin.Log.LogWarning($"Failed to load bundle: {bundleName}");
                }
            }
            catch (Exception ex)
            {
                AmbientLifePlugin.Log.LogError($"Error loading bundle {bundleName}: {ex.Message}");
            }
        }

        private static void CachePrefabs()
        {
            foreach (var kvp in loadedBundles)
            {
                string bundleName = kvp.Key;
                AssetBundle bundle = kvp.Value;

                try
                {
                    // Load all GameObjects from the bundle
                    GameObject[] prefabs = bundle.LoadAllAssets<GameObject>();
                    foreach (GameObject prefab in prefabs)
                    {
                        if (prefab != null)
                        {
                            string key = prefab.name.ToLowerInvariant();
                            if (!creaturePrefabs.ContainsKey(key))
                            {
                                creaturePrefabs[key] = prefab;
                                AmbientLifePlugin.LogDebug($"Cached prefab: {prefab.name} from {bundleName}");
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    AmbientLifePlugin.Log.LogError($"Error caching prefabs from {bundleName}: {ex.Message}");
                }
            }
        }

        public static GameObject GetPrefab(string prefabName)
        {
            if (!isInitialized)
            {
                Initialize();
            }

            string key = prefabName.ToLowerInvariant();
            if (creaturePrefabs.TryGetValue(key, out GameObject prefab))
            {
                return prefab;
            }

            // Try partial match
            foreach (var kvp in creaturePrefabs)
            {
                if (kvp.Key.Contains(key) || key.Contains(kvp.Key))
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        public static GameObject SpawnCreature(string prefabName, Vector3 position, Quaternion rotation)
        {
            GameObject prefab = GetPrefab(prefabName);
            if (prefab == null)
            {
                AmbientLifePlugin.LogDebug($"Prefab not found: {prefabName}, using procedural");
                return null;
            }

            GameObject instance = null;
            try
            {
                instance = UnityEngine.Object.Instantiate(prefab, position, rotation);
                if (instance == null)
                {
                    AmbientLifePlugin.Log.LogWarning($"Instantiate returned null for {prefabName}");
                    return null;
                }

                instance.name = $"Ambient_{prefabName}";

                // Clean and disable any existing AI/controllers (handles missing scripts)
                try
                {
                    CleanMissingScripts(instance);
                    DisableDefaultComponents(instance);
                }
                catch (Exception cleanEx)
                {
                    AmbientLifePlugin.LogDebug($"Non-fatal error cleaning {prefabName}: {cleanEx.Message}");
                }

                return instance;
            }
            catch (Exception ex)
            {
                AmbientLifePlugin.Log.LogError($"Error spawning {prefabName}: {ex.Message}");
                // Cleanup if instance was partially created
                if (instance != null)
                {
                    try { UnityEngine.Object.Destroy(instance); } catch { }
                }
                return null;
            }
        }

        private static void DisableDefaultComponents(GameObject obj)
        {
            // Disable any NavMeshAgent (we use our own movement)
            var navAgent = obj.GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (navAgent != null)
            {
                navAgent.enabled = false;
            }

            // Remove/disable any existing AI scripts
            // This handles missing scripts from asset bundles (e.g., ithappy.Animals_FREE.CreatureMover)
            var monoBehaviours = obj.GetComponents<MonoBehaviour>();
            foreach (var mb in monoBehaviours)
            {
                try
                {
                    // Check if this is a valid component (missing scripts return null-like behavior)
                    if (mb == null)
                    {
                        continue; // Skip null/missing scripts
                    }

                    // Try to access the type - this will fail for missing scripts
                    var type = mb.GetType();
                    if (type == null)
                    {
                        continue;
                    }

                    string typeName = type.Name.ToLowerInvariant();
                    string fullTypeName = type.FullName?.ToLowerInvariant() ?? "";

                    // Remove ithappy Animals scripts (they cause errors and we don't need them)
                    if (fullTypeName.Contains("ithappy") || fullTypeName.Contains("animals_free"))
                    {
                        UnityEngine.Object.DestroyImmediate(mb);
                        continue;
                    }

                    // Disable any existing AI scripts we want to keep but not run
                    if (typeName.Contains("ai") || typeName.Contains("controller") ||
                        typeName.Contains("agent") || typeName.Contains("enemy") ||
                        typeName.Contains("mover") || typeName.Contains("input"))
                    {
                        mb.enabled = false;
                    }
                }
                catch (Exception)
                {
                    // Missing script - try to destroy it
                    try
                    {
                        if (mb != null)
                        {
                            UnityEngine.Object.DestroyImmediate(mb);
                        }
                    }
                    catch { }
                }
            }

            // Apply to children
            foreach (Transform child in obj.transform)
            {
                DisableDefaultComponents(child.gameObject);
            }
        }

        /// <summary>
        /// Cleans up missing script components from a prefab/object
        /// </summary>
        private static void CleanMissingScripts(GameObject obj)
        {
            // Get all components including missing ones
            var components = obj.GetComponents<Component>();
            foreach (var comp in components)
            {
                if (comp == null)
                {
                    // This is a missing script - Unity returns null for missing script components
                    // Unfortunately we can't easily remove these at runtime without Editor APIs
                    // The warnings will still appear but the actual errors should be prevented
                    continue;
                }
            }

            // Apply to children
            foreach (Transform child in obj.transform)
            {
                CleanMissingScripts(child.gameObject);
            }
        }

        public static List<string> GetAvailablePrefabs()
        {
            if (!isInitialized)
            {
                Initialize();
            }

            return new List<string>(creaturePrefabs.Keys);
        }

        public static List<string> GetPrefabsForCategory(CreatureCategory category)
        {
            if (CategoryPrefabs.TryGetValue(category, out List<string> prefabs))
            {
                return prefabs;
            }
            return new List<string>();
        }

        public static void Unload()
        {
            foreach (var bundle in loadedBundles.Values)
            {
                if (bundle != null)
                {
                    bundle.Unload(true);
                }
            }
            loadedBundles.Clear();
            creaturePrefabs.Clear();
            isInitialized = false;
        }

        public static bool HasPrefab(string name)
        {
            if (!isInitialized) Initialize();
            return creaturePrefabs.ContainsKey(name.ToLowerInvariant());
        }

        public static int LoadedPrefabCount => creaturePrefabs.Count;
        public static int LoadedBundleCount => loadedBundles.Count;
    }
}
