using System;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace AmbientLife
{
    /// <summary>
    /// AmbientLife - Adds passive wildlife and ambient creatures for atmosphere
    /// Features: Butterflies, birds, fish, glowing creatures, ambient sounds
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class AmbientLifePlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.AmbientLife";
        public const string PluginName = "AmbientLife";
        public const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static AmbientLifePlugin Instance;
        public static string PluginPath;

        // Configuration
        public static ConfigEntry<bool> EnableAmbientLife;
        public static ConfigEntry<int> MaxCreatures;
        public static ConfigEntry<float> SpawnRadius;
        public static ConfigEntry<float> DespawnRadius;
        public static ConfigEntry<bool> EnableButterflies;
        public static ConfigEntry<bool> EnableBirds;
        public static ConfigEntry<bool> EnableFireflies;
        public static ConfigEntry<bool> EnableFish;
        public static ConfigEntry<bool> DebugMode;

        // Active creatures
        public static List<AmbientCreature> ActiveCreatures = new List<AmbientCreature>();
        private float spawnTimer = 0f;
        private const float SPAWN_INTERVAL = 2f;

        // Creature types
        public enum CreatureType
        {
            Butterfly,
            Bird,
            Firefly,
            Fish,
            Moth,
            Beetle,
            Dragonfly
        }

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            PluginPath = Path.GetDirectoryName(Info.Location);
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableAmbientLife = Config.Bind("General", "Enable Ambient Life", true, "Enable ambient creature spawning");
            MaxCreatures = Config.Bind("General", "Max Creatures", 30,
                new ConfigDescription("Maximum ambient creatures at once", new AcceptableValueRange<int>(5, 100)));
            SpawnRadius = Config.Bind("Spawning", "Spawn Radius", 40f,
                new ConfigDescription("Distance from player to spawn creatures", new AcceptableValueRange<float>(20f, 100f)));
            DespawnRadius = Config.Bind("Spawning", "Despawn Radius", 60f,
                new ConfigDescription("Distance from player to despawn creatures", new AcceptableValueRange<float>(30f, 150f)));
            EnableButterflies = Config.Bind("Creatures", "Enable Butterflies", true, "Spawn butterflies");
            EnableBirds = Config.Bind("Creatures", "Enable Birds", true, "Spawn birds");
            EnableFireflies = Config.Bind("Creatures", "Enable Fireflies", true, "Spawn fireflies (glow at night)");
            EnableFish = Config.Bind("Creatures", "Enable Fish", false, "Spawn fish (requires water detection)");
            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void Update()
        {
            if (!EnableAmbientLife.Value) return;
            if (Player.instance == null) return;

            // Update spawn timer
            spawnTimer += Time.deltaTime;
            if (spawnTimer >= SPAWN_INTERVAL)
            {
                spawnTimer = 0f;
                TrySpawnCreature();
            }

            // Update and clean up creatures
            UpdateCreatures();
        }

        private void TrySpawnCreature()
        {
            if (ActiveCreatures.Count >= MaxCreatures.Value) return;

            // Get enabled creature types
            List<CreatureType> enabledTypes = new List<CreatureType>();
            if (EnableButterflies.Value) enabledTypes.Add(CreatureType.Butterfly);
            if (EnableBirds.Value) enabledTypes.Add(CreatureType.Bird);
            if (EnableFireflies.Value) { enabledTypes.Add(CreatureType.Firefly); enabledTypes.Add(CreatureType.Moth); }
            if (enabledTypes.Count == 0) return;

            // Random creature type
            CreatureType type = enabledTypes[UnityEngine.Random.Range(0, enabledTypes.Count)];

            // Spawn position around player
            Vector3 playerPos = Player.instance.transform.position;
            float angle = UnityEngine.Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float dist = UnityEngine.Random.Range(SpawnRadius.Value * 0.5f, SpawnRadius.Value);
            float height = GetSpawnHeight(type);

            Vector3 spawnPos = playerPos + new Vector3(
                Mathf.Cos(angle) * dist,
                height,
                Mathf.Sin(angle) * dist
            );

            SpawnCreature(type, spawnPos);
        }

        private float GetSpawnHeight(CreatureType type)
        {
            switch (type)
            {
                case CreatureType.Butterfly:
                case CreatureType.Moth:
                case CreatureType.Dragonfly:
                    return UnityEngine.Random.Range(1f, 4f);
                case CreatureType.Bird:
                    return UnityEngine.Random.Range(5f, 15f);
                case CreatureType.Firefly:
                    return UnityEngine.Random.Range(0.5f, 3f);
                case CreatureType.Beetle:
                    return 0.2f;
                case CreatureType.Fish:
                    return -1f; // Below water surface
                default:
                    return 2f;
            }
        }

        private void SpawnCreature(CreatureType type, Vector3 position)
        {
            GameObject creatureObj = CreateCreatureObject(type, position);
            if (creatureObj != null)
            {
                AmbientCreature creature = creatureObj.AddComponent<AmbientCreature>();
                creature.Initialize(type);
                ActiveCreatures.Add(creature);
                LogDebug($"Spawned {type} at {position}");
            }
        }

        private GameObject CreateCreatureObject(CreatureType type, Vector3 position)
        {
            // Create simple visual for now - replace with proper models later
            GameObject obj = new GameObject($"Ambient_{type}");
            obj.transform.position = position;

            // Add visual based on type
            switch (type)
            {
                case CreatureType.Butterfly:
                case CreatureType.Moth:
                    CreateButterflyVisual(obj, type == CreatureType.Moth);
                    break;
                case CreatureType.Bird:
                    CreateBirdVisual(obj);
                    break;
                case CreatureType.Firefly:
                    CreateFireflyVisual(obj);
                    break;
                case CreatureType.Dragonfly:
                    CreateDragonflyVisual(obj);
                    break;
                default:
                    CreateDefaultVisual(obj);
                    break;
            }

            return obj;
        }

        private void CreateButterflyVisual(GameObject parent, bool isMoth)
        {
            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.02f, 0.05f, 0.02f);
            Destroy(body.GetComponent<Collider>());

            // Wings (two quads)
            for (int i = -1; i <= 1; i += 2)
            {
                GameObject wing = GameObject.CreatePrimitive(PrimitiveType.Quad);
                wing.transform.SetParent(parent.transform);
                wing.transform.localPosition = new Vector3(i * 0.04f, 0, 0);
                wing.transform.localScale = new Vector3(0.08f, 0.06f, 1f);
                wing.transform.localRotation = Quaternion.Euler(0, 90, 0);
                Destroy(wing.GetComponent<Collider>());

                var renderer = wing.GetComponent<Renderer>();
                if (renderer != null)
                {
                    Color wingColor = isMoth ?
                        new Color(0.4f, 0.35f, 0.3f, 0.8f) :
                        new Color(
                            UnityEngine.Random.Range(0.5f, 1f),
                            UnityEngine.Random.Range(0.3f, 0.8f),
                            UnityEngine.Random.Range(0.5f, 1f),
                            0.9f
                        );
                    renderer.material.color = wingColor;
                }
            }
        }

        private void CreateBirdVisual(GameObject parent)
        {
            // Simple bird shape
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.1f, 0.15f, 0.1f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Destroy(body.GetComponent<Collider>());

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(
                    UnityEngine.Random.Range(0.2f, 0.5f),
                    UnityEngine.Random.Range(0.2f, 0.4f),
                    UnityEngine.Random.Range(0.3f, 0.5f)
                );
            }
        }

        private void CreateFireflyVisual(GameObject parent)
        {
            // Small glowing sphere
            GameObject glow = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            glow.transform.SetParent(parent.transform);
            glow.transform.localPosition = Vector3.zero;
            glow.transform.localScale = Vector3.one * 0.03f;
            Destroy(glow.GetComponent<Collider>());

            var renderer = glow.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.8f, 1f, 0.3f);
                renderer.material.SetColor("_EmissionColor", new Color(0.8f, 1f, 0.3f) * 2f);
            }

            // Add point light
            Light light = parent.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.8f, 1f, 0.3f);
            light.intensity = 0.5f;
            light.range = 3f;
        }

        private void CreateDragonflyVisual(GameObject parent)
        {
            // Body
            GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(parent.transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localScale = new Vector3(0.015f, 0.08f, 0.015f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            Destroy(body.GetComponent<Collider>());

            var renderer = body.GetComponent<Renderer>();
            if (renderer != null)
            {
                renderer.material.color = new Color(0.2f, 0.4f, 0.6f);
            }
        }

        private void CreateDefaultVisual(GameObject parent)
        {
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(parent.transform);
            sphere.transform.localPosition = Vector3.zero;
            sphere.transform.localScale = Vector3.one * 0.05f;
            Destroy(sphere.GetComponent<Collider>());
        }

        private void UpdateCreatures()
        {
            Vector3 playerPos = Player.instance.transform.position;

            for (int i = ActiveCreatures.Count - 1; i >= 0; i--)
            {
                var creature = ActiveCreatures[i];
                if (creature == null)
                {
                    ActiveCreatures.RemoveAt(i);
                    continue;
                }

                // Despawn if too far
                float dist = Vector3.Distance(creature.transform.position, playerPos);
                if (dist > DespawnRadius.Value)
                {
                    ActiveCreatures.RemoveAt(i);
                    Destroy(creature.gameObject);
                }
            }
        }

        public static void LogDebug(string message)
        {
            if (DebugMode.Value)
                Log.LogInfo($"[DEBUG] {message}");
        }
    }

    /// <summary>
    /// Controls individual ambient creature behavior
    /// </summary>
    public class AmbientCreature : MonoBehaviour
    {
        public AmbientLifePlugin.CreatureType Type { get; private set; }

        private Vector3 targetPosition;
        private float moveSpeed;
        private float changeTargetTimer;
        private float changeTargetInterval;
        private float wingFlapPhase;
        private float glowPhase;

        public void Initialize(AmbientLifePlugin.CreatureType type)
        {
            Type = type;
            targetPosition = transform.position;
            SetupBehavior();
        }

        private void SetupBehavior()
        {
            switch (Type)
            {
                case AmbientLifePlugin.CreatureType.Butterfly:
                case AmbientLifePlugin.CreatureType.Moth:
                    moveSpeed = UnityEngine.Random.Range(1f, 2f);
                    changeTargetInterval = UnityEngine.Random.Range(1f, 3f);
                    break;
                case AmbientLifePlugin.CreatureType.Bird:
                    moveSpeed = UnityEngine.Random.Range(5f, 10f);
                    changeTargetInterval = UnityEngine.Random.Range(3f, 8f);
                    break;
                case AmbientLifePlugin.CreatureType.Firefly:
                    moveSpeed = UnityEngine.Random.Range(0.5f, 1.5f);
                    changeTargetInterval = UnityEngine.Random.Range(2f, 5f);
                    glowPhase = UnityEngine.Random.value * Mathf.PI * 2f;
                    break;
                case AmbientLifePlugin.CreatureType.Dragonfly:
                    moveSpeed = UnityEngine.Random.Range(3f, 6f);
                    changeTargetInterval = UnityEngine.Random.Range(0.5f, 2f);
                    break;
                default:
                    moveSpeed = 1f;
                    changeTargetInterval = 2f;
                    break;
            }

            wingFlapPhase = UnityEngine.Random.value * Mathf.PI * 2f;
            PickNewTarget();
        }

        private void Update()
        {
            // Update target timer
            changeTargetTimer += Time.deltaTime;
            if (changeTargetTimer >= changeTargetInterval)
            {
                changeTargetTimer = 0f;
                PickNewTarget();
            }

            // Move towards target
            MoveTowardsTarget();

            // Type-specific updates
            UpdateTypeBehavior();
        }

        private void PickNewTarget()
        {
            Vector3 currentPos = transform.position;
            float wanderDist = Type == AmbientLifePlugin.CreatureType.Bird ? 20f : 5f;

            targetPosition = currentPos + new Vector3(
                UnityEngine.Random.Range(-wanderDist, wanderDist),
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(-wanderDist, wanderDist)
            );

            // Keep height reasonable
            float minHeight = Type == AmbientLifePlugin.CreatureType.Bird ? 5f : 0.5f;
            float maxHeight = Type == AmbientLifePlugin.CreatureType.Bird ? 20f : 5f;
            targetPosition.y = Mathf.Clamp(targetPosition.y, minHeight, maxHeight);
        }

        private void MoveTowardsTarget()
        {
            Vector3 direction = (targetPosition - transform.position).normalized;
            float dist = Vector3.Distance(transform.position, targetPosition);

            if (dist > 0.5f)
            {
                // Smooth movement with some wobble for insects
                float wobble = Type == AmbientLifePlugin.CreatureType.Bird ? 0 : Mathf.Sin(Time.time * 10f) * 0.1f;
                Vector3 move = direction * moveSpeed * Time.deltaTime;
                move.y += wobble * Time.deltaTime;

                transform.position += move;

                // Face movement direction
                if (direction.sqrMagnitude > 0.01f)
                {
                    transform.rotation = Quaternion.Slerp(
                        transform.rotation,
                        Quaternion.LookRotation(direction),
                        Time.deltaTime * 3f
                    );
                }
            }
        }

        private void UpdateTypeBehavior()
        {
            wingFlapPhase += Time.deltaTime * 15f;

            switch (Type)
            {
                case AmbientLifePlugin.CreatureType.Butterfly:
                case AmbientLifePlugin.CreatureType.Moth:
                    // Wing flapping animation
                    AnimateWings();
                    break;

                case AmbientLifePlugin.CreatureType.Firefly:
                    // Pulsing glow
                    AnimateGlow();
                    break;

                case AmbientLifePlugin.CreatureType.Bird:
                    // Occasional calls/sounds could go here
                    break;
            }
        }

        private void AnimateWings()
        {
            // Find wing objects and animate them
            float flapAngle = Mathf.Sin(wingFlapPhase) * 30f;
            for (int i = 0; i < transform.childCount; i++)
            {
                Transform child = transform.GetChild(i);
                if (child.name.Contains("Quad"))
                {
                    float sign = child.localPosition.x > 0 ? 1f : -1f;
                    child.localRotation = Quaternion.Euler(0, 90 + flapAngle * sign, 0);
                }
            }
        }

        private void AnimateGlow()
        {
            glowPhase += Time.deltaTime * 2f;
            float intensity = (Mathf.Sin(glowPhase) + 1f) * 0.5f; // 0 to 1

            Light light = GetComponent<Light>();
            if (light != null)
            {
                light.intensity = 0.2f + intensity * 0.8f;
            }
        }
    }
}
