using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace EnhancedLogistics
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class EnhancedLogisticsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.EnhancedLogistics";
        private const string PluginName = "EnhancedLogistics";
        private const string VersionString = "2.0.4";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static EnhancedLogisticsPlugin Instance;

        // ============================================
        // SEARCH UI SETTINGS
        // ============================================
        public static ConfigEntry<KeyCode> SearchToggleKey;
        public static ConfigEntry<bool> EnableSearchUI;
        public static ConfigEntry<bool> SearchInventory;
        public static ConfigEntry<bool> SearchCrafting;
        public static ConfigEntry<bool> SearchTechTree;
        public static ConfigEntry<bool> SearchBuildMenu;

        // ============================================
        // STORAGE NETWORK SETTINGS
        // ============================================
        public static ConfigEntry<bool> EnableStorageNetwork;
        public static ConfigEntry<int> MaxNetworkDistance;
        public static ConfigEntry<bool> AutoRouteItems;
        public static ConfigEntry<bool> ShowNetworkVisualization;

        // ============================================
        // BETTER LOGISTICS SETTINGS
        // ============================================
        public static ConfigEntry<bool> EnableBetterLogistics;
        public static ConfigEntry<float> InserterRangeMultiplier;
        public static ConfigEntry<float> InserterSpeedMultiplier;
        public static ConfigEntry<bool> SmartFiltering;
        public static ConfigEntry<int> DefaultFilterStackSize;

        // ============================================
        // DRONE SYSTEM SETTINGS
        // ============================================
        public static ConfigEntry<bool> EnableDroneSystem;
        public static ConfigEntry<int> DroneCapacity;
        public static ConfigEntry<float> DroneSpeed;
        public static ConfigEntry<float> DroneRange;
        public static ConfigEntry<KeyCode> DroneMenuKey;

        // State
        public static bool searchWindowOpen = false;
        public static string currentSearchQuery = "";
        public static List<SearchResult> searchResults = new List<SearchResult>();

        // GUI
        private Rect searchWindowRect = new Rect(Screen.width / 2 - 200, Screen.height / 2 - 250, 400, 500);
        private Rect droneWindowRect = new Rect(20, 100, 350, 400);
        private Vector2 searchScrollPos = Vector2.zero;
        private Vector2 droneScrollPos = Vector2.zero;
        private bool droneWindowOpen = false;
        private int searchCategory = 0;
        private string[] searchCategories = { "All", "Items", "Recipes", "Tech", "Machines" };

        public struct SearchResult
        {
            public string name;
            public string category;
            public string description;
            public Sprite icon;
            public object reference;
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            // Search UI Settings
            SearchToggleKey = Config.Bind("Search UI", "Toggle Key", KeyCode.F,
                "Key to toggle the search UI (when not in text input)");
            EnableSearchUI = Config.Bind("Search UI", "Enable Search UI", true,
                "Enable the search functionality");
            SearchInventory = Config.Bind("Search UI", "Search Inventory", true,
                "Include inventory items in search");
            SearchCrafting = Config.Bind("Search UI", "Search Crafting", true,
                "Include crafting recipes in search");
            SearchTechTree = Config.Bind("Search UI", "Search Tech Tree", true,
                "Include tech tree unlocks in search");
            SearchBuildMenu = Config.Bind("Search UI", "Search Build Menu", true,
                "Include buildable machines in search");

            // Storage Network Settings
            EnableStorageNetwork = Config.Bind("Storage Network", "Enable Storage Network", true,
                "Enable the storage network system");
            MaxNetworkDistance = Config.Bind("Storage Network", "Max Network Distance", 100,
                new ConfigDescription("Maximum distance for storage network connections", new AcceptableValueRange<int>(10, 500)));
            AutoRouteItems = Config.Bind("Storage Network", "Auto Route Items", true,
                "Automatically route items between connected storage");
            ShowNetworkVisualization = Config.Bind("Storage Network", "Show Network Visualization", true,
                "Show visual connections between networked storage");

            // Better Logistics Settings
            EnableBetterLogistics = Config.Bind("Better Logistics", "Enable Better Logistics", true,
                "Enable enhanced logistics features");
            InserterRangeMultiplier = Config.Bind("Better Logistics", "Inserter Range Multiplier", 1.5f,
                new ConfigDescription("Multiplier for inserter reach range", new AcceptableValueRange<float>(1f, 3f)));
            InserterSpeedMultiplier = Config.Bind("Better Logistics", "Inserter Speed Multiplier", 1.5f,
                new ConfigDescription("Multiplier for inserter operation speed", new AcceptableValueRange<float>(1f, 3f)));
            SmartFiltering = Config.Bind("Better Logistics", "Smart Filtering", true,
                "Enable smart item filtering for inserters");
            DefaultFilterStackSize = Config.Bind("Better Logistics", "Default Filter Stack Size", 1,
                new ConfigDescription("Default stack size for filtered items", new AcceptableValueRange<int>(1, 100)));

            // Drone System Settings
            EnableDroneSystem = Config.Bind("Drone System", "Enable Drone System", true,
                "Enable the drone delivery system");
            DroneCapacity = Config.Bind("Drone System", "Drone Capacity", 32,
                new ConfigDescription("Number of items a drone can carry", new AcceptableValueRange<int>(8, 128)));
            DroneSpeed = Config.Bind("Drone System", "Drone Speed", 10f,
                new ConfigDescription("Drone movement speed (units/second)", new AcceptableValueRange<float>(5f, 50f)));
            DroneRange = Config.Bind("Drone System", "Drone Range", 200f,
                new ConfigDescription("Maximum drone delivery range", new AcceptableValueRange<float>(50f, 500f)));
            DroneMenuKey = Config.Bind("Drone System", "Drone Menu Key", KeyCode.J,
                "Key to open drone management menu");

            Harmony.PatchAll();

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log.LogInfo($"Press Ctrl+{SearchToggleKey.Value} for Search, {DroneMenuKey.Value} for Drone Menu");
        }

        private void Update()
        {
            // Search UI toggle (Ctrl + key to avoid conflicts)
            if (EnableSearchUI.Value && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown(SearchToggleKey.Value))
            {
                searchWindowOpen = !searchWindowOpen;
                if (searchWindowOpen)
                {
                    currentSearchQuery = "";
                    searchResults.Clear();
                }
            }

            // Drone menu toggle
            if (EnableDroneSystem.Value && Input.GetKeyDown(DroneMenuKey.Value))
            {
                droneWindowOpen = !droneWindowOpen;
            }

            // Close search with Escape
            if (searchWindowOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                searchWindowOpen = false;
            }

            // Update drone system
            if (EnableDroneSystem.Value)
            {
                DroneManager.Update();
            }
        }

        private void OnGUI()
        {
            if (searchWindowOpen && EnableSearchUI.Value)
            {
                searchWindowRect = GUILayout.Window(88888, searchWindowRect, DrawSearchWindow, "Search (Ctrl+F to close)");
            }

            if (droneWindowOpen && EnableDroneSystem.Value)
            {
                droneWindowRect = GUILayout.Window(88889, droneWindowRect, DrawDroneWindow, "Drone Management");
            }
        }

        private void DrawSearchWindow(int id)
        {
            GUILayout.BeginVertical();

            // Search input
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            GUI.SetNextControlName("SearchField");
            string newQuery = GUILayout.TextField(currentSearchQuery, GUILayout.ExpandWidth(true));
            if (newQuery != currentSearchQuery)
            {
                currentSearchQuery = newQuery;
                PerformSearch();
            }
            GUILayout.EndHorizontal();

            // Focus search field
            if (Event.current.type == EventType.Repaint && GUI.GetNameOfFocusedControl() != "SearchField")
            {
                GUI.FocusControl("SearchField");
            }

            GUILayout.Space(5);

            // Category tabs
            GUILayout.BeginHorizontal();
            for (int i = 0; i < searchCategories.Length; i++)
            {
                GUI.color = (searchCategory == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(searchCategories[i], GUILayout.Height(25)))
                {
                    searchCategory = i;
                    PerformSearch();
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Results
            GUILayout.Label($"Results: {searchResults.Count}");

            searchScrollPos = GUILayout.BeginScrollView(searchScrollPos, GUILayout.Height(350));

            foreach (var result in searchResults)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);

                // Icon placeholder
                GUILayout.Box("", GUILayout.Width(32), GUILayout.Height(32));

                GUILayout.BeginVertical();
                GUILayout.Label(result.name, new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                GUILayout.Label($"[{result.category}] {result.description}", new GUIStyle(GUI.skin.label) { fontSize = 10 });
                GUILayout.EndVertical();

                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Footer
            GUILayout.BeginHorizontal();
            GUILayout.Label("Tip: Type to search items, recipes, tech, and machines");
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60)))
            {
                searchWindowOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private void PerformSearch()
        {
            searchResults.Clear();

            if (string.IsNullOrEmpty(currentSearchQuery) || currentSearchQuery.Length < 2)
                return;

            string query = currentSearchQuery.ToLower();

            // Search Items (Resources)
            if (searchCategory == 0 || searchCategory == 1)
            {
                SearchResources(query);
            }

            // Search Recipes
            if (searchCategory == 0 || searchCategory == 2)
            {
                SearchRecipes(query);
            }

            // Search Tech Tree
            if (searchCategory == 0 || searchCategory == 3)
            {
                SearchTech(query);
            }

            // Search Machines (Buildables)
            if (searchCategory == 0 || searchCategory == 4)
            {
                SearchMachines(query);
            }

            // Sort by relevance (name match first)
            searchResults = searchResults
                .OrderByDescending(r => r.name.ToLower().StartsWith(query))
                .ThenBy(r => r.name)
                .Take(50)
                .ToList();
        }

        private void SearchResources(string query)
        {
            if (GameDefines.instance == null) return;

            try
            {
                foreach (var resource in GameDefines.instance.resources)
                {
                    if (resource == null) continue;

                    string name = resource.displayName ?? resource.name ?? "";
                    string desc = resource.description ?? "";

                    if (name.ToLower().Contains(query) || desc.ToLower().Contains(query))
                    {
                        searchResults.Add(new SearchResult
                        {
                            name = name,
                            category = "Item",
                            description = TruncateString(desc, 60),
                            icon = resource.sprite,
                            reference = resource
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Search resources error: {ex.Message}");
            }
        }

        private void SearchRecipes(string query)
        {
            if (GameDefines.instance == null) return;

            try
            {
                // Search through resources to find craftable items
                foreach (var resource in GameDefines.instance.resources)
                {
                    if (resource == null) continue;

                    string name = resource.displayName ?? resource.name ?? "";

                    // Check if this resource matches the search
                    if (name.ToLower().Contains(query))
                    {
                        string craftMethod = resource.craftingMethod.ToString();

                        searchResults.Add(new SearchResult
                        {
                            name = $"Craft: {name}",
                            category = "Recipe",
                            description = TruncateString($"Method: {craftMethod}", 60),
                            icon = resource.sprite,
                            reference = resource
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Search recipes error: {ex.Message}");
            }
        }

        private void SearchTech(string query)
        {
            if (GameDefines.instance == null) return;

            try
            {
                foreach (var unlock in GameDefines.instance.unlocks)
                {
                    if (unlock == null) continue;

                    string name = unlock.displayName ?? unlock.name ?? "";
                    string desc = unlock.description ?? "";

                    if (name.ToLower().Contains(query) || desc.ToLower().Contains(query))
                    {
                        bool isUnlocked = false;
                        if (TechTreeState.instance != null)
                        {
                            isUnlocked = TechTreeState.instance.IsUnlockActive(unlock.uniqueId);
                        }

                        searchResults.Add(new SearchResult
                        {
                            name = name + (isUnlocked ? " [UNLOCKED]" : ""),
                            category = "Tech",
                            description = TruncateString(desc, 60),
                            icon = unlock.sprite,
                            reference = unlock
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Search tech error: {ex.Message}");
            }
        }

        private void SearchMachines(string query)
        {
            if (GameDefines.instance == null) return;

            try
            {
                foreach (var resource in GameDefines.instance.resources)
                {
                    if (resource == null) continue;
                    if (!(resource is BuilderInfo builderInfo)) continue;

                    string name = resource.displayName ?? resource.name ?? "";
                    string desc = resource.description ?? "";

                    if (name.ToLower().Contains(query) || desc.ToLower().Contains(query))
                    {
                        searchResults.Add(new SearchResult
                        {
                            name = name,
                            category = "Machine",
                            description = TruncateString(desc, 60),
                            icon = resource.sprite,
                            reference = resource
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Search machines error: {ex.Message}");
            }
        }

        private string TruncateString(string str, int maxLength)
        {
            if (string.IsNullOrEmpty(str)) return "";
            if (str.Length <= maxLength) return str;
            return str.Substring(0, maxLength - 3) + "...";
        }

        private void DrawDroneWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Drone Delivery System", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUILayout.Space(10);

            // Drone status
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Active Drones: {DroneManager.activeDrones.Count}");
            GUILayout.Label($"Pending Deliveries: {DroneManager.pendingDeliveries.Count}");
            GUILayout.Label($"Drone Capacity: {DroneCapacity.Value} items");
            GUILayout.Label($"Drone Speed: {DroneSpeed.Value} u/s");
            GUILayout.Label($"Max Range: {DroneRange.Value}m");
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Drone list
            GUILayout.Label("Active Drones:");
            droneScrollPos = GUILayout.BeginScrollView(droneScrollPos, GUILayout.Height(150));

            foreach (var drone in DroneManager.activeDrones)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"Drone #{drone.id}", GUILayout.Width(80));
                GUILayout.Label(drone.state.ToString(), GUILayout.Width(80));
                GUILayout.Label($"{drone.cargoCount}/{DroneCapacity.Value}", GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }

            if (DroneManager.activeDrones.Count == 0)
            {
                GUILayout.Label("No active drones. Build a Drone Hub to deploy drones!");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Create Test Drone"))
            {
                if (Player.instance != null)
                {
                    DroneManager.SpawnDrone(Player.instance.transform.position + Vector3.up * 2f);
                }
            }
            if (GUILayout.Button("Clear All Drones"))
            {
                DroneManager.ClearAllDrones();
            }
            GUILayout.EndHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close"))
            {
                droneWindowOpen = false;
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }
    }

    /// <summary>
    /// Storage Network System - Extends wormhole chest concept
    /// </summary>
    public static class StorageNetwork
    {
        public static Dictionary<string, List<uint>> networkChannels = new Dictionary<string, List<uint>>();
        public static Dictionary<uint, string> chestToNetwork = new Dictionary<uint, string>();

        public static void RegisterChest(uint chestId, string networkId)
        {
            if (!networkChannels.ContainsKey(networkId))
            {
                networkChannels[networkId] = new List<uint>();
            }

            if (!networkChannels[networkId].Contains(chestId))
            {
                networkChannels[networkId].Add(chestId);
            }

            chestToNetwork[chestId] = networkId;
        }

        public static void UnregisterChest(uint chestId)
        {
            if (chestToNetwork.TryGetValue(chestId, out string networkId))
            {
                if (networkChannels.ContainsKey(networkId))
                {
                    networkChannels[networkId].Remove(chestId);
                    if (networkChannels[networkId].Count == 0)
                    {
                        networkChannels.Remove(networkId);
                    }
                }
                chestToNetwork.Remove(chestId);
            }
        }

        public static List<uint> GetNetworkChests(string networkId)
        {
            if (networkChannels.TryGetValue(networkId, out var chests))
            {
                return chests;
            }
            return new List<uint>();
        }

        public static bool TryRouteItem(string networkId, int resourceId, int count, out uint targetChestId)
        {
            targetChestId = 0;
            if (!EnhancedLogisticsPlugin.AutoRouteItems.Value) return false;

            var chests = GetNetworkChests(networkId);
            foreach (var chestId in chests)
            {
                // Find chest with space for this item
                try
                {
                    var chest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)chestId, MachineTypeEnum.Chest);
                    if (chest.commonInfo.instanceId == 0) continue;

                    var inv = chest.GetInventory();
                    if (inv.CanAddResources(resourceId, count))
                    {
                        targetChestId = chestId;
                        return true;
                    }
                }
                catch { }
            }
            return false;
        }
    }

    /// <summary>
    /// Drone Delivery System
    /// </summary>
    public static class DroneManager
    {
        public static List<DroneInstance> activeDrones = new List<DroneInstance>();
        public static Queue<DeliveryTask> pendingDeliveries = new Queue<DeliveryTask>();
        private static int nextDroneId = 1;

        public class DroneInstance
        {
            public int id;
            public GameObject gameObject;
            public Vector3 position;
            public Vector3 targetPosition;
            public DroneState state;
            public int cargoCount;
            public List<ResourceStack> cargo = new List<ResourceStack>();
            public uint sourceChestId;
            public uint targetChestId;
            public float stateTimer;
        }

        public enum DroneState
        {
            Idle,
            MovingToPickup,
            Loading,
            MovingToDropoff,
            Unloading,
            Returning
        }

        public class DeliveryTask
        {
            public uint sourceChestId;
            public uint targetChestId;
            public int resourceId;
            public int count;
        }

        public static DroneInstance SpawnDrone(Vector3 position)
        {
            var drone = new DroneInstance
            {
                id = nextDroneId++,
                position = position,
                state = DroneState.Idle,
                cargoCount = 0
            };

            // Create visual (using a simple sphere for now - could use research core model)
            drone.gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            drone.gameObject.name = $"Drone_{drone.id}";
            drone.gameObject.transform.position = position;
            drone.gameObject.transform.localScale = Vector3.one * 0.5f;

            // Remove collider so it doesn't interfere
            var collider = drone.gameObject.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            // Add glowing material
            var renderer = drone.gameObject.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.5f, 0.8f, 1f);
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", new Color(0.3f, 0.6f, 1f) * 2f);
            }

            activeDrones.Add(drone);
            EnhancedLogisticsPlugin.Log.LogInfo($"Spawned drone #{drone.id} at {position}");
            return drone;
        }

        public static void Update()
        {
            foreach (var drone in activeDrones.ToList())
            {
                UpdateDrone(drone);
            }

            // Assign pending deliveries to idle drones
            while (pendingDeliveries.Count > 0)
            {
                var idleDrone = activeDrones.FirstOrDefault(d => d.state == DroneState.Idle);
                if (idleDrone == null) break;

                var task = pendingDeliveries.Dequeue();
                AssignTask(idleDrone, task);
            }
        }

        private static void UpdateDrone(DroneInstance drone)
        {
            if (drone.gameObject == null) return;

            float speed = EnhancedLogisticsPlugin.DroneSpeed.Value;
            float dt = Time.deltaTime;

            switch (drone.state)
            {
                case DroneState.Idle:
                    // Hover in place with slight bob
                    float bob = Mathf.Sin(Time.time * 2f + drone.id) * 0.1f;
                    drone.gameObject.transform.position = drone.position + Vector3.up * bob;
                    break;

                case DroneState.MovingToPickup:
                case DroneState.MovingToDropoff:
                case DroneState.Returning:
                    // Move towards target
                    Vector3 direction = (drone.targetPosition - drone.position).normalized;
                    float distance = Vector3.Distance(drone.position, drone.targetPosition);

                    if (distance < 0.5f)
                    {
                        drone.position = drone.targetPosition;
                        OnArrived(drone);
                    }
                    else
                    {
                        drone.position += direction * speed * dt;
                    }

                    drone.gameObject.transform.position = drone.position;
                    break;

                case DroneState.Loading:
                case DroneState.Unloading:
                    drone.stateTimer -= dt;
                    if (drone.stateTimer <= 0)
                    {
                        OnLoadUnloadComplete(drone);
                    }
                    break;
            }
        }

        private static void OnArrived(DroneInstance drone)
        {
            switch (drone.state)
            {
                case DroneState.MovingToPickup:
                    drone.state = DroneState.Loading;
                    drone.stateTimer = 1f;
                    break;

                case DroneState.MovingToDropoff:
                    drone.state = DroneState.Unloading;
                    drone.stateTimer = 1f;
                    break;

                case DroneState.Returning:
                    drone.state = DroneState.Idle;
                    break;
            }
        }

        private static void OnLoadUnloadComplete(DroneInstance drone)
        {
            switch (drone.state)
            {
                case DroneState.Loading:
                    // Start moving to dropoff
                    try
                    {
                        var targetChest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)drone.targetChestId, MachineTypeEnum.Chest);
                        drone.targetPosition = targetChest.gridInfo.Center + Vector3.up * 2f;
                        drone.state = DroneState.MovingToDropoff;
                    }
                    catch
                    {
                        drone.state = DroneState.Returning;
                        drone.targetPosition = drone.position;
                    }
                    break;

                case DroneState.Unloading:
                    // Return to home/idle position
                    drone.cargo.Clear();
                    drone.cargoCount = 0;
                    drone.state = DroneState.Idle;
                    break;
            }
        }

        public static void AssignTask(DroneInstance drone, DeliveryTask task)
        {
            try
            {
                var sourceChest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)task.sourceChestId, MachineTypeEnum.Chest);
                drone.sourceChestId = task.sourceChestId;
                drone.targetChestId = task.targetChestId;
                drone.targetPosition = sourceChest.gridInfo.Center + Vector3.up * 2f;
                drone.state = DroneState.MovingToPickup;
            }
            catch
            {
                drone.state = DroneState.Idle;
            }
        }

        public static void QueueDelivery(uint sourceChestId, uint targetChestId, int resourceId, int count)
        {
            pendingDeliveries.Enqueue(new DeliveryTask
            {
                sourceChestId = sourceChestId,
                targetChestId = targetChestId,
                resourceId = resourceId,
                count = count
            });
        }

        public static void ClearAllDrones()
        {
            foreach (var drone in activeDrones)
            {
                if (drone.gameObject != null)
                {
                    UnityEngine.Object.Destroy(drone.gameObject);
                }
            }
            activeDrones.Clear();
            pendingDeliveries.Clear();
        }
    }

    /// <summary>
    /// Harmony patches for enhanced logistics features
    /// </summary>
    [HarmonyPatch]
    internal static class LogisticsPatches
    {
        // CheckPlacement patch removed - method doesn't exist in game
        // Inserter range is handled through InserterDefinition properties instead

        /// <summary>
        /// Speed up inserter operations
        /// </summary>
        [HarmonyPatch(typeof(InserterInstance), "SimUpdate")]
        [HarmonyPrefix]
        private static void SpeedUpInserter(InserterInstance __instance)
        {
            if (!EnhancedLogisticsPlugin.EnableBetterLogistics.Value) return;

            // Apply speed multiplier by adjusting the inserter's internal timer
            // This would need access to private fields via reflection
        }
    }
}
