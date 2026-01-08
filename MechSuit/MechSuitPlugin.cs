using System;
using System.Collections;
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

namespace MechSuit
{
    /// <summary>
    /// MechSuit - Player-wearable mech suits with enhanced abilities
    /// Uses mech_companion bundle with 3 variants (PBR, HP Heavy, Polyart Light)
    /// Features: Enhanced strength, damage resistance, special abilities
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    public class MechSuitPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.MechSuit";
        public const string PluginName = "MechSuit";
        public const string VersionString = "1.1.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static MechSuitPlugin Instance;
        public static string PluginPath;

        // ========== CONFIGURATION ==========
        public static ConfigEntry<bool> EnableMechSuits;
        public static ConfigEntry<KeyCode> ToggleSuitKey;
        public static ConfigEntry<KeyCode> AbilityKey;
        public static ConfigEntry<MechVariant> SelectedVariant;
        public static ConfigEntry<float> SuitScale;
        public static ConfigEntry<bool> DebugMode;

        // ========== ASSET MANAGEMENT ==========
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private static Shader capturedLitShader;

        // ========== MECH STATE ==========
        private MechSuitController activeSuit;
        private bool isSuitActive = false;
        private float abilityCooldown = 0f;

        // ========== UI ==========
        private bool showUI = false;
        private Rect uiRect = new Rect(20, 200, 250, 180);
        private GUIStyle windowStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        public enum MechVariant
        {
            Standard,   // PBRCharacter - balanced stats
            Heavy,      // HPCharacter - high armor, slow
            Light       // PolyartCharacter - fast, low armor
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            EMU.Events.GameLoaded += OnGameLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableMechSuits = Config.Bind("General", "Enable Mech Suits", true,
                "Enable mech suit functionality");
            ToggleSuitKey = Config.Bind("Controls", "Toggle Suit Key", KeyCode.Keypad5,
                "Key to toggle mech suit on/off (Numpad 5 - F10 conflicts with HazardousWorld)");
            AbilityKey = Config.Bind("Controls", "Ability Key", KeyCode.Keypad6,
                "Key to activate special ability (Numpad 6 - G may conflict with game interactions)");
            SelectedVariant = Config.Bind("Mech", "Mech Variant", MechVariant.Standard,
                "Selected mech suit variant (Standard/Heavy/Light)");
            SuitScale = Config.Bind("Visuals", "Suit Scale", 1.0f,
                new ConfigDescription("Scale multiplier for mech suit model", new AcceptableValueRange<float>(0.5f, 2f)));
            DebugMode = Config.Bind("Debug", "Debug Mode", false,
                "Enable debug logging");
        }

        private void OnGameLoaded()
        {
            Log.LogInfo("Game loaded - initializing MechSuit...");
            CaptureGameShaders();
            LoadAssetBundles();
            RegisterCraftingRecipes();
        }

