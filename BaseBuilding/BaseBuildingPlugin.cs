using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using EquinoxsModUtils.Additions;
using HarmonyLib;
using UnityEngine;

namespace BaseBuilding
{
    /// <summary>
    /// BaseBuilding - Decorative structures and base-building elements using Sci-Fi Modular Pack
    /// Adds walls, floors, doors, corridors, stairs, and decorative pieces for factory aesthetics
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    public class BaseBuildingPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.BaseBuilding";
        public const string PluginName = "BaseBuilding";
        public const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static BaseBuildingPlugin Instance;
        public static string PluginPath;

        // ========== CONFIGURATION ==========
        public static ConfigEntry<bool> EnableDecorations;
        public static ConfigEntry<KeyCode> SpawnMenuKey;
        public static ConfigEntry<float> DecorScale;
        public static ConfigEntry<bool> DebugMode;

        // ========== ASSET MANAGEMENT ==========
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();
        private static Shader capturedLitShader;
        private static Shader capturedUnlitShader;

        // ========== STRUCTURE CATEGORIES ==========
        public static readonly Dictionary<string, string[]> StructureCategories = new Dictionary<string, string[]>
        {
            { "Walls", new[] { "Wall_01", "Wall_02", "Wall_03", "Wall_04", "Wall_Corner", "Wall_Door_Frame" } },
            { "Floors", new[] { "Floor_01", "Floor_02", "Floor_03", "Floor_Grate", "Platform_01" } },
            { "Doors", new[] { "Door_01", "Door_02", "Door_Sliding", "Airlock_Door" } },
            { "Corridors", new[] { "Corridor_01", "Corridor_02", "Corridor_Corner", "Corridor_T", "Corridor_Cross" } },
            { "Stairs", new[] { "Stairs_01", "Stairs_02", "Ladder_01", "Ramp_01" } },
            { "Lights", new[] { "Light_01", "Light_02", "Light_Wall", "Light_Ceiling", "Light_Spot" } },
            { "Decorations", new[] { "Terminal_01", "Console_01", "Crate_01", "Crate_02", "Barrel_01", "Tank_01" } },
            { "Barriers", new[] { "Barrier_01", "Barrier_02", "Fence_01", "Railing_01" } }
        };

        // ========== UI STATE ==========
        private bool showSpawnMenu = false;
        private Rect menuRect = new Rect(100, 100, 350, 450);
        private Vector2 scrollPosition = Vector2.zero;
        private string selectedCategory = "Walls";
        private List<GameObject> placedStructures = new List<GameObject>();

