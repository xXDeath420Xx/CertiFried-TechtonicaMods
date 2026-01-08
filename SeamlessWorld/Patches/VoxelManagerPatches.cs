using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;
using UnityEngine;

namespace SeamlessWorld.Patches
{
    /// <summary>
    /// Patches VoxelManager to handle multiple strata voxel spaces simultaneously
    /// </summary>
    [HarmonyPatch]
    public static class VoxelManagerPatches
    {
        // Store voxel bounds for each loaded strata
        private static Dictionary<byte, VoxelBounds> strataBounds = new Dictionary<byte, VoxelBounds>();

        public struct VoxelBounds
        {
            public Vector3Int min;
            public Vector3Int max;
            public Vector3Int size;
            public Vector3 worldOffset;
        }

        /// <summary>
        /// Expand voxel bounds check to include all loaded strata
        /// </summary>
        [HarmonyPatch(typeof(VoxelManager), "IsWithinVoxelBounds")]
        [HarmonyPrefix]
        public static bool IsWithinVoxelBounds_Prefix(Vector3Int voxelPos, ref bool __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true; // Run original

            // Check if position is within ANY loaded strata's bounds
            foreach (byte loadedStrata in SeamlessWorldPlugin.LoadedStrata)
            {
                if (IsWithinStrataBounds(voxelPos, loadedStrata))
                {
                    __result = true;
                    return false; // Skip original
                }
            }

            __result = false;
            return false; // Skip original
        }

        /// <summary>
        /// Check if a voxel position is within a specific strata's bounds
        /// </summary>
        private static bool IsWithinStrataBounds(Vector3Int voxelPos, byte strataNum)
        {
            if (!strataBounds.TryGetValue(strataNum, out VoxelBounds bounds))
            {
                // Calculate bounds for this strata
                bounds = CalculateStrataBounds(strataNum);
                strataBounds[strataNum] = bounds;
            }

            return voxelPos.x >= bounds.min.x && voxelPos.x < bounds.max.x &&
                   voxelPos.y >= bounds.min.y && voxelPos.y < bounds.max.y &&
                   voxelPos.z >= bounds.min.z && voxelPos.z < bounds.max.z;
        }

        /// <summary>
        /// Calculate voxel bounds for a strata with world offset applied
        /// </summary>
        private static VoxelBounds CalculateStrataBounds(byte strataNum)
        {
            var levelDef = FlowManager.instance?.curLevel;
            if (levelDef == null || levelDef.strataDefinitions == null || strataNum >= levelDef.strataDefinitions.Length)
            {
                return new VoxelBounds();
            }

            var strataDef = levelDef.strataDefinitions[strataNum];
            if (strataDef == null)
            {
                return new VoxelBounds();
            }

            Vector3 worldOffset = SeamlessWorldPlugin.GetStrataOffset(strataNum);

            // Convert world offset to voxel offset
            Vector3Int voxelOffset = new Vector3Int(
                Mathf.RoundToInt(worldOffset.x),
                Mathf.RoundToInt(worldOffset.y),
                Mathf.RoundToInt(worldOffset.z)
            );

            VoxelBounds bounds = new VoxelBounds
            {
                min = strataDef.biomeOffsetPos + voxelOffset,
                size = strataDef.terrainBounds,
                worldOffset = worldOffset
            };
            bounds.max = bounds.min + bounds.size;

            SeamlessWorldPlugin.LogDebug($"Strata {strataNum} voxel bounds: {bounds.min} to {bounds.max}");

            return bounds;
        }

        /// <summary>
        /// Override GetCurrentStrataNum to return the strata the player is actually in
        /// </summary>
        [HarmonyPatch(typeof(VoxelManager), "GetCurrentStrataNum")]
        [HarmonyPostfix]
        public static void GetCurrentStrataNum_Postfix(ref byte __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return;

            // Determine strata based on player Y position
            var player = Player.instance;
            if (player != null)
            {
                (_, byte actualStrata) = SeamlessWorldPlugin.WorldToLocalPosition(player.transform.position);
                __result = actualStrata;
            }
        }

        /// <summary>
        /// Prevent voxel manager from resetting when changing strata
        /// </summary>
        [HarmonyPatch(typeof(VoxelManager), "SetCurrentStrata")]
        [HarmonyPrefix]
        public static bool SetCurrentStrata_Prefix(byte strataNum)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true; // Run original

            SeamlessWorldPlugin.LogDebug($"Intercepted SetCurrentStrata({strataNum}) - allowing seamless continuation");

            // Only update the tracking variable, don't reset anything
            // The original method clears voxel data which we want to avoid
            return false; // Skip original entirely in seamless mode
        }

        /// <summary>
        /// Clear cached bounds when strata is unloaded
        /// </summary>
        public static void ClearStrataBounds(byte strataNum)
        {
            strataBounds.Remove(strataNum);
        }

        /// <summary>
        /// Get all loaded strata bounds for debugging
        /// </summary>
        public static Dictionary<byte, VoxelBounds> GetAllStrataBounds()
        {
            return new Dictionary<byte, VoxelBounds>(strataBounds);
        }
    }
}
