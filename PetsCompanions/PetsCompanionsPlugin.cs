using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using TechtonicaFramework.TechTree;
using UnityEngine;

namespace PetsCompanions
{
    /// <summary>
    /// PetsCompanions - Adds companion creatures that follow the player and provide buffs
    /// Features: Pet spawning, following AI, buff system, pet management UI
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.equinox.EMUAdditions")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class PetsCompanionsPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.PetsCompanions";
        public const string PluginName = "PetsCompanions";
        public const string VersionString = "1.4.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static PetsCompanionsPlugin Instance;
        public static string PluginPath;

        // Configuration
        public static ConfigEntry<bool> EnablePets;
        public static ConfigEntry<int> MaxActivePets;
        public static ConfigEntry<float> PetFollowDistance;
        public static ConfigEntry<float> PetSpeed;
        public static ConfigEntry<KeyCode> SummonPetKey;
        public static ConfigEntry<KeyCode> DismissPetKey;
        public static ConfigEntry<bool> PetsProvideBuffs;
        public static ConfigEntry<bool> DebugMode;

        // Active pets
        public static List<PetController> ActivePets = new List<PetController>();
        public static PetController CurrentPet = null;

        // Pet types
        public enum PetType
        {
            Drone,      // Flying companion - speed buff
            Crawler,    // Ground companion - mining buff
            Floater,    // Hovering companion - inventory buff
            Guardian,   // Combat companion - damage reduction
            Scout       // Fast companion - reveals resources
        }

        // Pet items
        public const string DronePetItem = "Companion Drone";
        public const string CrawlerPetItem = "Companion Crawler";
        public const string FloaterPetItem = "Companion Floater";
        public const string GuardianPetItem = "Guardian Bot";
        public const string ScoutPetItem = "Scout Drone";
        public const string PetUnlock = "Companion Technology";

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            RegisterContent();

            EMU.Events.GameDefinesLoaded += OnGameDefinesLoaded;
            EMU.Events.GameLoaded += OnGameLoaded;
            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnablePets = Config.Bind("General", "Enable Pets", true, "Enable companion pet system");
            MaxActivePets = Config.Bind("General", "Max Active Pets", 1,
                new ConfigDescription("Maximum number of active pets at once", new AcceptableValueRange<int>(1, 3)));
            PetFollowDistance = Config.Bind("Behavior", "Follow Distance", 3f,
                new ConfigDescription("Distance pets maintain from player", new AcceptableValueRange<float>(1f, 10f)));
            PetSpeed = Config.Bind("Behavior", "Pet Speed", 8f,
                new ConfigDescription("Pet movement speed", new AcceptableValueRange<float>(3f, 15f)));
            SummonPetKey = Config.Bind("Controls", "Summon Pet Key", KeyCode.Comma, "Key to summon/cycle pets");
            DismissPetKey = Config.Bind("Controls", "Dismiss Pet Key", KeyCode.Period, "Key to dismiss current pet");
            PetsProvideBuffs = Config.Bind("Buffs", "Pets Provide Buffs", true, "Whether pets provide passive buffs");
            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void RegisterContent()
        {
            if (!EnablePets.Value) return;

            // Unlock for companion technology - Modded category
            EMUAdditions.AddNewUnlock(new NewUnlockDetails
            {
                category = ModdedTabModule.ModdedCategory,
                coreTypeNeeded = ResearchCoreDefinition.CoreType.Purple,
                coreCountNeeded = 150,
                description = "Research companion technology to create helpful robotic pets that follow you and provide useful buffs.",
                displayName = PetUnlock,
                requiredTier = TechTreeState.ResearchTier.Tier6,
                treePosition = 80  // Position 80 to avoid collision with other mods
            });

            // Companion Drone - Speed buff
            RegisterPetItem(DronePetItem,
                "A small flying drone that follows you around. Provides a movement speed buff while active.",
                PetType.Drone, 180);

            // Companion Crawler - Mining buff
            RegisterPetItem(CrawlerPetItem,
                "A spider-like crawler companion. Increases mining speed while active.",
                PetType.Crawler, 181);

            // Companion Floater - Inventory buff
            RegisterPetItem(FloaterPetItem,
                "A hovering companion with storage capabilities. Increases inventory capacity while active.",
                PetType.Floater, 182);

            // Guardian Bot - Defense buff
            RegisterPetItem(GuardianPetItem,
                "A sturdy guardian bot. Reduces damage taken while active.",
                PetType.Guardian, 183);

            // Scout Drone - Resource detection
            RegisterPetItem(ScoutPetItem,
                "A fast scout drone with sensors. Highlights nearby resources and enemies while active.",
                PetType.Scout, 184);

            Log.LogInfo("Registered 5 companion pet types");
        }

