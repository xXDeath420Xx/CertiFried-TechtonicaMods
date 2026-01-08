using System;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeamlessWorld.Patches
{
    /// <summary>
    /// Patches player controller to allow free vertical movement between strata
    /// </summary>
    [HarmonyPatch]
    public static class PlayerPatches
    {
        /// <summary>
        /// Remove Y-position clamping to allow falling/flying between floors
        /// </summary>
        [HarmonyPatch(typeof(PlayerFirstPersonController), "LateUpdate")]
        [HarmonyPostfix]
        public static void LateUpdate_Postfix(PlayerFirstPersonController __instance)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Auto-detect strata changes based on Y position
            UpdatePlayerStrata(__instance);
        }

        /// <summary>
        /// Update the player's current strata based on Y position
        /// </summary>
        private static void UpdatePlayerStrata(PlayerFirstPersonController player)
        {
            if (player == null) return;

            Vector3 worldPos = player.transform.position;
            (Vector3 localPos, byte newStrata) = SeamlessWorldPlugin.WorldToLocalPosition(worldPos);

            // Check if strata changed
            byte currentStrata = VoxelManager.curStrataNum;
            if (newStrata != currentStrata)
            {
                SeamlessWorldPlugin.LogDebug($"Player crossed strata boundary: {currentStrata} -> {newStrata}");

                // Ensure the new strata is loaded
                if (!SeamlessWorldPlugin.LoadedStrata.Contains(newStrata))
                {
                    // Trigger loading of the new strata
                    LoadAdjacentStrata(newStrata);
                }

                // Update current strata reference
                try
                {
                    var field = typeof(VoxelManager).GetField("curStrataNum",
                        BindingFlags.Static | BindingFlags.NonPublic | BindingFlags.Public);
                    if (field != null)
                    {
                        field.SetValue(null, newStrata);
                    }
                }
                catch (Exception ex)
                {
                    SeamlessWorldPlugin.Log.LogWarning($"Failed to update strata: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Trigger loading of adjacent strata
        /// </summary>
        private static void LoadAdjacentStrata(byte targetStrata)
        {
            // Start coroutine to load the strata
            if (FlowManager.instance != null)
            {
                SeamlessWorldPlugin.Instance.StartCoroutine(
                    FlowManager.instance.TransitionToNewStrata(targetStrata));
            }
        }

        /// <summary>
        /// Modify teleport to work with unified world space
        /// </summary>
        [HarmonyPatch(typeof(PlayerFirstPersonController), "TeleportTo",
            typeof(Vector3), typeof(Quaternion), typeof(byte))]
        [HarmonyPrefix]
        public static bool TeleportTo_Prefix(
            PlayerFirstPersonController __instance,
            ref Vector3 position,
            Quaternion rotation,
            byte strata)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true; // Run original

            // Convert strata-local position to unified world position
            Vector3 worldPos = SeamlessWorldPlugin.LocalToWorldPosition(position, strata);

            SeamlessWorldPlugin.LogDebug($"Teleport: local {position} @ strata {strata} -> world {worldPos}");

            // Update position to world coordinates
            position = worldPos;

            // Still run original but with modified position
            return true;
        }

        /// <summary>
        /// Prevent fall damage when transitioning between strata
        /// </summary>
        [HarmonyPatch(typeof(PlayerFirstPersonController), "HandleFallDamage")]
        [HarmonyPrefix]
        public static bool HandleFallDamage_Prefix(PlayerFirstPersonController __instance)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true;

            // Check if player is in a strata transition zone
            // If so, suppress fall damage calculation
            // TODO: Implement proper transition zone detection

            return true; // For now, still allow fall damage
        }

        /// <summary>
        /// Override GetPlayerPosition to return unified world position
        /// </summary>
        [HarmonyPatch(typeof(Player), "GetPosition")]
        [HarmonyPostfix]
        public static void GetPosition_Postfix(ref Vector3 __result)
        {
            // Position is already in world space in seamless mode
            // No modification needed
        }
    }
}
