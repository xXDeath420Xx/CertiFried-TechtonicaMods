using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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

namespace MorePlanters
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    public class MorePlantersPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.equinox.MorePlanters";
        private const string PluginName = "MorePlanters";
        private const string VersionString = "3.0.4";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        public const string PlanterMk2Name = "Planter MKII";
        public const string PlanterMk3Name = "Planter MKIII";

        public static ConfigEntry<bool> doublePlants;
        public static ConfigEntry<float> speedMultiplier;

        private static string dataFolder => Application.persistentDataPath + "/MorePlanters";

        // Track MKIII planter output settings
        public static Dictionary<uint, PlanterMk3Settings> planterSettings = new Dictionary<uint, PlanterMk3Settings>();

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loading...");

            Harmony.PatchAll();
            CreateConfigEntries();
            ApplyPatches();

            // Add Planter MKII unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = TechCategory.Synthesis,
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 100,
                description = "Produces plants at 2x speed. Can produce two plants per seed.",
                displayName = PlanterMk2Name,
                requiredTier = ResearchTier.Tier1,
                treePosition = 0
            });

            // Add Planter MKIII unlock
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = TechCategory.Synthesis,
                coreTypeNeeded = CoreType.Blue,
                coreCountNeeded = 250,
                description = "Produces plants at 2x speed with integrated Thresher. Outputs processed materials directly.",
                displayName = PlanterMk3Name,
                requiredTier = ResearchTier.Tier1,
                treePosition = 0
            });

            // Add Planter MKII machine (uses existing Planter prefab)
            // Note: Don't set headerTitle/subHeaderTitle - inherit from parent
            PlanterDefinition mk2Def = ScriptableObject.CreateInstance<PlanterDefinition>();
            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(mk2Def, new NewResourceDetails
            {
                name = PlanterMk2Name,
                description = "Produces plants at 2x speed. Can produce two plants per seed.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                maxStackCount = 50,
                sortPriority = 100,
                unlockName = PlanterMk2Name,
                parentName = "Planter"
            });

            // Add Planter MKIII machine
            PlanterDefinition mk3Def = ScriptableObject.CreateInstance<PlanterDefinition>();
            EMUAdditions.AddNewMachine<PlanterInstance, PlanterDefinition>(mk3Def, new NewResourceDetails
            {
                name = PlanterMk3Name,
                description = "Produces plants at 2x speed with integrated Thresher. Outputs processed materials directly.",
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                maxStackCount = 50,
                sortPriority = 101,
                unlockName = PlanterMk3Name,
                parentName = "Planter"
            });

            // Add recipes
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 5f,
                unlockName = PlanterMk2Name,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo("Planter", 1),
                    new RecipeResourceInfo("Mechanical Components", 5),
                    new RecipeResourceInfo("Copper Wire", 10)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(PlanterMk2Name, 1)
                },
                sortPriority = 100
            });

            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 10f,
                unlockName = PlanterMk3Name,
                ingredients = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(PlanterMk2Name, 1),
                    new RecipeResourceInfo("Thresher", 1),
                    new RecipeResourceInfo("Processor Unit", 2)
                },
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(PlanterMk3Name, 1)
                },
                sortPriority = 101
            });

            // Hook events - Note: EMUAdditions handles unlock linking via unlockName
            EMU.Events.SaveStateLoaded += OnSaveStateLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeLoaded;

            Log.LogInfo($"PluginName: {PluginName}, VersionString: {VersionString} is loaded.");
        }

        private void CreateConfigEntries()
        {
            speedMultiplier = Config.Bind("General", "Speed Multiplier", 2.0f,
                new ConfigDescription("Speed multiplier for upgraded planters (default 2x).", new AcceptableValueRange<float>(1.0f, 10.0f)));
            doublePlants = Config.Bind("General", "Double Plants", true,
                new ConfigDescription("Whether the Planter MKII should produce two plants per seed."));
        }

        private void ApplyPatches()
        {
            Harmony.CreateAndPatchAll(typeof(PlanterInstancePatch));
        }

        private void OnSaveStateLoaded(object sender, EventArgs e)
        {
            LoadData(SaveState.instance.metadata.worldName);
        }

        private void OnTechTreeLoaded()
        {
            // Position unlocks in tech tree
            Unlock atlantum = EMU.Unlocks.GetUnlockByName("Atlantum Ingot");
            Unlock thresherMk2 = EMU.Unlocks.GetUnlockByName("Thresher MKII");
            Unlock assemblerMk2 = EMU.Unlocks.GetUnlockByName("Assembler MKII");

            Unlock mk2Unlock = EMU.Unlocks.GetUnlockByName(PlanterMk2Name);
            if (mk2Unlock != null && atlantum != null && assemblerMk2 != null)
            {
                mk2Unlock.requiredTier = atlantum.requiredTier;
                mk2Unlock.treePosition = assemblerMk2.treePosition;
            }

            Unlock mk3Unlock = EMU.Unlocks.GetUnlockByName(PlanterMk3Name);
            if (mk3Unlock != null && thresherMk2 != null && assemblerMk2 != null)
            {
                mk3Unlock.requiredTier = thresherMk2.requiredTier;
                mk3Unlock.treePosition = assemblerMk2.treePosition;
            }
        }

        public static void SaveData(string worldName)
        {
            Directory.CreateDirectory(dataFolder);
            string path = dataFolder + "/" + worldName + ".txt";
            List<string> lines = new List<string>();
            foreach (var kvp in planterSettings)
            {
                lines.Add(kvp.Value.Serialize());
            }
            File.WriteAllLines(path, lines);
        }

        public static void LoadData(string worldName)
        {
            string path = dataFolder + "/" + worldName + ".txt";
            if (!File.Exists(path))
            {
                Log.LogInfo($"No save file found for world '{worldName}'");
                return;
            }

            planterSettings.Clear();
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                PlanterMk3Settings settings = PlanterMk3Settings.Deserialize(line);
                planterSettings[settings.instanceId] = settings;
            }
        }

        public static bool IsPlanterMk2(PlanterInstance planter)
        {
            return planter.myDef != null && planter.myDef.displayName == PlanterMk2Name;
        }

        public static bool IsPlanterMk3(PlanterInstance planter)
        {
            return planter.myDef != null && planter.myDef.displayName == PlanterMk3Name;
        }

        public static bool IsUpgradedPlanter(PlanterInstance planter)
        {
            return IsPlanterMk2(planter) || IsPlanterMk3(planter);
        }
    }

    public class PlanterMk3Settings
    {
        public uint instanceId;
        public int[] slotOutputMode = new int[4]; // 0=stems/buds, 1=extract, 2=plantmatter

        public PlanterMk3Settings(uint id)
        {
            instanceId = id;
        }

        public string Serialize()
        {
            return $"{instanceId}|{slotOutputMode[0]}|{slotOutputMode[1]}|{slotOutputMode[2]}|{slotOutputMode[3]}";
        }

        public static PlanterMk3Settings Deserialize(string input)
        {
            string[] parts = input.Split('|');
            var settings = new PlanterMk3Settings(uint.Parse(parts[0]));
            for (int i = 0; i < 4 && i + 1 < parts.Length; i++)
            {
                settings.slotOutputMode[i] = int.Parse(parts[i + 1]);
            }
            return settings;
        }
    }

    internal class PlanterInstancePatch
    {
        // Track which slots were growing before SimUpdate (to detect when they become harvestable)
        private static Dictionary<uint, bool[]> wasGrowing = new Dictionary<uint, bool[]>();

        // Speed boost and output handling for MKII and MKIII planters
        [HarmonyPatch(typeof(PlanterInstance), "SimUpdate")]
        [HarmonyPrefix]
        private static void SimUpdatePrefix(ref PlanterInstance __instance)
        {
            if (!MorePlantersPlugin.IsUpgradedPlanter(__instance))
                return;

            uint instanceId = __instance.commonInfo.instanceId;

            // Track which slots are currently growing (before update)
            if (!wasGrowing.ContainsKey(instanceId))
                wasGrowing[instanceId] = new bool[4];

            for (int i = 0; i < __instance.plantSlots.Length && i < 4; i++)
            {
                wasGrowing[instanceId][i] = __instance.plantSlots[i].state == PlanterInstance.PlantState.Growing;
            }

            // Apply 2x speed by halving growth duration
            for (int i = 0; i < __instance.plantSlots.Length; i++)
            {
                ref var slot = ref __instance.plantSlots[i];
                if (slot.plantId == -1) continue;

                // Default growth is 120 seconds, we want 60 for 2x speed
                if (slot.totalGrowthDuration == 120f)
                {
                    slot.totalGrowthDuration = 120f / MorePlantersPlugin.speedMultiplier.Value;
                }
            }
        }

        [HarmonyPatch(typeof(PlanterInstance), "SimUpdate")]
        [HarmonyPostfix]
        private static void SimUpdatePostfix(ref PlanterInstance __instance)
        {
            if (!MorePlantersPlugin.IsUpgradedPlanter(__instance))
                return;

            uint instanceId = __instance.commonInfo.instanceId;
            if (!wasGrowing.ContainsKey(instanceId))
                return;

            // Check if any slot just became harvestable
            for (int i = 0; i < __instance.plantSlots.Length && i < 4; i++)
            {
                ref var slot = ref __instance.plantSlots[i];
                bool justHarvested = wasGrowing[instanceId][i] &&
                    slot.state == PlanterInstance.PlantState.Harvestable;

                if (!justHarvested)
                    continue;

                // MKII: Double output
                if (MorePlantersPlugin.IsPlanterMk2(__instance) && MorePlantersPlugin.doublePlants.Value)
                {
                    if (slot.plantId != -1)
                    {
                        ref var outputInv = ref __instance.GetOutputInventory();
                        outputInv.AddResources(slot.plantId, 1, true);
                    }
                }

                // MKIII: Convert to processed materials
                if (MorePlantersPlugin.IsPlanterMk3(__instance))
                {
                    ref var outputInv = ref __instance.GetOutputInventory();

                    // Find the raw plant in the output and convert it
                    for (int j = 0; j < outputInv.myStacks.Length; j++)
                    {
                        ref var stack = ref outputInv.myStacks[j];
                        if (stack.isEmpty || stack.info == null) continue;

                        string resourceName = stack.info.displayName;
                        string processedName = GetProcessedName(resourceName);

                        if (processedName != null)
                        {
                            ResourceInfo processed = EMU.Resources.GetResourceInfoByName(processedName);
                            if (processed != null)
                            {
                                int count = stack.count;
                                outputInv.TryRemoveResources(stack.info.uniqueId, count);
                                outputInv.AddResources(processed.uniqueId, count, true);
                            }
                        }
                    }
                }
            }
        }

        private static string GetProcessedName(string rawPlantName)
        {
            switch (rawPlantName)
            {
                case "Kindlevine": return "Kindlevine Stems";
                case "Shiverthorn": return "Shiverthorn Buds";
                case "Plantmatter": return "Plantmatter Fiber";
                default: return null;
            }
        }

        // TakeAll postfix for double output when player/inserter takes from MKII
        [HarmonyPatch(typeof(PlanterInstance), "TakeAll")]
        [HarmonyPostfix]
        private static void TakeAllPostfix(ref PlanterInstance __instance, bool actuallyTake, ref List<ResourceStack> __result)
        {
            if (!actuallyTake)
                return;

            if (!MorePlantersPlugin.IsPlanterMk2(__instance))
                return;

            if (!MorePlantersPlugin.doublePlants.Value)
                return;

            // Double the result list (already taken, so just double what was returned)
            var doubled = new List<ResourceStack>();
            foreach (var stack in __result)
            {
                doubled.Add(stack);
                doubled.Add(ResourceStack.CreateSimpleStack(stack.info.uniqueId, stack.count));
            }
            __result.Clear();
            __result.AddRange(doubled);
        }
    }

    // Save data when game saves
    [HarmonyPatch(typeof(SaveState), "SaveToFile")]
    internal class SaveStatePatch
    {
        [HarmonyPostfix]
        private static void SavePlanterData()
        {
            MorePlantersPlugin.SaveData(SaveState.instance.metadata.worldName);
            MorePlantersPlugin.Log.LogInfo("MorePlanters data saved");
        }
    }
}
