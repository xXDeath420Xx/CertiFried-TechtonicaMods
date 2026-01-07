using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace AtlantumEnrichment
{
    [BepInPlugin(GUID, NAME, VERSION)]
    [BepInDependency("com.equinox.EquinoxsModUtils", BepInDependency.DependencyFlags.HardDependency)]
    public class AtlantumEnrichmentPlugin : BaseUnityPlugin
    {
        public const string GUID = "com.certifried.atlantumenrichment";
        public const string NAME = "AtlantumEnrichment";
        public const string VERSION = "1.0.0";

        private static AtlantumEnrichmentPlugin instance;
        public static ManualLogSource Log;
        private Harmony harmony;

        // Configuration
        public static ConfigEntry<float> BaseExplosionRisk;
        public static ConfigEntry<float> SafetyModuleReduction;
        public static ConfigEntry<float> RadiationDamage;
        public static ConfigEntry<float> RadiationRadius;
        public static ConfigEntry<float> RadiationDecayTime;
        public static ConfigEntry<bool> EnableExplosions;

        // Active facilities
        public static List<CentrifugeController> ActiveCentrifuges = new List<CentrifugeController>();
        public static List<EnrichmentChamberController> ActiveChambers = new List<EnrichmentChamberController>();
        public static List<WasteProcessorController> ActiveWasteProcessors = new List<WasteProcessorController>();

        // Radiation zones
        public static List<RadiationZone> ActiveRadiationZones = new List<RadiationZone>();

        // Statistics
        public static float TotalEnrichedAtlantum = 0f;
        public static int TotalExplosions = 0;
        public static float TotalWasteProduced = 0f;

        void Awake()
        {
            instance = this;
            Log = Logger;
            Logger.LogInfo($"{NAME} v{VERSION} loading...");

            InitializeConfig();

            harmony = new Harmony(GUID);
            harmony.PatchAll();

            Logger.LogInfo($"{NAME} loaded successfully!");
            Logger.LogWarning("WARNING: Atlantum enrichment is dangerous! Use safety modules!");
        }

        private void InitializeConfig()
        {
            BaseExplosionRisk = Config.Bind("Risk", "BaseExplosionRisk", 0.25f,
                new ConfigDescription("Base explosion risk per enrichment cycle (0.25 = 25%)",
                    new AcceptableValueRange<float>(0f, 1f)));

            SafetyModuleReduction = Config.Bind("Risk", "SafetyModuleReduction", 0.10f,
                new ConfigDescription("Risk reduction per safety module installed",
                    new AcceptableValueRange<float>(0.05f, 0.25f)));

            RadiationDamage = Config.Bind("Radiation", "RadiationDamage", 5f,
                new ConfigDescription("Damage per second in radiation zones",
                    new AcceptableValueRange<float>(1f, 20f)));

            RadiationRadius = Config.Bind("Radiation", "RadiationRadius", 15f,
                new ConfigDescription("Radius of radiation zones in meters",
                    new AcceptableValueRange<float>(5f, 50f)));

            RadiationDecayTime = Config.Bind("Radiation", "RadiationDecayTime", 300f,
                new ConfigDescription("Time in seconds for radiation to decay",
                    new AcceptableValueRange<float>(60f, 600f)));

            EnableExplosions = Config.Bind("Risk", "EnableExplosions", true,
                "Enable explosions from enrichment failures (disable for safer gameplay)");
        }

        void Update()
        {
            // Clean up destroyed facilities
            ActiveCentrifuges.RemoveAll(c => c == null);
            ActiveChambers.RemoveAll(c => c == null);
            ActiveWasteProcessors.RemoveAll(w => w == null);
            ActiveRadiationZones.RemoveAll(z => z == null);

            // Update radiation zones
            foreach (var zone in ActiveRadiationZones)
            {
                if (zone != null)
                {
                    zone.UpdateZone();
                }
            }
        }

        void OnDestroy()
        {
            harmony?.UnpatchSelf();
        }

        #region Facility Spawning

        public static CentrifugeController SpawnCentrifuge(Vector3 position)
        {
            var obj = CreateCentrifugePrimitive();
            obj.transform.position = position;
            obj.name = $"Centrifuge_{ActiveCentrifuges.Count}";

            var controller = obj.AddComponent<CentrifugeController>();
            controller.Initialize();

            ActiveCentrifuges.Add(controller);
            Log.LogInfo($"Spawned Centrifuge at {position}");

            return controller;
        }

        public static EnrichmentChamberController SpawnEnrichmentChamber(Vector3 position)
        {
            var obj = CreateEnrichmentChamberPrimitive();
            obj.transform.position = position;
            obj.name = $"EnrichmentChamber_{ActiveChambers.Count}";

            var controller = obj.AddComponent<EnrichmentChamberController>();
            controller.Initialize();

            ActiveChambers.Add(controller);
            Log.LogInfo($"Spawned Enrichment Chamber at {position}");

            return controller;
        }

        public static WasteProcessorController SpawnWasteProcessor(Vector3 position)
        {
            var obj = CreateWasteProcessorPrimitive();
            obj.transform.position = position;
            obj.name = $"WasteProcessor_{ActiveWasteProcessors.Count}";

            var controller = obj.AddComponent<WasteProcessorController>();
            controller.Initialize();

            ActiveWasteProcessors.Add(controller);
            Log.LogInfo($"Spawned Waste Processor at {position}");

            return controller;
        }

        #endregion

        #region Radiation System

        public static RadiationZone CreateRadiationZone(Vector3 center, float intensity = 1f)
        {
            var zoneObj = new GameObject($"RadiationZone_{ActiveRadiationZones.Count}");
            zoneObj.transform.position = center;

            var zone = zoneObj.AddComponent<RadiationZone>();
            zone.Initialize(intensity);

            ActiveRadiationZones.Add(zone);
            Log.LogWarning($"RADIATION LEAK at {center}! Intensity: {intensity:P0}");

            return zone;
        }

        public static void TriggerExplosion(Vector3 position, float radius, float damage)
        {
            if (!EnableExplosions.Value)
            {
                Log.LogInfo("Explosion prevented (disabled in config)");
                return;
            }

            TotalExplosions++;
            Log.LogError($"EXPLOSION at {position}! Radius: {radius}m");

            // Create visual explosion
            SpawnExplosionEffect(position, radius);

            // Create radiation zone
            CreateRadiationZone(position, 1f);

            // Damage nearby facilities
            DamageNearbyFacilities(position, radius, damage);
        }

        private static void SpawnExplosionEffect(Vector3 position, float radius)
        {
            var explosionObj = new GameObject("AtlantumExplosion");
            explosionObj.transform.position = position;

            // Main explosion
            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1.5f;
            main.startSpeed = 20f;
            main.startSize = 2f;
            main.startColor = new Color(0.2f, 0.8f, 1f); // Cyan/teal for Atlantum

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 100)
            });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = radius * 0.3f;

            // Light flash
            var light = explosionObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = radius * 2f;
            light.intensity = 10f;
            light.color = new Color(0.2f, 0.9f, 1f);

            UnityEngine.Object.Destroy(explosionObj, 3f);
        }

        private static void DamageNearbyFacilities(Vector3 center, float radius, float damage)
        {
            // Damage centrifuges
            foreach (var centrifuge in ActiveCentrifuges)
            {
                if (centrifuge == null) continue;
                float dist = Vector3.Distance(center, centrifuge.transform.position);
                if (dist < radius)
                {
                    float falloff = 1f - (dist / radius);
                    centrifuge.TakeDamage(damage * falloff);
                }
            }

            // Damage enrichment chambers
            foreach (var chamber in ActiveChambers)
            {
                if (chamber == null) continue;
                float dist = Vector3.Distance(center, chamber.transform.position);
                if (dist < radius)
                {
                    float falloff = 1f - (dist / radius);
                    chamber.TakeDamage(damage * falloff);
                }
            }
        }

        #endregion

        #region Primitive Creation

        private static GameObject CreateCentrifugePrimitive()
        {
            var centrifuge = new GameObject("Centrifuge");

            // Base
            var baseObj = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            baseObj.name = "Base";
            baseObj.transform.SetParent(centrifuge.transform);
            baseObj.transform.localPosition = Vector3.up * 0.25f;
            baseObj.transform.localScale = new Vector3(2f, 0.5f, 2f);
            baseObj.GetComponent<Renderer>().material = GetMaterial(new Color(0.4f, 0.4f, 0.45f));

            // Drum
            var drum = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            drum.name = "Drum";
            drum.transform.SetParent(centrifuge.transform);
            drum.transform.localPosition = Vector3.up * 1.5f;
            drum.transform.localScale = new Vector3(1.5f, 2f, 1.5f);
            drum.GetComponent<Renderer>().material = GetMaterial(new Color(0.6f, 0.6f, 0.65f));

            // Glow ring
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.name = "GlowRing";
            ring.transform.SetParent(centrifuge.transform);
            ring.transform.localPosition = Vector3.up * 1.5f;
            ring.transform.localScale = new Vector3(1.7f, 0.1f, 1.7f);
            ring.GetComponent<Renderer>().material = GetMaterial(new Color(0.2f, 0.8f, 1f));

            return centrifuge;
        }

        private static GameObject CreateEnrichmentChamberPrimitive()
        {
            var chamber = new GameObject("EnrichmentChamber");

            // Heavy shielded container
            var container = GameObject.CreatePrimitive(PrimitiveType.Cube);
            container.name = "Container";
            container.transform.SetParent(chamber.transform);
            container.transform.localPosition = Vector3.up * 1.5f;
            container.transform.localScale = new Vector3(3f, 3f, 3f);
            container.GetComponent<Renderer>().material = GetMaterial(new Color(0.3f, 0.35f, 0.4f));

            // Warning stripes
            var stripes = GameObject.CreatePrimitive(PrimitiveType.Cube);
            stripes.name = "WarningStripes";
            stripes.transform.SetParent(chamber.transform);
            stripes.transform.localPosition = new Vector3(0, 1.5f, 1.51f);
            stripes.transform.localScale = new Vector3(2f, 2f, 0.02f);
            stripes.GetComponent<Renderer>().material = GetMaterial(new Color(1f, 0.8f, 0f));

            // Core window
            var window = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            window.name = "CoreWindow";
            window.transform.SetParent(chamber.transform);
            window.transform.localPosition = new Vector3(0, 1.5f, 1.3f);
            window.transform.localScale = new Vector3(0.8f, 0.8f, 0.3f);
            window.GetComponent<Renderer>().material = GetMaterial(new Color(0.1f, 0.9f, 1f, 0.7f));

            // Safety module slots (visual indicators)
            for (int i = 0; i < 4; i++)
            {
                var slot = GameObject.CreatePrimitive(PrimitiveType.Cube);
                slot.name = $"SafetySlot_{i}";
                slot.transform.SetParent(chamber.transform);

                float angle = i * 90f * Mathf.Deg2Rad;
                slot.transform.localPosition = new Vector3(
                    Mathf.Cos(angle) * 1.8f,
                    0.5f,
                    Mathf.Sin(angle) * 1.8f
                );
                slot.transform.localScale = new Vector3(0.4f, 1f, 0.4f);
                slot.GetComponent<Renderer>().material = GetMaterial(new Color(0.2f, 0.2f, 0.25f));
            }

            return chamber;
        }

        private static GameObject CreateWasteProcessorPrimitive()
        {
            var processor = new GameObject("WasteProcessor");

            // Main tank
            var tank = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            tank.name = "Tank";
            tank.transform.SetParent(processor.transform);
            tank.transform.localPosition = Vector3.up * 1.5f;
            tank.transform.localScale = new Vector3(2f, 2f, 2f);
            tank.GetComponent<Renderer>().material = GetMaterial(new Color(0.5f, 0.5f, 0.3f));

            // Hazard symbol
            var hazard = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            hazard.name = "HazardSymbol";
            hazard.transform.SetParent(processor.transform);
            hazard.transform.localPosition = new Vector3(0, 1.5f, 1.01f);
            hazard.transform.localRotation = Quaternion.Euler(90, 0, 0);
            hazard.transform.localScale = new Vector3(0.8f, 0.02f, 0.8f);
            hazard.GetComponent<Renderer>().material = GetMaterial(new Color(1f, 1f, 0f));

            // Output pipe
            var pipe = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            pipe.name = "OutputPipe";
            pipe.transform.SetParent(processor.transform);
            pipe.transform.localPosition = new Vector3(1.5f, 0.5f, 0);
            pipe.transform.localRotation = Quaternion.Euler(0, 0, 90);
            pipe.transform.localScale = new Vector3(0.3f, 1f, 0.3f);
            pipe.GetComponent<Renderer>().material = GetMaterial(new Color(0.4f, 0.4f, 0.4f));

            return processor;
        }

        private static Material GetMaterial(Color color)
        {
            var mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = color;
            return mat;
        }

        #endregion

        #region Utility

        public static void LogInfo(string message) => Log?.LogInfo(message);
        public static void LogWarning(string message) => Log?.LogWarning(message);
        public static void LogError(string message) => Log?.LogError(message);

        #endregion
    }
}
