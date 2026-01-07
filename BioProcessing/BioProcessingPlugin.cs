using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.IO;
using System.Collections.Generic;

namespace BioProcessing
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.equinox.EquinoxsModUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class BioProcessingPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.certifried.bioprocessing";
        public const string NAME = "BioProcessing";
        public const string VERSION = "1.0.0";

        private static BioProcessingPlugin instance;
        public static ManualLogSource Log;
        private Harmony harmony;

        // Configuration
        public static ConfigEntry<float> AlgaeGrowthRate;
        public static ConfigEntry<float> MushroomGrowthRate;
        public static ConfigEntry<float> BiofuelYield;
        public static ConfigEntry<float> CompostEfficiency;
        public static ConfigEntry<bool> EnableBioRemediation;

        // Asset Bundles
        private static Dictionary<string, AssetBundle> loadedBundles = new Dictionary<string, AssetBundle>();
        private static Dictionary<string, GameObject> prefabCache = new Dictionary<string, GameObject>();

        // Active facilities
        public static List<AlgaeVatController> ActiveAlgaeVats = new List<AlgaeVatController>();
        public static List<MushroomFarmController> ActiveMushroomFarms = new List<MushroomFarmController>();
        public static List<BioReactorController> ActiveBioReactors = new List<BioReactorController>();
        public static List<ComposterController> ActiveComposters = new List<ComposterController>();

        // Resource tracking
        public static float TotalBiofuelProduced = 0f;
        public static float TotalCompostProduced = 0f;
        public static int TotalPlantsHarvested = 0;

        // Material cache
        private static Material cachedMaterial;

        void Awake()
        {
            instance = this;
            Log = Logger;
            Logger.LogInfo($"{NAME} v{VERSION} loading...");

            InitializeConfig();
            LoadAssetBundles();

            harmony = new Harmony(GUID);
            harmony.PatchAll();

            Logger.LogInfo($"{NAME} loaded successfully!");
        }

        private void InitializeConfig()
        {
            AlgaeGrowthRate = Config.Bind("Production", "AlgaeGrowthRate", 1f,
                new ConfigDescription("Multiplier for algae growth speed",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            MushroomGrowthRate = Config.Bind("Production", "MushroomGrowthRate", 1f,
                new ConfigDescription("Multiplier for mushroom growth speed",
                    new AcceptableValueRange<float>(0.1f, 5f)));

            BiofuelYield = Config.Bind("Production", "BiofuelYield", 1f,
                new ConfigDescription("Multiplier for biofuel output",
                    new AcceptableValueRange<float>(0.5f, 3f)));

            CompostEfficiency = Config.Bind("Production", "CompostEfficiency", 1f,
                new ConfigDescription("Multiplier for compost conversion rate",
                    new AcceptableValueRange<float>(0.5f, 2f)));

            EnableBioRemediation = Config.Bind("Features", "EnableBioRemediation", true,
                "Allow biological cleanup of hazardous zones");
        }

        private void LoadAssetBundles()
        {
            string bundlePath = Path.Combine(Path.GetDirectoryName(Info.Location), "Bundles");

            if (!Directory.Exists(bundlePath))
            {
                Logger.LogWarning($"Bundles folder not found at {bundlePath}");
                return;
            }

            string[] bundleNames = { "mushroom_forest", "lava_plants", "fauna_turtle" };

            foreach (var bundleName in bundleNames)
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
                            Logger.LogInfo($"Loaded bundle: {bundleName}");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to load bundle {bundleName}: {ex.Message}");
                    }
                }
            }
        }

        void Update()
        {
            // Clean up destroyed facilities
            ActiveAlgaeVats.RemoveAll(v => v == null);
            ActiveMushroomFarms.RemoveAll(f => f == null);
            ActiveBioReactors.RemoveAll(r => r == null);
            ActiveComposters.RemoveAll(c => c == null);
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();

            foreach (var bundle in loadedBundles.Values)
            {
                bundle?.Unload(true);
            }
            loadedBundles.Clear();
        }

        #region Asset Loading

        public static GameObject GetPrefab(string bundleName, string prefabName)
        {
            string key = $"{bundleName}/{prefabName}";

            if (prefabCache.TryGetValue(key, out var cached))
                return cached;

            if (!loadedBundles.TryGetValue(bundleName, out var bundle))
            {
                instance?.Logger.LogWarning($"Bundle not loaded: {bundleName}");
                return null;
            }

            var prefab = bundle.LoadAsset<GameObject>(prefabName);
            if (prefab != null)
            {
                prefabCache[key] = prefab;
            }

            return prefab;
        }

        public static void FixPrefabMaterials(GameObject obj)
        {
            if (cachedMaterial == null)
            {
                var gameRenderers = UnityEngine.Object.FindObjectsOfType<Renderer>();
                foreach (var r in gameRenderers)
                {
                    if (r.material != null && r.material.shader != null &&
                        r.material.shader.name.Contains("Universal"))
                    {
                        cachedMaterial = r.material;
                        break;
                    }
                }
            }

            if (cachedMaterial == null) return;

            var renderers = obj.GetComponentsInChildren<Renderer>(true);
            foreach (var renderer in renderers)
            {
                var materials = renderer.materials;
                for (int i = 0; i < materials.Length; i++)
                {
                    if (materials[i] != null && materials[i].shader != null)
                    {
                        if (!materials[i].shader.name.Contains("Universal") &&
                            !materials[i].shader.name.Contains("URP"))
                        {
                            var newMat = new Material(cachedMaterial);
                            if (materials[i].mainTexture != null)
                                newMat.mainTexture = materials[i].mainTexture;
                            newMat.color = materials[i].color;
                            materials[i] = newMat;
                        }
                    }
                }
                renderer.materials = materials;
            }
        }

        public static Material GetEffectMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }

        #endregion

        #region Facility Spawning

        public static AlgaeVatController SpawnAlgaeVat(Vector3 position)
        {
            var vatObj = CreateAlgaeVatPrimitive();
            vatObj.transform.position = position;
            vatObj.name = $"AlgaeVat_{ActiveAlgaeVats.Count}";

            var controller = vatObj.AddComponent<AlgaeVatController>();
            controller.Initialize();

            ActiveAlgaeVats.Add(controller);
            Log.LogInfo($"Spawned Algae Vat at {position}");

            return controller;
        }

        public static MushroomFarmController SpawnMushroomFarm(Vector3 position)
        {
            // Try to load from bundle
            var prefab = GetPrefab("mushroom_forest", "MushroomCluster");
            GameObject farmObj;

            if (prefab != null)
            {
                farmObj = UnityEngine.Object.Instantiate(prefab, position, Quaternion.identity);
                FixPrefabMaterials(farmObj);
            }
            else
            {
                farmObj = CreateMushroomFarmPrimitive();
                farmObj.transform.position = position;
            }

            farmObj.name = $"MushroomFarm_{ActiveMushroomFarms.Count}";

            var controller = farmObj.AddComponent<MushroomFarmController>();
            controller.Initialize();

            ActiveMushroomFarms.Add(controller);
            Log.LogInfo($"Spawned Mushroom Farm at {position}");

            return controller;
        }

        public static BioReactorController SpawnBioReactor(Vector3 position)
        {
            var reactorObj = CreateBioReactorPrimitive();
            reactorObj.transform.position = position;
            reactorObj.name = $"BioReactor_{ActiveBioReactors.Count}";

            var controller = reactorObj.AddComponent<BioReactorController>();
            controller.Initialize();

            ActiveBioReactors.Add(controller);
            Log.LogInfo($"Spawned Bio Reactor at {position}");

            return controller;
        }

        public static ComposterController SpawnComposter(Vector3 position)
        {
            var composterObj = CreateComposterPrimitive();
            composterObj.transform.position = position;
            composterObj.name = $"Composter_{ActiveComposters.Count}";

            var controller = composterObj.AddComponent<ComposterController>();
            controller.Initialize();

            ActiveComposters.Add(controller);
            Log.LogInfo($"Spawned Composter at {position}");

            return controller;
        }

        #endregion

        #region Primitive Creation

        private static GameObject CreateAlgaeVatPrimitive()
        {
            var vat = new GameObject("AlgaeVat");

            // Tank body
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = "Tank";
            tank.transform.SetParent(vat.transform);
            tank.transform.localPosition = Vector3.up * 1f;
            tank.transform.localScale = new Vector3(2f, 2f, 2f);

            var tankMat = GetEffectMaterial(new Color(0.2f, 0.6f, 0.3f, 0.7f));
            tank.GetComponent<Renderer>().material = tankMat;

            // Bubbles effect
            var bubblesObj = new GameObject("Bubbles");
            bubblesObj.transform.SetParent(vat.transform);
            bubblesObj.transform.localPosition = Vector3.up * 0.5f;

            var bubbles = bubblesObj.AddComponent<ParticleSystem>();
            var main = bubbles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.5f, 1f, 0.5f, 0.5f);

            var emission = bubbles.emission;
            emission.rateOverTime = 10f;

            var shape = bubbles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.8f;

            return vat;
        }

        private static GameObject CreateMushroomFarmPrimitive()
        {
            var farm = new GameObject("MushroomFarm");

            // Base plot
            var plot = GameObject.CreatePrimitive(PrimitiveType.Cube);
            plot.name = "Plot";
            plot.transform.SetParent(farm.transform);
            plot.transform.localPosition = Vector3.up * 0.1f;
            plot.transform.localScale = new Vector3(3f, 0.2f, 3f);
            plot.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.3f, 0.2f, 0.1f));

            // Mushroom caps (simple representation)
            for (int i = 0; i < 5; i++)
            {
                var mushroom = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                mushroom.name = $"Mushroom_{i}";
                mushroom.transform.SetParent(farm.transform);

                float x = UnityEngine.Random.Range(-1f, 1f);
                float z = UnityEngine.Random.Range(-1f, 1f);
                float height = UnityEngine.Random.Range(0.5f, 1f);

                mushroom.transform.localPosition = new Vector3(x, 0.2f + height * 0.5f, z);
                mushroom.transform.localScale = new Vector3(0.4f, height, 0.4f);

                Color mushroomColor = new Color(
                    0.8f + UnityEngine.Random.value * 0.2f,
                    0.4f + UnityEngine.Random.value * 0.2f,
                    0.2f + UnityEngine.Random.value * 0.2f
                );
                mushroom.GetComponent<Renderer>().material = GetEffectMaterial(mushroomColor);
            }

            return farm;
        }

        private static GameObject CreateBioReactorPrimitive()
        {
            var reactor = new GameObject("BioReactor");

            // Main chamber
            var chamber = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            chamber.name = "Chamber";
            chamber.transform.SetParent(reactor.transform);
            chamber.transform.localPosition = Vector3.up * 1.5f;
            chamber.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            chamber.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.4f, 0.5f, 0.3f, 0.8f));

            // Pipes
            for (int i = 0; i < 4; i++)
            {
                var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                pipe.name = $"Pipe_{i}";
                pipe.transform.SetParent(reactor.transform);

                float angle = i * 90f * Mathf.Deg2Rad;
                pipe.transform.localPosition = new Vector3(Mathf.Cos(angle) * 1f, 0.5f, Mathf.Sin(angle) * 1f);
                pipe.transform.localScale = new Vector3(0.2f, 0.5f, 0.2f);
                pipe.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.5f, 0.5f, 0.5f));
            }

            // Output tank
            var output = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            output.name = "OutputTank";
            output.transform.SetParent(reactor.transform);
            output.transform.localPosition = new Vector3(2f, 0.5f, 0);
            output.transform.localScale = new Vector3(0.8f, 1f, 0.8f);
            output.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.8f, 0.9f, 0.2f, 0.9f));

            return reactor;
        }

        private static GameObject CreateComposterPrimitive()
        {
            var composter = new GameObject("Composter");

            // Bin
            var bin = GameObject.CreatePrimitive(PrimitiveType.Cube);
            bin.name = "Bin";
            bin.transform.SetParent(composter.transform);
            bin.transform.localPosition = Vector3.up * 0.75f;
            bin.transform.localScale = new Vector3(2f, 1.5f, 2f);
            bin.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.4f, 0.3f, 0.2f));

            // Organic material inside
            var organic = GameObject.CreatePrimitive(PrimitiveType.Cube);
            organic.name = "OrganicMatter";
            organic.transform.SetParent(composter.transform);
            organic.transform.localPosition = Vector3.up * 1.2f;
            organic.transform.localScale = new Vector3(1.8f, 0.6f, 1.8f);
            organic.GetComponent<Renderer>().material = GetEffectMaterial(new Color(0.3f, 0.4f, 0.1f));

            return composter;
        }

        #endregion

        #region Bio-Remediation

        /// <summary>
        /// Clean up a hazardous zone using biological processes
        /// Integration point for HazardousWorld mod
        /// </summary>
        public static bool BioRemediateZone(Vector3 center, float radius)
        {
            if (!EnableBioRemediation.Value)
                return false;

            // This would integrate with HazardousWorld to clean radiation/toxic zones
            Log.LogInfo($"Bio-remediation initiated at {center} (radius: {radius}m)");

            // Spawn visual effect
            SpawnRemediationEffect(center, radius);

            return true;
        }

        private static void SpawnRemediationEffect(Vector3 position, float radius)
        {
            var effectObj = new GameObject("RemediationEffect");
            effectObj.transform.position = position;

            var particles = effectObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 3f;
            main.startSpeed = 2f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.2f, 0.8f, 0.3f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = particles.emission;
            emission.rateOverTime = 50f;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius;

            UnityEngine.Object.Destroy(effectObj, 5f);
        }

        #endregion

        #region Utility

        public static void LogInfo(string message) => Log?.LogInfo(message);
        public static void LogWarning(string message) => Log?.LogWarning(message);
        public static void LogError(string message) => Log?.LogError(message);

        #endregion
    }
}
