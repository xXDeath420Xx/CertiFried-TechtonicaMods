using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
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
        private const string MyGUID = "com.certifired.AtlantumReactor";
        private const string PluginName = "AtlantumReactor";
        private const string VersionString = "4.0.9";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        public const string ReactorName = "Atlantum Reactor";

        // Fuel resource names
        public const string AtlantumBrickName = "Atlantum Mixture Brick";
        public const string CoolantName = "Shiverthorn Coolant";

        // Power settings (in kW - game uses kW internally)
        // 20 MW = 20,000 kW output
        public static ConfigEntry<int> PowerOutputKW;
        public static ConfigEntry<bool> DebugMode;

        // Track our reactor definition
        public static int reactorDefinitionId = -1;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            // Config
            PowerOutputKW = Config.Bind("Power", "Power Output (kW)", 20000,
                new ConfigDescription("Power output in kW (20000 = 20MW)", new AcceptableValueRange<int>(1000, 100000)));
            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable verbose debug logging");

            Harmony.PatchAll();

            // Add Atlantum Reactor unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = TechCategory.Energy,
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 1000,
                description = $"Consumes {AtlantumBrickName} and {CoolantName} to produce {PowerOutputKW.Value / 1000}MW of power.",
                displayName = ReactorName,
                requiredTier = ResearchTier.Tier1,
                treePosition = 0
            });

            // Create the PowerGeneratorDefinition
            var reactorDefinition = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();

            // Configure as fuel-based generator
            reactorDefinition.usesFuel = true;
            reactorDefinition.isCrankDriven = false;
            reactorDefinition.powerDuration = 0f;
            reactorDefinition.torqueDemand = 0;

            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(reactorDefinition, new NewResourceDetails
            {
                name = ReactorName,
                description = $"Advanced nuclear reactor that consumes {AtlantumBrickName} and {CoolantName} to produce {PowerOutputKW.Value / 1000}MW of power. High-output endgame power solution.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Production",
                // subHeaderTitle inherited from parent
                maxStackCount = 1,
                sortPriority = 999,
                unlockName = ReactorName,
                parentName = "Crank Generator MKII" // Use Crank Generator MKII as visual base
            });

            // Add crafting recipe
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 30f,
                unlockName = ReactorName,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Crank Generator MKII", 5),
                    new RecipeResourceInfo("Steel Frame", 20),
                    new RecipeResourceInfo("Processor Unit", 10),
                    new RecipeResourceInfo("Cooling System", 10),
                    new RecipeResourceInfo("Atlantum Mixture Brick", 5)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(ReactorName, 1)
                },
                sortPriority = 999
            });

            // Hook events
            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void OnGameDefinesLoaded()
        {
            // Link unlock to resource
            ResourceInfo reactorInfo = EMU.Resources.GetResourceInfoByName(ReactorName);
            if (reactorInfo != null)
            {
                reactorInfo.unlock = EMU.Unlocks.GetUnlockByName(ReactorName);
                reactorDefinitionId = reactorInfo.uniqueId;
                LogDebug($"Reactor registered with ID: {reactorDefinitionId}");
            }

            // Register fuel resources
            RegisterFuelResources();
        }

        private void RegisterFuelResources()
        {
            // Set fuelAmount on Atlantum Mixture Brick to make it recognized as fuel
            ResourceInfo atlantumBrick = EMU.Resources.GetResourceInfoByName(AtlantumBrickName);
            if (atlantumBrick != null)
            {
                if (atlantumBrick.fuelAmount <= 0)
                {
                    atlantumBrick.fuelAmount = 200f; // High fuel value - lasts a long time
                }
                LogDebug($"Registered {AtlantumBrickName} as fuel with amount {atlantumBrick.fuelAmount}");
            }
            else
            {
                Log.LogWarning($"Could not find {AtlantumBrickName} to register as fuel");
            }

            // Set fuelAmount on Shiverthorn Coolant to make it recognized as fuel
            ResourceInfo coolant = EMU.Resources.GetResourceInfoByName(CoolantName);
            if (coolant != null)
            {
                if (coolant.fuelAmount <= 0)
                {
                    coolant.fuelAmount = 100f; // Moderate fuel value
                }
                LogDebug($"Registered {CoolantName} as fuel with amount {coolant.fuelAmount}");
            }
            else
            {
                Log.LogWarning($"Could not find {CoolantName} to register as fuel");
            }
        }

        private void OnTechTreeStateLoaded()
        {
            // Position in tech tree after HVC Reach IV (endgame power)
            Unlock hvcReach = EMU.Unlocks.GetUnlockByName("HVC Reach IV");
            Unlock reactorUnlock = EMU.Unlocks.GetUnlockByName(ReactorName);

            if (reactorUnlock != null && hvcReach != null)
            {
                reactorUnlock.requiredTier = hvcReach.requiredTier;
                reactorUnlock.treePosition = hvcReach.treePosition;
                LogDebug($"Positioned reactor unlock after HVC Reach IV");
            }
        }

        public static void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }

    /// <summary>
    /// Patches to make the reactor actually generate power
    /// </summary>
    [HarmonyPatch]
    internal static class ReactorPowerPatches
    {
        /// <summary>
        /// Ensure reactor generates power during simulation updates
        /// </summary>
        [HarmonyPatch(typeof(PowerGeneratorInstance), nameof(PowerGeneratorInstance.SimUpdate))]
        [HarmonyPostfix]
        private static void EnsureReactorPowerGeneration(ref PowerGeneratorInstance __instance)
        {
            try
            {
                if (__instance.myDef == null) return;
                if (__instance.myDef.displayName != AtlantumReactorPlugin.ReactorName) return;

                // Set power generation (negative consumption = generation)
                ref var powerInfo = ref __instance.powerInfo;
                powerInfo.curPowerConsumption = -AtlantumReactorPlugin.PowerOutputKW.Value;
                powerInfo.isGenerator = true;

                AtlantumReactorPlugin.LogDebug($"Reactor generating {AtlantumReactorPlugin.PowerOutputKW.Value}kW");
            }
            catch
            {
                // Silently ignore errors
            }
        }
    }

    /// <summary>
    /// Patches to apply green tint to Atlantum Reactor visuals
    /// </summary>
    [HarmonyPatch]
    internal static class ReactorVisualPatches
    {
        // Bright radioactive green color
        private static readonly Color ReactorGreen = new Color(0.2f, 1f, 0.3f);
        private static readonly Color EmissionGreen = new Color(0.1f, 0.8f, 0.2f);

        // Track which reactors we've already tinted
        private static readonly HashSet<uint> tintedReactors = new HashSet<uint>();

        /// <summary>
        /// Apply green tint during SimUpdate
        /// </summary>
        [HarmonyPatch(typeof(PowerGeneratorInstance), nameof(PowerGeneratorInstance.SimUpdate))]
        [HarmonyPostfix]
        private static void ApplyGreenTintOnUpdate(ref PowerGeneratorInstance __instance)
        {
            try
            {
                if (__instance.myDef == null) return;
                if (__instance.myDef.displayName != AtlantumReactorPlugin.ReactorName) return;

                uint instanceId = __instance.commonInfo.instanceId;
                if (tintedReactors.Contains(instanceId)) return;

                var visual = __instance.commonInfo.refGameObj;
                if (visual == null) return;

                ApplyGreenMaterial(visual);
                tintedReactors.Add(instanceId);
                AtlantumReactorPlugin.LogDebug($"Applied green tint to reactor {instanceId}");
            }
            catch
            {
                // Silently ignore
            }
        }

        /// <summary>
        /// Clear tinted reactors when machine is deconstructed
        /// </summary>
        [HarmonyPatch(typeof(GridManager), "RemoveObj", new Type[] { typeof(GenericMachineInstanceRef) })]
        [HarmonyPrefix]
        private static void ClearTintedReactor(GenericMachineInstanceRef machineRef)
        {
            if (machineRef.IsValid())
            {
                tintedReactors.Remove(machineRef.instanceId);
            }
        }

        private static void ApplyGreenMaterial(GameObject visual)
        {
            var renderers = visual.GetComponentsInChildren<MeshRenderer>(true);
            foreach (var renderer in renderers)
            {
                // Use MaterialPropertyBlock to avoid modifying shared materials
                MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);

                propBlock.SetColor("_Color", ReactorGreen);
                propBlock.SetColor("_BaseColor", ReactorGreen);
                propBlock.SetColor("_EmissionColor", EmissionGreen * 3f);

                renderer.SetPropertyBlock(propBlock);
            }

            // Also try direct material modification as fallback
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    var mat = materials[i];

                    if (mat.HasProperty("_Color"))
                        mat.color = ReactorGreen;
                    if (mat.HasProperty("_BaseColor"))
                        mat.SetColor("_BaseColor", ReactorGreen);
                    if (mat.HasProperty("_EmissionColor"))
                    {
                        mat.EnableKeyword("_EMISSION");
                        mat.SetColor("_EmissionColor", EmissionGreen * 3f);
                    }
                }
                renderer.materials = materials;
            }
        }
    }

    // Note: OmniseekerPatches removed - OmniseekableType, OmniseekerUI, OmniseekerIndicator
    // types may not be available. AtlantumOreVein should already be scannable in the game
    // if it exists as part of the base game content.
}