        private void RegisterCraftingRecipes()
        {
            Log.LogInfo("Registering MechSuit crafting recipes...");

            // ========== STANDARD MECH SUIT ==========
            // Balanced stats - moderate materials
            RegisterMechRecipe("MechSuit_Standard", "Standard Mech Suit", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 20),
                new RecipeResourceInfo("Processor Unit", 8),
                new RecipeResourceInfo("Mechanical Components", 15),
                new RecipeResourceInfo("Electrical Components", 10)
            });

            // ========== HEAVY MECH SUIT ==========
            // Tank build - heavy armor materials
            RegisterMechRecipe("MechSuit_Heavy", "Heavy Mech Suit", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 40),
                new RecipeResourceInfo("Atlantum Mixture Brick", 10),
                new RecipeResourceInfo("Mechanical Components", 25),
                new RecipeResourceInfo("Concrete", 20)
            });

            // ========== LIGHT MECH SUIT ==========
            // Speed build - precision materials
            RegisterMechRecipe("MechSuit_Light", "Light Mech Suit", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 10),
                new RecipeResourceInfo("Processor Unit", 15),
                new RecipeResourceInfo("Electrical Components", 20),
                new RecipeResourceInfo("Cooling System", 8)
            });

            // ========== MECH SUIT MODULES ==========
            // Upgrades for mech suits
            RegisterMechRecipe("MechModule_PowerCore", "Mech Power Core", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Processor Unit", 5),
                new RecipeResourceInfo("Electrical Components", 10),
                new RecipeResourceInfo("Shiverthorn Coolant", 5)
            });

            RegisterMechRecipe("MechModule_Boosters", "Mech Boosters", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Mechanical Components", 10),
                new RecipeResourceInfo("Steel Frame", 5),
                new RecipeResourceInfo("Kindlevine Extract", 10)
            });

            RegisterMechRecipe("MechModule_Armor", "Mech Armor Plating", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 15),
                new RecipeResourceInfo("Atlantum Mixture Brick", 5),
                new RecipeResourceInfo("Iron Plate", 20)
            });

            RegisterMechRecipe("MechModule_Shield", "Mech Energy Shield", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Processor Unit", 10),
                new RecipeResourceInfo("Electrical Components", 15),
                new RecipeResourceInfo("Shiverthorn Coolant", 10)
            });

            Log.LogInfo("Registered 7 mech suit recipes");
        }

        private void RegisterMechRecipe(string mechId, string displayName, List<RecipeResourceInfo> ingredients)
        {
            try
            {
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    GUID = MyGUID,
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 2,  // Requires advanced assembler
                    duration = 30f,  // Mech suits take longer to craft
                    unlockName = mechId,
                    ingredients = ingredients,
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo(mechId, 1)
                    },
                    sortPriority = 75
                });
                LogDebug($"Registered recipe for {mechId}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register recipe for {mechId}: {ex.Message}");
            }
        }

        private void CaptureGameShaders()
        {
            try
            {
                foreach (var mat in Resources.FindObjectsOfTypeAll<Material>())
                {
                    if (mat.shader != null)
                    {
                        string shaderName = mat.shader.name;
                        if (shaderName.Contains("Universal Render Pipeline/Lit") || shaderName.Contains("URP/Lit"))
                        {
                            capturedLitShader = mat.shader;
                            LogDebug($"Captured URP Lit shader: {shaderName}");
                            break;
                        }
                    }
                }

                if (capturedLitShader == null)
                {
                    capturedLitShader = Shader.Find("Standard");
                    LogDebug("Using Standard shader as fallback");
                }
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to capture shaders: {ex.Message}");
            }
        }

        private void LoadAssetBundles()
        {
            string bundlePath = Path.Combine(PluginPath, "Bundles");
            if (!Directory.Exists(bundlePath))
            {
                Log.LogWarning($"Bundles folder not found at {bundlePath}");
                return;
            }

            string[] bundlesToLoad = { "mech_companion" };

            foreach (string bundleName in bundlesToLoad)
            {
                string fullPath = Path.Combine(bundlePath, bundleName);
                if (File.Exists(fullPath))
                {
                    try
                    {
                        var bundle = AssetBundle.LoadFromFile(fullPath);
                        if (bundle != null)
                        {
                            loadedBundles[bundleName] = bundle;
                            string[] assetNames = bundle.GetAllAssetNames();
                            Log.LogInfo($"Loaded bundle '{bundleName}' with {assetNames.Length} assets");

                            foreach (var name in assetNames.Take(10))
                            {
                                LogDebug($"  - {name}");
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Log.LogError($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
                else
                {
                    LogDebug($"Bundle not found: {fullPath}");
                }
            }
        }

        public static GameObject GetPrefab(string bundleName, string prefabName)
        {
            string cacheKey = $"{bundleName}/{prefabName}";

            if (prefabCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                LogDebug($"Bundle not loaded: {bundleName}");
                return null;
            }

            string[] searchPatterns = {
                prefabName,
                prefabName.ToLower(),
                $"{prefabName}.prefab"
            };

            GameObject prefab = null;
            foreach (var pattern in searchPatterns)
            {
                prefab = bundle.LoadAsset<GameObject>(pattern);
                if (prefab != null) break;

                var allNames = bundle.GetAllAssetNames();
                var match = allNames.FirstOrDefault(n => n.Contains(pattern.ToLower()));
                if (match != null)
                {
                    prefab = bundle.LoadAsset<GameObject>(match);
                    if (prefab != null) break;
                }
            }

            if (prefab != null)
            {
                prefabCache[cacheKey] = prefab;
                LogDebug($"Loaded prefab: {prefabName} from {bundleName}");
            }

            return prefab;
        }

        private void Update()
        {
            if (!EnableMechSuits.Value || Player.instance == null) return;

            // Toggle suit
            if (Input.GetKeyDown(ToggleSuitKey.Value))
            {
                ToggleMechSuit();
            }

            // Use ability
            if (Input.GetKeyDown(AbilityKey.Value) && isSuitActive && abilityCooldown <= 0)
            {
                ActivateAbility();
            }

            // Update cooldown
            if (abilityCooldown > 0)
            {
                abilityCooldown -= Time.deltaTime;
            }

            // Update suit if active
            if (activeSuit != null)
            {
                activeSuit.UpdateSuit();
            }
        }

        private void ToggleMechSuit()
        {
            if (isSuitActive)
            {
                DeactivateSuit();
            }
            else
            {
                ActivateSuit();
            }
        }

        private void ActivateSuit()
        {
            if (Player.instance == null) return;

            string prefabName = SelectedVariant.Value switch
            {
                MechVariant.Heavy => "HPCharacter",
                MechVariant.Light => "PolyartCharacter",
                _ => "PBRCharacter"
            };

            GameObject prefab = GetPrefab("mech_companion", prefabName);
            GameObject suitObj;

            if (prefab != null)
            {
                suitObj = Instantiate(prefab, Player.instance.transform);
                suitObj.transform.localPosition = Vector3.zero;
                suitObj.transform.localRotation = Quaternion.identity;
                suitObj.transform.localScale = Vector3.one * SuitScale.Value;
                FixMaterials(suitObj);
            }
            else
            {
                // Create placeholder suit
                suitObj = CreatePlaceholderSuit();
            }

            suitObj.name = $"MechSuit_{SelectedVariant.Value}";

            // Add controller
            activeSuit = suitObj.AddComponent<MechSuitController>();
            activeSuit.Initialize(SelectedVariant.Value);

            isSuitActive = true;
            showUI = true;

            Log.LogInfo($"Mech suit activated: {SelectedVariant.Value}");
        }

        private void DeactivateSuit()
        {
            if (activeSuit != null)
            {
                activeSuit.Deactivate();
                Destroy(activeSuit.gameObject);
                activeSuit = null;
            }

            isSuitActive = false;
            showUI = false;

            Log.LogInfo("Mech suit deactivated");
        }

        private GameObject CreatePlaceholderSuit()
        {
            if (Player.instance == null) return null;

            GameObject suit = new GameObject("PlaceholderMechSuit");
            suit.transform.SetParent(Player.instance.transform);
            suit.transform.localPosition = Vector3.zero;
            suit.transform.localRotation = Quaternion.identity;

            // Create simple mech frame
            Color suitColor = SelectedVariant.Value switch
            {
                MechVariant.Heavy => new Color(0.3f, 0.35f, 0.4f),
                MechVariant.Light => new Color(0.5f, 0.6f, 0.7f),
                _ => new Color(0.4f, 0.45f, 0.5f)
            };

            // Torso
            var torso = GameObject.CreatePrimitive(PrimitiveType.Cube);
            torso.transform.SetParent(suit.transform);
            torso.transform.localPosition = new Vector3(0, 0.3f, 0);
            torso.transform.localScale = new Vector3(0.8f, 0.6f, 0.5f) * SuitScale.Value;
            torso.GetComponent<Renderer>().material.color = suitColor;
            Destroy(torso.GetComponent<Collider>());

            // Shoulder pads
            for (int side = -1; side <= 1; side += 2)
            {
                var shoulder = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                shoulder.transform.SetParent(suit.transform);
                shoulder.transform.localPosition = new Vector3(side * 0.5f, 0.4f, 0);
                shoulder.transform.localScale = Vector3.one * 0.25f * SuitScale.Value;
                shoulder.GetComponent<Renderer>().material.color = suitColor;
                Destroy(shoulder.GetComponent<Collider>());
            }

            // Helmet visor glow
            var visor = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visor.transform.SetParent(suit.transform);
            visor.transform.localPosition = new Vector3(0, 0.7f, 0.2f);
            visor.transform.localScale = new Vector3(0.15f, 0.08f, 0.05f) * SuitScale.Value;
            visor.GetComponent<Renderer>().material.color = new Color(0.2f, 0.8f, 1f);
            visor.GetComponent<Renderer>().material.SetColor("_EmissionColor", new Color(0.2f, 0.8f, 1f) * 2f);
            Destroy(visor.GetComponent<Collider>());

            return suit;
        }

        private void ActivateAbility()
        {
            if (activeSuit == null) return;

            string abilityName = SelectedVariant.Value switch
            {
                MechVariant.Heavy => "Ground Pound",
                MechVariant.Light => "Dash Burst",
                _ => "Power Boost"
            };

            activeSuit.TriggerAbility();

            float cooldown = SelectedVariant.Value switch
            {
                MechVariant.Heavy => 15f,
                MechVariant.Light => 8f,
                _ => 12f
            };

            abilityCooldown = cooldown;
            Log.LogInfo($"Activated ability: {abilityName} (cooldown: {cooldown}s)");
        }

        private void FixMaterials(GameObject obj)
        {
            if (capturedLitShader == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                foreach (var mat in renderer.materials)
                {
                    if (mat != null && mat.shader != null)
                    {
                        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                        mat.shader = capturedLitShader;

                        if (mainTex != null)
                        {
                            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                    }
                }
            }
        }

        private void OnGUI()
        {
            if (!showUI || !isSuitActive) return;

            InitStyles();
            uiRect = GUI.Window(56789, uiRect, DrawSuitUI, "Mech Suit HUD", windowStyle);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTex(2, 2, new Color(0.1f, 0.15f, 0.2f, 0.85f));
            windowStyle.padding = new RectOffset(10, 10, 25, 10);

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.normal.textColor = new Color(0.7f, 0.9f, 1f);
            labelStyle.fontSize = 12;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.3f, 0.4f));
            buttonStyle.hover.background = MakeTex(2, 2, new Color(0.3f, 0.4f, 0.5f));
            buttonStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++) pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawSuitUI(int windowID)
        {
            GUILayout.BeginVertical();

            GUILayout.Label($"Variant: {SelectedVariant.Value}", labelStyle);
            GUILayout.Space(5);

            if (activeSuit != null)
            {
                GUILayout.Label($"Speed Boost: {activeSuit.SpeedMultiplier:F1}x", labelStyle);
                GUILayout.Label($"Damage Resist: {(1f - activeSuit.DamageResistance) * 100:F0}%", labelStyle);
                GUILayout.Label($"Jump Power: {activeSuit.JumpMultiplier:F1}x", labelStyle);
            }

            GUILayout.Space(10);

            string abilityName = SelectedVariant.Value switch
            {
                MechVariant.Heavy => "Ground Pound",
                MechVariant.Light => "Dash Burst",
                _ => "Power Boost"
            };

            if (abilityCooldown > 0)
            {
                GUILayout.Label($"Ability: {abilityName} ({abilityCooldown:F1}s)", labelStyle);
            }
            else
            {
                GUILayout.Label($"Ability: {abilityName} [Press {AbilityKey.Value}]", labelStyle);
            }

            GUILayout.Space(10);

            if (GUILayout.Button($"Deactivate ({ToggleSuitKey.Value})", buttonStyle))
            {
                DeactivateSuit();
            }

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0, 0, uiRect.width, 25));
        }

        private void OnDestroy()
        {
            foreach (var bundle in loadedBundles.Values)
            {
                bundle?.Unload(true);
            }
            loadedBundles.Clear();
            prefabCache.Clear();
        }

        public static void LogDebug(string message)
        {
            if (DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }

    /// <summary>
    /// Controller for active mech suit - handles buffs and abilities
    /// </summary>
    public class MechSuitController : MonoBehaviour
    {
        public MechSuitPlugin.MechVariant Variant { get; private set; }
        public float SpeedMultiplier { get; private set; } = 1f;
        public float DamageResistance { get; private set; } = 1f;
        public float JumpMultiplier { get; private set; } = 1f;

        private float abilityDuration = 0f;
        private bool isAbilityActive = false;
        private ParticleSystem suitParticles;

        public void Initialize(MechSuitPlugin.MechVariant variant)
        {
            Variant = variant;

            // Set stats based on variant
            switch (Variant)
            {
                case MechSuitPlugin.MechVariant.Heavy:
                    SpeedMultiplier = 0.8f;
                    DamageResistance = 0.5f;  // 50% damage taken
                    JumpMultiplier = 0.7f;
                    break;

                case MechSuitPlugin.MechVariant.Light:
                    SpeedMultiplier = 1.4f;
                    DamageResistance = 0.85f;  // 85% damage taken
                    JumpMultiplier = 1.5f;
                    break;

                default: // Standard
                    SpeedMultiplier = 1.15f;
                    DamageResistance = 0.7f;  // 70% damage taken
                    JumpMultiplier = 1.2f;
                    break;
            }

            // Add particle effects
            SetupParticles();

            MechSuitPlugin.Log.LogInfo($"MechSuit initialized: Speed={SpeedMultiplier:F2}x, Resist={DamageResistance:F2}, Jump={JumpMultiplier:F2}x");
        }

        private void SetupParticles()
        {
            var particleObj = new GameObject("SuitParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.down * 0.5f;

            suitParticles = particleObj.AddComponent<ParticleSystem>();
            var main = suitParticles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = Variant switch
            {
                MechSuitPlugin.MechVariant.Heavy => new Color(0.8f, 0.4f, 0.2f, 0.5f),
                MechSuitPlugin.MechVariant.Light => new Color(0.3f, 0.7f, 1f, 0.5f),
                _ => new Color(0.5f, 1f, 0.5f, 0.5f)
            };
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = suitParticles.emission;
            emission.rateOverTime = 10;

            var shape = suitParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;
        }

        public void UpdateSuit()
        {
            // Follow player
            if (Player.instance != null)
            {
                transform.position = Player.instance.transform.position;
                transform.rotation = Player.instance.transform.rotation;
            }

            // Handle ability duration
            if (isAbilityActive)
            {
                abilityDuration -= Time.deltaTime;
                if (abilityDuration <= 0)
                {
                    EndAbility();
                }
            }
        }

        public void TriggerAbility()
        {
            isAbilityActive = true;

            switch (Variant)
            {
                case MechSuitPlugin.MechVariant.Heavy:
                    // Ground Pound - AOE stun effect
                    abilityDuration = 0.5f;
                    StartCoroutine(GroundPoundEffect());
                    break;

                case MechSuitPlugin.MechVariant.Light:
                    // Dash Burst - quick forward dash
                    abilityDuration = 0.3f;
                    StartCoroutine(DashBurstEffect());
                    break;

                default:
                    // Power Boost - temporary stat increase
                    abilityDuration = 5f;
                    SpeedMultiplier *= 1.5f;
                    JumpMultiplier *= 1.3f;
                    break;
            }

            // Enhance particles during ability
            if (suitParticles != null)
            {
                var emission = suitParticles.emission;
                emission.rateOverTime = 50;
            }
        }

        private void EndAbility()
        {
            isAbilityActive = false;

            // Reset stats if power boost was active
            if (Variant == MechSuitPlugin.MechVariant.Standard)
            {
                SpeedMultiplier = 1.15f;
                JumpMultiplier = 1.2f;
            }

            // Reset particles
            if (suitParticles != null)
            {
                var emission = suitParticles.emission;
                emission.rateOverTime = 10;
            }
        }

        private IEnumerator GroundPoundEffect()
        {
            // Create shockwave
            var shockwave = new GameObject("GroundPoundShockwave");
            shockwave.transform.position = transform.position;

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(shockwave.transform);
            ring.transform.localScale = new Vector3(1f, 0.1f, 1f);
            ring.GetComponent<Renderer>().material.color = new Color(0.8f, 0.4f, 0.2f, 0.7f);
            Destroy(ring.GetComponent<Collider>());

            // Expand ring
            float elapsed = 0f;
            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 15f, elapsed / 0.5f);
                ring.transform.localScale = new Vector3(scale, 0.1f * (1f - elapsed), scale);
                yield return null;
            }

            Destroy(shockwave);
        }

        private IEnumerator DashBurstEffect()
        {
            if (Player.instance == null) yield break;

            Vector3 dashDir = Player.instance.transform.forward;
            float dashDistance = 10f;
            float dashTime = 0.3f;
            Vector3 startPos = Player.instance.transform.position;
            Vector3 endPos = startPos + dashDir * dashDistance;

            float elapsed = 0f;
            while (elapsed < dashTime)
            {
                elapsed += Time.deltaTime;
                // Note: In real implementation, would need to move the player
                // This is a visual effect placeholder
                yield return null;
            }

            // Trail effect
            var trail = new GameObject("DashTrail");
            trail.transform.position = startPos;

            var line = trail.AddComponent<LineRenderer>();
            line.startWidth = 0.5f;
            line.endWidth = 0.1f;
            line.positionCount = 2;
            line.SetPosition(0, startPos);
            line.SetPosition(1, endPos);
            line.material = new Material(Shader.Find("Standard"));
            line.material.color = new Color(0.3f, 0.7f, 1f, 0.5f);

            yield return new WaitForSeconds(0.5f);
            Destroy(trail);
        }

        public void Deactivate()
        {
            // Cleanup
            if (suitParticles != null)
            {
                suitParticles.Stop();
            }
        }
    }
}
