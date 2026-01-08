using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Reflection.Emit;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SeamlessWorld.Patches
{
    /// <summary>
    /// Patches FlowManager to prevent scene unloading and keep multiple strata loaded
    /// </summary>
    [HarmonyPatch]
    public static class FlowManagerPatches
    {
        private static List<string> allLoadedScenes = new List<string>();

        /// <summary>
        /// Intercept strata transitions - load new strata WITHOUT unloading old ones
        /// </summary>
        [HarmonyPatch(typeof(FlowManager), nameof(FlowManager.TransitionToNewStrata))]
        [HarmonyPrefix]
        public static bool TransitionToNewStrata_Prefix(
            ref FlowManager __instance,
            byte strata,
            ref IEnumerator __result)
        {
            if (!SeamlessWorldPlugin.EnableSeamlessWorld.Value)
                return true; // Run original

            SeamlessWorldPlugin.LogDebug($"Intercepted strata transition to {strata}");

            // Replace with our seamless transition
            __result = SeamlessStrataTransition(__instance, strata);
            return false; // Skip original
        }

        /// <summary>
        /// Custom strata transition that loads without unloading
        /// </summary>
        private static IEnumerator SeamlessStrataTransition(FlowManager flowManager, byte targetStrata)
        {
            SeamlessWorldPlugin.Log.LogInfo($"Seamless transition to strata {targetStrata}");

            // Get current level definition
            var levelDef = FlowManager.instance?.curLevel;
            if (levelDef == null || levelDef.strataDefinitions == null)
            {
                SeamlessWorldPlugin.Log.LogError("No level definition found!");
                yield break;
            }

            if (targetStrata >= levelDef.strataDefinitions.Length)
            {
                SeamlessWorldPlugin.Log.LogError($"Invalid strata index: {targetStrata}");
                yield break;
            }

            var targetStrataDef = levelDef.strataDefinitions[targetStrata];
            if (targetStrataDef == null)
            {
                SeamlessWorldPlugin.Log.LogError($"Strata definition {targetStrata} is null!");
                yield break;
            }

            // Check if this strata is already loaded
            if (SeamlessWorldPlugin.LoadedStrata.Contains(targetStrata))
            {
                SeamlessWorldPlugin.LogDebug($"Strata {targetStrata} already loaded, skipping load");

                // Just update the current strata reference
                UpdateCurrentStrata(targetStrata);
                yield break;
            }

            // Load the new strata scenes additively
            var scenesToLoad = targetStrataDef.namesOfRuntimeScenes;
            if (scenesToLoad != null && scenesToLoad.Count > 0)
            {
                List<AsyncOperation> loadOps = new List<AsyncOperation>();

                foreach (string sceneName in scenesToLoad)
                {
                    if (string.IsNullOrEmpty(sceneName)) continue;
                    if (IsSceneLoaded(sceneName)) continue;

                    SeamlessWorldPlugin.LogDebug($"Loading scene: {sceneName}");
                    var loadOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
                    if (loadOp != null)
                    {
                        loadOps.Add(loadOp);
                        allLoadedScenes.Add(sceneName);
                    }
                }

                // Wait for all scenes to load
                foreach (var op in loadOps)
                {
                    while (!op.isDone)
                    {
                        yield return null;
                    }
                }
            }

            // Mark strata as loaded
            SeamlessWorldPlugin.LoadedStrata.Add(targetStrata);

            // Apply Y-offset to all objects in the new strata
            ApplyStrataOffset(targetStrata);

            // Update the current strata
            UpdateCurrentStrata(targetStrata);

            // Trim loaded strata if we exceed max
            TrimLoadedStrata(targetStrata);

            SeamlessWorldPlugin.Log.LogInfo($"Strata {targetStrata} loaded seamlessly. Total loaded: {SeamlessWorldPlugin.LoadedStrata.Count}");
        }

        /// <summary>
        /// Check if a scene is already loaded
        /// </summary>
        private static bool IsSceneLoaded(string sceneName)
        {
            for (int i = 0; i < SceneManager.sceneCount; i++)
            {
                if (SceneManager.GetSceneAt(i).name == sceneName)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Apply Y-offset to all root objects in a strata's scenes
        /// </summary>
        private static void ApplyStrataOffset(byte strataNum)
        {
            Vector3 offset = SeamlessWorldPlugin.GetStrataOffset(strataNum);
            if (offset == Vector3.zero) return;

            SeamlessWorldPlugin.LogDebug($"Applying offset {offset} to strata {strataNum}");

            // Find all root objects that belong to this strata and offset them
            // This is tricky - we need to identify which objects belong to which strata
            // For now, we'll use a tag or component-based approach

            // TODO: Implement proper object offsetting based on strata ownership
        }

        /// <summary>
        /// Update the game's current strata reference
        /// </summary>
        private static void UpdateCurrentStrata(byte strataNum)
        {
            try
            {
                // Update VoxelManager's current strata
                var voxelManagerField = typeof(VoxelManager).GetField("curStrataNum",
                    BindingFlags.Static | BindingFlags.NonPublic);
                if (voxelManagerField != null)
                {
                    voxelManagerField.SetValue(null, strataNum);
                }

                SeamlessWorldPlugin.LogDebug($"Updated current strata to {strataNum}");
            }
            catch (Exception ex)
            {
                SeamlessWorldPlugin.Log.LogError($"Failed to update current strata: {ex.Message}");
            }
        }

        /// <summary>
        /// Unload excess strata to manage memory
        /// </summary>
        private static void TrimLoadedStrata(byte currentStrata)
        {
            int maxLoaded = SeamlessWorldPlugin.MaxLoadedStrata.Value;
            if (SeamlessWorldPlugin.LoadedStrata.Count <= maxLoaded) return;

            // Find strata furthest from current to unload
            List<byte> strataToUnload = new List<byte>();
            foreach (byte loadedStrata in SeamlessWorldPlugin.LoadedStrata)
            {
                int distance = Math.Abs(loadedStrata - currentStrata);
                if (distance > maxLoaded / 2)
                {
                    strataToUnload.Add(loadedStrata);
                }
            }

            // Unload distant strata
            foreach (byte strata in strataToUnload)
            {
                if (SeamlessWorldPlugin.LoadedStrata.Count <= maxLoaded) break;
                UnloadStrata(strata);
            }
        }

        /// <summary>
        /// Unload a specific strata's scenes
        /// </summary>
        private static void UnloadStrata(byte strataNum)
        {
            try
            {
                var levelDef = FlowManager.instance?.curLevel;
                if (levelDef == null) return;

                var strataDef = levelDef.strataDefinitions[strataNum];
                if (strataDef?.namesOfRuntimeScenes == null) return;

                foreach (string sceneName in strataDef.namesOfRuntimeScenes)
                {
                    if (string.IsNullOrEmpty(sceneName)) continue;
                    if (IsSceneLoaded(sceneName))
                    {
                        SceneManager.UnloadSceneAsync(sceneName);
                        allLoadedScenes.Remove(sceneName);
                    }
                }

                SeamlessWorldPlugin.LoadedStrata.Remove(strataNum);
                SeamlessWorldPlugin.Log.LogInfo($"Unloaded distant strata {strataNum}");
            }
            catch (Exception ex)
            {
                SeamlessWorldPlugin.Log.LogWarning($"Failed to unload strata {strataNum}: {ex.Message}");
            }
        }
    }
}
