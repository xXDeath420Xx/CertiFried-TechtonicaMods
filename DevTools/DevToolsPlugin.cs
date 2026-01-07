using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace DevTools
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class DevToolsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.DevTools";
        private const string PluginName = "DevTools";
        private const string VersionString = "2.2.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // ============================================
        // GUI SETTINGS
        // ============================================
        public static ConfigEntry<KeyCode> GuiToggleKey;
        public static ConfigEntry<bool> ShowGui;

        // ============================================
        // PLAYER CHEATS (from PlayerCheats class)
        // ============================================
        public static ConfigEntry<bool> InfiniteCrafting;
        public static ConfigEntry<bool> MaxPower;
        public static ConfigEntry<bool> UltraPickaxe;
        public static ConfigEntry<bool> FasterMole;
        public static ConfigEntry<bool> DisableEncumbrance;
        public static ConfigEntry<bool> ShowDebugCoords;
        public static ConfigEntry<bool> HideMachineLights;
        public static ConfigEntry<bool> HideMachineParticles;
        public static ConfigEntry<float> SimSpeed;
        public static ConfigEntry<int> FreeCameraMode;

        // ============================================
        // GAME MODE SETTINGS (from EGameModeSettingType)
        // ============================================
        public static ConfigEntry<bool> AllDoorsUnlocked;
        public static ConfigEntry<bool> MoleBitsAlwaysAvailable;
        public static ConfigEntry<bool> InfiniteOre;
        public static ConfigEntry<int> PlayerSpeedPercent;
        public static ConfigEntry<int> MoleSpeedPercent;

        // Machine speed multipliers (as percentages)
        public static ConfigEntry<int> SmelterSpeedPercent;
        public static ConfigEntry<int> AssemblerSpeedPercent;
        public static ConfigEntry<int> ThresherSpeedPercent;
        public static ConfigEntry<int> PlanterSpeedPercent;

        // Power settings
        public static ConfigEntry<int> FuelConsumptionPercent;
        public static ConfigEntry<int> PowerConsumptionPercent;
        public static ConfigEntry<int> PowerGenerationPercent;

        // Inserter settings
        public static ConfigEntry<int> InserterBaseStackSize;

        // ============================================
        // SPECIAL CHEATS
        // ============================================
        public static ConfigEntry<bool> CatSounds;
        public static ConfigEntry<bool> RainbowCores;
        public static ConfigEntry<bool> PlaceholderVoice;

        private static bool isInitialized = false;
        private static bool settingsApplied = false;
        private static float rainbowHue = 0f;

        // GUI State
        private static bool guiVisible = false;
        private static Rect windowRect = new Rect(20, 20, 420, 600);
        private static Vector2 scrollPosition = Vector2.zero;
        private static int currentTab = 0;
        private static string[] tabNames = { "Player", "Game Mode", "Machines", "Power", "Special" };
        private static GUIStyle headerStyle;
        private static GUIStyle boxStyle;
        private static bool stylesInitialized = false;

        // Slider temp values
        private static string simSpeedInput = "1";
        private static string playerSpeedInput = "100";
        private static string moleSpeedInput = "100";
        private static string smelterSpeedInput = "100";
        private static string assemblerSpeedInput = "100";
        private static string thresherSpeedInput = "100";
        private static string planterSpeedInput = "100";
        private static string fuelConsumptionInput = "100";
        private static string powerConsumptionInput = "100";
        private static string powerGenerationInput = "100";
        private static string inserterStackInput = "1";

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            // GUI Settings
            GuiToggleKey = Config.Bind("GUI", "Toggle Key", KeyCode.Insert,
                "Key to toggle the DevTools GUI");
            ShowGui = Config.Bind("GUI", "Show GUI", false,
                "Whether the GUI is currently visible");

            // Bind PlayerCheats configs
            InfiniteCrafting = Config.Bind("PlayerCheats", "Infinite Crafting", false,
                "Build machines without consuming resources");
            MaxPower = Config.Bind("PlayerCheats", "Max Power", false,
                "All machines operate at full power regardless of network status");
            UltraPickaxe = Config.Bind("PlayerCheats", "Ultra Pickaxe", false,
                "Enhanced mining speed and power");
            FasterMole = Config.Bind("PlayerCheats", "Faster MOLE", false,
                "MOLE operates at 4x speed");
            DisableEncumbrance = Config.Bind("PlayerCheats", "Disable Encumbrance", false,
                "Remove weight/inventory limits");
            ShowDebugCoords = Config.Bind("PlayerCheats", "Show Debug Coordinates", false,
                "Display debug position information");
            HideMachineLights = Config.Bind("PlayerCheats", "Hide Machine Lights", false,
                "Disable machine light effects (performance)");
            HideMachineParticles = Config.Bind("PlayerCheats", "Hide Machine Particles", false,
                "Disable machine particle effects (performance)");
            SimSpeed = Config.Bind("PlayerCheats", "Simulation Speed", 1f,
                new ConfigDescription("Game simulation speed multiplier", new AcceptableValueRange<float>(0.1f, 10f)));
            FreeCameraMode = Config.Bind("PlayerCheats", "Free Camera Mode", 0,
                new ConfigDescription("Camera mode: 0=Normal, 1=Free, 2=ScriptedAnimation, 3=Benchmark, 4=CheatAnimation",
                new AcceptableValueRange<int>(0, 4)));

            // Bind Game Mode Settings configs
            AllDoorsUnlocked = Config.Bind("GameModeSettings", "All Doors Unlocked", false,
                "Unlock all doors in the game");
            MoleBitsAlwaysAvailable = Config.Bind("GameModeSettings", "MOLE Bits Always Available", false,
                "All MOLE drill bits are always accessible");
            InfiniteOre = Config.Bind("GameModeSettings", "Infinite Ore", false,
                "Ore veins never deplete");
            PlayerSpeedPercent = Config.Bind("GameModeSettings", "Player Speed Percent", 100,
                new ConfigDescription("Player movement speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            MoleSpeedPercent = Config.Bind("GameModeSettings", "MOLE Speed Percent", 100,
                new ConfigDescription("MOLE digging speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));

            // Machine speed multipliers
            SmelterSpeedPercent = Config.Bind("MachineSettings", "Smelter Speed Percent", 100,
                new ConfigDescription("Smelter crafting speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            AssemblerSpeedPercent = Config.Bind("MachineSettings", "Assembler Speed Percent", 100,
                new ConfigDescription("Assembler crafting speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            ThresherSpeedPercent = Config.Bind("MachineSettings", "Thresher Speed Percent", 100,
                new ConfigDescription("Thresher processing speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            PlanterSpeedPercent = Config.Bind("MachineSettings", "Planter Speed Percent", 100,
                new ConfigDescription("Planter growth speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));

            // Power settings
            FuelConsumptionPercent = Config.Bind("PowerSettings", "Fuel Consumption Percent", 100,
                new ConfigDescription("Fuel consumption percentage (lower = less fuel used)", new AcceptableValueRange<int>(10, 500)));
            PowerConsumptionPercent = Config.Bind("PowerSettings", "Power Consumption Percent", 100,
                new ConfigDescription("Power consumption percentage (lower = less power used)", new AcceptableValueRange<int>(10, 500)));
            PowerGenerationPercent = Config.Bind("PowerSettings", "Power Generation Percent", 100,
                new ConfigDescription("Power generation percentage (higher = more power)", new AcceptableValueRange<int>(50, 1000)));

            // Inserter settings
            InserterBaseStackSize = Config.Bind("MachineSettings", "Inserter Base Stack Size", 1,
                new ConfigDescription("Base stack size for inserters", new AcceptableValueRange<int>(1, 100)));

            // Special Cheats
            CatSounds = Config.Bind("SpecialCheats", "Cat Sounds", false,
                "Replace machine sounds with cat meowing (FMOD Cat Cheat parameter)");
            RainbowCores = Config.Bind("SpecialCheats", "Rainbow Cores", false,
                "Memory Tree cores cycle through rainbow colors");
            PlaceholderVoice = Config.Bind("SpecialCheats", "Placeholder Voice", false,
                "Enable placeholder voice lines (developer audio)");

            // Initialize input fields
            simSpeedInput = SimSpeed.Value.ToString("F1");
            playerSpeedInput = PlayerSpeedPercent.Value.ToString();
            moleSpeedInput = MoleSpeedPercent.Value.ToString();
            smelterSpeedInput = SmelterSpeedPercent.Value.ToString();
            assemblerSpeedInput = AssemblerSpeedPercent.Value.ToString();
            thresherSpeedInput = ThresherSpeedPercent.Value.ToString();
            planterSpeedInput = PlanterSpeedPercent.Value.ToString();
            fuelConsumptionInput = FuelConsumptionPercent.Value.ToString();
            powerConsumptionInput = PowerConsumptionPercent.Value.ToString();
            powerGenerationInput = PowerGenerationPercent.Value.ToString();
            inserterStackInput = InserterBaseStackSize.Value.ToString();

            guiVisible = ShowGui.Value;

            // Apply patches
            Harmony.PatchAll();

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
            Log.LogInfo($"Press {GuiToggleKey.Value} to toggle the DevTools GUI");
        }

        private void Update()
        {
            // Toggle GUI with hotkey
            if (Input.GetKeyDown(GuiToggleKey.Value))
            {
                guiVisible = !guiVisible;
                ShowGui.Value = guiVisible;
                Log.LogInfo($"DevTools GUI {(guiVisible ? "opened" : "closed")}");
            }

            // Apply cheats once Player is available
            if (Player.instance != null && Player.instance.cheats != null)
            {
                ApplyPlayerCheats();
                if (!isInitialized)
                {
                    isInitialized = true;
                    Log.LogInfo("DevTools initialized - PlayerCheats active");
                }
            }

            // Apply game mode settings continuously for live updates
            if (GameState.instance != null)
            {
                ApplyGameModeSettings();
                if (!settingsApplied)
                {
                    settingsApplied = true;
                    Log.LogInfo("DevTools applied game mode settings");
                }
            }

            // Apply Cat Sounds FMOD parameter
            ApplyCatSounds();

            // Update rainbow cores effect
            if (RainbowCores.Value)
            {
                UpdateRainbowCores();
            }
        }

        private void OnGUI()
        {
            if (!guiVisible) return;

            InitStyles();

            // Make the window draggable and create it
            windowRect = GUILayout.Window(12345, windowRect, DrawWindow, "DevTools v" + VersionString);

            // Keep window on screen
            windowRect.x = Mathf.Clamp(windowRect.x, 0, Screen.width - windowRect.width);
            windowRect.y = Mathf.Clamp(windowRect.y, 0, Screen.height - windowRect.height);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.cyan;

            boxStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(10, 10, 10, 10)
            };

            stylesInitialized = true;
        }

        private void DrawWindow(int windowId)
        {
            GUILayout.BeginVertical();

            // Tab buttons
            GUILayout.BeginHorizontal();
            for (int i = 0; i < tabNames.Length; i++)
            {
                GUI.color = (currentTab == i) ? Color.cyan : Color.white;
                if (GUILayout.Button(tabNames[i], GUILayout.Height(30)))
                {
                    currentTab = i;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Scrollable content area
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(480));

            switch (currentTab)
            {
                case 0: DrawPlayerTab(); break;
                case 1: DrawGameModeTab(); break;
                case 2: DrawMachinesTab(); break;
                case 3: DrawPowerTab(); break;
                case 4: DrawSpecialTab(); break;
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);

            // Footer
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Press {GuiToggleKey.Value} to toggle", GUILayout.Width(150));
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(80)))
            {
                guiVisible = false;
                ShowGui.Value = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, 10000, 30));
        }

        private void DrawPlayerTab()
        {
            GUILayout.Label("Player Cheats", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginVertical(boxStyle);

            InfiniteCrafting.Value = GUILayout.Toggle(InfiniteCrafting.Value, " Infinite Crafting");
            GUILayout.Label("  Build machines without consuming resources", GUI.skin.label);
            GUILayout.Space(5);

            MaxPower.Value = GUILayout.Toggle(MaxPower.Value, " Max Power");
            GUILayout.Label("  All machines operate at full power", GUI.skin.label);
            GUILayout.Space(5);

            UltraPickaxe.Value = GUILayout.Toggle(UltraPickaxe.Value, " Ultra Pickaxe");
            GUILayout.Label("  Enhanced mining speed and power", GUI.skin.label);
            GUILayout.Space(5);

            FasterMole.Value = GUILayout.Toggle(FasterMole.Value, " Faster MOLE");
            GUILayout.Label("  MOLE operates at 4x speed", GUI.skin.label);
            GUILayout.Space(5);

            DisableEncumbrance.Value = GUILayout.Toggle(DisableEncumbrance.Value, " Disable Encumbrance");
            GUILayout.Label("  Remove weight/inventory limits", GUI.skin.label);
            GUILayout.Space(5);

            ShowDebugCoords.Value = GUILayout.Toggle(ShowDebugCoords.Value, " Show Debug Coordinates");
            GUILayout.Label("  Display debug position information", GUI.skin.label);
            GUILayout.Space(5);

            HideMachineLights.Value = GUILayout.Toggle(HideMachineLights.Value, " Hide Machine Lights");
            GUILayout.Label("  Disable machine light effects (performance)", GUI.skin.label);
            GUILayout.Space(5);

            HideMachineParticles.Value = GUILayout.Toggle(HideMachineParticles.Value, " Hide Machine Particles");
            GUILayout.Label("  Disable machine particle effects (performance)", GUI.skin.label);
            GUILayout.Space(10);

            // Simulation Speed slider
            GUILayout.Label($"Simulation Speed: {SimSpeed.Value:F1}x");
            GUILayout.BeginHorizontal();
            SimSpeed.Value = GUILayout.HorizontalSlider(SimSpeed.Value, 0.1f, 10f, GUILayout.Width(280));
            simSpeedInput = GUILayout.TextField(simSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (float.TryParse(simSpeedInput, out float val))
                {
                    SimSpeed.Value = Mathf.Clamp(val, 0.1f, 10f);
                }
                simSpeedInput = SimSpeed.Value.ToString("F1");
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Free Camera Mode
            GUILayout.Label($"Free Camera Mode: {FreeCameraMode.Value}");
            GUILayout.BeginHorizontal();
            string[] cameraModes = { "Normal", "Free", "Scripted", "Benchmark", "Cheat" };
            for (int i = 0; i < cameraModes.Length; i++)
            {
                GUI.color = (FreeCameraMode.Value == i) ? Color.green : Color.white;
                if (GUILayout.Button(cameraModes[i]))
                {
                    FreeCameraMode.Value = i;
                }
            }
            GUI.color = Color.white;
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawGameModeTab()
        {
            GUILayout.Label("Game Mode Settings", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginVertical(boxStyle);

            AllDoorsUnlocked.Value = GUILayout.Toggle(AllDoorsUnlocked.Value, " All Doors Unlocked");
            GUILayout.Label("  Unlock all doors in the game", GUI.skin.label);
            GUILayout.Space(5);

            MoleBitsAlwaysAvailable.Value = GUILayout.Toggle(MoleBitsAlwaysAvailable.Value, " MOLE Bits Always Available");
            GUILayout.Label("  All MOLE drill bits are accessible", GUI.skin.label);
            GUILayout.Space(5);

            InfiniteOre.Value = GUILayout.Toggle(InfiniteOre.Value, " Infinite Ore");
            GUILayout.Label("  Ore veins never deplete", GUI.skin.label);
            GUILayout.Space(15);

            // Player Speed
            GUILayout.Label($"Player Speed: {PlayerSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            PlayerSpeedPercent.Value = (int)GUILayout.HorizontalSlider(PlayerSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            playerSpeedInput = GUILayout.TextField(playerSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(playerSpeedInput, out int val))
                {
                    PlayerSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                playerSpeedInput = PlayerSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // MOLE Speed
            GUILayout.Label($"MOLE Speed: {MoleSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            MoleSpeedPercent.Value = (int)GUILayout.HorizontalSlider(MoleSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            moleSpeedInput = GUILayout.TextField(moleSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(moleSpeedInput, out int val))
                {
                    MoleSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                moleSpeedInput = MoleSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawMachinesTab()
        {
            GUILayout.Label("Machine Settings", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginVertical(boxStyle);

            // Smelter Speed
            GUILayout.Label($"Smelter Speed: {SmelterSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            SmelterSpeedPercent.Value = (int)GUILayout.HorizontalSlider(SmelterSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            smelterSpeedInput = GUILayout.TextField(smelterSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(smelterSpeedInput, out int val))
                {
                    SmelterSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                smelterSpeedInput = SmelterSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Assembler Speed
            GUILayout.Label($"Assembler Speed: {AssemblerSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            AssemblerSpeedPercent.Value = (int)GUILayout.HorizontalSlider(AssemblerSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            assemblerSpeedInput = GUILayout.TextField(assemblerSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(assemblerSpeedInput, out int val))
                {
                    AssemblerSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                assemblerSpeedInput = AssemblerSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Thresher Speed
            GUILayout.Label($"Thresher Speed: {ThresherSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            ThresherSpeedPercent.Value = (int)GUILayout.HorizontalSlider(ThresherSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            thresherSpeedInput = GUILayout.TextField(thresherSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(thresherSpeedInput, out int val))
                {
                    ThresherSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                thresherSpeedInput = ThresherSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(10);

            // Planter Speed
            GUILayout.Label($"Planter Speed: {PlanterSpeedPercent.Value}%");
            GUILayout.BeginHorizontal();
            PlanterSpeedPercent.Value = (int)GUILayout.HorizontalSlider(PlanterSpeedPercent.Value, 50, 1000, GUILayout.Width(280));
            planterSpeedInput = GUILayout.TextField(planterSpeedInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(planterSpeedInput, out int val))
                {
                    PlanterSpeedPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                planterSpeedInput = PlanterSpeedPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            // Inserter Stack Size
            GUILayout.Label($"Inserter Base Stack Size: {InserterBaseStackSize.Value}");
            GUILayout.BeginHorizontal();
            InserterBaseStackSize.Value = (int)GUILayout.HorizontalSlider(InserterBaseStackSize.Value, 1, 100, GUILayout.Width(280));
            inserterStackInput = GUILayout.TextField(inserterStackInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(inserterStackInput, out int val))
                {
                    InserterBaseStackSize.Value = Mathf.Clamp(val, 1, 100);
                }
                inserterStackInput = InserterBaseStackSize.Value.ToString();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawPowerTab()
        {
            GUILayout.Label("Power Settings", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginVertical(boxStyle);

            // Fuel Consumption
            GUILayout.Label($"Fuel Consumption: {FuelConsumptionPercent.Value}%");
            GUILayout.Label("  Lower = less fuel used", GUI.skin.label);
            GUILayout.BeginHorizontal();
            FuelConsumptionPercent.Value = (int)GUILayout.HorizontalSlider(FuelConsumptionPercent.Value, 10, 500, GUILayout.Width(280));
            fuelConsumptionInput = GUILayout.TextField(fuelConsumptionInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(fuelConsumptionInput, out int val))
                {
                    FuelConsumptionPercent.Value = Mathf.Clamp(val, 10, 500);
                }
                fuelConsumptionInput = FuelConsumptionPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            // Power Consumption
            GUILayout.Label($"Power Consumption: {PowerConsumptionPercent.Value}%");
            GUILayout.Label("  Lower = less power used", GUI.skin.label);
            GUILayout.BeginHorizontal();
            PowerConsumptionPercent.Value = (int)GUILayout.HorizontalSlider(PowerConsumptionPercent.Value, 10, 500, GUILayout.Width(280));
            powerConsumptionInput = GUILayout.TextField(powerConsumptionInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(powerConsumptionInput, out int val))
                {
                    PowerConsumptionPercent.Value = Mathf.Clamp(val, 10, 500);
                }
                powerConsumptionInput = PowerConsumptionPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();
            GUILayout.Space(15);

            // Power Generation
            GUILayout.Label($"Power Generation: {PowerGenerationPercent.Value}%");
            GUILayout.Label("  Higher = more power generated", GUI.skin.label);
            GUILayout.BeginHorizontal();
            PowerGenerationPercent.Value = (int)GUILayout.HorizontalSlider(PowerGenerationPercent.Value, 50, 1000, GUILayout.Width(280));
            powerGenerationInput = GUILayout.TextField(powerGenerationInput, GUILayout.Width(50));
            if (GUILayout.Button("Set", GUILayout.Width(40)))
            {
                if (int.TryParse(powerGenerationInput, out int val))
                {
                    PowerGenerationPercent.Value = Mathf.Clamp(val, 50, 1000);
                }
                powerGenerationInput = PowerGenerationPercent.Value.ToString();
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private void DrawSpecialTab()
        {
            GUILayout.Label("Special Cheats", headerStyle);
            GUILayout.Space(5);

            GUILayout.BeginVertical(boxStyle);

            GUI.color = CatSounds.Value ? Color.yellow : Color.white;
            CatSounds.Value = GUILayout.Toggle(CatSounds.Value, " 🐱 Cat Sounds");
            GUI.color = Color.white;
            GUILayout.Label("  Replace machine sounds with cat meowing", GUI.skin.label);
            GUILayout.Label("  Uses FMOD 'Cat Cheat' parameter", GUI.skin.label);
            GUILayout.Space(10);

            GUI.color = RainbowCores.Value ? Color.magenta : Color.white;
            RainbowCores.Value = GUILayout.Toggle(RainbowCores.Value, " 🌈 Rainbow Cores");
            GUI.color = Color.white;
            GUILayout.Label("  Memory Tree cores cycle through rainbow colors", GUI.skin.label);
            if (RainbowCores.Value)
            {
                // Show current rainbow color preview
                Color preview = GetRainbowColor();
                GUILayout.BeginHorizontal();
                GUILayout.Label("  Current color: ");
                GUI.color = preview;
                GUILayout.Box("", GUILayout.Width(50), GUILayout.Height(20));
                GUI.color = Color.white;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(10);

            GUI.color = PlaceholderVoice.Value ? Color.cyan : Color.white;
            PlaceholderVoice.Value = GUILayout.Toggle(PlaceholderVoice.Value, " 🎤 Placeholder Voice");
            GUI.color = Color.white;
            GUILayout.Label("  Enable developer placeholder voice lines", GUI.skin.label);
            GUILayout.Label("  Hear the original temp audio recordings!", GUI.skin.label);

            GUILayout.Space(20);

            // Status info
            GUILayout.Label("Status", headerStyle);
            GUILayout.Space(5);

            GUILayout.Label($"Player Cheats: {(isInitialized ? "Active ✓" : "Waiting...")}");
            GUILayout.Label($"Game Mode Settings: {(settingsApplied ? "Applied ✓" : "Waiting...")}");
            if (RainbowCores.Value)
            {
                GUILayout.Label($"Rainbow Hue: {rainbowHue:F2}");
            }

            GUILayout.EndVertical();
        }

        private void ApplyPlayerCheats()
        {
            var cheats = Player.instance?.cheats;
            if (cheats == null) return;

            cheats.infiniteCrafting = InfiniteCrafting.Value;
            cheats.maxPower = MaxPower.Value;
            cheats.ultraPickaxe = UltraPickaxe.Value;
            cheats.fasterMole = FasterMole.Value;
            cheats.disableEncumbrance = DisableEncumbrance.Value;
            cheats.showDebugCoords = ShowDebugCoords.Value;
            cheats.hideMachineLights = HideMachineLights.Value;
            cheats.hideMachineParticles = HideMachineParticles.Value;
            cheats.simSpeed = SimSpeed.Value;
            cheats.freeCameraMode = (PlayerCheats.FreeCameraMode)FreeCameraMode.Value;
        }

        private void ApplyGameModeSettings()
        {
            try
            {
                ref GameInstanceSettings settings = ref GameState.instance.gameModeSettings;
                if (settings.Values == null) return;

                // Apply boolean settings
                SetBoolSetting(ref settings, EGameModeSettingType.General_AllDoorsUnlocked, AllDoorsUnlocked.Value);
                SetBoolSetting(ref settings, EGameModeSettingType.General_CatSounds, CatSounds.Value);
                SetBoolSetting(ref settings, EGameModeSettingType.Player_MoleBitsAlwaysAvailable, MoleBitsAlwaysAvailable.Value);
                SetBoolSetting(ref settings, EGameModeSettingType.World_InfiniteOre, InfiniteOre.Value);

                // Apply percentage settings
                SetIntSetting(ref settings, EGameModeSettingType.Player_BaseSpeedMultiplier, PlayerSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Player_MoleSpeedMultiplier, MoleSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.General_FuelConsumptionMultiplier, FuelConsumptionPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.General_PowerConsumptionMultiplier, PowerConsumptionPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.General_PowerGenerationMultiplier, PowerGenerationPercent.Value);

                // Machine speed settings
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Smelter1SpeedMultiplier, SmelterSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Smelter2SpeedMultiplier, SmelterSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Assembler1SpeedMultiplier, AssemblerSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Assembler2SpeedMultiplier, AssemblerSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Thresher1SpeedMultiplier, ThresherSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_Thresher2SpeedMultiplier, ThresherSpeedPercent.Value);
                SetIntSetting(ref settings, EGameModeSettingType.Machines_PlanterSpeedMultiplier, PlanterSpeedPercent.Value);

                // Inserter settings
                SetIntSetting(ref settings, EGameModeSettingType.Machines_InsertersBaseStackSize, InserterBaseStackSize.Value);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to apply game mode settings: {ex.Message}");
            }
        }

        private static void SetBoolSetting(ref GameInstanceSettings settings, EGameModeSettingType settingType, bool value)
        {
            try
            {
                int index = (int)settingType;
                if (index >= 0 && index < settings.Values.Length)
                {
                    settings.Values[index].BoolValue = value;
                }
            }
            catch { }
        }

        private static void SetIntSetting(ref GameInstanceSettings settings, EGameModeSettingType settingType, int value)
        {
            try
            {
                int index = (int)settingType;
                if (index >= 0 && index < settings.Values.Length)
                {
                    settings.Values[index].IntValue = value;
                }
            }
            catch { }
        }

        /// <summary>
        /// Apply Cat Sounds via FMOD global parameter
        /// The game has a "Cat Cheat" FMOD parameter that replaces sounds with cat meows
        /// </summary>
        private static void ApplyCatSounds()
        {
            try
            {
                // Set the FMOD global parameter for cat sounds
                // Parameter name is "Cat Cheat" - value 1.0 = enabled, 0.0 = disabled
                FMODUnity.RuntimeManager.StudioSystem.setParameterByName("Cat Cheat", CatSounds.Value ? 1f : 0f);
            }
            catch
            {
                // FMOD may not be initialized yet - silently ignore
            }
        }

        /// <summary>
        /// Update rainbow cores effect - cycles Memory Tree cores through rainbow colors
        /// </summary>
        private void UpdateRainbowCores()
        {
            rainbowHue += Time.deltaTime * 0.2f; // Slow rainbow cycle
            if (rainbowHue > 1f) rainbowHue = 0f;

            // Rainbow effect is applied via Harmony patch on MemoryTreeInstance
        }

        public static Color GetRainbowColor()
        {
            return Color.HSVToRGB(rainbowHue, 1f, 1f);
        }

        public static float GetRainbowHue()
        {
            return rainbowHue;
        }
    }

    /// <summary>
    /// Harmony patches for special cheats
    /// </summary>
    [HarmonyPatch]
    internal static class DevToolsPatches
    {
        /// <summary>
        /// Rainbow Cores - Apply rainbow color to Memory Tree core visuals
        /// </summary>
        [HarmonyPatch(typeof(MemoryTreeInstance), nameof(MemoryTreeInstance.SimUpdate))]
        [HarmonyPostfix]
        private static void ApplyRainbowCores(ref MemoryTreeInstance __instance)
        {
            if (!DevToolsPlugin.RainbowCores.Value) return;

            try
            {
                // Get the visual component
                var visual = __instance.commonInfo.refGameObj;
                if (visual == null) return;

                // Find core renderers and apply rainbow color
                var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
                Color rainbow = DevToolsPlugin.GetRainbowColor();

                foreach (var renderer in renderers)
                {
                    // Only affect core-like materials (emissive parts)
                    var materials = renderer.materials;
                    foreach (var mat in materials)
                    {
                        if (mat.HasProperty("_EmissionColor"))
                        {
                            mat.EnableKeyword("_EMISSION");
                            mat.SetColor("_EmissionColor", rainbow * 2f);
                        }
                    }
                }
            }
            catch
            {
                // Silently ignore
            }
        }

        // Note: Placeholder Voice patch disabled - DialogueManager may not be available
        // The feature is still configurable but requires manual patch verification

        // Note: OnGameModeSettingsChanged patch removed - method doesn't exist in current game version
        // Cat sounds are applied directly in Update() via ApplyCatSounds()
    }
}
