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
        private const string VersionString = "2.0.3";

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

        // ============================================
        // DRONE RELAY SETTINGS
        // ============================================
        public static ConfigEntry<bool> EnableDroneRelays;
        public static ConfigEntry<float> RelayBaseRange;
        public static ConfigEntry<bool> ShowRelayNetwork;
        public static ConfigEntry<int> MaxDronesPerNetwork;

        // ============================================
        // DRONE RESEARCH SETTINGS
        // ============================================
        public static ConfigEntry<int> CurrentDroneTier;
        public static ConfigEntry<bool> AutoUnlockTiers;

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

            // Drone Relay Settings
            EnableDroneRelays = Config.Bind("Drone Relays", "Enable Drone Relays", true,
                "Enable the drone relay system for extended network coverage");
            RelayBaseRange = Config.Bind("Drone Relays", "Relay Base Range", 150f,
                new ConfigDescription("Base range for drone relays (extends network)", new AcceptableValueRange<float>(50f, 500f)));
            ShowRelayNetwork = Config.Bind("Drone Relays", "Show Relay Network", true,
                "Display visual connections between relays");
            MaxDronesPerNetwork = Config.Bind("Drone Relays", "Max Drones Per Network", 5,
                new ConfigDescription("Maximum drones that can operate in a single network", new AcceptableValueRange<int>(1, 20)));

            // Drone Research Settings
            CurrentDroneTier = Config.Bind("Drone Research", "Current Drone Tier", 1,
                new ConfigDescription("Current unlocked drone technology tier", new AcceptableValueRange<int>(1, 5)));
            AutoUnlockTiers = Config.Bind("Drone Research", "Auto Unlock Tiers", false,
                "Automatically unlock drone tiers (for testing)");

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

            // Update drone relay network
            if (EnableDroneRelays.Value)
            {
                DroneRelayNetwork.Update();
            }
        }

        private void Start()
        {
            // Initialize relay network
            DroneRelayNetwork.Initialize();
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

        private int droneWindowTab = 0;
        private string[] droneTabNames = { "Status", "Research", "Relays" };

        private void DrawDroneWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("Drone Delivery System", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUILayout.Space(5);

            // Tab buttons
            GUILayout.BeginHorizontal();
            for (int i = 0; i < droneTabNames.Length; i++)
            {
                GUI.color = (droneWindowTab == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(droneTabNames[i], GUILayout.Height(25)))
                {
                    droneWindowTab = i;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            switch (droneWindowTab)
            {
                case 0: DrawDroneStatusTab(); break;
                case 1: DrawDroneResearchTab(); break;
                case 2: DrawDroneRelayTab(); break;
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("Close"))
            {
                droneWindowOpen = false;
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private void DrawDroneStatusTab()
        {
            // Drone status with tiered stats
            GUILayout.BeginVertical(GUI.skin.box);
            var tierData = DroneResearch.GetCurrentTierData();
            GUILayout.Label($"Tech Level: {DroneResearch.GetTierProgressString()}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Space(5);
            GUILayout.Label($"Active Drones: {DroneManager.activeDrones.Count}/{DroneManager.GetMaxDrones()}");
            GUILayout.Label($"Pending Deliveries: {DroneManager.pendingDeliveries.Count}");
            GUILayout.Label($"Capacity: {DroneManager.GetTieredCapacity()} items");
            GUILayout.Label($"Speed: {DroneManager.GetTieredSpeed():F1} u/s");
            GUILayout.Label($"Range: {DroneManager.GetTieredRange():F0}m");
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Drone list
            GUILayout.Label("Active Drones:");
            droneScrollPos = GUILayout.BeginScrollView(droneScrollPos, GUILayout.Height(120));

            foreach (var drone in DroneManager.activeDrones)
            {
                GUILayout.BeginHorizontal(GUI.skin.box);
                GUILayout.Label($"#{drone.id}", GUILayout.Width(30));
                GUI.color = drone.state == DroneManager.DroneState.Idle ? Color.green : Color.yellow;
                GUILayout.Label(drone.state.ToString(), GUILayout.Width(90));
                GUI.color = Color.white;
                GUILayout.Label($"{drone.cargoCount}/{DroneManager.GetTieredCapacity()}", GUILayout.Width(50));
                GUILayout.EndHorizontal();
            }

            if (DroneManager.activeDrones.Count == 0)
            {
                GUILayout.Label("No active drones.");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Controls
            GUILayout.BeginHorizontal();
            if (DroneManager.activeDrones.Count < DroneManager.GetMaxDrones())
            {
                if (GUILayout.Button("Spawn Drone"))
                {
                    if (Player.instance != null)
                    {
                        DroneManager.SpawnDrone(Player.instance.transform.position + Vector3.up * 2f);
                    }
                }
            }
            else
            {
                GUI.enabled = false;
                GUILayout.Button("Max Drones");
                GUI.enabled = true;
            }

            if (GUILayout.Button("Clear All"))
            {
                DroneManager.ClearAllDrones();
            }
            GUILayout.EndHorizontal();
        }

        private void DrawDroneResearchTab()
        {
            GUILayout.Label("Drone Technology Research", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Space(5);

            foreach (DroneResearch.DroneTier tier in Enum.GetValues(typeof(DroneResearch.DroneTier)))
            {
                var data = DroneResearch.tierData[tier];
                bool isUnlocked = (int)tier <= CurrentDroneTier.Value;
                bool canUnlock = DroneResearch.CanUnlockTier(tier);

                GUILayout.BeginVertical(GUI.skin.box);

                // Tier header
                GUILayout.BeginHorizontal();
                GUI.color = isUnlocked ? Color.green : (canUnlock ? Color.yellow : Color.gray);
                GUILayout.Label($"Tier {(int)tier}: {data.name}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
                GUI.color = Color.white;

                if (isUnlocked)
                {
                    GUILayout.Label("[UNLOCKED]", GUILayout.Width(80));
                }
                else if (canUnlock)
                {
                    if (GUILayout.Button("Unlock", GUILayout.Width(60)))
                    {
                        DroneResearch.TryUnlockTier(tier);
                    }
                }
                else
                {
                    GUILayout.Label("[LOCKED]", GUILayout.Width(80));
                }
                GUILayout.EndHorizontal();

                // Stats
                GUILayout.Label(data.description, new GUIStyle(GUI.skin.label) { fontSize = 10 });
                GUILayout.BeginHorizontal();
                GUILayout.Label($"Speed: x{data.speedMultiplier:F2}", GUILayout.Width(80));
                GUILayout.Label($"Capacity: x{data.capacityMultiplier:F2}", GUILayout.Width(90));
                GUILayout.Label($"Range: x{data.rangeMultiplier:F2}", GUILayout.Width(80));
                GUILayout.Label($"+{data.bonusDrones} drones", GUILayout.Width(70));
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
            }
        }

        private void DrawDroneRelayTab()
        {
            GUILayout.Label("Drone Relay Network", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Space(5);

            // Relay settings
            GUILayout.BeginVertical(GUI.skin.box);
            EnableDroneRelays.Value = GUILayout.Toggle(EnableDroneRelays.Value, " Enable Relay System");
            ShowRelayNetwork.Value = GUILayout.Toggle(ShowRelayNetwork.Value, " Show Network Visualization");
            GUILayout.Label($"Relay Base Range: {RelayBaseRange.Value:F0}m");
            GUILayout.Label($"Tiered Range: {RelayBaseRange.Value * DroneResearch.GetRangeMultiplier():F0}m");
            GUILayout.EndVertical();

            GUILayout.Space(5);

            // Network status by color
            GUILayout.Label("Networks:");
            droneScrollPos = GUILayout.BeginScrollView(droneScrollPos, GUILayout.Height(120));

            foreach (DroneRelayNetwork.RelayColor color in Enum.GetValues(typeof(DroneRelayNetwork.RelayColor)))
            {
                if (color == DroneRelayNetwork.RelayColor.None) continue;

                int relayCount = DroneRelayNetwork.GetNetworkRelayCount(color);
                if (relayCount == 0) continue;

                GUILayout.BeginHorizontal(GUI.skin.box);
                GUI.color = DroneRelayNetwork.GetRelayColorValue(color);
                GUILayout.Box("", GUILayout.Width(20), GUILayout.Height(20));
                GUI.color = Color.white;
                GUILayout.Label($"{color}: {relayCount} relays", GUILayout.Width(120));
                GUILayout.Label($"Coverage: {DroneRelayNetwork.GetNetworkCoverage(color):F0}m", GUILayout.Width(100));
                GUILayout.EndHorizontal();
            }

            if (DroneRelayNetwork.relays.Count == 0)
            {
                GUILayout.Label("No relay networks detected.");
                GUILayout.Label("Paint storage chests with relay colors to create networks.");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Instructions
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label("Relay Colors (paint indices 10-14):", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Label("10: Cyan  | 11: Magenta | 12: Orange");
            GUILayout.Label("13: Lime  | 14: Purple");
            GUILayout.EndVertical();
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

        /// <summary>
        /// Get stats modified by current research tier
        /// </summary>
        public static float GetTieredSpeed()
        {
            return EnhancedLogisticsPlugin.DroneSpeed.Value * DroneResearch.GetSpeedMultiplier();
        }

        public static int GetTieredCapacity()
        {
            return (int)(EnhancedLogisticsPlugin.DroneCapacity.Value * DroneResearch.GetCapacityMultiplier());
        }

        public static float GetTieredRange()
        {
            return EnhancedLogisticsPlugin.DroneRange.Value * DroneResearch.GetRangeMultiplier();
        }

        public static int GetMaxDrones()
        {
            return EnhancedLogisticsPlugin.MaxDronesPerNetwork.Value + DroneResearch.GetBonusDrones();
        }
    }

    /// <summary>
    /// Drone Relay System - Extends network coverage using special relay chests
    /// </summary>
    public static class DroneRelayNetwork
    {
        // Relay colors (different from standard game chest colors)
        public enum RelayColor
        {
            None = -1,
            Cyan = 0,      // Network A
            Magenta = 1,   // Network B
            Orange = 2,    // Network C
            Lime = 3,      // Network D
            Purple = 4     // Network E
        }

        public class DroneRelay
        {
            public uint chestId;
            public Vector3 position;
            public RelayColor networkColor;
            public float range;
            public bool isHub; // Primary hub spawns drones
            public List<uint> connectedRelays = new List<uint>();
            public GameObject visualIndicator;
        }

        public static Dictionary<uint, DroneRelay> relays = new Dictionary<uint, DroneRelay>();
        public static Dictionary<RelayColor, List<uint>> networksByColor = new Dictionary<RelayColor, List<uint>>();
        public static Dictionary<RelayColor, List<DroneManager.DroneInstance>> networkDrones = new Dictionary<RelayColor, List<DroneManager.DroneInstance>>();

        private static List<LineRenderer> connectionLines = new List<LineRenderer>();
        private static float updateTimer = 0f;

        public static void Initialize()
        {
            // Initialize network dictionaries
            foreach (RelayColor color in Enum.GetValues(typeof(RelayColor)))
            {
                if (color != RelayColor.None)
                {
                    networksByColor[color] = new List<uint>();
                    networkDrones[color] = new List<DroneManager.DroneInstance>();
                }
            }
        }

        public static void Update()
        {
            if (!EnhancedLogisticsPlugin.EnableDroneRelays.Value) return;

            updateTimer -= Time.deltaTime;
            if (updateTimer <= 0f)
            {
                updateTimer = 1f; // Update every second
                ScanForRelays();
                UpdateConnections();
                if (EnhancedLogisticsPlugin.ShowRelayNetwork.Value)
                {
                    UpdateVisuals();
                }
            }
        }

        // Manual relay registration (since automatic paint detection isn't available)
        // Users can register relays via console or GUI
        private static Dictionary<uint, RelayColor> manualRelayAssignments = new Dictionary<uint, RelayColor>();

        /// <summary>
        /// Scan for manually registered relays
        /// </summary>
        private static void ScanForRelays()
        {
            if (MachineManager.instance == null) return;

            try
            {
                // Process manually assigned relays
                foreach (var assignment in manualRelayAssignments.ToList())
                {
                    uint chestId = assignment.Key;
                    RelayColor color = assignment.Value;

                    try
                    {
                        var chest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)chestId, MachineTypeEnum.Chest);
                        if (chest.commonInfo.instanceId == 0)
                        {
                            // Chest no longer exists
                            manualRelayAssignments.Remove(chestId);
                            if (relays.ContainsKey(chestId))
                            {
                                UnregisterRelay(chestId);
                            }
                            continue;
                        }

                        if (!relays.ContainsKey(chestId))
                        {
                            RegisterRelay(chestId, chest.gridInfo.Center, color);
                        }
                    }
                    catch
                    {
                        manualRelayAssignments.Remove(chestId);
                        if (relays.ContainsKey(chestId))
                        {
                            UnregisterRelay(chestId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EnhancedLogisticsPlugin.Log.LogWarning($"Relay scan error: {ex.Message}");
            }
        }

        /// <summary>
        /// Manually assign a chest as a relay
        /// </summary>
        public static bool AssignChestAsRelay(uint chestId, RelayColor color)
        {
            try
            {
                var chest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)chestId, MachineTypeEnum.Chest);
                if (chest.commonInfo.instanceId == 0) return false;

                manualRelayAssignments[chestId] = color;
                RegisterRelay(chestId, chest.gridInfo.Center, color);
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Remove relay assignment from a chest
        /// </summary>
        public static void RemoveRelayAssignment(uint chestId)
        {
            manualRelayAssignments.Remove(chestId);
            UnregisterRelay(chestId);
        }

        public static void RegisterRelay(uint chestId, Vector3 position, RelayColor color, bool isHub = false)
        {
            var relay = new DroneRelay
            {
                chestId = chestId,
                position = position,
                networkColor = color,
                range = EnhancedLogisticsPlugin.RelayBaseRange.Value * DroneResearch.GetRangeMultiplier(),
                isHub = isHub
            };

            relays[chestId] = relay;

            if (!networksByColor.ContainsKey(color))
            {
                networksByColor[color] = new List<uint>();
            }
            networksByColor[color].Add(chestId);

            // Create visual indicator
            if (EnhancedLogisticsPlugin.ShowRelayNetwork.Value)
            {
                CreateRelayVisual(relay);
            }

            EnhancedLogisticsPlugin.Log.LogInfo($"Registered relay #{chestId} on network {color}");
        }

        public static void UnregisterRelay(uint chestId)
        {
            if (relays.TryGetValue(chestId, out var relay))
            {
                if (relay.visualIndicator != null)
                {
                    UnityEngine.Object.Destroy(relay.visualIndicator);
                }

                if (networksByColor.ContainsKey(relay.networkColor))
                {
                    networksByColor[relay.networkColor].Remove(chestId);
                }

                relays.Remove(chestId);
            }
        }

        private static void CreateRelayVisual(DroneRelay relay)
        {
            // Create a glowing sphere indicator above the relay
            relay.visualIndicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            relay.visualIndicator.name = $"RelayIndicator_{relay.chestId}";
            relay.visualIndicator.transform.position = relay.position + Vector3.up * 3f;
            relay.visualIndicator.transform.localScale = Vector3.one * 0.3f;

            var collider = relay.visualIndicator.GetComponent<Collider>();
            if (collider != null) UnityEngine.Object.Destroy(collider);

            var renderer = relay.visualIndicator.GetComponent<Renderer>();
            if (renderer != null)
            {
                Color relayColor = GetRelayColorValue(relay.networkColor);
                renderer.material.color = relayColor;
                renderer.material.EnableKeyword("_EMISSION");
                renderer.material.SetColor("_EmissionColor", relayColor * 3f);
            }
        }

        public static Color GetRelayColorValue(RelayColor color)
        {
            switch (color)
            {
                case RelayColor.Cyan: return Color.cyan;
                case RelayColor.Magenta: return Color.magenta;
                case RelayColor.Orange: return new Color(1f, 0.5f, 0f);
                case RelayColor.Lime: return new Color(0.5f, 1f, 0.2f);
                case RelayColor.Purple: return new Color(0.6f, 0.2f, 1f);
                default: return Color.white;
            }
        }

        private static void UpdateConnections()
        {
            // Build connection graph between relays in same network
            foreach (var relay in relays.Values)
            {
                relay.connectedRelays.Clear();

                foreach (var otherRelay in relays.Values)
                {
                    if (otherRelay.chestId == relay.chestId) continue;
                    if (otherRelay.networkColor != relay.networkColor) continue;

                    float distance = Vector3.Distance(relay.position, otherRelay.position);
                    if (distance <= relay.range + otherRelay.range)
                    {
                        relay.connectedRelays.Add(otherRelay.chestId);
                    }
                }
            }
        }

        private static void UpdateVisuals()
        {
            // Clear old lines
            foreach (var line in connectionLines)
            {
                if (line != null) UnityEngine.Object.Destroy(line.gameObject);
            }
            connectionLines.Clear();

            // Draw connections between relays
            HashSet<string> drawnConnections = new HashSet<string>();

            foreach (var relay in relays.Values)
            {
                foreach (var connectedId in relay.connectedRelays)
                {
                    string connectionKey = relay.chestId < connectedId
                        ? $"{relay.chestId}_{connectedId}"
                        : $"{connectedId}_{relay.chestId}";

                    if (drawnConnections.Contains(connectionKey)) continue;
                    drawnConnections.Add(connectionKey);

                    if (relays.TryGetValue(connectedId, out var connected))
                    {
                        DrawConnection(relay.position + Vector3.up * 3f,
                                      connected.position + Vector3.up * 3f,
                                      GetRelayColorValue(relay.networkColor));
                    }
                }
            }
        }

        private static void DrawConnection(Vector3 start, Vector3 end, Color color)
        {
            var lineObj = new GameObject("RelayConnection");
            var lineRenderer = lineObj.AddComponent<LineRenderer>();

            lineRenderer.positionCount = 2;
            lineRenderer.SetPosition(0, start);
            lineRenderer.SetPosition(1, end);
            lineRenderer.startWidth = 0.1f;
            lineRenderer.endWidth = 0.1f;
            lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
            lineRenderer.startColor = color;
            lineRenderer.endColor = color;

            connectionLines.Add(lineRenderer);
        }

        /// <summary>
        /// Check if two positions are connected via relay network
        /// </summary>
        public static bool ArePositionsConnected(Vector3 posA, Vector3 posB, out RelayColor network)
        {
            network = RelayColor.None;

            // Find relays near each position
            DroneRelay relayA = null, relayB = null;

            foreach (var relay in relays.Values)
            {
                float distA = Vector3.Distance(relay.position, posA);
                float distB = Vector3.Distance(relay.position, posB);

                if (distA <= relay.range && (relayA == null || distA < Vector3.Distance(relayA.position, posA)))
                {
                    relayA = relay;
                }
                if (distB <= relay.range && (relayB == null || distB < Vector3.Distance(relayB.position, posB)))
                {
                    relayB = relay;
                }
            }

            if (relayA == null || relayB == null) return false;
            if (relayA.networkColor != relayB.networkColor) return false;

            // Check if relays are connected (directly or via multi-hop)
            network = relayA.networkColor;
            return AreRelaysConnected(relayA.chestId, relayB.chestId, new HashSet<uint>());
        }

        private static bool AreRelaysConnected(uint fromId, uint toId, HashSet<uint> visited)
        {
            if (fromId == toId) return true;
            if (visited.Contains(fromId)) return false;
            visited.Add(fromId);

            if (!relays.TryGetValue(fromId, out var relay)) return false;

            foreach (var connectedId in relay.connectedRelays)
            {
                if (AreRelaysConnected(connectedId, toId, visited))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get total network coverage range (sum of all connected relay ranges)
        /// </summary>
        public static float GetNetworkCoverage(RelayColor network)
        {
            if (!networksByColor.ContainsKey(network)) return 0f;

            float totalCoverage = 0f;
            foreach (var relayId in networksByColor[network])
            {
                if (relays.TryGetValue(relayId, out var relay))
                {
                    totalCoverage += relay.range;
                }
            }
            return totalCoverage;
        }

        public static int GetNetworkRelayCount(RelayColor network)
        {
            if (!networksByColor.ContainsKey(network)) return 0;
            return networksByColor[network].Count;
        }

        public static void ClearAll()
        {
            foreach (var relay in relays.Values)
            {
                if (relay.visualIndicator != null)
                {
                    UnityEngine.Object.Destroy(relay.visualIndicator);
                }
            }
            relays.Clear();

            foreach (var line in connectionLines)
            {
                if (line != null) UnityEngine.Object.Destroy(line.gameObject);
            }
            connectionLines.Clear();

            foreach (var network in networksByColor.Values)
            {
                network.Clear();
            }
        }
    }

    /// <summary>
    /// Drone Research System - Upgrade drone capabilities through tech tiers
    /// </summary>
    public static class DroneResearch
    {
        public enum DroneTier
        {
            Basic = 1,      // Starting tier
            Improved = 2,   // Basic upgrades
            Advanced = 3,   // Mid-game
            Superior = 4,   // Late-game
            Ultimate = 5    // End-game
        }

        public class TierData
        {
            public string name;
            public string description;
            public float speedMultiplier;
            public float capacityMultiplier;
            public float rangeMultiplier;
            public int bonusDrones;
            public int researchCost; // In research cores
        }

        public static Dictionary<DroneTier, TierData> tierData = new Dictionary<DroneTier, TierData>
        {
            { DroneTier.Basic, new TierData {
                name = "Basic Drones",
                description = "Standard drone technology. Slow but reliable.",
                speedMultiplier = 1.0f,
                capacityMultiplier = 1.0f,
                rangeMultiplier = 1.0f,
                bonusDrones = 0,
                researchCost = 0
            }},
            { DroneTier.Improved, new TierData {
                name = "Improved Drones",
                description = "Enhanced motors and batteries. +25% speed/range.",
                speedMultiplier = 1.25f,
                capacityMultiplier = 1.25f,
                rangeMultiplier = 1.25f,
                bonusDrones = 2,
                researchCost = 50
            }},
            { DroneTier.Advanced, new TierData {
                name = "Advanced Drones",
                description = "Optimized flight systems. +50% all stats.",
                speedMultiplier = 1.5f,
                capacityMultiplier = 1.5f,
                rangeMultiplier = 1.5f,
                bonusDrones = 4,
                researchCost = 150
            }},
            { DroneTier.Superior, new TierData {
                name = "Superior Drones",
                description = "High-efficiency cores. +100% all stats.",
                speedMultiplier = 2.0f,
                capacityMultiplier = 2.0f,
                rangeMultiplier = 2.0f,
                bonusDrones = 6,
                researchCost = 400
            }},
            { DroneTier.Ultimate, new TierData {
                name = "Ultimate Drones",
                description = "Cutting-edge technology. +150% all stats.",
                speedMultiplier = 2.5f,
                capacityMultiplier = 2.5f,
                rangeMultiplier = 2.5f,
                bonusDrones = 10,
                researchCost = 1000
            }}
        };

        public static DroneTier CurrentTier => (DroneTier)EnhancedLogisticsPlugin.CurrentDroneTier.Value;

        public static TierData GetCurrentTierData()
        {
            return tierData[CurrentTier];
        }

        public static float GetSpeedMultiplier()
        {
            return tierData[CurrentTier].speedMultiplier;
        }

        public static float GetCapacityMultiplier()
        {
            return tierData[CurrentTier].capacityMultiplier;
        }

        public static float GetRangeMultiplier()
        {
            return tierData[CurrentTier].rangeMultiplier;
        }

        public static int GetBonusDrones()
        {
            return tierData[CurrentTier].bonusDrones;
        }

        public static bool CanUnlockTier(DroneTier tier)
        {
            if ((int)tier <= EnhancedLogisticsPlugin.CurrentDroneTier.Value) return false;
            if ((int)tier > EnhancedLogisticsPlugin.CurrentDroneTier.Value + 1) return false;

            // Check if player has enough research cores
            // This would integrate with the game's resource system
            return true; // For now, always allow (gated by UI)
        }

        public static bool TryUnlockTier(DroneTier tier)
        {
            if (!CanUnlockTier(tier)) return false;

            // Deduct research cost (integrate with game's inventory system)
            // For now, just unlock
            EnhancedLogisticsPlugin.CurrentDroneTier.Value = (int)tier;
            EnhancedLogisticsPlugin.Log.LogInfo($"Unlocked drone tier: {tier}");
            return true;
        }

        public static string GetTierProgressString()
        {
            var current = GetCurrentTierData();
            return $"Tier {(int)CurrentTier}/5: {current.name}";
        }
    }

    /// <summary>
    /// Harmony patches for enhanced logistics features
    /// </summary>
    [HarmonyPatch]
    internal static class LogisticsPatches
    {
        /// <summary>
        /// Increase inserter speed by modifying cyclesPerMinute after InitOverrideSettings
        /// </summary>
        [HarmonyPatch(typeof(InserterDefinition), "InitOverrideSettings")]
        [HarmonyPostfix]
        private static void BoostInserterSpeed(InserterDefinition __instance)
        {
            if (!EnhancedLogisticsPlugin.EnableBetterLogistics.Value) return;

            try
            {
                // Access the runtimeSettings and multiply cyclesPerMinute
                float multiplier = EnhancedLogisticsPlugin.InserterSpeedMultiplier.Value;
                if (multiplier > 1f)
                {
                    __instance.runtimeSettings.cyclesPerMinute *= multiplier;
                    EnhancedLogisticsPlugin.Log.LogDebug($"Boosted inserter {__instance.displayName} speed to {__instance.runtimeSettings.cyclesPerMinute} cycles/min");
                }
            }
            catch (System.Exception ex)
            {
                EnhancedLogisticsPlugin.Log.LogWarning($"Failed to boost inserter speed: {ex.Message}");
            }
        }

        /// <summary>
        /// Increase inserter arm length after instance is created
        /// </summary>
        [HarmonyPatch(typeof(InserterDefinition), "InitInstance")]
        [HarmonyPostfix]
        private static void ExtendInserterArm(InserterDefinition __instance, ref InserterInstance newInstance)
        {
            if (!EnhancedLogisticsPlugin.EnableBetterLogistics.Value) return;

            try
            {
                // Extend arm length based on range multiplier
                float rangeMultiplier = EnhancedLogisticsPlugin.InserterRangeMultiplier.Value;
                if (rangeMultiplier > 1f)
                {
                    int originalArmLength = newInstance.armLength;
                    newInstance.armLength = (int)(originalArmLength * rangeMultiplier);

                    // Cap at reasonable maximum
                    if (newInstance.armLength > 5) newInstance.armLength = 5;

                    if (newInstance.armLength != originalArmLength)
                    {
                        EnhancedLogisticsPlugin.Log.LogDebug($"Extended inserter arm from {originalArmLength} to {newInstance.armLength}");
                    }
                }
            }
            catch (System.Exception ex)
            {
                EnhancedLogisticsPlugin.Log.LogWarning($"Failed to extend inserter arm: {ex.Message}");
            }
        }
    }
}
