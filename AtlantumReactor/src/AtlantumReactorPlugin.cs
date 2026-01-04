using System;
using Mirror;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;

using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;

namespace AtlantumReactor
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    public class AtlantumReactorPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.AtlantumReactor";
        private const string PluginName = "AtlantumReactor";
        private const string VersionString = "3.0.1";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        public const string ReactorName = "Atlantum Reactor";

        // Power settings
        public const float BasePowerMW = 20f; // 20 MW base
        public const float PerCoolantBoost = 0.5f; // +0.5 MW per coolant
        public const float FuelConsumptionRate = 0.1f; // Consume 1 fuel per 10 seconds

        // Track reactor states
        public static Dictionary<uint, ReactorState> reactorStates = new Dictionary<uint, ReactorState>();

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            Harmony.PatchAll();
            ApplyPatches();

            // Add Atlantum Reactor unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = TechCategory.Energy,
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 1000,
                description = $"Consumes Atlantum Mixture Brick and Shiverthorn Coolant to produce {BasePowerMW}MW of power. Coolant boosts output.",
                displayName = ReactorName,
                requiredTier = ResearchTier.Tier1,
                treePosition = 0
            });

            // Add Atlantum Reactor machine (based on existing Core Composer / MemoryTree)
            MemoryTreeDefinition reactorDef = ScriptableObject.CreateInstance<MemoryTreeDefinition>();
            EMUAdditions.AddNewMachine<MemoryTreeInstance, MemoryTreeDefinition>(reactorDef, new NewResourceDetails
            {
                name = ReactorName,
                description = $"Consumes Atlantum Mixture Brick and Shiverthorn Coolant to produce {BasePowerMW}MW of power.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                subHeaderTitle = "Power",
                maxStackCount = 1,
                sortPriority = 999,
                unlockName = ReactorName,
                parentName = "Core Composer"
            });

            // Add recipe
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 10f,
                unlockName = ReactorName,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Core Composer", 1),
                    new RecipeResourceInfo("Crank Generator MKII", 5),
                    new RecipeResourceInfo("Steel Slab", 10)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(ReactorName, 1)
                },
                sortPriority = 999
            });

            // Hook events
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void ApplyPatches()
        {
            Harmony.CreateAndPatchAll(typeof(MemoryTreeInstancePatch));
        }

        private void OnGameDefinesLoaded()
        {
            // Link unlock to resource
            ResourceInfo reactorInfo = EMU.Resources.GetResourceInfoByName(ReactorName);
            if (reactorInfo != null)
            {
                reactorInfo.unlock = EMU.Unlocks.GetUnlockByName(ReactorName);
            }
        }

        private void OnTechTreeStateLoaded()
        {
            // Position in tech tree
            Unlock crankMk2 = EMU.Unlocks.GetUnlockByName("Crank Generator MKII");
            Unlock hvcReach = EMU.Unlocks.GetUnlockByName("HVC Reach IV");

            Unlock reactorUnlock = EMU.Unlocks.GetUnlockByName(ReactorName);
            if (reactorUnlock != null)
            {
                if (crankMk2 != null)
                    reactorUnlock.treePosition = crankMk2.treePosition;
                if (hvcReach != null)
                    reactorUnlock.requiredTier = hvcReach.requiredTier;
            }
        }

        public static bool IsAtlantumReactor(MemoryTreeInstance instance)
        {
            return instance.myDef != null && instance.myDef.displayName == ReactorName;
        }

        public static ReactorState GetOrCreateState(uint instanceId)
        {
            if (!reactorStates.TryGetValue(instanceId, out ReactorState state))
            {
                state = new ReactorState(instanceId);
                reactorStates[instanceId] = state;
            }
            return state;
        }
    }

    public class ReactorState
    {
        public uint instanceId;
        public float fuelAmount;
        public float coolantAmount;
        public float powerOutput;
        public bool isRunning;

        public ReactorState(uint id)
        {
            instanceId = id;
        }
    }

    internal class MemoryTreeInstancePatch
    {
        private static float lastUpdateTime = 0f;
        private static ResourceInfo atlantumBrickInfo;
        private static ResourceInfo coolantInfo;

        [HarmonyPatch(typeof(MemoryTreeInstance), "SimUpdate")]
        [HarmonyPrefix]
        private static void ReactorSimUpdate(ref MemoryTreeInstance __instance)
        {
            // MULTIPLAYER FIX: Only run on host/server to prevent desync
            if (!NetworkServer.active) return;

            if (!AtlantumReactorPlugin.IsAtlantumReactor(__instance))
                return;

            // Only update every second
            if (Time.time - lastUpdateTime < 1f)
                return;
            lastUpdateTime = Time.time;

            // Cache resource info
            if (atlantumBrickInfo == null)
                atlantumBrickInfo = EMU.Resources.GetResourceInfoByName("Atlantum Mixture Brick");
            if (coolantInfo == null)
                coolantInfo = EMU.Resources.GetResourceInfoByName("Shiverthorn Coolant");

            if (atlantumBrickInfo == null || coolantInfo == null)
                return;

            uint instanceId = __instance.commonInfo.instanceId;
            ReactorState state = AtlantumReactorPlugin.GetOrCreateState(instanceId);

            // Check inventory for fuel
            var inventories = __instance.commonInfo.inventories;
            if (inventories == null || inventories.Length == 0)
                return;

            ref var inputInv = ref inventories[0];

            int fuelCount = 0;
            int coolantCount = 0;

            // Count fuel and coolant in inventory
            for (int i = 0; i < inputInv.myStacks.Length; i++)
            {
                ref var stack = ref inputInv.myStacks[i];
                if (stack.isEmpty) continue;

                if (stack.info.uniqueId == atlantumBrickInfo.uniqueId)
                    fuelCount += stack.count;
                else if (stack.info.uniqueId == coolantInfo.uniqueId)
                    coolantCount += stack.count;
            }

            // Calculate power output in kW (game uses kW internally)
            // 20 MW = 20,000 kW
            float powerOutputKW = 0f;

            if (fuelCount > 0)
            {
                state.isRunning = true;
                float powerMW = AtlantumReactorPlugin.BasePowerMW + (coolantCount * AtlantumReactorPlugin.PerCoolantBoost);
                state.powerOutput = powerMW;
                powerOutputKW = powerMW * 1000f; // Convert MW to kW

                // Consume fuel slowly (1 per 10 seconds)
                state.fuelAmount += AtlantumReactorPlugin.FuelConsumptionRate;
                if (state.fuelAmount >= 1f)
                {
                    state.fuelAmount -= 1f;
                    inputInv.TryRemoveResources(atlantumBrickInfo.uniqueId, 1);

                    // Also consume coolant occasionally
                    if (coolantCount > 0 && UnityEngine.Random.value < 0.1f)
                    {
                        inputInv.TryRemoveResources(coolantInfo.uniqueId, 1);
                    }
                }
            }
            else
            {
                state.isRunning = false;
                state.powerOutput = 0f;
            }

            // Set power generation on the PowerInfo
            // isGenerator = true means this machine produces power
            // curPowerConsumption is NEGATIVE for generators (power output)
            __instance.powerInfo.isGenerator = state.isRunning;
            __instance.powerInfo.curPowerConsumption = state.isRunning ? -(int)powerOutputKW : 0;
        }
    }
}
