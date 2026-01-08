using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using TechtonicaFramework.TechTree;
using TechtonicaFramework.BuildMenu;
using UnityEngine;

namespace EnhancedLogistics
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    [BepInDependency("com.certifired.TurretDefense", BepInDependency.DependencyFlags.SoftDependency)]
    public class EnhancedLogisticsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.EnhancedLogistics";
        private const string PluginName = "EnhancedLogistics";
        private const string VersionString = "3.4.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static EnhancedLogisticsPlugin Instance;
        public static DroneAssetLoader AssetLoader;

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

        // Drone Type Settings
        public static ConfigEntry<float> DeliveryDroneSpeed;
        public static ConfigEntry<float> CombatDroneSpeed;
        public static ConfigEntry<float> CombatDroneDamage;
        public static ConfigEntry<float> CombatDroneFireRate;
        public static ConfigEntry<float> RepairDroneSpeed;
        public static ConfigEntry<float> RepairDroneRate;
        public static ConfigEntry<int> MaxDronesPerPort;

        // ============================================
        // STACK LIMIT FIXES (from Gratak-StackLimitsFixesAndCustomization)
        // ============================================
        public static ConfigEntry<bool> EnableStackLimitFixes;
        public static ConfigEntry<int> SpectralCubeStackLimit;
        public static ConfigEntry<int> CarbonPowderStackLimit;
        public static ConfigEntry<int> ScrapOreStackLimit;
        public static ConfigEntry<string> CustomStackOverrides;

        private static bool stackLimitsApplied = false;

        // Machine Names
        public const string DronePortName = "Drone Port";
        public const string CombatDronePortName = "Combat Drone Port";
        public const string RepairDronePortName = "Repair Drone Port";
        public const string DroneUnlockName = "Drone Technology";
        public const string AdvancedDroneUnlockName = "Advanced Drone Systems";

        // Captured game materials (shared with TurretDefense if available)
        public static Material CapturedMaterial;
        public static Shader CapturedShader;

        // State
        public static bool searchWindowOpen = false;
        public static string currentSearchQuery = "";
        public static List<SearchResult> searchResults = new List<SearchResult>();

        // GUI - positions recalculated in OnGUI for resolution independence
        private Rect searchWindowRect;
        private Rect droneWindowRect;
        private bool windowsInitialized = false;
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
            SearchToggleKey = Config.Bind("Search UI", "Toggle Key", KeyCode.RightBracket,
                "Key to toggle the search UI (] for 'locate' - L is game's Log menu)");
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
            DroneMenuKey = Config.Bind("Drone System", "Drone Menu Key", KeyCode.LeftBracket,
                "Key to open drone management menu ([ - J is game's Journal menu)");

            // Drone Type Settings
            DeliveryDroneSpeed = Config.Bind("Drone Types", "Delivery Drone Speed", 12f,
                new ConfigDescription("Movement speed for delivery drones", new AcceptableValueRange<float>(5f, 30f)));
            CombatDroneSpeed = Config.Bind("Drone Types", "Combat Drone Speed", 15f,
                new ConfigDescription("Movement speed for combat drones", new AcceptableValueRange<float>(5f, 40f)));
            CombatDroneDamage = Config.Bind("Drone Types", "Combat Drone Damage", 15f,
                new ConfigDescription("Damage per shot for combat drones", new AcceptableValueRange<float>(5f, 100f)));
            CombatDroneFireRate = Config.Bind("Drone Types", "Combat Drone Fire Rate", 2f,
                new ConfigDescription("Shots per second for combat drones", new AcceptableValueRange<float>(0.5f, 10f)));
            RepairDroneSpeed = Config.Bind("Drone Types", "Repair Drone Speed", 8f,
                new ConfigDescription("Movement speed for repair drones", new AcceptableValueRange<float>(3f, 20f)));
            RepairDroneRate = Config.Bind("Drone Types", "Repair Drone Rate", 10f,
                new ConfigDescription("Health restored per second by repair drones", new AcceptableValueRange<float>(1f, 50f)));
            MaxDronesPerPort = Config.Bind("Drone Types", "Max Drones Per Port", 3,
                new ConfigDescription("Maximum drones each port can deploy", new AcceptableValueRange<int>(1, 10)));

            // Stack Limit Fixes (from Gratak-StackLimitsFixesAndCustomization)
            EnableStackLimitFixes = Config.Bind("Stack Limits", "Enable Stack Limit Fixes", true,
                "Enable fixes for broken stack limits (Spectral Cube, Carbon Powder, Scrap Ore)");
            SpectralCubeStackLimit = Config.Bind("Stack Limits", "Spectral Cube X100 Limit", 500,
                new ConfigDescription("Stack limit for Spectral Cube X100 (default 1 is bugged, should be 500)", new AcceptableValueRange<int>(1, 1000)));
            CarbonPowderStackLimit = Config.Bind("Stack Limits", "Carbon Powder Limit", 1000,
                new ConfigDescription("Stack limit for Carbon Powder (default 500 causes issues with blast smelting)", new AcceptableValueRange<int>(500, 5000)));
            ScrapOreStackLimit = Config.Bind("Stack Limits", "Scrap Ore Limit", 500,
                new ConfigDescription("Stack limit for Scrap Ore (default 250 causes crushing to get stuck)", new AcceptableValueRange<int>(250, 2000)));
            CustomStackOverrides = Config.Bind("Stack Limits", "Custom Stack Overrides", "",
                "Custom stack limits in format: ItemName:Limit,ItemName2:Limit2 (e.g., 'Copper Ingot:2000,Iron Ingot:2000')");

            Harmony.PatchAll();

            // Load drone asset bundles
            LoadDroneAssetBundles();

            // Register content with EMU
            RegisterDroneContent();

            // Subscribe to EMU events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;
            EMU.Events.GameLoaded += OnGameLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log.LogInfo($"Press Ctrl+{SearchToggleKey.Value} for Search, {DroneMenuKey.Value} for Drone Menu");
        }

        private void LoadDroneAssetBundles()
        {
            string pluginPath = Path.GetDirectoryName(Info.Location);
            AssetLoader = new DroneAssetLoader(pluginPath);

            string bundlesPath = Path.Combine(pluginPath, "Bundles");
            if (!Directory.Exists(bundlesPath))
            {
                Log.LogInfo($"Bundles directory not found at {bundlesPath} - drones will use primitive visuals");
                return;
            }

            string[] bundleNames = { "drones_voodooplay", "drones_scifi", "drones_simple" };
            foreach (var bundleName in bundleNames)
            {
                if (AssetLoader.LoadBundle(bundleName))
                    Log.LogInfo($"Loaded drone asset bundle: {bundleName}");
            }
        }

        private void RegisterDroneContent()
        {
            if (!EnableDroneSystem.Value) return;

            // ========== UNLOCKS ==========
            // Use Modded category (index 7) - provided by TechtonicaFramework
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Gold,
                coreCountNeeded = 150,
                description = "Unlock autonomous drone technology. Drones can deliver items between storage, patrol for threats, and assist with repairs.",
                displayName = DroneUnlockName,
                requiredTier = TechTreeState.ResearchTier.Tier0,
                treePosition = 0
            });

            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Green,
                coreCountNeeded = 200,
                description = "Advanced drone AI and combat capabilities. Enables combat and repair drone ports.",
                displayName = AdvancedDroneUnlockName,
                requiredTier = TechTreeState.ResearchTier.Tier0,
                treePosition = 0
            });

            // ========== DRONE PORT (Delivery) ==========
            var deliveryPortDef = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();
            deliveryPortDef.usesFuel = false;
            deliveryPortDef.isCrankDriven = false;

            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(deliveryPortDef, new NewResourceDetails
            {
                name = DronePortName,
                description = "Deploys delivery drones that automatically transport items between connected storage. Requires power (30kW).",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 10,
                sortPriority = 300,
                unlockName = DroneUnlockName,
                parentName = "Crank Generator"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_droneport",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 30f,
                unlockName = DroneUnlockName,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Steel Frame", 15),
                    new RecipeResourceInfo("Copper Wire", 25),
                    new RecipeResourceInfo("Processor Unit", 5),
                    new RecipeResourceInfo("Iron Components", 20)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(DronePortName, 1)
                },
                sortPriority = 300
            });

            // ========== COMBAT DRONE PORT ==========
            var combatPortDef = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();
            combatPortDef.usesFuel = false;
            combatPortDef.isCrankDriven = false;

            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(combatPortDef, new NewResourceDetails
            {
                name = CombatDronePortName,
                description = "Deploys combat drones that patrol and engage hostile targets. Requires power (60kW). Best paired with TurretDefense mod.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 10,
                sortPriority = 301,
                unlockName = AdvancedDroneUnlockName,
                parentName = "Crank Generator"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_combatdroneport",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 45f,
                unlockName = AdvancedDroneUnlockName,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Steel Frame", 20),
                    new RecipeResourceInfo("Copper Wire", 40),
                    new RecipeResourceInfo("Processor Unit", 8),
                    new RecipeResourceInfo("Iron Components", 30)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(CombatDronePortName, 1)
                },
                sortPriority = 301
            });

            // ========== REPAIR DRONE PORT ==========
            var repairPortDef = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();
            repairPortDef.usesFuel = false;
            repairPortDef.isCrankDriven = false;

            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(repairPortDef, new NewResourceDetails
            {
                name = RepairDronePortName,
                description = "Deploys repair drones that automatically fix damaged machines. Requires power (45kW). Best paired with SurvivalElements mod.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                maxStackCount = 10,
                sortPriority = 302,
                unlockName = AdvancedDroneUnlockName,
                parentName = "Crank Generator"
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_repairdroneport",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 45f,
                unlockName = AdvancedDroneUnlockName,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Steel Frame", 15),
                    new RecipeResourceInfo("Copper Wire", 30),
                    new RecipeResourceInfo("Processor Unit", 6),
                    new RecipeResourceInfo("Iron Components", 25)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(RepairDronePortName, 1)
                },
                sortPriority = 302
            });

            Log.LogInfo("Drone ports registered!");
        }

        private void OnGameDefinesLoaded()
        {
            InitializeMaterials();
            ApplyStackLimitFixes();
        }

        /// <summary>
        /// Apply stack limit fixes for broken items (from Gratak-StackLimitsFixesAndCustomization)
        /// </summary>
        private void ApplyStackLimitFixes()
        {
            if (!EnableStackLimitFixes.Value || stackLimitsApplied) return;

            try
            {
                int fixedCount = 0;

                // Fix Spectral Cube X100 (displayName contains "Spectral Cube" and is colorless/x100 variant)
                fixedCount += FixStackLimit("Spectral Cube Colorless X100", SpectralCubeStackLimit.Value);

                // Fix Carbon Powder
                fixedCount += FixStackLimit("Carbon Powder", CarbonPowderStackLimit.Value);

                // Fix Scrap Ore
                fixedCount += FixStackLimit("Scrap Ore", ScrapOreStackLimit.Value);

                // Apply custom overrides
                if (!string.IsNullOrEmpty(CustomStackOverrides.Value))
                {
                    var overrides = CustomStackOverrides.Value.Split(',');
                    foreach (var entry in overrides)
                    {
                        var parts = entry.Trim().Split(':');
                        if (parts.Length == 2 && int.TryParse(parts[1].Trim(), out int limit))
                        {
                            string itemName = parts[0].Trim();
                            fixedCount += FixStackLimit(itemName, limit);
                        }
                    }
                }

                stackLimitsApplied = true;
                Log.LogInfo($"Stack limit fixes applied: {fixedCount} items modified");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to apply stack limit fixes: {ex.Message}");
            }
        }

        private int FixStackLimit(string itemName, int newLimit)
        {
            try
            {
                // Try to find the resource by display name or internal name
                ResourceInfo resource = null;

                // First try direct name lookup
                resource = EMU.Resources.GetResourceInfoByName(itemName);

                // If not found, search through all resources
                if (resource == null && GameDefines.instance?.resources != null)
                {
                    foreach (var res in GameDefines.instance.resources)
                    {
                        if (res != null &&
                            (res.displayName.Equals(itemName, StringComparison.OrdinalIgnoreCase) ||
                             res.name.Equals(itemName, StringComparison.OrdinalIgnoreCase)))
                        {
                            resource = res;
                            break;
                        }
                    }
                }

                if (resource != null)
                {
                    int oldLimit = resource.maxStackCount;
                    if (oldLimit != newLimit)
                    {
                        resource.maxStackCount = newLimit;
                        Log.LogInfo($"Stack limit fixed: {resource.displayName} ({oldLimit} -> {newLimit})");
                        return 1;
                    }
                }
                else
                {
                    Log.LogWarning($"Could not find item for stack fix: {itemName}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to fix stack limit for {itemName}: {ex.Message}");
            }
            return 0;
        }

        private void OnTechTreeStateLoaded()
        {
            // PT level tier mapping (from game):
            // - LIMA: Tier1-Tier4
            // - VICTOR: Tier5-Tier11
            // - XRAY: Tier12-Tier16
            // - SIERRA: Tier17-Tier24

            // Drone Technology: VICTOR (Tier8), position 70
            // Advanced Drone: XRAY (Tier13), position 70
            ConfigureUnlock(DroneUnlockName, TechTreeState.ResearchTier.Tier8, 70);
            ConfigureUnlock(AdvancedDroneUnlockName, TechTreeState.ResearchTier.Tier13, 70);

            Log.LogInfo("Drone tech tree positions configured");
        }

        private void ConfigureUnlock(string unlockName, TechTreeState.ResearchTier tier, int position)
        {
            try
            {
                var unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                if (unlock == null)
                {
                    Log.LogWarning($"Could not find unlock: {unlockName}");
                    return;
                }
                unlock.requiredTier = tier;
                unlock.treePosition = position;

                // Always try to set sprite for proper icon display
                Sprite sprite = null;

                // Try Inserter first
                var inserter = EMU.Resources.GetResourceInfoByName("Inserter");
                if (inserter?.sprite != null)
                    sprite = inserter.sprite;

                // Fallback to Filter Inserter
                if (sprite == null)
                {
                    var filterInserter = EMU.Resources.GetResourceInfoByName("Filter Inserter");
                    if (filterInserter?.sprite != null)
                        sprite = filterInserter.sprite;
                }

                if (sprite != null)
                    unlock.sprite = sprite;

                Log.LogInfo($"Configured {unlockName}: tier={tier}, pos={position}, sprite={(unlock.sprite != null ? "SET" : "NULL")}");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure unlock {unlockName}: {ex.Message}");
            }
        }

        private void OnGameLoaded()
        {
            // Initialize drone manager
            DroneManager.Initialize();
            Log.LogInfo("Drone system initialized");
        }

        private void InitializeMaterials()
        {
            // Capture a game shader for proper URP rendering
            try
            {
                var renderers = UnityEngine.Object.FindObjectsOfType<MeshRenderer>();
                foreach (var renderer in renderers)
                {
                    if (renderer?.sharedMaterial?.shader == null) continue;
                    var shader = renderer.sharedMaterial.shader;
                    if (shader.name.Contains("Error") || shader.name.Contains("Hidden")) continue;

                    if (shader.name.Contains("Lit") || shader.name.Contains("Standard") || shader.name.Contains("URP"))
                    {
                        CapturedShader = shader;
                        CapturedMaterial = new Material(renderer.sharedMaterial);
                        CapturedMaterial.name = "EnhancedLogistics_Drone";
                        Log.LogInfo($"Captured shader: {shader.name}");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Material init error: {ex.Message}");
            }
        }

        public static Material GetDroneMaterial(Color color)
        {
            Material mat;
            if (CapturedMaterial != null)
            {
                mat = new Material(CapturedMaterial);
            }
            else if (CapturedShader != null)
            {
                mat = new Material(CapturedShader);
            }
            else
            {
                mat = new Material(Shader.Find("Standard") ?? Shader.Find("Diffuse"));
            }

            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
            mat.color = color;

            return mat;
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
            // Initialize window positions based on current screen resolution
            if (!windowsInitialized || searchWindowRect.width == 0)
            {
                // Search window: centered, 400x500 (or scaled for smaller screens)
                float searchWidth = Mathf.Min(400, Screen.width * 0.4f);
                float searchHeight = Mathf.Min(500, Screen.height * 0.6f);
                searchWindowRect = new Rect(
                    (Screen.width - searchWidth) / 2f,
                    (Screen.height - searchHeight) / 2f,
                    searchWidth, searchHeight);

                // Drone window: top-left area, leaving room for game UI
                float droneWidth = Mathf.Min(350, Screen.width * 0.25f);
                float droneHeight = Mathf.Min(400, Screen.height * 0.5f);
                droneWindowRect = new Rect(
                    Screen.width * 0.01f,  // 1% from left
                    Screen.height * 0.08f, // 8% from top
                    droneWidth, droneHeight);

                windowsInitialized = true;
            }

            if (searchWindowOpen && EnableSearchUI.Value)
            {
                searchWindowRect = GUILayout.Window(88888, searchWindowRect, DrawSearchWindow, "Search (Ctrl+F to close)");
                // Keep window on screen
                searchWindowRect.x = Mathf.Clamp(searchWindowRect.x, 0, Screen.width - searchWindowRect.width);
                searchWindowRect.y = Mathf.Clamp(searchWindowRect.y, 0, Screen.height - searchWindowRect.height);
            }

            if (droneWindowOpen && EnableDroneSystem.Value)
            {
                droneWindowRect = GUILayout.Window(88889, droneWindowRect, DrawDroneWindow, "Drone Management");
                // Keep window on screen
                droneWindowRect.x = Mathf.Clamp(droneWindowRect.x, 0, Screen.width - droneWindowRect.width);
                droneWindowRect.y = Mathf.Clamp(droneWindowRect.y, 0, Screen.height - droneWindowRect.height);
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

            GUILayout.Label("Drone System v3.0", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold, fontSize = 14 });
            GUILayout.Space(10);

            // Drone status overview
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label($"Active Drones: {DroneManager.AllDrones.Count}");
            GUILayout.Label($"  - Delivery: {DroneManager.DeliveryDrones.Count}");
            GUILayout.Label($"  - Combat: {DroneManager.CombatDrones.Count}");
            GUILayout.Label($"  - Repair: {DroneManager.RepairDrones.Count}");
            GUILayout.Label($"Drone Ports: {DroneManager.DronePorts.Count}");
            GUILayout.Label($"Pending Deliveries: {DroneManager.PendingDeliveries.Count}");
            GUILayout.EndVertical();

            GUILayout.Space(10);

            // Drone list
            GUILayout.Label("Active Drones:");
            droneScrollPos = GUILayout.BeginScrollView(droneScrollPos, GUILayout.Height(150));

            foreach (var drone in DroneManager.AllDrones)
            {
                if (drone == null) continue;
                GUILayout.BeginHorizontal(GUI.skin.box);
                Color typeColor = drone.Type == DroneType.Combat ? Color.red : drone.Type == DroneType.Repair ? Color.green : Color.cyan;
                GUIStyle colorStyle = new GUIStyle(GUI.skin.label) { normal = { textColor = typeColor } };
                GUILayout.Label($"#{drone.Id} [{drone.Type}]", colorStyle, GUILayout.Width(120));
                GUILayout.Label(drone.State.ToString(), GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }

            if (DroneManager.AllDrones.Count == 0)
            {
                GUILayout.Label("No active drones. Build Drone Ports to deploy drones!");
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Test Delivery"))
            {
                if (Player.instance != null)
                {
                    DroneManager.SpawnDrone(DroneType.Delivery, Player.instance.transform.position + Vector3.up * 3f, null);
                }
            }
            if (GUILayout.Button("Test Combat"))
            {
                if (Player.instance != null)
                {
                    DroneManager.SpawnDrone(DroneType.Combat, Player.instance.transform.position + Vector3.up * 3f, null);
                }
            }
            if (GUILayout.Button("Test Repair"))
            {
                if (Player.instance != null)
                {
                    DroneManager.SpawnDrone(DroneType.Repair, Player.instance.transform.position + Vector3.up * 3f, null);
                }
            }
            GUILayout.EndHorizontal();

            if (GUILayout.Button("Clear All Drones"))
            {
                DroneManager.ClearAllDrones();
            }

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

    // =========================================================================
    // DRONE SYSTEM - Comprehensive drone management with multiple types
    // =========================================================================

    public enum DroneType
    {
        Delivery,   // Transports items between storage
        Combat,     // Engages hostile targets
        Repair      // Repairs damaged machines
    }

    public enum DroneState
    {
        Idle,
        MovingToTarget,
        Working,        // Loading/Unloading/Attacking/Repairing
        Returning
    }

    /// <summary>
    /// Central drone management system
    /// </summary>
    public static class DroneManager
    {
        public static List<DroneController> AllDrones = new List<DroneController>();
        public static List<DeliveryDrone> DeliveryDrones = new List<DeliveryDrone>();
        public static List<CombatDrone> CombatDrones = new List<CombatDrone>();
        public static List<RepairDrone> RepairDrones = new List<RepairDrone>();
        public static List<DronePort> DronePorts = new List<DronePort>();

        public static Queue<DeliveryTask> PendingDeliveries = new Queue<DeliveryTask>();
        private static int nextDroneId = 1;
        private static bool initialized = false;

        public static void Initialize()
        {
            if (initialized) return;
            initialized = true;
            EnhancedLogisticsPlugin.Log.LogInfo("DroneManager initialized");
        }

        public static void Update()
        {
            if (!initialized) return;

            // Update all drone ports
            foreach (var port in DronePorts.ToList())
            {
                if (port == null)
                {
                    DronePorts.Remove(port);
                    continue;
                }
                port.UpdatePort();
            }

            // Update all drones
            foreach (var drone in AllDrones.ToList())
            {
                if (drone == null || drone.gameObject == null)
                {
                    AllDrones.Remove(drone);
                    continue;
                }
                drone.UpdateDrone();
            }

            // Cleanup type-specific lists
            DeliveryDrones.RemoveAll(d => d == null || d.gameObject == null);
            CombatDrones.RemoveAll(d => d == null || d.gameObject == null);
            RepairDrones.RemoveAll(d => d == null || d.gameObject == null);

            // Assign pending deliveries to idle delivery drones
            while (PendingDeliveries.Count > 0)
            {
                var idleDrone = DeliveryDrones.FirstOrDefault(d => d.State == DroneState.Idle);
                if (idleDrone == null) break;

                var task = PendingDeliveries.Dequeue();
                idleDrone.AssignDeliveryTask(task);
            }
        }

        public static int GetNextId() => nextDroneId++;

        public static DroneController SpawnDrone(DroneType type, Vector3 position, DronePort ownerPort)
        {
            DroneController drone = null;

            switch (type)
            {
                case DroneType.Delivery:
                    var delivery = new DeliveryDrone(position, ownerPort);
                    DeliveryDrones.Add(delivery);
                    drone = delivery;
                    break;

                case DroneType.Combat:
                    var combat = new CombatDrone(position, ownerPort);
                    CombatDrones.Add(combat);
                    drone = combat;
                    break;

                case DroneType.Repair:
                    var repair = new RepairDrone(position, ownerPort);
                    RepairDrones.Add(repair);
                    drone = repair;
                    break;
            }

            if (drone != null)
            {
                AllDrones.Add(drone);
                EnhancedLogisticsPlugin.Log.LogInfo($"Spawned {type} drone #{drone.Id} at {position}");
            }

            return drone;
        }

        public static void RegisterPort(DronePort port)
        {
            if (!DronePorts.Contains(port))
            {
                DronePorts.Add(port);
                EnhancedLogisticsPlugin.Log.LogInfo($"Registered {port.PortType} port");
            }
        }

        public static void UnregisterPort(DronePort port)
        {
            DronePorts.Remove(port);
            // Recall all drones from this port
            foreach (var drone in AllDrones.Where(d => d.HomePort == port).ToList())
            {
                drone.Destroy();
            }
        }

        public static void QueueDelivery(uint sourceChestId, uint targetChestId, int resourceId, int count)
        {
            PendingDeliveries.Enqueue(new DeliveryTask
            {
                sourceChestId = sourceChestId,
                targetChestId = targetChestId,
                resourceId = resourceId,
                count = count
            });
        }

        public static void ClearAllDrones()
        {
            foreach (var drone in AllDrones.ToList())
            {
                drone?.Destroy();
            }
            AllDrones.Clear();
            DeliveryDrones.Clear();
            CombatDrones.Clear();
            RepairDrones.Clear();
            PendingDeliveries.Clear();
        }

        /// <summary>
        /// Get active aliens from TurretDefense mod (if loaded)
        /// </summary>
        public static List<Transform> GetActiveAliens()
        {
            var aliens = new List<Transform>();
            try
            {
                // Try to access TurretDefense.TurretDefensePlugin.ActiveAliens via reflection
                var turretDefenseType = System.Type.GetType("TurretDefense.TurretDefensePlugin, TurretDefense");
                if (turretDefenseType != null)
                {
                    var activeAliensField = turretDefenseType.GetField("ActiveAliens", BindingFlags.Public | BindingFlags.Static);
                    if (activeAliensField != null)
                    {
                        var alienList = activeAliensField.GetValue(null) as System.Collections.IList;
                        if (alienList != null)
                        {
                            foreach (var alien in alienList)
                            {
                                var controllerType = alien.GetType();
                                var isAliveProperty = controllerType.GetProperty("IsAlive");

                                // Get the MonoBehaviour's transform
                                if (alien is MonoBehaviour mb && mb != null)
                                {
                                    bool isAlive = true;
                                    if (isAliveProperty != null)
                                    {
                                        isAlive = (bool)isAliveProperty.GetValue(alien);
                                    }

                                    if (isAlive && mb.transform != null)
                                    {
                                        aliens.Add(mb.transform);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // TurretDefense not loaded or error accessing
                EnhancedLogisticsPlugin.Log.LogDebug($"Could not access TurretDefense aliens: {ex.Message}");
            }
            return aliens;
        }
    }

    public class DeliveryTask
    {
        public uint sourceChestId;
        public uint targetChestId;
        public int resourceId;
        public int count;
    }

    // =========================================================================
    // DRONE PORT - Machine that spawns and manages drones
    // =========================================================================

    public class DronePort
    {
        public DroneType PortType;
        public Vector3 Position;
        public uint MachineInstanceId;
        public List<DroneController> OwnedDrones = new List<DroneController>();
        public int MaxDrones => EnhancedLogisticsPlugin.MaxDronesPerPort.Value;
        public float SpawnCooldown = 0f;
        public bool HasPower = true;

        public DronePort(DroneType type, Vector3 position, uint instanceId)
        {
            PortType = type;
            Position = position;
            MachineInstanceId = instanceId;
            DroneManager.RegisterPort(this);
        }

        public void UpdatePort()
        {
            // Spawn drones if we have capacity
            SpawnCooldown -= Time.deltaTime;
            if (SpawnCooldown <= 0 && HasPower && OwnedDrones.Count < MaxDrones)
            {
                SpawnDrone();
                SpawnCooldown = 5f; // 5 second cooldown between spawns
            }

            // Cleanup dead drones
            OwnedDrones.RemoveAll(d => d == null || d.gameObject == null);
        }

        public void SpawnDrone()
        {
            Vector3 spawnPos = Position + Vector3.up * 3f + UnityEngine.Random.insideUnitSphere * 0.5f;
            var drone = DroneManager.SpawnDrone(PortType, spawnPos, this);
            if (drone != null)
            {
                OwnedDrones.Add(drone);
            }
        }

        public void Destroy()
        {
            DroneManager.UnregisterPort(this);
        }
    }

    // =========================================================================
    // DRONE CONTROLLER - Base class for all drones
    // =========================================================================

    public abstract class DroneController
    {
        public int Id;
        public DroneType Type;
        public DroneState State = DroneState.Idle;
        public GameObject gameObject;
        public Vector3 Position;
        public Vector3 TargetPosition;
        public DronePort HomePort;
        public float StateTimer = 0f;

        // Visual components
        protected GameObject bodyObj;
        protected GameObject[] propellers;
        protected float propellerSpeed = 1000f;

        // Movement
        protected float hoverHeight = 3f;
        protected float bobAmplitude = 0.15f;
        protected float bobSpeed = 2f;

        public abstract float Speed { get; }
        public abstract Color DroneColor { get; }
        public abstract void UpdateBehavior();

        protected DroneController(DroneType type, Vector3 position, DronePort owner)
        {
            Id = DroneManager.GetNextId();
            Type = type;
            Position = position;
            HomePort = owner;
            CreateVisual();
        }

        protected virtual void CreateVisual()
        {
            // Try to use asset bundle prefab first
            GameObject prefab = EnhancedLogisticsPlugin.AssetLoader?.GetDronePrefab(Type);
            if (prefab != null)
            {
                gameObject = UnityEngine.Object.Instantiate(prefab, Position, Quaternion.identity);
                gameObject.name = $"Drone_{Type}_{Id}";
                gameObject.transform.localScale = Vector3.one * 0.8f;

                // Find or create propeller references (may not have spinning propellers)
                propellers = new GameObject[0];

                // Add a simple light indicator
                var lightObj = new GameObject("DroneLight");
                lightObj.transform.SetParent(gameObject.transform);
                lightObj.transform.localPosition = Vector3.zero;
                var light = lightObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 3f;
                light.intensity = 1f;
                light.color = DroneColor;

                EnhancedLogisticsPlugin.Log.LogDebug($"Created {Type} drone from prefab");
                return;
            }

            // Fallback to primitive visual
            gameObject = new GameObject($"Drone_{Type}_{Id}");
            gameObject.transform.position = Position;

            // Main body - flattened sphere (drone body shape)
            bodyObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bodyObj.name = "Body";
            bodyObj.transform.SetParent(gameObject.transform);
            bodyObj.transform.localPosition = Vector3.zero;
            bodyObj.transform.localScale = new Vector3(0.8f, 0.3f, 0.8f);

            var bodyCollider = bodyObj.GetComponent<Collider>();
            if (bodyCollider != null) UnityEngine.Object.Destroy(bodyCollider);

            var bodyRenderer = bodyObj.GetComponent<Renderer>();
            if (bodyRenderer != null)
            {
                bodyRenderer.material = EnhancedLogisticsPlugin.GetDroneMaterial(DroneColor);
            }

            // Create 4 propeller arms with rotors
            propellers = new GameObject[4];
            for (int i = 0; i < 4; i++)
            {
                float angle = (i / 4f) * Mathf.PI * 2f + Mathf.PI / 4f;
                Vector3 armOffset = new Vector3(Mathf.Cos(angle) * 0.5f, 0.1f, Mathf.Sin(angle) * 0.5f);

                // Arm
                var arm = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                arm.name = $"Arm_{i}";
                arm.transform.SetParent(gameObject.transform);
                arm.transform.localPosition = armOffset * 0.5f;
                arm.transform.localRotation = Quaternion.Euler(0, 0, 90) * Quaternion.Euler(0, Mathf.Rad2Deg * angle, 0);
                arm.transform.localScale = new Vector3(0.05f, 0.3f, 0.05f);

                var armCollider = arm.GetComponent<Collider>();
                if (armCollider != null) UnityEngine.Object.Destroy(armCollider);

                var armRenderer = arm.GetComponent<Renderer>();
                if (armRenderer != null)
                {
                    armRenderer.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(0.2f, 0.2f, 0.25f));
                }

                // Propeller (cylinder as rotor disc)
                propellers[i] = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                propellers[i].name = $"Propeller_{i}";
                propellers[i].transform.SetParent(gameObject.transform);
                propellers[i].transform.localPosition = armOffset + Vector3.up * 0.15f;
                propellers[i].transform.localScale = new Vector3(0.3f, 0.02f, 0.3f);

                var propCollider = propellers[i].GetComponent<Collider>();
                if (propCollider != null) UnityEngine.Object.Destroy(propCollider);

                var propRenderer = propellers[i].GetComponent<Renderer>();
                if (propRenderer != null)
                {
                    propRenderer.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(0.15f, 0.15f, 0.2f, 0.7f));
                }
            }

            // Center light/indicator
            var indicator = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            indicator.name = "Indicator";
            indicator.transform.SetParent(gameObject.transform);
            indicator.transform.localPosition = Vector3.down * 0.1f;
            indicator.transform.localScale = Vector3.one * 0.15f;

            var indCollider = indicator.GetComponent<Collider>();
            if (indCollider != null) UnityEngine.Object.Destroy(indCollider);

            var indRenderer = indicator.GetComponent<Renderer>();
            if (indRenderer != null)
            {
                Color glowColor = DroneColor * 2f;
                var mat = EnhancedLogisticsPlugin.GetDroneMaterial(glowColor);
                if (mat.HasProperty("_EmissionColor"))
                {
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", glowColor);
                }
                indRenderer.material = mat;
            }
        }

        public void UpdateDrone()
        {
            if (gameObject == null) return;

            float dt = Time.deltaTime;

            // Spin propellers
            foreach (var prop in propellers)
            {
                if (prop != null)
                {
                    prop.transform.Rotate(Vector3.up, propellerSpeed * dt);
                }
            }

            // State machine
            switch (State)
            {
                case DroneState.Idle:
                    UpdateIdle(dt);
                    break;

                case DroneState.MovingToTarget:
                    UpdateMoving(dt);
                    break;

                case DroneState.Working:
                    UpdateWorking(dt);
                    break;

                case DroneState.Returning:
                    UpdateReturning(dt);
                    break;
            }

            // Type-specific behavior
            UpdateBehavior();
        }

        protected virtual void UpdateIdle(float dt)
        {
            // Hover in place with bob
            float bob = Mathf.Sin(Time.time * bobSpeed + Id) * bobAmplitude;
            gameObject.transform.position = Position + Vector3.up * bob;

            // Slight random rotation
            gameObject.transform.Rotate(Vector3.up, Mathf.Sin(Time.time * 0.5f + Id) * 5f * dt);
        }

        protected virtual void UpdateMoving(float dt)
        {
            Vector3 direction = (TargetPosition - Position).normalized;
            float distance = Vector3.Distance(Position, TargetPosition);

            if (distance < 1f)
            {
                Position = TargetPosition;
                OnArrivedAtTarget();
            }
            else
            {
                Position += direction * Speed * dt;
                // Face movement direction
                if (direction.sqrMagnitude > 0.001f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(direction, Vector3.up);
                    gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, targetRot, dt * 5f);
                    // Tilt forward when moving
                    gameObject.transform.Rotate(Vector3.right, 10f);
                }
            }

            // Add bob even while moving
            float bob = Mathf.Sin(Time.time * bobSpeed + Id) * bobAmplitude * 0.5f;
            gameObject.transform.position = Position + Vector3.up * bob;
        }

        protected virtual void UpdateWorking(float dt)
        {
            StateTimer -= dt;
            if (StateTimer <= 0)
            {
                OnWorkComplete();
            }
        }

        protected virtual void UpdateReturning(float dt)
        {
            if (HomePort == null)
            {
                State = DroneState.Idle;
                return;
            }

            Vector3 homePos = HomePort.Position + Vector3.up * hoverHeight;
            Vector3 direction = (homePos - Position).normalized;
            float distance = Vector3.Distance(Position, homePos);

            if (distance < 1.5f)
            {
                Position = homePos;
                State = DroneState.Idle;
            }
            else
            {
                Position += direction * Speed * dt;
            }

            float bob = Mathf.Sin(Time.time * bobSpeed + Id) * bobAmplitude * 0.5f;
            gameObject.transform.position = Position + Vector3.up * bob;
        }

        protected abstract void OnArrivedAtTarget();
        protected abstract void OnWorkComplete();

        public void MoveTo(Vector3 target)
        {
            TargetPosition = target;
            State = DroneState.MovingToTarget;
        }

        public void ReturnHome()
        {
            State = DroneState.Returning;
        }

        public void Destroy()
        {
            if (gameObject != null)
            {
                UnityEngine.Object.Destroy(gameObject);
            }
            DroneManager.AllDrones.Remove(this);
        }
    }

    // =========================================================================
    // DELIVERY DRONE - Transports items between storage
    // =========================================================================

    public class DeliveryDrone : DroneController
    {
        public override float Speed => EnhancedLogisticsPlugin.DeliveryDroneSpeed.Value;
        public override Color DroneColor => new Color(0.3f, 0.6f, 0.9f); // Blue

        public uint SourceChestId;
        public uint TargetChestId;
        public int CargoResourceId;
        public int CargoCount;
        public bool HasCargo = false;

        public DeliveryDrone(Vector3 position, DronePort owner) : base(DroneType.Delivery, position, owner)
        {
        }

        public void AssignDeliveryTask(DeliveryTask task)
        {
            SourceChestId = task.sourceChestId;
            TargetChestId = task.targetChestId;
            CargoResourceId = task.resourceId;
            CargoCount = task.count;
            HasCargo = false;

            // Move to source chest
            try
            {
                var sourceChest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)task.sourceChestId, MachineTypeEnum.Chest);
                MoveTo(sourceChest.gridInfo.Center + Vector3.up * hoverHeight);
            }
            catch
            {
                State = DroneState.Idle;
            }
        }

        public override void UpdateBehavior()
        {
            // Look for work if idle
            if (State == DroneState.Idle && DroneManager.PendingDeliveries.Count > 0)
            {
                var task = DroneManager.PendingDeliveries.Dequeue();
                AssignDeliveryTask(task);
            }
        }

        protected override void OnArrivedAtTarget()
        {
            if (!HasCargo)
            {
                // At source - pickup items
                State = DroneState.Working;
                StateTimer = 1.5f;
            }
            else
            {
                // At destination - drop off items
                State = DroneState.Working;
                StateTimer = 1.5f;
            }
        }

        protected override void OnWorkComplete()
        {
            if (!HasCargo)
            {
                // Finished loading
                try
                {
                    var sourceChest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)SourceChestId, MachineTypeEnum.Chest);
                    var inv = sourceChest.GetInventory();

                    int maxPickup = Mathf.Min(CargoCount, EnhancedLogisticsPlugin.DroneCapacity.Value);
                    int picked = inv.TryRemoveResources(CargoResourceId, maxPickup);

                    if (picked > 0)
                    {
                        CargoCount = picked;
                        HasCargo = true;

                        // Move to target chest
                        var targetChest = MachineManager.instance.Get<ChestInstance, ChestDefinition>((int)TargetChestId, MachineTypeEnum.Chest);
                        MoveTo(targetChest.gridInfo.Center + Vector3.up * hoverHeight);
                    }
                    else
                    {
                        ReturnHome();
                    }
                }
                catch
                {
                    ReturnHome();
                }
            }
            else
            {
                // Finished unloading - stub for now
                // TODO: Implement actual inventory transfer once we have proper API
                EnhancedLogisticsPlugin.Log.LogDebug($"Delivery drone #{Id} delivered {CargoCount} items");

                HasCargo = false;
                CargoCount = 0;
                ReturnHome();
            }
        }
    }

    // =========================================================================
    // COMBAT DRONE - Engages hostile targets
    // =========================================================================

    public class CombatDrone : DroneController
    {
        public override float Speed => EnhancedLogisticsPlugin.CombatDroneSpeed.Value;
        public override Color DroneColor => new Color(0.9f, 0.3f, 0.2f); // Red

        public Transform CurrentTarget;
        public float AttackRange = 25f;
        public float FireCooldown = 0f;
        public float PatrolRadius = 30f;
        public Vector3 PatrolCenter;

        // Visual effects
        private LineRenderer laserLine;
        private float laserTimer = 0f;

        public CombatDrone(Vector3 position, DronePort owner) : base(DroneType.Combat, position, owner)
        {
            PatrolCenter = position;
            CreateWeaponVisual();
        }

        private void CreateWeaponVisual()
        {
            // Add a small turret/gun underneath
            var gun = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            gun.name = "Gun";
            gun.transform.SetParent(gameObject.transform);
            gun.transform.localPosition = Vector3.down * 0.25f;
            gun.transform.localRotation = Quaternion.Euler(90, 0, 0);
            gun.transform.localScale = new Vector3(0.08f, 0.2f, 0.08f);

            var gunCollider = gun.GetComponent<Collider>();
            if (gunCollider != null) UnityEngine.Object.Destroy(gunCollider);

            var gunRenderer = gun.GetComponent<Renderer>();
            if (gunRenderer != null)
            {
                gunRenderer.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(0.15f, 0.15f, 0.2f));
            }

            // Laser line renderer for attack visual
            laserLine = gameObject.AddComponent<LineRenderer>();
            laserLine.startWidth = 0.05f;
            laserLine.endWidth = 0.02f;
            laserLine.positionCount = 2;
            laserLine.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(1f, 0.3f, 0.1f));
            laserLine.enabled = false;
        }

        public override void UpdateBehavior()
        {
            FireCooldown -= Time.deltaTime;
            laserTimer -= Time.deltaTime;

            if (laserTimer <= 0 && laserLine != null)
            {
                laserLine.enabled = false;
            }

            if (State == DroneState.Idle)
            {
                // Look for targets
                FindTarget();

                if (CurrentTarget == null)
                {
                    // Patrol around home base
                    PatrolAroundHome();
                }
            }
            else if (State == DroneState.Working)
            {
                // Attacking
                AttackTarget();
            }
        }

        private void FindTarget()
        {
            var aliens = DroneManager.GetActiveAliens();
            if (aliens.Count == 0)
            {
                CurrentTarget = null;
                return;
            }

            // Find closest alien in range
            float closestDist = float.MaxValue;
            Transform closest = null;

            foreach (var alien in aliens)
            {
                if (alien == null) continue;
                float dist = Vector3.Distance(Position, alien.position);
                if (dist < AttackRange && dist < closestDist)
                {
                    closestDist = dist;
                    closest = alien;
                }
            }

            if (closest != null)
            {
                CurrentTarget = closest;
                MoveTo(closest.position + Vector3.up * 2f);
            }
        }

        private void PatrolAroundHome()
        {
            if (HomePort == null) return;

            // Random patrol within radius
            if (UnityEngine.Random.value < 0.01f) // 1% chance per frame to pick new patrol point
            {
                Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * PatrolRadius;
                Vector3 patrolPoint = HomePort.Position + new Vector3(randomCircle.x, hoverHeight, randomCircle.y);
                MoveTo(patrolPoint);
            }
        }

        private void AttackTarget()
        {
            if (CurrentTarget == null)
            {
                State = DroneState.Idle;
                return;
            }

            float dist = Vector3.Distance(Position, CurrentTarget.position);

            if (dist > AttackRange * 1.5f)
            {
                // Target too far, re-engage
                CurrentTarget = null;
                State = DroneState.Idle;
                return;
            }

            // Fire at target
            if (FireCooldown <= 0)
            {
                FireAtTarget();
                FireCooldown = 1f / EnhancedLogisticsPlugin.CombatDroneFireRate.Value;
            }

            // Stay at attack range
            if (dist < AttackRange * 0.5f)
            {
                // Back up a bit
                Vector3 away = (Position - CurrentTarget.position).normalized;
                Position += away * Speed * 0.5f * Time.deltaTime;
            }

            // Face target
            Vector3 toTarget = (CurrentTarget.position - Position).normalized;
            if (toTarget.sqrMagnitude > 0.001f)
            {
                Quaternion targetRot = Quaternion.LookRotation(toTarget, Vector3.up);
                gameObject.transform.rotation = Quaternion.Slerp(gameObject.transform.rotation, targetRot, Time.deltaTime * 8f);
            }
        }

        private void FireAtTarget()
        {
            if (CurrentTarget == null) return;

            // Visual laser
            if (laserLine != null)
            {
                laserLine.enabled = true;
                laserLine.SetPosition(0, Position + Vector3.down * 0.25f);
                laserLine.SetPosition(1, CurrentTarget.position);
                laserTimer = 0.1f;
            }

            // Apply damage via TurretDefense if available
            try
            {
                var turretDefenseType = System.Type.GetType("TurretDefense.TurretDefensePlugin, TurretDefense");
                if (turretDefenseType != null)
                {
                    // Try to find the alien controller and deal damage
                    var alienController = CurrentTarget.GetComponent("AlienShipController");
                    if (alienController != null)
                    {
                        var takeDamageMethod = alienController.GetType().GetMethod("TakeDamage");
                        if (takeDamageMethod != null)
                        {
                            float damage = EnhancedLogisticsPlugin.CombatDroneDamage.Value;
                            // TakeDamage expects (float damage, bool isCritical)
                            bool isCritical = UnityEngine.Random.value < 0.15f; // 15% crit chance
                            if (isCritical) damage *= 2f;
                            takeDamageMethod.Invoke(alienController, new object[] { damage, isCritical });
                            string critText = isCritical ? " (CRITICAL!)" : "";
                            EnhancedLogisticsPlugin.Log.LogDebug($"Combat drone dealt {damage:F0} damage{critText}");
                        }
                    }
                }
            }
            catch { }
        }

        protected override void OnArrivedAtTarget()
        {
            if (CurrentTarget != null)
            {
                State = DroneState.Working;
            }
            else
            {
                State = DroneState.Idle;
            }
        }

        protected override void OnWorkComplete()
        {
            State = DroneState.Idle;
            CurrentTarget = null;
        }
    }

    // =========================================================================
    // REPAIR DRONE - Repairs damaged machines
    // =========================================================================

    public class RepairDrone : DroneController
    {
        public override float Speed => EnhancedLogisticsPlugin.RepairDroneSpeed.Value;
        public override Color DroneColor => new Color(0.3f, 0.9f, 0.4f); // Green

        public Transform RepairTarget;
        public float RepairRange = 20f;
        public float ScanCooldown = 0f;

        // Visual effects
        private LineRenderer repairBeam;

        public RepairDrone(Vector3 position, DronePort owner) : base(DroneType.Repair, position, owner)
        {
            CreateRepairVisual();
        }

        private void CreateRepairVisual()
        {
            // Repair arm/tool underneath
            var tool = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tool.name = "RepairTool";
            tool.transform.SetParent(gameObject.transform);
            tool.transform.localPosition = Vector3.down * 0.25f;
            tool.transform.localScale = new Vector3(0.1f, 0.15f, 0.1f);

            var toolCollider = tool.GetComponent<Collider>();
            if (toolCollider != null) UnityEngine.Object.Destroy(toolCollider);

            var toolRenderer = tool.GetComponent<Renderer>();
            if (toolRenderer != null)
            {
                toolRenderer.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(0.8f, 0.7f, 0.2f));
            }

            // Repair beam
            repairBeam = gameObject.AddComponent<LineRenderer>();
            repairBeam.startWidth = 0.08f;
            repairBeam.endWidth = 0.03f;
            repairBeam.positionCount = 2;
            repairBeam.material = EnhancedLogisticsPlugin.GetDroneMaterial(new Color(0.2f, 1f, 0.4f));
            repairBeam.enabled = false;
        }

        public override void UpdateBehavior()
        {
            ScanCooldown -= Time.deltaTime;

            if (State == DroneState.Idle)
            {
                if (ScanCooldown <= 0)
                {
                    FindDamagedMachine();
                    ScanCooldown = 2f; // Scan every 2 seconds
                }
            }
            else if (State == DroneState.Working)
            {
                PerformRepair();
            }
        }

        private void FindDamagedMachine()
        {
            // This requires integration with TechtonicaFramework's health system
            // For now, check if SurvivalElements mod is loaded
            try
            {
                var survivalType = System.Type.GetType("SurvivalElements.SurvivalElementsPlugin, SurvivalElements");
                if (survivalType != null)
                {
                    // Try to get damaged machines list
                    var damagedMachinesField = survivalType.GetField("DamagedMachines", BindingFlags.Public | BindingFlags.Static);
                    if (damagedMachinesField != null)
                    {
                        var damagedList = damagedMachinesField.GetValue(null) as System.Collections.IList;
                        if (damagedList != null && damagedList.Count > 0)
                        {
                            // Find closest damaged machine
                            foreach (var damaged in damagedList)
                            {
                                // Would need proper type handling here
                                // For now, this is a stub
                            }
                        }
                    }
                }
            }
            catch { }

            // If no SurvivalElements, just patrol
            if (RepairTarget == null && HomePort != null)
            {
                // Patrol around home
                if (UnityEngine.Random.value < 0.02f)
                {
                    Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * RepairRange;
                    Vector3 patrolPoint = HomePort.Position + new Vector3(randomCircle.x, hoverHeight, randomCircle.y);
                    MoveTo(patrolPoint);
                }
            }
        }

        private void PerformRepair()
        {
            if (RepairTarget == null)
            {
                State = DroneState.Idle;
                repairBeam.enabled = false;
                return;
            }

            // Show repair beam
            if (repairBeam != null)
            {
                repairBeam.enabled = true;
                repairBeam.SetPosition(0, Position + Vector3.down * 0.25f);
                repairBeam.SetPosition(1, RepairTarget.position);
            }

            // Apply repair via SurvivalElements if available
            // For now, this is a stub until proper integration
        }

        protected override void OnArrivedAtTarget()
        {
            if (RepairTarget != null)
            {
                State = DroneState.Working;
                StateTimer = 5f; // 5 seconds of repair
            }
            else
            {
                State = DroneState.Idle;
            }
        }

        protected override void OnWorkComplete()
        {
            State = DroneState.Idle;
            RepairTarget = null;
            if (repairBeam != null)
            {
                repairBeam.enabled = false;
            }
            ReturnHome();
        }
    }

    /// <summary>
    /// Harmony patches for enhanced logistics features
    /// </summary>
    [HarmonyPatch]
    internal static class LogisticsPatches
    {
        // Track drone ports by machine instance ID
        private static readonly Dictionary<uint, DronePort> dronePortInstances = new Dictionary<uint, DronePort>();

        // Power consumption per port type (in kW)
        private static readonly Dictionary<string, int> portPowerConsumption = new Dictionary<string, int>
        {
            { EnhancedLogisticsPlugin.DronePortName, 30 },
            { EnhancedLogisticsPlugin.CombatDronePortName, 60 },
            { EnhancedLogisticsPlugin.RepairDronePortName, 45 }
        };

        /// <summary>
        /// Check if this is one of our drone port machines
        /// </summary>
        private static bool IsDronePort(ref PowerGeneratorInstance instance, out DroneType portType)
        {
            portType = DroneType.Delivery;
            if (instance.myDef == null) return false;

            string name = instance.myDef.displayName;
            if (name == EnhancedLogisticsPlugin.DronePortName)
            {
                portType = DroneType.Delivery;
                return true;
            }
            if (name == EnhancedLogisticsPlugin.CombatDronePortName)
            {
                portType = DroneType.Combat;
                return true;
            }
            if (name == EnhancedLogisticsPlugin.RepairDronePortName)
            {
                portType = DroneType.Repair;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Make drone ports consume power and manage their drones
        /// </summary>
        [HarmonyPatch(typeof(PowerGeneratorInstance), nameof(PowerGeneratorInstance.SimUpdate))]
        [HarmonyPostfix]
        private static void DronePortSimUpdate(ref PowerGeneratorInstance __instance)
        {
            try
            {
                if (!IsDronePort(ref __instance, out DroneType portType)) return;

                string portName = __instance.myDef.displayName;
                uint instanceId = __instance.commonInfo.instanceId;

                // Set power consumption
                if (portPowerConsumption.TryGetValue(portName, out int powerKW))
                {
                    ref var powerInfo = ref __instance.powerInfo;
                    powerInfo.curPowerConsumption = powerKW;
                    powerInfo.isGenerator = false;
                }

                // Check if machine is operating (simplified power check)
                bool hasPower = true; // Assume power available if machine is running

                // Initialize drone port if needed
                var visual = __instance.commonInfo.refGameObj;
                if (visual != null && !dronePortInstances.ContainsKey(instanceId))
                {
                    Vector3 position = visual.transform.position;
                    var dronePort = new DronePort(portType, position, instanceId);
                    dronePortInstances[instanceId] = dronePort;
                    EnhancedLogisticsPlugin.Log.LogInfo($"Initialized {portType} drone port at {position}");
                }

                // Update power status on the port
                if (dronePortInstances.TryGetValue(instanceId, out var port))
                {
                    port.HasPower = hasPower;
                }
            }
            catch (Exception ex)
            {
                EnhancedLogisticsPlugin.Log.LogError($"DronePortSimUpdate error: {ex.Message}");
            }
        }

        /// <summary>
        /// Clean up when drone port is deconstructed
        /// </summary>
        [HarmonyPatch(typeof(GridManager), "RemoveObj", new Type[] { typeof(GenericMachineInstanceRef) })]
        [HarmonyPrefix]
        private static void OnMachineRemoved(GenericMachineInstanceRef machineRef)
        {
            try
            {
                if (machineRef.IsValid() && dronePortInstances.TryGetValue(machineRef.instanceId, out var port))
                {
                    port.Destroy();
                    dronePortInstances.Remove(machineRef.instanceId);
                    EnhancedLogisticsPlugin.Log.LogInfo($"Drone port {machineRef.instanceId} removed");
                }
            }
            catch { }
        }

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

    /// <summary>
    /// Loads and manages drone asset bundles
    /// </summary>
    public class DroneAssetLoader
    {
        private string basePath;
        private Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private Dictionary<string, Dictionary<string, GameObject>> cachedPrefabs = new Dictionary<string, Dictionary<string, GameObject>>();

        // Mapping from drone type to bundle and prefab name
        private static readonly Dictionary<DroneType, (string bundle, string prefab)> DronePrefabMap = new Dictionary<DroneType, (string, string)>
        {
            { DroneType.Delivery, ("drones_simple", "drone blue") },
            { DroneType.Combat, ("drones_voodooplay", "Drone") },
            { DroneType.Repair, ("drones_simple", "drone green Variant") }
        };

        public DroneAssetLoader(string pluginPath)
        {
            basePath = pluginPath;
        }

        public bool LoadBundle(string bundleName)
        {
            if (loadedBundles.ContainsKey(bundleName)) return true;

            string[] possiblePaths = {
                Path.Combine(basePath, "Bundles", bundleName),
                Path.Combine(basePath, bundleName),
                Path.Combine(basePath, "Bundles", bundleName + ".bundle"),
                Path.Combine(basePath, bundleName + ".bundle")
            };

            foreach (var path in possiblePaths)
            {
                if (File.Exists(path))
                {
                    try
                    {
                        AssetBundle bundle = AssetBundle.LoadFromFile(path);
                        if (bundle != null)
                        {
                            loadedBundles[bundleName] = bundle;
                            cachedPrefabs[bundleName] = new Dictionary<string, GameObject>();
                            EnhancedLogisticsPlugin.Log.LogDebug($"Loaded bundle: {path}");

                            // Log available assets
                            var names = bundle.GetAllAssetNames();
                            EnhancedLogisticsPlugin.Log.LogDebug($"Bundle {bundleName} contains {names.Length} assets");

                            return true;
                        }
                    }
                    catch (Exception ex)
                    {
                        EnhancedLogisticsPlugin.Log.LogError($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
            }
            return false;
        }

        public GameObject GetPrefab(string bundleName, string prefabName)
        {
            if (cachedPrefabs.TryGetValue(bundleName, out var cache))
            {
                if (cache.TryGetValue(prefabName, out var cached))
                    return cached;
            }

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
                return null;

            // Try exact name
            GameObject prefab = bundle.LoadAsset<GameObject>(prefabName);

            // Try with .prefab extension
            if (prefab == null) prefab = bundle.LoadAsset<GameObject>(prefabName + ".prefab");

            // Search for partial match
            if (prefab == null)
            {
                foreach (var assetName in bundle.GetAllAssetNames())
                {
                    if (assetName.ToLower().Contains(prefabName.ToLower()) && assetName.EndsWith(".prefab"))
                    {
                        prefab = bundle.LoadAsset<GameObject>(assetName);
                        if (prefab != null)
                        {
                            EnhancedLogisticsPlugin.Log.LogDebug($"Found prefab {prefabName} as {assetName}");
                            break;
                        }
                    }
                }
            }

            if (prefab != null && cachedPrefabs.ContainsKey(bundleName))
                cachedPrefabs[bundleName][prefabName] = prefab;

            return prefab;
        }

        /// <summary>
        /// Get the drone prefab for a specific drone type
        /// </summary>
        public GameObject GetDronePrefab(DroneType droneType)
        {
            if (DronePrefabMap.TryGetValue(droneType, out var mapping))
            {
                return GetPrefab(mapping.bundle, mapping.prefab);
            }
            return null;
        }

        /// <summary>
        /// Check if bundles are loaded and drone prefabs are available
        /// </summary>
        public bool HasDronePrefabs => loadedBundles.Count > 0;
    }
}
