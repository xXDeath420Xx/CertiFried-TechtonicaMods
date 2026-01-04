using System;
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
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

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
        public static ConfigEntry<bool> CatSounds;
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

        private static bool isInitialized = false;
        private static bool settingsApplied = false;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

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

            // Bind Game Mode Settings configs (as percentages since game uses int values * 0.01)
            AllDoorsUnlocked = Config.Bind("GameModeSettings", "All Doors Unlocked", false,
                "Unlock all doors in the game");
            CatSounds = Config.Bind("GameModeSettings", "Cat Sounds", false,
                "Replace sounds with meowing cat sounds");
            MoleBitsAlwaysAvailable = Config.Bind("GameModeSettings", "MOLE Bits Always Available", false,
                "All MOLE drill bits are always accessible");
            InfiniteOre = Config.Bind("GameModeSettings", "Infinite Ore", false,
                "Ore veins never deplete");
            PlayerSpeedPercent = Config.Bind("GameModeSettings", "Player Speed Percent", 100,
                new ConfigDescription("Player movement speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            MoleSpeedPercent = Config.Bind("GameModeSettings", "MOLE Speed Percent", 100,
                new ConfigDescription("MOLE digging speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));

            // Machine speed multipliers (as percentages)
            SmelterSpeedPercent = Config.Bind("MachineSettings", "Smelter Speed Percent", 100,
                new ConfigDescription("Smelter crafting speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            AssemblerSpeedPercent = Config.Bind("MachineSettings", "Assembler Speed Percent", 100,
                new ConfigDescription("Assembler crafting speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            ThresherSpeedPercent = Config.Bind("MachineSettings", "Thresher Speed Percent", 100,
                new ConfigDescription("Thresher processing speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));
            PlanterSpeedPercent = Config.Bind("MachineSettings", "Planter Speed Percent", 100,
                new ConfigDescription("Planter growth speed percentage (100 = normal)", new AcceptableValueRange<int>(50, 1000)));

            // Power settings (as percentages)
            FuelConsumptionPercent = Config.Bind("PowerSettings", "Fuel Consumption Percent", 100,
                new ConfigDescription("Fuel consumption percentage (lower = less fuel used)", new AcceptableValueRange<int>(10, 500)));
            PowerConsumptionPercent = Config.Bind("PowerSettings", "Power Consumption Percent", 100,
                new ConfigDescription("Power consumption percentage (lower = less power used)", new AcceptableValueRange<int>(10, 500)));
            PowerGenerationPercent = Config.Bind("PowerSettings", "Power Generation Percent", 100,
                new ConfigDescription("Power generation percentage (higher = more power)", new AcceptableValueRange<int>(50, 1000)));

            // Inserter settings
            InserterBaseStackSize = Config.Bind("MachineSettings", "Inserter Base Stack Size", 1,
                new ConfigDescription("Base stack size for inserters", new AcceptableValueRange<int>(1, 100)));

            // Apply patches
            Harmony.PatchAll();

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void Update()
        {
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

            // Apply game mode settings once GameState is available
            if (!settingsApplied && GameState.instance != null)
            {
                ApplyGameModeSettings();
                settingsApplied = true;
                Log.LogInfo("DevTools applied game mode settings");
            }
        }

        private void ApplyPlayerCheats()
        {
            var cheats = Player.instance?.cheats;
            if (cheats == null) return;

            // Apply PlayerCheats settings
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

                // Apply percentage settings (game stores as int, then multiplies by 0.01 to get percentage)
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
    }
}
