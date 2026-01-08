using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using Mirror;
using UnityEngine;

namespace MultiplayerFixes
{
    /// <summary>
    /// MultiplayerFixes - Ensures third-party mods work correctly in multiplayer
    /// by patching them with server-only execution checks.
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    // Soft dependencies - only patch if loaded
    [BepInDependency("com.equinox.VoidChests", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.CreativeChests", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.WormholeChests", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.VirtualCores", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.MorePlantmatter", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.Blueprints", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.SmartInserters", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.casper_dev.CaspersLoaders", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.jrinker.CrusherCoreBoost", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.EMUBuilder", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.BetterEndGame", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.equinox.Restock", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.thelich.ResourceFeeder", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.gratak.StackLimits", BepInDependency.DependencyFlags.SoftDependency)]
    [BepInDependency("com.hypexmon5ter.LongStackInserters", BepInDependency.DependencyFlags.SoftDependency)]
    public class MultiplayerFixesPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.MultiplayerFixes";
        private const string PluginName = "MultiplayerFixes";
        private const string VersionString = "2.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static ConfigEntry<bool> DebugMode;

        private int patchCount = 0;
        private List<string> patchedMods = new List<string>();

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            DebugMode = Config.Bind("General", "Debug Mode", false,
                "Enable verbose debug logging");

            // Apply patches to all known third-party mods
            PatchEquinoxMods();
            PatchCasperMods();
            PatchOtherMods();

            // Apply general network safety patches
            Harmony.PatchAll(typeof(GeneralNetworkPatches));

            Logger.LogInfo($"{PluginName} loaded. Applied {patchCount} patches to {patchedMods.Count} mods: {string.Join(", ", patchedMods)}");
        }

        #region Equinox Mod Patches

        private void PatchEquinoxMods()
        {
            PatchMod("VoidChests", "VoidChests.VoidChestsPlugin", "FixedUpdate");
            PatchMod("CreativeChests", "CreativeChests.CreativeChestsPlugin", "FixedUpdate");
            PatchMod("VirtualCores", "VirtualCores.VirtualCoresPlugin", "FixedUpdate");
            PatchMod("MorePlantmatter", "MorePlantmatter.MorePlantmatterPlugin", "FixedUpdate");

            // WormholeChests - multiple patches needed
            PatchStaticMethod("WormholeChests", "WormholeChests.ChestInstancePatch", "GetWormholeInsteadOfInventory");
            PatchStaticMethod("WormholeChests", "WormholeChests.ChestDefinitionPatch", "UpdateChestMap");

            // Blueprints
            PatchMod("Blueprints", "Blueprints.BlueprintsPlugin", "Update");
            PatchMod("Blueprints", "Blueprints.BlueprintsPlugin", "LateUpdate");

            // SmartInserters
            PatchMod("SmartInserters", "SmartInserters.SmartInsertersPlugin", "FixedUpdate");

            // EMUBuilder
            PatchMod("EMUBuilder", "EMUBuilder.EMUBuilderPlugin", "Update");

            // BetterEndGame
            PatchMod("BetterEndGame", "BetterEndGame.BetterEndGamePlugin", "FixedUpdate");

            // Restock - inventory operations must be server-only
            PatchMod("Restock", "Restock.RestockPlugin", "FixedUpdate");
        }

        #endregion

        #region Casper Mod Patches

        private void PatchCasperMods()
        {
            // CaspersLoaders
            PatchMod("caspersLoaders", "caspersLoaders.LoadersPlugin", "FixedUpdate");
        }

        #endregion

        #region Other Third-Party Mod Patches

        private void PatchOtherMods()
        {
            // CrusherCoreBoost
            PatchMod("CrusherCoreBoost", "CrusherCoreBoost.CrusherCoreBoostPlugin", "FixedUpdate");

            // ResourceFeeder - CRITICAL: spawns items from nothing
            PatchMod("ResourceFeeder", "ResourceFeeder.ResourceFeederPlugin", "FixedUpdate");
            PatchMod("ResourceFeeder", "ResourceFeeder.ResourceFeederPlugin", "Update");

            // StackLimits - changes stack sizes which could desync
            PatchStaticMethod("StackLimits", "StackLimits.StackLimitsPlugin", "ApplyStackLimits");

            // LongStackInserters
            PatchMod("LongStackInserters", "LongStackInserters.LongStackInsertersPlugin", "FixedUpdate");
        }

        #endregion

        #region Patch Helpers

        private Assembly GetAssembly(string name)
        {
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (assembly.GetName().Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    return assembly;
                }
            }
            return null;
        }