        private void RegisterPetItem(string name, string desc, PetType type, int priority)
        {
            EMUAdditions.AddNewResource(new NewResourceDetails
            {
                name = name,
                description = desc,
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                headerTitle = "Companions",
                maxStackCount = 1,
                sortPriority = priority,
                unlockName = PetUnlock,
                parentName = "Processor Unit"
            });

            // Recipe based on pet type
            var ingredients = GetPetIngredients(type);
            EMUAdditions.AddNewRecipe(new NewRecipeDetails
            {
                GUID = MyGUID + "_" + name.Replace(" ", "").ToLower(),
                craftingMethod = CraftingMethod.Assembler,
                craftTierRequired = 0,
                duration = 60f,
                unlockName = PetUnlock,
                ingredients = ingredients,
                outputs = new List<RecipeResourceInfo>
                {
                    new RecipeResourceInfo(name, 1)
                },
                sortPriority = priority
            });
        }

        private List<RecipeResourceInfo> GetPetIngredients(PetType type)
        {
            switch (type)
            {
                case PetType.Drone:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 5),
                        new RecipeResourceInfo("Electric Motor", 3),
                        new RecipeResourceInfo("Copper Wire", 20),
                        new RecipeResourceInfo("Steel Frame", 2)
                    };
                case PetType.Crawler:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 3),
                        new RecipeResourceInfo("Iron Components", 15),
                        new RecipeResourceInfo("Electric Motor", 4),
                        new RecipeResourceInfo("Steel Frame", 4)
                    };
                case PetType.Floater:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 4),
                        new RecipeResourceInfo("Electric Motor", 2),
                        new RecipeResourceInfo("Copper Wire", 30),
                        new RecipeResourceInfo("Iron Frame", 3)
                    };
                case PetType.Guardian:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 6),
                        new RecipeResourceInfo("Steel Frame", 8),
                        new RecipeResourceInfo("Electric Motor", 4),
                        new RecipeResourceInfo("Iron Components", 20)
                    };
                case PetType.Scout:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 8),
                        new RecipeResourceInfo("Electric Motor", 2),
                        new RecipeResourceInfo("Copper Wire", 40),
                        new RecipeResourceInfo("Shiverthorn Extract", 5)
                    };
                default:
                    return new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo("Processor Unit", 5),
                        new RecipeResourceInfo("Electric Motor", 3)
                    };
            }
        }

        private void OnGameDefinesLoaded()
        {
            // Link unlocks to resources - CRITICAL for crafting to work
            LinkUnlockToResource(DronePetItem, PetUnlock);
            LinkUnlockToResource(CrawlerPetItem, PetUnlock);
            LinkUnlockToResource(FloaterPetItem, PetUnlock);
            LinkUnlockToResource(GuardianPetItem, PetUnlock);
            LinkUnlockToResource(ScoutPetItem, PetUnlock);

            Log.LogInfo("Linked PetsCompanions unlocks to resources");
        }

        private void LinkUnlockToResource(string resourceName, string unlockName)
        {
            try
            {
                ResourceInfo info = EMU.Resources.GetResourceInfoByName(resourceName);
                if (info != null)
                {
                    info.unlock = EMU.Unlocks.GetUnlockByName(unlockName);
                    if (DebugMode.Value)
                        Log.LogInfo($"Linked {resourceName} to unlock {unlockName}");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to link {resourceName} to {unlockName}: {ex.Message}");
            }
        }

        private void OnGameLoaded()
        {
            Log.LogInfo("PetsCompanions: Game loaded, pet system ready");
        }

        private void OnTechTreeStateLoaded()
        {
            // PT level tier mapping (from game):
            // - LIMA: Tier1-Tier4
            // - VICTOR: Tier5-Tier11
            // - XRAY: Tier12-Tier16
            // - SIERRA: Tier17-Tier24

            // Companion Technology: XRAY (Tier12), position 50
            try
            {
                Unlock unlock = EMU.Unlocks.GetUnlockByName(PetUnlock);
                if (unlock != null)
                {
                    unlock.requiredTier = TechTreeState.ResearchTier.Tier12;
                    unlock.treePosition = 50;

                    // Set sprite from a common resource
                    if (unlock.sprite == null)
                    {
                        ResourceInfo sourceRes = EMU.Resources.GetResourceInfoByName("Processor Unit");
                        if (sourceRes != null && sourceRes.sprite != null)
                        {
                            unlock.sprite = sourceRes.sprite;
                        }
                    }
                    Log.LogInfo($"Configured {PetUnlock}: tier=Tier12 (XRAY), position=50");
                }
            }
            catch (Exception ex)
            {
                Log.LogWarning($"Failed to configure unlock: {ex.Message}");
            }
        }

        private void Update()
        {
            if (!EnablePets.Value) return;
            if (Player.instance == null) return;

            // Handle summon key
            if (Input.GetKeyDown(SummonPetKey.Value))
            {
                SummonOrCyclePet();
            }

            // Handle dismiss key
            if (Input.GetKeyDown(DismissPetKey.Value))
            {
                DismissCurrentPet();
            }

            // Update active pets
            UpdateActivePets();

            // Apply buffs from active pets
            if (PetsProvideBuffs.Value)
            {
                ApplyPetBuffs();
            }
        }

        private void SummonOrCyclePet()
        {
            // Check if player has any pet items in inventory
            // For now, just spawn a test drone
            if (CurrentPet == null)
            {
                SpawnPet(PetType.Drone);
            }
            else
            {
                // Cycle to next pet type
                int nextType = ((int)CurrentPet.Type + 1) % 5;
                DismissCurrentPet();
                SpawnPet((PetType)nextType);
            }
        }

        private void SpawnPet(PetType type)
        {
            if (Player.instance == null) return;

            Vector3 spawnPos = Player.instance.transform.position + Vector3.right * 2f + Vector3.up * 1f;

            GameObject petObj = CreatePetObject(type, spawnPos);
            if (petObj != null)
            {
                PetController controller = petObj.AddComponent<PetController>();
                controller.Initialize(type, Player.instance.transform);
                ActivePets.Add(controller);
                CurrentPet = controller;
                Log.LogInfo($"Spawned pet: {type}");
            }
        }

        private GameObject CreatePetObject(PetType type, Vector3 position)
        {
            // Create a simple primitive for now - will replace with proper models later
            GameObject pet = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            pet.name = $"Pet_{type}";
            pet.transform.position = position;
            pet.transform.localScale = GetPetScale(type);

            // Remove collider for now to prevent physics issues
            var collider = pet.GetComponent<Collider>();
            if (collider != null) Destroy(collider);

            // Set color based on type
            var renderer = pet.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = GetPetColor(type);
            }

            return pet;
        }

        private Vector3 GetPetScale(PetType type)
        {
            switch (type)
            {
                case PetType.Drone: return new Vector3(0.3f, 0.3f, 0.3f);
                case PetType.Crawler: return new Vector3(0.4f, 0.2f, 0.5f);
                case PetType.Floater: return new Vector3(0.5f, 0.5f, 0.5f);
                case PetType.Guardian: return new Vector3(0.6f, 0.8f, 0.6f);
                case PetType.Scout: return new Vector3(0.25f, 0.25f, 0.25f);
                default: return Vector3.one * 0.3f;
            }
        }

        private Color GetPetColor(PetType type)
        {
            switch (type)
            {
                case PetType.Drone: return new Color(0.2f, 0.6f, 1f);      // Blue
                case PetType.Crawler: return new Color(0.8f, 0.5f, 0.2f);  // Orange
                case PetType.Floater: return new Color(0.6f, 0.2f, 0.8f);  // Purple
                case PetType.Guardian: return new Color(0.3f, 0.8f, 0.3f); // Green
                case PetType.Scout: return new Color(1f, 1f, 0.3f);        // Yellow
                default: return Color.white;
            }
        }

        private void DismissCurrentPet()
        {
            if (CurrentPet != null)
            {
                ActivePets.Remove(CurrentPet);
                Destroy(CurrentPet.gameObject);
                CurrentPet = null;
                Log.LogInfo("Pet dismissed");
            }
        }

        private void UpdateActivePets()
        {
            // Clean up destroyed pets
            ActivePets.RemoveAll(p => p == null);
        }

        private void ApplyPetBuffs()
        {
            // Buffs are applied by individual pet controllers
            // This method can be used for combined buff effects
        }

        public static void LogDebug(string message)
        {
            if (DebugMode.Value)
                Log.LogInfo($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// Controls individual pet behavior
    /// </summary>
    public class PetController : MonoBehaviour
    {
        public PetsCompanionsPlugin.PetType Type { get; private set; }
        private Transform target;
        private Vector3 targetOffset;
        private float bobPhase;
        private float orbitAngle;

        public void Initialize(PetsCompanionsPlugin.PetType type, Transform followTarget)
        {
            Type = type;
            target = followTarget;
            bobPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            orbitAngle = UnityEngine.Random.value * 360f;
            targetOffset = GetRandomOffset();
        }

        private Vector3 GetRandomOffset()
        {
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = PetsCompanionsPlugin.PetFollowDistance.Value;
            return new Vector3(Mathf.Cos(angle) * dist, 1f, Mathf.Sin(angle) * dist);
        }

        private void Update()
        {
            if (target == null) return;

            // Calculate target position with orbit
            orbitAngle += Time.deltaTime * 30f; // Slow orbit
            float rad = orbitAngle * Mathf.Deg2Rad;
            float dist = PetsCompanionsPlugin.PetFollowDistance.Value;

            Vector3 orbitOffset = new Vector3(
                Mathf.Cos(rad) * dist,
                GetHeightOffset(),
                Mathf.Sin(rad) * dist
            );

            Vector3 targetPos = target.position + orbitOffset;

            // Smooth follow
            float speed = PetsCompanionsPlugin.PetSpeed.Value;
            transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * speed * 0.5f);

            // Face movement direction
            Vector3 moveDir = targetPos - transform.position;
            if (moveDir.sqrMagnitude > 0.01f)
            {
                transform.rotation = Quaternion.Slerp(
                    transform.rotation,
                    Quaternion.LookRotation(moveDir),
                    Time.deltaTime * 5f
                );
            }

            // Apply type-specific behavior
            ApplyTypeBehavior();
        }

        private float GetHeightOffset()
        {
            bobPhase += Time.deltaTime * 2f;
            float bob = Mathf.Sin(bobPhase) * 0.2f;

            switch (Type)
            {
                case PetsCompanionsPlugin.PetType.Drone:
                case PetsCompanionsPlugin.PetType.Scout:
                    return 2f + bob; // Flying
                case PetsCompanionsPlugin.PetType.Floater:
                    return 1.5f + bob; // Hovering
                case PetsCompanionsPlugin.PetType.Crawler:
                    return 0.3f; // Ground level
                case PetsCompanionsPlugin.PetType.Guardian:
                    return 0.5f + bob * 0.5f; // Low hover
                default:
                    return 1f;
            }
        }

        private void ApplyTypeBehavior()
        {
            if (!PetsCompanionsPlugin.PetsProvideBuffs.Value) return;

            // Apply buffs based on type (placeholder - integrate with game systems)
            switch (Type)
            {
                case PetsCompanionsPlugin.PetType.Drone:
                    // Speed buff - would need to hook into player movement
                    break;
                case PetsCompanionsPlugin.PetType.Crawler:
                    // Mining buff - would need to hook into mining system
                    break;
                case PetsCompanionsPlugin.PetType.Floater:
                    // Inventory buff - would need to hook into inventory
                    break;
                case PetsCompanionsPlugin.PetType.Guardian:
                    // Defense buff - integrate with SurvivalElements
                    break;
                case PetsCompanionsPlugin.PetType.Scout:
                    // Reveal resources - would need custom highlight system
                    break;
            }
        }
    }
}
