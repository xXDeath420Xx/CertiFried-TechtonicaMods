using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeamlessWorld.Patches
{
    /// <summary>
    /// Patches machine systems to work across all loaded strata simultaneously
    /// </summary>
    [HarmonyPatch]
    public static class MachinePatches
    {
        /// <summary>
        /// Initialize machine patches - called from plugin Awake
        /// </summary>
        public static void ApplyManualPatches(Harmony harmony)
        {
            // FlagForStrataChange may not exist in all game versions
            TryApplyPatch(harmony, typeof(GameState), "FlagForStrataChange",
                nameof(FlagForStrataChange_Prefix), "FlagForStrataChange");

            // MachineManager.UpdateAll - the actual machine update method (SimUpdate doesn't exist)
            TryApplyPatch(harmony, typeof(MachineManager), "UpdateAll",
                nameof(UpdateAll_Prefix), "MachineManager.UpdateAll");
        }

        private static void TryApplyPatch(Harmony harmony, Type targetType, string methodName,
            string prefixMethodName, string patchDescription)
        {
            try
            {
                var targetMethod = targetType.GetMethod(methodName,
                    BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

                if (targetMethod != null)
                {
                    var prefix = typeof(MachinePatches).GetMethod(prefixMethodName,
                        BindingFlags.Public | BindingFlags.Static);
                    harmony.Patch(targetMethod, prefix: new HarmonyMethod(prefix));
                    SeamlessWorldPlugin.Log.LogInfo($"Applied {patchDescription} patch");
                }
                else
                {
                    SeamlessWorldPlugin.Log.LogInfo($"{patchDescription} method not found - skipping patch");
                }
            }
            catch (Exception ex)
            {
                SeamlessWorldPlugin.Log.LogWarning($"Could not apply {patchDescription} patch: {ex.Message}");
            }
        }

        /// <summary>
        /// Prevent machines from being flagged inactive when strata changes
        /// NOTE: This patch is applied manually in ApplyManualPatches if the method exists
        /// </summary>
        public static bool FlagForStrataChange_Prefix()
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true; // Run original

            // In seamless mode, don't flag anything - all strata machines stay active
            SeamlessWorldPlugin.LogDebug("Skipping FlagForStrataChange - seamless mode active");
            return false; // Skip original
        }

        /// <summary>
        /// Keep all machines updating regardless of strata
        /// This method is called via manual patching in ApplyManualPatches
        /// Method signature: public void UpdateAll(bool isRefresh = true, bool isIncrement = true)
        /// </summary>
        public static void UpdateAll_Prefix(MachineManager __instance)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Ensure all machines from all loaded strata are being simulated
            // The original only simulates machines in current strata
            // TODO: Implement cross-strata machine simulation
        }

        /// <summary>
        /// Make machine world positions use unified coordinates
        /// </summary>
        [HarmonyPatch(typeof(GenericMachineInstanceRef), "GetWorldPosition")]
        [HarmonyPostfix]
        public static void GetWorldPosition_Postfix(
            GenericMachineInstanceRef __instance,
            ref Vector3 __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Get the machine's strata and apply offset
            // TODO: Need to track which strata each machine belongs to
        }

        /// <summary>
        /// Fix conveyor connections across strata boundaries
        /// </summary>
        [HarmonyPatch(typeof(ConveyorInstance), "TryGetOutputInventory")]
        [HarmonyPostfix]
        public static void ConveyorOutput_Postfix(ConveyorInstance __instance, ref Inventory __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Allow conveyors to connect to machines in different strata
            // This enables vertical factory designs
            // TODO: Implement cross-strata conveyor connections
        }
    }

    /// <summary>
    /// Patches for power network to span all strata
    /// </summary>
    [HarmonyPatch]
    public static class PowerNetworkPatches
    {
        /// <summary>
        /// Allow high voltage cables to connect across strata boundaries
        /// </summary>
        [HarmonyPatch(typeof(HighVoltageCableInstance), "CanConnectTo")]
        [HarmonyPostfix]
        public static void CanConnectTo_Postfix(
            HighVoltageCableInstance __instance,
            ref bool __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // If connection was denied due to strata mismatch, override
            // TODO: Implement cross-strata power connections
        }

        /// <summary>
        /// Unify power grids across all strata
        /// </summary>
        [HarmonyPatch(typeof(PowerNetwork), "Tick")]
        [HarmonyPrefix]
        public static void PowerNetworkTick_Prefix(PowerNetwork __instance)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Process power networks from all loaded strata
            // TODO: Merge power network handling
        }
    }
}