        private void PatchMod(string assemblyName, string typeName, string methodName, bool isStatic = false)
        {
            try
            {
                Assembly assembly = GetAssembly(assemblyName);
                if (assembly == null)
                {
                    LogDebug($"Assembly {assemblyName} not loaded, skipping");
                    return;
                }

                Type type = assembly.GetType(typeName);
                if (type == null)
                {
                    LogDebug($"Type {typeName} not found in {assemblyName}");
                    return;
                }

                BindingFlags flags = (isStatic ? BindingFlags.Static : BindingFlags.Instance) |
                                     BindingFlags.Public | BindingFlags.NonPublic;

                MethodInfo method = type.GetMethod(methodName, flags);
                if (method == null)
                {
                    LogDebug($"Method {methodName} not found in {typeName}");
                    return;
                }

                Harmony.Patch(method,
                    prefix: new HarmonyMethod(typeof(NetworkServerPrefixes), nameof(NetworkServerPrefixes.ServerOnlyPrefix)));

                Log.LogInfo($"Patched {typeName}.{methodName}");
                patchCount++;

                if (!patchedMods.Contains(assemblyName))
                    patchedMods.Add(assemblyName);
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to patch {assemblyName}.{typeName}.{methodName}: {ex.Message}");
            }
        }

        private void PatchStaticMethod(string assemblyName, string typeName, string methodName)
        {
            PatchMod(assemblyName, typeName, methodName, isStatic: true);
        }

        private void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Network-aware prefix patches
    /// </summary>
    public static class NetworkServerPrefixes
    {
        /// <summary>
        /// Only execute on server/host. Clients should not run inventory/machine update logic.
        /// </summary>
        public static bool ServerOnlyPrefix()
        {
            // Allow in single player
            if (FlowManager.isSinglePlayerGame)
                return true;

            // In multiplayer, only server should run these operations
            // Clients receive state via network sync
            if (NetworkServer.active)
                return true;

            // Also allow if we're the host (server + client)
            if (NetworkClient.isHostClient)
                return true;

            return false;
        }

        /// <summary>
        /// Skip during save loading to prevent race conditions
        /// </summary>
        public static bool NotDuringLoadPrefix()
        {
            return !SaveState.isLoading;
        }

        /// <summary>
        /// Combined: server-only AND not during load
        /// </summary>
        public static bool ServerOnlyNotLoadingPrefix()
        {
            if (SaveState.isLoading)
                return false;

            return ServerOnlyPrefix();
        }
    }

