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
        /// Prevent machines from being flagged inactive when strata changes
        /// </summary>
        [HarmonyPatch(typeof(GameState), "FlagForStrataChange")]
        [HarmonyPrefix]
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
        /// </summary>
        [HarmonyPatch(typeof(MachineManager), "SimUpdate")]
        [HarmonyPrefix]
        public static void SimUpdate_Prefix(MachineManager __instance)
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
