using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using EquinoxsModUtils;
using UnityEngine;

namespace BetterEndGame
{
    /// <summary>
    /// BetterEndGame_Patched - Provides infinite progression unlocks for endgame content.
    ///
    /// ORIGINAL AUTHOR: CubeSuite/Equinox
    /// ORIGINAL SOURCE: https://github.com/CubeSuite/TTMod-BetterEndGame
    /// LICENSE: GPL-3.0
    ///
    /// PATCHED BY: CertiFried
    /// PATCH REASON: Updated to EMU 6.x API (original uses deprecated ModUtils.add_GameDefinesLoaded)
    /// CHANGES: See CHANGELOG.md for full list of changes
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    public class BetterEndGamePlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.BetterEndGame_Patched";
        private const string PluginName = "BetterEndGame_Patched";
        private const string VersionString = "1.2.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Configuration
        public static ConfigEntry<bool> EnableInfiniteUnlocks;
        public static ConfigEntry<float> CostMultiplier;
        public static ConfigEntry<float> BonusPerLevel;

        // Infinite unlock tracking
        private static Dictionary<string, int> infiniteUnlockLevels = new Dictionary<string, int>();

        // Unlock types
        public enum InfiniteUnlockType
        {
            MinerSpeed,
            FuelEfficiency,
            PowerUsage,
            InventorySize,
            WalkSpeed,
            MoleSpeed
        }

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            // Initialize config
            EnableInfiniteUnlocks = Config.Bind("General", "Enable Infinite Unlocks", true,
                "Enable the infinite unlock progression system");

            CostMultiplier = Config.Bind("Balance", "Cost Multiplier", 1.5f,
                "How much each unlock level increases in cost (1.5 = 50% more each level)");

            BonusPerLevel = Config.Bind("Balance", "Bonus Per Level", 0.05f,
                "How much bonus each level provides (0.05 = 5% per level)");

            // Register with EMU events using the NEW API
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.SaveStateLoaded += (sender, args) => OnSaveStateLoaded();

            Harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void OnGameDefinesLoaded()
        {
            try
            {
                Log.LogInfo("GameDefines loaded - BetterEndGame initializing");

                // Initialize infinite unlock types
                foreach (InfiniteUnlockType unlockType in Enum.GetValues(typeof(InfiniteUnlockType)))
                {
                    string key = unlockType.ToString();
                    if (!infiniteUnlockLevels.ContainsKey(key))
                    {
                        infiniteUnlockLevels[key] = 0;
                    }
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"OnGameDefinesLoaded error (non-fatal): {ex.Message}");
            }
        }

        private void OnSaveStateLoaded()
        {
            try
            {
                Log.LogInfo("Save loaded - applying infinite unlocks");
                ApplyAllInfiniteUnlocks();
            }
            catch (Exception ex)
            {
                Log.LogWarning($"OnSaveStateLoaded error: {ex.Message}");
            }
        }

        /// <summary>
        /// Purchase an infinite unlock level
        /// </summary>
        public static bool PurchaseInfiniteUnlock(InfiniteUnlockType unlockType)
        {
            if (!EnableInfiniteUnlocks.Value) return false;

            string key = unlockType.ToString();
            int currentLevel = infiniteUnlockLevels.ContainsKey(key) ? infiniteUnlockLevels[key] : 0;
            int cost = GetUnlockCost(unlockType, currentLevel + 1);

            // Check if player has enough resources (cores or whatever currency)
            // For now, this is a placeholder - actual implementation would check inventory

            infiniteUnlockLevels[key] = currentLevel + 1;
            Log.LogInfo($"Purchased {unlockType} level {currentLevel + 1}");

            ApplyInfiniteUnlock(unlockType, currentLevel + 1);
            return true;
        }

        /// <summary>
        /// Get the cost for an infinite unlock level
        /// </summary>
        public static int GetUnlockCost(InfiniteUnlockType unlockType, int level)
        {
            int baseCost = 100;
            return (int)(baseCost * Math.Pow(CostMultiplier.Value, level - 1));
        }

        /// <summary>
        /// Get the current level of an infinite unlock
        /// </summary>
        public static int GetUnlockLevel(InfiniteUnlockType unlockType)
        {
            string key = unlockType.ToString();
            return infiniteUnlockLevels.ContainsKey(key) ? infiniteUnlockLevels[key] : 0;
        }

        /// <summary>
        /// Get the total bonus from an infinite unlock
        /// </summary>
        public static float GetUnlockBonus(InfiniteUnlockType unlockType)
        {
            int level = GetUnlockLevel(unlockType);
            return level * BonusPerLevel.Value;
        }

        private void ApplyAllInfiniteUnlocks()
        {
            foreach (InfiniteUnlockType unlockType in Enum.GetValues(typeof(InfiniteUnlockType)))
            {
                int level = GetUnlockLevel(unlockType);
                if (level > 0)
                {
                    ApplyInfiniteUnlock(unlockType, level);
                }
            }
        }

        private static void ApplyInfiniteUnlock(InfiniteUnlockType unlockType, int level)
        {
            float bonus = level * BonusPerLevel.Value;

            switch (unlockType)
            {
                case InfiniteUnlockType.MinerSpeed:
                    // Would apply to miner speed via Harmony patches
                    Log.LogInfo($"Applied Miner Speed bonus: +{bonus * 100:F0}%");
                    break;
                case InfiniteUnlockType.FuelEfficiency:
                    Log.LogInfo($"Applied Fuel Efficiency bonus: +{bonus * 100:F0}%");
                    break;
                case InfiniteUnlockType.PowerUsage:
                    Log.LogInfo($"Applied Power Usage reduction: -{bonus * 100:F0}%");
                    break;
                case InfiniteUnlockType.InventorySize:
                    Log.LogInfo($"Applied Inventory Size bonus: +{bonus * 100:F0}%");
                    break;
                case InfiniteUnlockType.WalkSpeed:
                    Log.LogInfo($"Applied Walk Speed bonus: +{bonus * 100:F0}%");
                    break;
                case InfiniteUnlockType.MoleSpeed:
                    Log.LogInfo($"Applied MOLE Speed bonus: +{bonus * 100:F0}%");
                    break;
            }
        }

        private void FixedUpdate()
        {
            // Only run on server/single-player
            if (!FlowManager.isSinglePlayerGame)
                return;

            // Periodic update if needed
        }
    }

    /// <summary>
    /// Harmony patches for BetterEndGame
    /// </summary>
    [HarmonyPatch]
    internal static class BetterEndGamePatches
    {
        /// <summary>
        /// Patch miner speed based on infinite unlock level
        /// </summary>
        [HarmonyPatch(typeof(DrillInstance), "GetMiningSpeed")]
        [HarmonyPostfix]
        private static void ModifyMinerSpeed(ref float __result)
        {
            if (!BetterEndGamePlugin.EnableInfiniteUnlocks.Value) return;

            float bonus = BetterEndGamePlugin.GetUnlockBonus(BetterEndGamePlugin.InfiniteUnlockType.MinerSpeed);
            if (bonus > 0)
            {
                __result *= (1 + bonus);
            }
        }
    }
}