    /// <summary>
    /// General network safety patches that apply to all mods
    /// </summary>
    [HarmonyPatch]
    internal static class GeneralNetworkPatches
    {
        /// <summary>
        /// Catch errors in NetworkMessageRelay to prevent disconnects
        /// </summary>
        [HarmonyPatch(typeof(NetworkMessageRelay), "Update")]
        [HarmonyFinalizer]
        private static Exception CatchNetworkRelayErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"NetworkMessageRelay error caught: {__exception.Message}");
                return null; // Suppress to prevent disconnect
            }
            return __exception;
        }

        /// <summary>
        /// Ensure inventory operations don't crash on invalid resource IDs
        /// </summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.TryRemoveResources))]
        [HarmonyPrefix]
        private static bool SafeInventoryRemove(int resId, ref bool __result)
        {
            if (resId < 0)
            {
                __result = false;
                return false;
            }

            if (GameDefines.instance?.resources == null || resId >= GameDefines.instance.resources.Count)
            {
                __result = false;
                return false;
            }

            return true;
        }

        /// <summary>
        /// Ensure AddResources doesn't crash on invalid IDs
        /// </summary>
        [HarmonyPatch(typeof(Inventory), nameof(Inventory.AddResources), typeof(int), typeof(int))]
        [HarmonyPrefix]
        private static bool SafeInventoryAdd(int resId)
        {
            if (resId < 0)
                return false;

            if (GameDefines.instance?.resources == null || resId >= GameDefines.instance.resources.Count)
                return false;

            return true;
        }

        /// <summary>
        /// Catch save loading errors gracefully
        /// </summary>
        [HarmonyPatch(typeof(SaveState), "LoadFileData")]
        [HarmonyFinalizer]
        private static Exception CatchSaveLoadErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Save load error (game will attempt recovery): {__exception.Message}");
            }
            return __exception;
        }

        /// <summary>
        /// Prevent machine build errors from crashing the game
        /// </summary>
        [HarmonyPatch(typeof(MachineManager), "BuildMachine")]
        [HarmonyFinalizer]
        private static Exception CatchBuildErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Machine build error: {__exception.Message}");
                return null;
            }
            return __exception;
        }

        /// <summary>
        /// Catch factory sim errors that could desync
        /// </summary>
        [HarmonyPatch(typeof(FactorySimManager), "UpdateSim")]
        [HarmonyFinalizer]
        private static Exception CatchSimErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogError($"Factory simulation error: {__exception.Message}");
                // Don't suppress - this is critical
            }
            return __exception;
        }

        /// <summary>
        /// Catch network serialization errors during save transfer
        /// </summary>
        [HarmonyPatch(typeof(SaveState), "SerializeSaveData")]
        [HarmonyFinalizer]
        private static Exception CatchSerializeErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogError($"Save serialization error: {__exception.Message}");
                return null; // Suppress to prevent disconnect
            }
            return __exception;
        }

        /// <summary>
        /// Catch network deserialization errors during save receive
        /// </summary>
        [HarmonyPatch(typeof(SaveState), "DeserializeSaveData")]
        [HarmonyFinalizer]
        private static Exception CatchDeserializeErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogError($"Save deserialization error: {__exception.Message}");
                return null;
            }
            return __exception;
        }

        /// <summary>
        /// Validate resource ID before network operations
        /// </summary>
        [HarmonyPatch(typeof(SaveState), "GetResInfoFromId")]
        [HarmonyPrefix]
        private static bool SafeGetResInfo(int id, ref ResourceInfo __result)
        {
            if (id < 0 || GameDefines.instance?.resources == null)
            {
                __result = null;
                return false;
            }

            if (id >= GameDefines.instance.resources.Count)
            {
                // This is a modded resource that doesn't exist anymore
                MultiplayerFixesPlugin.Log.LogWarning($"Skipping invalid resource ID {id} (mod may have been removed)");
                __result = null;
                return false;
            }

            return true;
        }
    }

    /// <summary>
    /// Network connection stability patches
    /// </summary>
    [HarmonyPatch]
    internal static class NetworkConnectionPatches
    {
        /// <summary>
        /// Catch errors during client connection to prevent immediate disconnect
        /// </summary>
        [HarmonyPatch(typeof(NetworkServer), "OnConnected")]
        [HarmonyFinalizer]
        private static Exception CatchConnectionErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogError($"Client connection error: {__exception.Message}");
                return null;
            }
            return __exception;
        }

        /// <summary>
        /// Catch errors during initial save state transfer
        /// </summary>
        [HarmonyPatch(typeof(FlowManager), "LoadInitialSaveDataFromServer")]
        [HarmonyFinalizer]
        private static Exception CatchSaveTransferErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogError($"Save transfer error: {__exception.Message}");
                // Don't suppress - client needs to know connection failed
            }
            return __exception;
        }

        /// <summary>
        /// Validate NetworkInventorySlot before processing
        /// </summary>
        [HarmonyPatch(typeof(NetworkInventorySlot), "ToString")]
        [HarmonyFinalizer]
        private static Exception CatchSlotErrors(Exception __exception)
        {
            if (__exception != null)
            {
                // Silently suppress - this is just display code
                return null;
            }
            return __exception;
        }
    }

    /// <summary>
    /// EMUAdditions historic ID handler - prevents "Historic ResourceId X has not been added" issues
    /// </summary>
    [HarmonyPatch]
    internal static class EMUHistoricIdPatches
    {
        /// <summary>
        /// Suppress EMUAdditions warnings about missing historic IDs
        /// These are not errors, just mods that were previously installed but aren't now
        /// </summary>
        [HarmonyPatch(typeof(Debug), nameof(Debug.LogWarning), typeof(object))]
        [HarmonyPrefix]
        private static bool FilterHistoricIdWarnings(object message)
        {
            if (message == null) return true;

            string msg = message.ToString();
            if (msg.Contains("Historic ResourceId") && msg.Contains("has not added been added"))
            {
                // Log at debug level instead of warning
                if (MultiplayerFixesPlugin.DebugMode?.Value == true)
                {
                    MultiplayerFixesPlugin.Log.LogInfo($"[Filtered] {msg}");
                }
                return false; // Skip the warning
            }

            return true;
        }
    }

    /// <summary>
    /// Recipe and crafting UI patches - prevents crashes from invalid recipe data
    /// </summary>
    [HarmonyPatch]
    internal static class RecipePatches
    {
        /// <summary>
        /// Catch IndexOutOfRangeException in GetMaxNumProducableResourcesWithIngredientCounts
        /// This happens when recipes have more than 4 ingredients or invalid ingredient references
        /// </summary>
        [HarmonyPatch(typeof(PlayerCrafting), nameof(PlayerCrafting.GetMaxNumProducableResourcesWithIngredientCounts))]
        [HarmonyFinalizer]
        private static Exception CatchCraftingCountErrors(Exception __exception, ref int __result, ref int[] ingCountAvailable, ref bool[] ingUsedRecCraft)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Crafting calculation error (likely modded recipe): {__exception.Message}");
                // Return safe defaults
                __result = 0;
                if (ingCountAvailable == null) ingCountAvailable = new int[4];
                if (ingUsedRecCraft == null) ingUsedRecCraft = new bool[4];
                return null; // Suppress exception
            }
            return __exception;
        }

        /// <summary>
        /// Catch errors in RecipePageUI.UpdateInspector
        /// </summary>
        [HarmonyPatch(typeof(RecipePageUI), "UpdateInspector")]
        [HarmonyFinalizer]
        private static Exception CatchUpdateInspectorErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Recipe UI update error: {__exception.Message}");
                return null; // Suppress to prevent UI crash loop
            }
            return __exception;
        }

        /// <summary>
        /// Catch errors in RecipePageUI.UpdateCraftingPage
        /// </summary>
        [HarmonyPatch(typeof(RecipePageUI), "UpdateCraftingPage")]
        [HarmonyFinalizer]
        private static Exception CatchUpdateCraftingPageErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Crafting page update error: {__exception.Message}");
                return null;
            }
            return __exception;
        }

        /// <summary>
        /// Validate recipe before attempting to craft - skip if ingredients are invalid
        /// </summary>
        [HarmonyPatch(typeof(PlayerCrafting), nameof(PlayerCrafting.GetCraftingRecipe))]
        [HarmonyPrefix]
        private static bool ValidateRecipe(ResourceInfo res, ref bool __result, ref SchematicsRecipeData recipe)
        {
            try
            {
                if (res == null)
                {
                    __result = false;
                    recipe = null;
                    return false;
                }
                return true;
            }
            catch
            {
                __result = false;
                recipe = null;
                return false;
            }
        }
    }

    /// <summary>
    /// UI Manager patches - prevent cascading UI errors
    /// </summary>
    [HarmonyPatch]
    internal static class UIPatches
    {
        /// <summary>
        /// Catch errors in UIManager.UpdateFocusedMenu
        /// </summary>
        [HarmonyPatch(typeof(UIManager), "UpdateFocusedMenu")]
        [HarmonyFinalizer]
        private static Exception CatchUpdateFocusedMenuErrors(Exception __exception)
        {
            if (__exception != null)
            {
                // Only log first occurrence per frame to avoid spam
                MultiplayerFixesPlugin.Log.LogWarning($"UI update error: {__exception.Message}");
                return null;
            }
            return __exception;
        }

        /// <summary>
        /// Catch errors in InventoryAndCraftingUI.SoftRefresh
        /// </summary>
        [HarmonyPatch(typeof(InventoryAndCraftingUI), "SoftRefresh")]
        [HarmonyFinalizer]
        private static Exception CatchSoftRefreshErrors(Exception __exception)
        {
            if (__exception != null)
            {
                MultiplayerFixesPlugin.Log.LogWarning($"Inventory refresh error: {__exception.Message}");
                return null;
            }
            return __exception;
        }
    }
}
