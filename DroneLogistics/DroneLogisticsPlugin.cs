using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace DroneLogistics
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.equinox.EquinoxsModUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class DroneLogisticsPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.certifried.dronelogistics";
        public const string NAME = "DroneLogistics";
        public const string VERSION = "1.0.0";

        private static DroneLogisticsPlugin instance;
        private Harmony harmony;

        // Configuration
        public static ConfigEntry<int> MaxDronesPerPad;
        public static ConfigEntry<float> DroneSpeed;
        public static ConfigEntry<float> DroneRange;
        public static ConfigEntry<int> DroneCapacity;
        public static ConfigEntry<bool> UseCargoCrates;
        public static ConfigEntry<float> ChargingTime;
        public static ConfigEntry<bool> EnableBiofuel;

        // Asset Bundles
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        // Active systems
        public static List<DronePadController> ActivePads = new List<DronePadController>();
        public static List<DroneController> ActiveDrones = new List<DroneController>();
        public static List<PackingStationController> ActivePackingStations = new List<PackingStationController>();

        // Material cache for URP compatibility
        private static Material cachedMaterial;
        private static Material cachedEffectMaterial;

        // Route management
        public static RouteManager Routes { get; private set; }

        void Awake()
        {
            instance = this;
            Logger.LogInfo($"{NAME} v{VERSION} loading...");

            InitializeConfig();
            LoadAssetBundles();

            harmony = new Harmony(GUID);
            harmony.PatchAll();

            Routes = new RouteManager();

            Logger.LogInfo($"{NAME} loaded successfully!");
        }

        private void InitializeConfig()
        {
            MaxDronesPerPad = Config.Bind("Drones", "MaxDronesPerPad", 4,
                "Maximum number of drones that can operate from a single pad");

            DroneSpeed = Config.Bind("Drones", "DroneSpeed", 15f,
                "Base drone flight speed (m/s)");

            DroneRange = Config.Bind("Drones", "DroneRange", 200f,
                "Maximum drone operating range from pad (meters)");

            DroneCapacity = Config.Bind("Drones", "DroneCapacity", 1,
                "Number of stacks/crates a drone can carry");

            UseCargoCrates = Config.Bind("Advanced", "UseCargoCrates", false,
                "Enable cargo crate system (requires packing stations). When disabled, drones carry items directly.");

            ChargingTime = Config.Bind("Drones", "ChargingTime", 10f,
                "Time in seconds for a drone to fully recharge");

            EnableBiofuel = Config.Bind("Advanced", "EnableBiofuel", false,
                "Enable biofuel-powered drones (requires BioProcessing mod)");
        }

        private void LoadAssetBundles()
        {
            string bundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Bundles");

            if (!Directory.Exists(bundlePath))
            {
                Logger.LogWarning($"Bundles folder not found at {bundlePath}");
                return;
            }

            string[] bundleNames = { "drones_voodooplay", "drones_scifi", "drones_simple" };

            foreach (var bundleName in bundleNames)
            {
                string fullPath = Path.Combine(bundlePath, bundleName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(fullPath);
                        if (bundle != null)
                        {
                            loadedBundles[bundleName] = bundle;
                            Logger.LogInfo($"Loaded bundle: {bundleName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
            }
        }

        void Update()
        {
            // Clean up destroyed objects
            ActivePads.RemoveAll(p => p == null);
            ActiveDrones.RemoveAll(d => d == null);
            ActivePackingStations.RemoveAll(s => s == null);
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();

            foreach (var bundle in loadedBundles.Values)
            {
                bundle?.Unload(true);
            }
            loadedBundles.Clear();
        }

        #region Asset Loading

        public static GameObject GetPrefab(string bundleName, string prefabName)
        {
            string key = $"{bundleName}/{prefabName}";

            if (prefabCache.TryGetValue(key, out var cached))
                return cached;

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                instance?.Logger.LogWarning($"Bundle not loaded: {bundleName}");
                return null;
            }

            var prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab != null)
            {
                prefabCache[key] = prefab;
            }

            return prefab;
        }

        public static void FixPrefabMaterials(GameObject obj)
        {
            if (cachedMaterial == null)
            {
                // Find a valid URP material from the game
                var gameRenderers = FindObjectsOfType<Renderer>();
                foreach (var r in gameRenderers)
                {
                    if (r.material != null && r.material.shader != null &&
                        r.material.shader.name.Contains("Universal"))
                    {
                        cachedMaterial = r.material;
                        break;
                    }
                }
            }

            if (cachedMaterial == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader != null)
                    {
                        // Check if shader is broken (pink)
                        if (!materials[i].shader.name.Contains("Universal") &&
                            !materials[i].shader.name.Contains("URP"))
                        {
                            var newMat = new Material(cachedMaterial);
                            if (materials[i].mainTexture != null)
                                newMat.mainTexture = materials[i].mainTexture;
                            newMat.color = materials[i].color;
                            materials[i] = newMat;
                        }
                    }
                }
                renderer.materials = materials;
            }
        }

        public static Material GetEffectMaterial(Color color)
        {
            if (cachedEffectMaterial == null)
            {
                cachedEffectMaterial = new Material(Shader.Find("Sprites/Default"));
            }

            var mat = new Material(cachedEffectMaterial);
            mat.color = color;
            return mat;
        }

        #endregion

        #region Spawning

        public static DroneController SpawnDrone(DroneType type, Vector3 position, DronePadController homePad)
        {
            string bundleName = "drones_simple";
            string prefabName = "drone";

            switch (type)
            {
                case DroneType.Scout:
                    bundleName = "drones_simple";
                    prefabName = "drone blue";
                    break;
                case DroneType.Cargo:
                    bundleName = "drones_voodooplay";
                    prefabName = "Drone";
                    break;
                case DroneType.HeavyLifter:
                    bundleName = "drones_scifi";
                    prefabName = "Robot_Collector";
                    break;
                case DroneType.Combat:
                    bundleName = "drones_scifi";
                    prefabName = "Robot_Guardian";
                    break;
            }

            var prefab = GetPrefab(bundleName, prefabName);
            if (prefab == null)
            {
                instance?.Logger.LogWarning($"Could not find drone prefab: {bundleName}/{prefabName}");
                return null;
            }

            var droneObj = Instantiate(prefab, position, Quaternion.identity);
            droneObj.name = $"Drone_{type}_{ActiveDrones.Count}";

            FixPrefabMaterials(droneObj);

            var drone = droneObj.AddComponent<DroneController>();
            drone.Initialize(type, homePad);

            ActiveDrones.Add(drone);

            return drone;
        }

        public static DronePadController SpawnDronePad(Vector3 position)
        {
            // Create a simple pad (can be replaced with proper model)
            var padObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            padObj.transform.position = position;
            padObj.transform.localScale = new Vector3(3f, 0.2f, 3f);
            padObj.name = $"DronePad_{ActivePads.Count}";

            var renderer = padObj.GetComponent<Renderer>();
            renderer.material = GetEffectMaterial(new Color(0.2f, 0.3f, 0.8f, 1f));

            var pad = padObj.AddComponent<DronePadController>();
            pad.Initialize();

            ActivePads.Add(pad);

            return pad;
        }

        #endregion

        #region Cargo System

        /// <summary>
        /// Pack items into a cargo crate (if UseCargoCrates enabled)
        /// Otherwise just return the items as-is for direct transport
        /// </summary>
        public static CargoData PackItems(ResourceInfo resource, int quantity)
        {
            var cargo = new CargoData
            {
                ResourceType = resource,
                Quantity = quantity,
                IsCrate = UseCargoCrates.Value
            };

            if (UseCargoCrates.Value)
            {
                // Crates hold 2 stacks worth
                cargo.MaxQuantity = resource.maxStackCount * 2;
            }
            else
            {
                // Direct transport uses 1 stack
                cargo.MaxQuantity = resource.maxStackCount;
            }

            cargo.Quantity = Mathf.Min(quantity, cargo.MaxQuantity);

            return cargo;
        }

        #endregion

        #region Utility

        public static DronePadController FindNearestPad(Vector3 position)
        {
            DronePadController nearest = null;
            float nearestDist = float.MaxValue;

            foreach (var pad in ActivePads)
            {
                if (pad == null) continue;
                float dist = Vector3.Distance(position, pad.transform.position);
                if (dist < nearestDist)
                {
                    nearestDist = dist;
                    nearest = pad;
                }
            }

            return nearest;
        }

        public static DroneController FindAvailableDrone(Vector3 position, float maxRange = -1)
        {
            if (maxRange < 0) maxRange = DroneRange.Value;

            foreach (var drone in ActiveDrones)
            {
                if (drone == null || !drone.IsAvailable) continue;

                float dist = Vector3.Distance(position, drone.transform.position);
                if (dist <= maxRange)
                {
                    return drone;
                }
            }

            return null;
        }

        public static void Log(string message)
        {
            instance?.Logger.LogInfo(message);
        }

        public static void LogWarning(string message)
        {
            instance?.Logger.LogWarning(message);
        }

        #endregion
    }

    #region Data Structures

    public enum DroneType
    {
        Scout,      // Fast, low capacity, short range
        Cargo,      // Balanced for transport
        HeavyLifter,// Slow, high capacity, long range
        Combat      // For TurretDefense integration - ammo resupply
    }

    public enum DroneState
    {
        Idle,
        Departing,
        Traveling,
        Loading,
        Unloading,
        Returning,
        Charging,
        Disabled
    }

    public class CargoData
    {
        public ResourceInfo ResourceType;
        public int Quantity;
        public int MaxQuantity;
        public bool IsCrate;

        public bool IsFull => Quantity >= MaxQuantity;
        public bool IsEmpty => Quantity <= 0;
    }

    public class DeliveryRequest
    {
        public Vector3 PickupLocation;
        public Vector3 DeliveryLocation;
        public ResourceInfo Resource;
        public int Quantity;
        public int Priority;
        public float TimeRequested;

        public DeliveryRequest(Vector3 pickup, Vector3 delivery, ResourceInfo resource, int qty, int priority = 0)
        {
            PickupLocation = pickup;
            DeliveryLocation = delivery;
            Resource = resource;
            Quantity = qty;
            Priority = priority;
            TimeRequested = Time.time;
        }
    }

    #endregion
}
