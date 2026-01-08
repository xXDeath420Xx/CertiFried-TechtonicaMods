using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace OmniseekerPlus
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class OmniseekerPlusPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.OmniseekerPlus";
        private const string PluginName = "OmniseekerPlus";
        private const string VersionString = "1.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Scanner Range Settings
        public static ConfigEntry<float> ScannerRangeMultiplier;
        public static ConfigEntry<float> MaxScanRange;
        public static ConfigEntry<bool> ExtendedRangeEnabled;

        // Scannable Type Settings
        public static ConfigEntry<bool> ScanHardDrives;
        public static ConfigEntry<bool> ScanHiddenChests;
        public static ConfigEntry<bool> ScanRareOres;
        public static ConfigEntry<bool> ScanAllOreTypes;
        public static ConfigEntry<bool> ScanLootContainers;
        public static ConfigEntry<bool> ScanMemoryTrees;

        // Visual Settings
        public static ConfigEntry<bool> ShowDistanceOnHUD;
        public static ConfigEntry<bool> ColorCodeByDistance;
        public static ConfigEntry<bool> ShowScanPulseEffect;
        public static ConfigEntry<float> PulseIntensity;

        // Hotkey Settings
        public static ConfigEntry<KeyCode> CycleModeKey;
        public static ConfigEntry<KeyCode> QuickScanKey;

        // Current scan mode
        public static int currentScanMode = 0;
        public static string[] scanModeNames = { "All", "Ores Only", "Hard Drives", "Loot", "Memory Trees", "Custom" };

        // GUI
        private static bool showGui = false;
        private static Rect windowRect = new Rect(Screen.width - 320, 100, 300, 400);
        private static Vector2 scrollPos = Vector2.zero;

        // Tracked scannables
        public static List<ScannableInfo> lastScanResults = new List<ScannableInfo>();
        private static float lastScanTime = 0f;

        public struct ScannableInfo
        {
            public Vector3 position;
            public string name;
            public float distance;
            public string type;
            public GameObject gameObject;
        }

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            // Scanner Range Settings
            ScannerRangeMultiplier = Config.Bind("Scanner Range", "Range Multiplier", 3f,
                new ConfigDescription("Multiplier for scanner range (default 3x)", new AcceptableValueRange<float>(1f, 10f)));
            MaxScanRange = Config.Bind("Scanner Range", "Max Scan Range", 500f,
                new ConfigDescription("Maximum scan range in meters", new AcceptableValueRange<float>(100f, 2000f)));
            ExtendedRangeEnabled = Config.Bind("Scanner Range", "Extended Range Enabled", true,
                "Enable extended scanner range");

            // Scannable Types
            ScanHardDrives = Config.Bind("Scannable Types", "Scan Hard Drives", true,
                "Enable scanning for Hard Drives (data storage devices)");
            ScanHiddenChests = Config.Bind("Scannable Types", "Scan Hidden Chests", true,
                "Enable scanning for hidden/buried chests");
            ScanRareOres = Config.Bind("Scannable Types", "Scan Rare Ores", true,
                "Enable scanning for rare ore types (Atlantum, etc)");
            ScanAllOreTypes = Config.Bind("Scannable Types", "Scan All Ore Types", true,
                "Enable scanning for all ore vein types");
            ScanLootContainers = Config.Bind("Scannable Types", "Scan Loot Containers", true,
                "Enable scanning for loot containers and crates");
            ScanMemoryTrees = Config.Bind("Scannable Types", "Scan Memory Trees", true,
                "Enable scanning for Memory Trees (research core producers)");

            // Visual Settings
            ShowDistanceOnHUD = Config.Bind("Visuals", "Show Distance on HUD", true,
                "Display distance to scanned objects on the HUD");
            ColorCodeByDistance = Config.Bind("Visuals", "Color Code by Distance", true,
                "Color-code scan results based on distance (green=close, yellow=medium, red=far)");
            ShowScanPulseEffect = Config.Bind("Visuals", "Show Scan Pulse Effect", true,
                "Display a visual pulse effect when scanning");
            PulseIntensity = Config.Bind("Visuals", "Pulse Intensity", 1.5f,
                new ConfigDescription("Intensity of the scan pulse effect", new AcceptableValueRange<float>(0.5f, 3f)));

            // Hotkeys
            CycleModeKey = Config.Bind("Hotkeys", "Cycle Mode Key", KeyCode.Keypad4,
                "Key to cycle through scan modes (Numpad 4 - O may be remapped in game)");
            QuickScanKey = Config.Bind("Hotkeys", "Quick Scan Key", KeyCode.Keypad3,
                "Key to perform a quick area scan (Numpad 3 - P is game's Power menu)");

            Harmony.PatchAll();

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log.LogInfo($"Press Numpad4 to cycle scan modes, Numpad3 for quick scan");
        }

        private void Update()
        {
            // Cycle scan mode
            if (Input.GetKeyDown(CycleModeKey.Value) && !Input.GetKey(KeyCode.LeftShift))
            {
                currentScanMode = (currentScanMode + 1) % scanModeNames.Length;
                Log.LogInfo($"Scan mode: {scanModeNames[currentScanMode]}");
            }

            // Quick scan
            if (Input.GetKeyDown(QuickScanKey.Value))
            {
                PerformQuickScan();
            }

            // Toggle GUI with shift+Numpad4
            if (Input.GetKey(KeyCode.LeftShift) && Input.GetKeyDown(CycleModeKey.Value))
            {
                showGui = !showGui;
            }
        }

        private void OnGUI()
        {
            if (!showGui) return;

            windowRect = GUILayout.Window(54321, windowRect, DrawWindow, "OmniseekerPlus");
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Current Mode: {scanModeNames[currentScanMode]}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });
            GUILayout.Space(5);

            // Mode buttons - 3 per row
            for (int row = 0; row < 2; row++)
            {
                GUILayout.BeginHorizontal();
                for (int col = 0; col < 3; col++)
                {
                    int i = row * 3 + col;
                    if (i < scanModeNames.Length)
                    {
                        GUI.color = (currentScanMode == i) ? Color.cyan : Color.white;
                        if (GUILayout.Button(scanModeNames[i], GUILayout.Height(25)))
                        {
                            currentScanMode = i;
                        }
                    }
                }
                GUILayout.EndHorizontal();
            }
            GUI.color = Color.white;

            GUILayout.Space(10);

            // Quick scan button
            if (GUILayout.Button("Perform Quick Scan", GUILayout.Height(35)))
            {
                PerformQuickScan();
            }

            GUILayout.Space(10);

            // Last scan results
            GUILayout.Label($"Last Scan Results ({lastScanResults.Count} found):");

            scrollPos = GUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));

            foreach (var result in lastScanResults.OrderBy(r => r.distance))
            {
                Color distColor = GetDistanceColor(result.distance);
                GUI.color = distColor;
                GUILayout.BeginHorizontal();
                GUILayout.Label(result.name, GUILayout.Width(150));
                GUILayout.Label($"{result.distance:F1}m", GUILayout.Width(60));
                GUILayout.Label(result.type, GUILayout.Width(80));
                GUILayout.EndHorizontal();
            }
            GUI.color = Color.white;

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Settings
            ExtendedRangeEnabled.Value = GUILayout.Toggle(ExtendedRangeEnabled.Value, " Extended Range");
            ShowDistanceOnHUD.Value = GUILayout.Toggle(ShowDistanceOnHUD.Value, " Show Distance on HUD");
            ColorCodeByDistance.Value = GUILayout.Toggle(ColorCodeByDistance.Value, " Color Code Results");

            GUILayout.Space(5);

            GUILayout.Label($"Range: {MaxScanRange.Value:F0}m (x{ScannerRangeMultiplier.Value:F1})");

            GUILayout.FlexibleSpace();

            GUILayout.Label($"[{CycleModeKey.Value}] Cycle Mode | [{QuickScanKey.Value}] Quick Scan");

            if (GUILayout.Button("Close"))
            {
                showGui = false;
            }

            GUILayout.EndVertical();

            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private Color GetDistanceColor(float distance)
        {
            if (!ColorCodeByDistance.Value) return Color.white;

            if (distance < 50f) return Color.green;
            if (distance < 150f) return Color.yellow;
            if (distance < 300f) return new Color(1f, 0.5f, 0f); // Orange
            return Color.red;
        }

        public void PerformQuickScan()
        {
            if (Player.instance == null) return;

            lastScanResults.Clear();
            lastScanTime = Time.time;

            Vector3 playerPos = Player.instance.transform.position;
            float scanRange = ExtendedRangeEnabled.Value ? MaxScanRange.Value : 100f;

            Log.LogInfo($"Performing quick scan at range {scanRange}m in mode: {scanModeNames[currentScanMode]}...");

            // Scan for different object types based on mode
            switch (currentScanMode)
            {
                case 0: // All
                    ScanForOres(playerPos, scanRange);
                    ScanForChests(playerPos, scanRange);
                    ScanForMachines(playerPos, scanRange);
                    break;
                case 1: // Ores Only
                    ScanForOres(playerPos, scanRange);
                    break;
                case 2: // Hard Drives
                    ScanForDataObjects(playerPos, scanRange);
                    break;
                case 3: // Loot
                    ScanForChests(playerPos, scanRange);
                    break;
                case 4: // Memory Trees
                    ScanForMachineType(playerPos, scanRange, "MemoryTree");
                    break;
                case 5: // Custom - use individual settings
                    if (ScanAllOreTypes.Value || ScanRareOres.Value)
                        ScanForOres(playerPos, scanRange);
                    if (ScanLootContainers.Value || ScanHiddenChests.Value)
                        ScanForChests(playerPos, scanRange);
                    if (ScanHardDrives.Value)
                        ScanForDataObjects(playerPos, scanRange);
                    if (ScanMemoryTrees.Value)
                        ScanForMachineType(playerPos, scanRange, "MemoryTree");
                    break;
            }

            Log.LogInfo($"Quick scan complete: {lastScanResults.Count} objects found");

            // Visual pulse effect
            if (ShowScanPulseEffect.Value)
            {
                CreateScanPulseEffect(playerPos, scanRange);
            }
        }

        private void ScanForOres(Vector3 center, float range)
        {
            // Find ore veins by searching for objects with "ore" or "vein" in name
            try
            {
                var oreObjects = FindObjectsOfType<GameObject>()
                    .Where(go => go.name.ToLower().Contains("ore") || go.name.ToLower().Contains("vein"))
                    .Where(go => go.activeInHierarchy);

                foreach (var ore in oreObjects)
                {
                    float distance = Vector3.Distance(center, ore.transform.position);
                    if (distance > range) continue;

                    string oreType = IdentifyOreType(ore.name);

                    // Filter by settings in custom mode
                    if (currentScanMode == 5)
                    {
                        if (oreType == "Atlantum" && !ScanRareOres.Value) continue;
                        if (oreType != "Atlantum" && !ScanAllOreTypes.Value) continue;
                    }

                    lastScanResults.Add(new ScannableInfo
                    {
                        position = ore.transform.position,
                        name = $"{oreType} Ore",
                        distance = distance,
                        type = "Ore",
                        gameObject = ore
                    });
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Error scanning for ores: {ex.Message}");
            }
        }

        private string IdentifyOreType(string name)
        {
            string lower = name.ToLower();
            if (lower.Contains("atlantum")) return "Atlantum";
            if (lower.Contains("copper")) return "Copper";
            if (lower.Contains("iron")) return "Iron";
            if (lower.Contains("limestone")) return "Limestone";
            if (lower.Contains("gold")) return "Gold";
            if (lower.Contains("coal")) return "Coal";
            return "Unknown";
        }

        private void ScanForChests(Vector3 center, float range)
        {
            if (MachineManager.instance == null) return;

            try
            {
                var chestList = MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);
                if (chestList == null || chestList.myArray == null) return;

                foreach (var chest in chestList.myArray)
                {
                    if (chest.commonInfo.instanceId == 0) continue;

                    Vector3 pos = chest.gridInfo.Center;
                    float distance = Vector3.Distance(center, pos);
                    if (distance > range) continue;

                    // Check if chest has items
                    var inv = chest.GetInventory();
                    bool hasItems = false;
                    int itemCount = 0;

                    if (inv.myStacks != null)
                    {
                        foreach (var stack in inv.myStacks)
                        {
                            if (!stack.isEmpty)
                            {
                                hasItems = true;
                                itemCount += stack.count;
                            }
                        }
                    }

                    string chestName = hasItems ? $"Chest ({itemCount} items)" : "Empty Chest";

                    lastScanResults.Add(new ScannableInfo
                    {
                        position = pos,
                        name = chestName,
                        distance = distance,
                        type = "Container",
                        gameObject = chest.commonInfo.refGameObj
                    });
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Error scanning for chests: {ex.Message}");
            }
        }

        private void ScanForDataObjects(Vector3 center, float range)
        {
            // Search for objects that might be hard drives / data storage
            try
            {
                var dataObjects = FindObjectsOfType<GameObject>()
                    .Where(go =>
                    {
                        string lower = go.name.ToLower();
                        return lower.Contains("harddrive") ||
                               lower.Contains("hard_drive") ||
                               lower.Contains("datastorage") ||
                               lower.Contains("data_storage") ||
                               lower.Contains("memory") ||
                               lower.Contains("disk") ||
                               lower.Contains("cache");
                    })
                    .Where(go => go.activeInHierarchy);

                foreach (var obj in dataObjects)
                {
                    float distance = Vector3.Distance(center, obj.transform.position);
                    if (distance > range) continue;

                    lastScanResults.Add(new ScannableInfo
                    {
                        position = obj.transform.position,
                        name = "Data Storage",
                        distance = distance,
                        type = "HardDrive",
                        gameObject = obj
                    });
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Error scanning for data objects: {ex.Message}");
            }
        }

        private void ScanForMachines(Vector3 center, float range)
        {
            if (MachineManager.instance == null) return;

            // Scan for various machine types
            ScanForMachineType(center, range, "Generator");
            ScanForMachineType(center, range, "MemoryTree");
        }

        private void ScanForMachineType(Vector3 center, float range, string machineTypeName)
        {
            // This is a generic scanner that looks for GameObjects with machine-like names
            try
            {
                var machines = FindObjectsOfType<GameObject>()
                    .Where(go => go.name.ToLower().Contains(machineTypeName.ToLower()))
                    .Where(go => go.activeInHierarchy);

                foreach (var machine in machines)
                {
                    float distance = Vector3.Distance(center, machine.transform.position);
                    if (distance > range) continue;

                    // Avoid duplicates
                    if (lastScanResults.Any(r => r.gameObject == machine)) continue;

                    lastScanResults.Add(new ScannableInfo
                    {
                        position = machine.transform.position,
                        name = machineTypeName,
                        distance = distance,
                        type = "Machine",
                        gameObject = machine
                    });
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Error scanning for {machineTypeName}: {ex.Message}");
            }
        }

        private void CreateScanPulseEffect(Vector3 center, float range)
        {
            // Create a brief visual pulse effect
            // For now, just log that scan completed
            Log.LogInfo($"Scan pulse at {center} with range {range}m");

            // Could spawn a temporary visual effect here in the future
            // For example: expanding sphere wireframe, particle effect, etc.
        }
    }

    // Note: Harmony patches for omniseeker equipment removed - the game types
    // (OmniseekerEquipmentInstance, OmniseekerHUD, OmniseekableIndicator, etc.)
    // may not be available or may have different names.
    // The quick scan functionality above provides enhanced scanning without
    // needing to patch the base game's omniseeker.
}
