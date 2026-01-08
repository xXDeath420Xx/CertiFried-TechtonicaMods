using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using UnityEngine;

namespace VoidChests
{
    /// <summary>
    /// VoidChests_Patched - Adds a chest that automatically deletes any items placed in it.
    ///
    /// ORIGINAL AUTHOR: CubeSuite/Equinox
    /// ORIGINAL SOURCE: https://github.com/CubeSuite/TTMod-VoidChests
    /// LICENSE: GPL-3.0
    ///
    /// PATCHED BY: CertiFried
    /// PATCH REASON: Updated to EMU 6.x API (original uses deprecated ModUtils.add_GameDefinesLoaded)
    /// CHANGES: See CHANGELOG.md for full list of changes
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    public class VoidChestsPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.VoidChests_Patched";
        private const string PluginName = "VoidChests_Patched";
        private const string VersionString = "2.2.0";

        private const string VoidChestName = "Void Chest";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Track void chest resource ID
        private static int voidChestResId = -1;
        private static bool initialized = false;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            // Register with EMU events using the NEW API
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Harmony.PatchAll();

            Logger.LogInfo($"{PluginName} v{VersionString} has loaded.");
        }

        private void OnGameDefinesLoaded()
        {
            try
            {
                Log.LogInfo("Registering Void Chest...");

                // Create Void Chest Definition (based on standard Chest)
                ChestDefinition voidChestDef = ScriptableObject.CreateInstance<ChestDefinition>();

                // Register the Void Chest machine
                EMUAdditions.AddNewMachine<ChestInstance, ChestDefinition>(voidChestDef, new NewResourceDetails
                {
                    name = VoidChestName,
                    description = "A mysterious chest that voids any items placed within. Items are permanently destroyed.",
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    headerTitle = "Modded",
                    subHeaderTitle = "Storage",
                    maxStackCount = 50,
                    sortPriority = 200,
                    unlockName = VoidChestName,
                    parentName = "Container (Small)"  // Base model on small container
                });

                // Register recipe for Void Chest
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 10f,
                    ingredients = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Container (Small)", 1),
                        new RecipeResourceInfo("Iron Frame", 4),
                        new RecipeResourceInfo("Copper Wire", 10)
                    },
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo(VoidChestName, 1)
                    },
                    sortPriority = 200
                });

                // Register unlock
                EMUAdditions.AddNewUnlock(new NewUnlockDetails
                {
                    displayName = VoidChestName,
                    description = "Unlocks the Void Chest - a container that automatically destroys any items placed inside.",
                    category = Unlock.TechCategory.Logistics,
                    requiredTier = TechTreeState.ResearchTier.Tier1,
                    coreTypeNeeded = ResearchCoreDefinition.CoreType.Blue,
                    coreCountNeeded = 5,
                    treePosition = 0
                });

                Log.LogInfo("Void Chest registered successfully!");

                // Now get the resource ID for runtime voiding
                var voidChestInfo = EMU.Resources.GetResourceInfoByName(VoidChestName);
                if (voidChestInfo != null)
                {
                    voidChestResId = voidChestInfo.uniqueId;
                    Log.LogInfo($"Void Chest ID: {voidChestResId}");
                    initialized = true;
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"OnGameDefinesLoaded error: {ex.Message}");
                Log.LogError(ex.StackTrace);
            }
        }

        private void FixedUpdate()
        {
            // Only run on single-player or as host
            if (!FlowManager.isSinglePlayerGame)
                return;

            // Only process when game is fully loaded
            if (!initialized || !EMU.LoadingStates.hasGameStateLoaded)
                return;

            if (MachineManager.instance == null)
                return;

            // Process void chests
            ProcessVoidChests();
        }

        private void ProcessVoidChests()
        {
            if (voidChestResId < 0) return;

            try
            {
                // Get all chest machines
                var chestList = MachineManager.instance.GetMachineList<ChestInstance, ChestDefinition>(MachineTypeEnum.Chest);
                if (chestList == null || chestList.myArray == null) return;

                for (int i = 0; i < chestList.myArray.Length; i++)
                {
                    try
                    {
                        ChestInstance chest = chestList.myArray[i];
                        if (chest.commonInfo.instanceId == 0) continue; // Skip invalid

                        if (chest.commonInfo.resId == voidChestResId)
                        {
                            VoidInventoryContents(chest.commonInfo.inventories);
                        }
                    }
                    catch
                    {
                        // Skip invalid machines
                    }
                }
            }
            catch (Exception ex)
            {
                // Only log once to avoid spam
                Log.LogWarning($"ProcessVoidChests error: {ex.Message}");
            }
        }

        private void VoidInventoryContents(Inventory[] inventories)
        {
            if (inventories == null) return;

            for (int inv = 0; inv < inventories.Length; inv++)
            {
                var inventory = inventories[inv];
                if (inventory.myStacks == null) continue;

                // Clear all items in the inventory
                for (int i = 0; i < inventory.myStacks.Length; i++)
                {
                    ref var stack = ref inventory.myStacks[i];
                    if (stack.id != -1 && stack.count > 0)
                    {
                        stack.id = -1;
                        stack.count = 0;
                    }
                }
            }
        }
    }
}