        // ========== UI STYLES ==========
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle buttonStyle;
        private GUIStyle categoryStyle;
        private bool stylesInitialized = false;

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
            EnableDecorations = Config.Bind("General", "Enable Decorations", true,
                "Enable decorative structure spawning");
            SpawnMenuKey = Config.Bind("Controls", "Spawn Menu Key", KeyCode.F9,
                "Key to open the structure spawn menu (F9 - no conflicts with game or other mods)");
            DecorScale = Config.Bind("Visuals", "Decoration Scale", 1.5f,
                new ConfigDescription("Scale multiplier for spawned structures", new AcceptableValueRange<float>(0.5f, 5f)));
            DebugMode = Config.Bind("Debug", "Debug Mode", false,
                "Enable debug logging");
        }

        private void OnGameLoaded()
        {
            Log.LogInfo("Game loaded - initializing BaseBuilding...");
            CaptureGameShaders();
            LoadAssetBundles();
            RegisterCraftingRecipes();
        }

        private void RegisterCraftingRecipes()
        {
            Log.LogInfo("Registering BaseBuilding crafting recipes...");

            // ========== WALLS ==========
            RegisterStructureRecipe("Wall_01", "Basic Wall Panel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 4),
                new RecipeResourceInfo("Limestone", 2)
            });
            RegisterStructureRecipe("Wall_02", "Reinforced Wall Panel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Iron Plate", 4)
            });
            RegisterStructureRecipe("Wall_03", "Heavy Wall Panel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Concrete", 4)
            });
            RegisterStructureRecipe("Wall_04", "Armored Wall Panel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 6),
                new RecipeResourceInfo("Steel Mixture", 4),
                new RecipeResourceInfo("Concrete", 2)
            });
            RegisterStructureRecipe("Wall_Corner", "Wall Corner Piece", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 3),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Wall_Door_Frame", "Door Frame Wall", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Mechanical Components", 2)
            });

            // ========== FLOORS ==========
            RegisterStructureRecipe("Floor_01", "Basic Floor Tile", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 4),
                new RecipeResourceInfo("Concrete", 2)
            });
            RegisterStructureRecipe("Floor_02", "Reinforced Floor Tile", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Floor_03", "Industrial Floor Tile", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Concrete", 4)
            });
            RegisterStructureRecipe("Floor_Grate", "Grated Floor Panel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 6),
                new RecipeResourceInfo("Steel Frame", 1)
            });
            RegisterStructureRecipe("Platform_01", "Platform Base", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Concrete", 6)
            });

            // ========== DOORS ==========
            RegisterStructureRecipe("Door_01", "Basic Door", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Plate", 4),
                new RecipeResourceInfo("Mechanical Components", 2)
            });
            RegisterStructureRecipe("Door_02", "Reinforced Door", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Mechanical Components", 4)
            });
            RegisterStructureRecipe("Door_Sliding", "Sliding Door", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Mechanical Components", 4),
                new RecipeResourceInfo("Electrical Components", 2)
            });
            RegisterStructureRecipe("Airlock_Door", "Airlock Door", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Mechanical Components", 6),
                new RecipeResourceInfo("Processor Unit", 1)
            });

            // ========== CORRIDORS ==========
            RegisterStructureRecipe("Corridor_01", "Corridor Segment", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Iron Plate", 6)
            });
            RegisterStructureRecipe("Corridor_02", "Wide Corridor Segment", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 6),
                new RecipeResourceInfo("Iron Plate", 8)
            });
            RegisterStructureRecipe("Corridor_Corner", "Corridor Corner", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 5),
                new RecipeResourceInfo("Iron Plate", 6)
            });
            RegisterStructureRecipe("Corridor_T", "Corridor T-Junction", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 6),
                new RecipeResourceInfo("Iron Plate", 8)
            });
            RegisterStructureRecipe("Corridor_Cross", "Corridor Crossroads", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 8),
                new RecipeResourceInfo("Iron Plate", 10)
            });

            // ========== STAIRS ==========
            RegisterStructureRecipe("Stairs_01", "Basic Staircase", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Iron Plate", 4)
            });
            RegisterStructureRecipe("Stairs_02", "Industrial Staircase", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 6),
                new RecipeResourceInfo("Iron Plate", 6)
            });
            RegisterStructureRecipe("Ladder_01", "Access Ladder", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 4),
                new RecipeResourceInfo("Steel Frame", 1)
            });
            RegisterStructureRecipe("Ramp_01", "Access Ramp", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 3),
                new RecipeResourceInfo("Concrete", 4)
            });

            // ========== LIGHTS ==========
            RegisterStructureRecipe("Light_01", "Basic Light", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Electrical Components", 2),
                new RecipeResourceInfo("Iron Plate", 1)
            });
            RegisterStructureRecipe("Light_02", "Industrial Light", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Electrical Components", 4),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Light_Wall", "Wall Mounted Light", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Electrical Components", 2),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Light_Ceiling", "Ceiling Light", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Electrical Components", 3),
                new RecipeResourceInfo("Steel Frame", 1)
            });
            RegisterStructureRecipe("Light_Spot", "Spotlight", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Electrical Components", 4),
                new RecipeResourceInfo("Processor Unit", 1)
            });

            // ========== DECORATIONS ==========
            RegisterStructureRecipe("Terminal_01", "Control Terminal", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Processor Unit", 2),
                new RecipeResourceInfo("Electrical Components", 4),
                new RecipeResourceInfo("Iron Plate", 4)
            });
            RegisterStructureRecipe("Console_01", "Console Unit", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Processor Unit", 1),
                new RecipeResourceInfo("Electrical Components", 2),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Crate_01", "Storage Crate", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Plate", 4),
                new RecipeResourceInfo("Iron Ingot", 2)
            });
            RegisterStructureRecipe("Crate_02", "Large Storage Crate", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Iron Plate", 6)
            });
            RegisterStructureRecipe("Barrel_01", "Storage Barrel", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Plate", 3),
                new RecipeResourceInfo("Iron Ingot", 2)
            });
            RegisterStructureRecipe("Tank_01", "Fluid Tank", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 4),
                new RecipeResourceInfo("Iron Plate", 8),
                new RecipeResourceInfo("Mechanical Components", 2)
            });

            // ========== BARRIERS ==========
            RegisterStructureRecipe("Barrier_01", "Safety Barrier", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 4),
                new RecipeResourceInfo("Iron Plate", 2)
            });
            RegisterStructureRecipe("Barrier_02", "Heavy Barrier", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Steel Frame", 2),
                new RecipeResourceInfo("Concrete", 2)
            });
            RegisterStructureRecipe("Fence_01", "Security Fence", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 6),
                new RecipeResourceInfo("Steel Frame", 1)
            });
            RegisterStructureRecipe("Railing_01", "Safety Railing", new List<RecipeResourceInfo>
            {
                new RecipeResourceInfo("Iron Ingot", 3),
                new RecipeResourceInfo("Iron Plate", 1)
            });

            Log.LogInfo($"Registered {StructureCategories.Values.Sum(s => s.Length)} structure recipes");
        }

        private void RegisterStructureRecipe(string structureId, string displayName, List<RecipeResourceInfo> ingredients)
        {
            try
            {
                EMUAdditions.AddNewRecipe(new NewRecipeDetails
                {
                    GUID = MyGUID,
                    craftingMethod = CraftingMethod.Assembler,
                    craftTierRequired = 0,
                    duration = 10f,
                    unlockName = $"BaseBuilding_{structureId}",
                    ingredients = ingredients,
                    outputs = new List<RecipeResourceInfo>
                    {
                        new RecipeResourceInfo(structureId, 1)
                    },
                    sortPriority = 50
                });
                LogDebug($"Registered recipe for {structureId}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Failed to register recipe for {structureId}: {ex.Message}");
            }
        }

        private void CaptureGameShaders()
        {
            try
            {
                // Find URP Lit shader from existing game materials
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

                // Fallback to Standard shader if URP not found
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

            string[] bundlesToLoad = {
                "scifi_modular"  // Sci-Fi Styled Modular Pack
            };

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

                            // Log first few assets for debugging
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

            // Try different name variations
            string[] searchPatterns = {
                prefabName,
                prefabName.ToLower(),
                $"{prefabName}.prefab",
                $"assets/{prefabName}.prefab",
                $"prefabs/{prefabName}.prefab"
            };

            GameObject prefab = null;
            foreach (var pattern in searchPatterns)
            {
                prefab = bundle.LoadAsset<GameObject>(pattern);
                if (prefab != null) break;

                // Also try partial match
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
            else
            {
                LogDebug($"Prefab not found: {prefabName} in {bundleName}");
            }

            return prefab;
        }

        private void Update()
        {
            if (!EnableDecorations.Value) return;

            // Toggle spawn menu
            if (Input.GetKeyDown(SpawnMenuKey.Value))
            {
                showSpawnMenu = !showSpawnMenu;
            }
        }

        private void OnGUI()
        {
            if (!showSpawnMenu) return;

            InitStyles();
            menuRect = GUI.Window(45678, menuRect, DrawSpawnMenu, "BaseBuilding - Structure Spawner", windowStyle);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTex(2, 2, new Color(0.12f, 0.14f, 0.18f, 0.95f));
            windowStyle.padding = new RectOffset(10, 10, 25, 10);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.7f, 0.85f, 1f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.25f, 0.3f, 0.4f));
            buttonStyle.hover.background = MakeTex(2, 2, new Color(0.35f, 0.4f, 0.5f));
            buttonStyle.active.background = MakeTex(2, 2, new Color(0.2f, 0.5f, 0.7f));
            buttonStyle.normal.textColor = Color.white;

            categoryStyle = new GUIStyle(GUI.skin.button);
            categoryStyle.normal.background = MakeTex(2, 2, new Color(0.2f, 0.25f, 0.35f));
            categoryStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawSpawnMenu(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label($"Press {SpawnMenuKey.Value} to toggle | Scale: {DecorScale.Value:F1}x", headerStyle);
            GUILayout.Space(5);

            // Category tabs
            GUILayout.BeginHorizontal();
            foreach (var category in StructureCategories.Keys)
            {
                var style = category == selectedCategory ? buttonStyle : categoryStyle;
                if (GUILayout.Button(category, style, GUILayout.Height(25)))
                {
                    selectedCategory = category;
                }
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Structure list
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(250));

            if (StructureCategories.TryGetValue(selectedCategory, out var structures))
            {
                foreach (var structureName in structures)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(structureName, GUILayout.Width(180));
                    if (GUILayout.Button("Spawn", buttonStyle, GUILayout.Width(70)))
                    {
                        SpawnStructure(structureName);
                    }
                    GUILayout.EndHorizontal();
                    GUILayout.Space(2);
                }
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Info section
            GUILayout.Label($"Placed structures: {placedStructures.Count}", headerStyle);

            // Controls
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Clear All", buttonStyle))
            {
                ClearAllStructures();
            }
            if (GUILayout.Button("Undo Last", buttonStyle))
            {
                UndoLastStructure();
            }
            if (GUILayout.Button("Close", buttonStyle))
            {
                showSpawnMenu = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, menuRect.width, 25));
        }

        private void SpawnStructure(string structureName)
        {
            if (Player.instance == null) return;

            Vector3 spawnPos = Player.instance.transform.position + Player.instance.transform.forward * 5f;

            // Try to load from bundle
            GameObject prefab = GetPrefab("scifi_modular", structureName);
            GameObject instance;

            if (prefab != null)
            {
                instance = Instantiate(prefab, spawnPos, Quaternion.identity);
                instance.transform.localScale = Vector3.one * DecorScale.Value;
                FixMaterials(instance);
                Log.LogInfo($"Spawned structure '{structureName}' from bundle");
            }
            else
            {
                // Fallback: create primitive placeholder
                instance = CreatePlaceholderStructure(structureName, spawnPos);
                Log.LogInfo($"Created placeholder for '{structureName}' (bundle prefab not found)");
            }

            instance.name = $"BaseBuilding_{structureName}";
            placedStructures.Add(instance);
        }

        private GameObject CreatePlaceholderStructure(string structureName, Vector3 position)
        {
            GameObject structure = new GameObject(structureName);
            structure.transform.position = position;

            PrimitiveType primType = structureName.ToLower() switch
            {
                var s when s.Contains("wall") => PrimitiveType.Cube,
                var s when s.Contains("floor") || s.Contains("platform") => PrimitiveType.Cube,
                var s when s.Contains("door") => PrimitiveType.Cube,
                var s when s.Contains("corridor") => PrimitiveType.Cube,
                var s when s.Contains("stair") || s.Contains("ramp") => PrimitiveType.Cube,
                var s when s.Contains("light") => PrimitiveType.Sphere,
                var s when s.Contains("barrel") || s.Contains("tank") => PrimitiveType.Cylinder,
                var s when s.Contains("crate") => PrimitiveType.Cube,
                _ => PrimitiveType.Cube
            };

            var primitive = GameObject.CreatePrimitive(primType);
            primitive.transform.SetParent(structure.transform);
            primitive.transform.localPosition = Vector3.zero;

            // Scale based on type
            Vector3 scale = structureName.ToLower() switch
            {
                var s when s.Contains("wall") => new Vector3(4f, 3f, 0.3f) * DecorScale.Value,
                var s when s.Contains("floor") => new Vector3(4f, 0.2f, 4f) * DecorScale.Value,
                var s when s.Contains("door") => new Vector3(1.2f, 2.5f, 0.2f) * DecorScale.Value,
                var s when s.Contains("corridor") => new Vector3(3f, 3f, 4f) * DecorScale.Value,
                var s when s.Contains("stair") => new Vector3(2f, 2f, 4f) * DecorScale.Value,
                var s when s.Contains("light") => Vector3.one * 0.3f * DecorScale.Value,
                var s when s.Contains("crate") => Vector3.one * 1f * DecorScale.Value,
                var s when s.Contains("barrel") => new Vector3(0.6f, 1f, 0.6f) * DecorScale.Value,
                _ => Vector3.one * DecorScale.Value
            };
            primitive.transform.localScale = scale;

            // Color based on category
            Color color = selectedCategory switch
            {
                "Walls" => new Color(0.4f, 0.45f, 0.5f),
                "Floors" => new Color(0.35f, 0.35f, 0.4f),
                "Doors" => new Color(0.5f, 0.5f, 0.55f),
                "Corridors" => new Color(0.38f, 0.4f, 0.45f),
                "Stairs" => new Color(0.42f, 0.42f, 0.48f),
                "Lights" => new Color(1f, 0.9f, 0.7f),
                "Decorations" => new Color(0.5f, 0.4f, 0.3f),
                "Barriers" => new Color(0.6f, 0.4f, 0.2f),
                _ => Color.gray
            };

            var renderer = primitive.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = color;
            }

            // Add light component for light structures
            if (structureName.ToLower().Contains("light"))
            {
                var light = structure.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 10f;
                light.intensity = 2f;
                light.color = new Color(1f, 0.95f, 0.85f);
            }

            return structure;
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
                        // Store texture references
                        Texture mainTex = mat.HasProperty("_MainTex") ? mat.GetTexture("_MainTex") : null;
                        Texture normalMap = mat.HasProperty("_BumpMap") ? mat.GetTexture("_BumpMap") : null;
                        Color color = mat.HasProperty("_Color") ? mat.GetColor("_Color") : Color.white;

                        // Switch to captured shader
                        mat.shader = capturedLitShader;

                        // Restore textures
                        if (mainTex != null)
                        {
                            if (mat.HasProperty("_BaseMap")) mat.SetTexture("_BaseMap", mainTex);
                            if (mat.HasProperty("_MainTex")) mat.SetTexture("_MainTex", mainTex);
                        }
                        if (normalMap != null && mat.HasProperty("_BumpMap"))
                        {
                            mat.SetTexture("_BumpMap", normalMap);
                        }
                        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
                        if (mat.HasProperty("_Color")) mat.SetColor("_Color", color);
                    }
                }
            }
        }

        private void ClearAllStructures()
        {
            foreach (var structure in placedStructures)
            {
                if (structure != null)
                {
                    Destroy(structure);
                }
            }
            placedStructures.Clear();
            Log.LogInfo("Cleared all placed structures");
        }

        private void UndoLastStructure()
        {
            if (placedStructures.Count > 0)
            {
                var last = placedStructures[placedStructures.Count - 1];
                if (last != null)
                {
                    Destroy(last);
                }
                placedStructures.RemoveAt(placedStructures.Count - 1);
                Log.LogInfo("Undid last structure placement");
            }
        }

        private void OnDestroy()
        {
            // Unload bundles
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
}
