using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;
using TechtonicaFramework.TechTree;
using TechtonicaFramework.BuildMenu;

using TechCategory = Unlock.TechCategory;
using CoreType = ResearchCoreDefinition.CoreType;
using ResearchTier = TechTreeState.ResearchTier;

namespace AtlantumReactor
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class AtlantumReactorPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.AtlantumReactor";
        private const string PluginName = "AtlantumReactor";
        private const string VersionString = "4.1.0";

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

            // Add Atlantum Reactor unlock - use Modded category to avoid vanilla tech tree conflicts
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory, // Category 7 (Modded)
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 1000,
                description = $"Consumes {AtlantumBrickName} and {CoolantName} to produce {PowerOutputKW.Value / 1000}MW of power.",
                displayName = ReactorName,
                requiredTier = ResearchTier.Tier18, // Level 4 endgame tier
                treePosition = 100 // Unique position in Modded tab
            });

            // Create the PowerGeneratorDefinition
            var reactorDefinition = ScriptableObject.CreateInstance<PowerGeneratorDefinition>();

            // Configure as fuel-based generator (not crank-driven)
            reactorDefinition.usesFuel = true;
            reactorDefinition.isCrankDriven = false;
            reactorDefinition.fuelConsumptionRate = 0.1f; // Slow fuel consumption for high-output reactor
            reactorDefinition.powerDuration = 0f;
            reactorDefinition.torqueDemand = 0;

            EMUAdditions.AddNewMachine<PowerGeneratorInstance, PowerGeneratorDefinition>(reactorDefinition, new NewResourceDetails
            {
                name = ReactorName,
                description = $"Advanced nuclear reactor that consumes {AtlantumBrickName} and {CoolantName} to produce {PowerOutputKW.Value / 1000}MW of power. High-output endgame power solution.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Modded",
                subHeaderTitle = "Power",
                maxStackCount = 50, // Fixed: Was 1, now stacks properly
                sortPriority = 999,
                unlockName = ReactorName,
                parentName = "Smelter MKII" // Use Smelter MKII model (vanilla) - has proper input ports for fuel
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

        private void OnTechTreeStateLoaded()
        {
            // Clone sprite from Crank Generator MKII (power generator theme)
            Sprite reactorSprite = CloneGameSprite("Crank Generator MKII");

            // Configure unlock sprite and position
            Unlock reactorUnlock = EMU.Unlocks.GetUnlockByName(ReactorName);
            if (reactorUnlock != null)
            {
                if (reactorSprite != null)
                {
                    reactorUnlock.sprite = reactorSprite;
                    Log.LogInfo($"Set {ReactorName} unlock sprite from Crank Generator MKII");
                }

                // Ensure correct tier and position
                reactorUnlock.requiredTier = ResearchTier.Tier18;
                reactorUnlock.treePosition = 100;
                Log.LogInfo($"Configured {ReactorName}: Tier18, position=100");
            }
            else
            {
                Log.LogWarning($"Could not find unlock: {ReactorName}");
            }

            // Also apply cloned sprite to the reactor resource itself
            ResourceInfo reactorInfo = EMU.Resources.GetResourceInfoByName(ReactorName);
            if (reactorInfo != null && reactorSprite != null)
            {
                // Use reflection to set the rawSprite field
                var spriteField = typeof(ResourceInfo).GetField("rawSprite", BindingFlags.Public | BindingFlags.Instance);
                if (spriteField != null)
                {
                    spriteField.SetValue(reactorInfo, reactorSprite);
                    Log.LogInfo($"Applied cloned sprite to {ReactorName} resource");
                }
            }
        }

        /// <summary>
        /// Clone a sprite from an existing game resource to match Techtonica's icon style
        /// </summary>
        private static Sprite CloneGameSprite(string sourceResourceName)
        {
            try
            {
                ResourceInfo sourceResource = EMU.Resources.GetResourceInfoByName(sourceResourceName);
                if (sourceResource != null && sourceResource.sprite != null)
                {
                    Log.LogInfo($"Cloned sprite from {sourceResourceName}");
                    return sourceResource.sprite;
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to clone sprite from {sourceResourceName}: {ex.Message}");
            }
            return null;
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
