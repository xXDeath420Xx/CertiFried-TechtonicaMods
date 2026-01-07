using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Boss types with unique mechanics
    /// </summary>
    public enum BossType
    {
        Overlord,       // Flying command ship - summons minions, orbital strikes
        Behemoth,       // Massive ground walker - stomping, laser sweeps
        Hivemind,       // Organic mass - spreads corruption, splits into parts
        Dreadnought,    // Heavy warship - shield phases, missile barrages
        Harvester       // Resource-stealing boss - must protect machines
    }

    /// <summary>
    /// Boss phases with escalating difficulty
    /// </summary>
    public enum BossPhase
    {
        Phase1,     // Normal attacks
        Phase2,     // 66% HP - New abilities unlocked
        Phase3,     // 33% HP - Enrage mode
        Dying       // Death sequence
    }

    /// <summary>
    /// Epic boss encounter controller with multi-phase battles
    /// </summary>
    public class BossController : MonoBehaviour
    {
        // ========== IDENTITY ==========
        public string BossName { get; private set; }
        public BossType Type { get; private set; }
        public BossPhase CurrentPhase { get; private set; } = BossPhase.Phase1;

        // ========== STATS ==========
        public float MaxHealth { get; private set; } = 5000f;
        public float Health { get; private set; } = 5000f;
        public float Shield { get; private set; } = 0f;
        public float MaxShield { get; private set; } = 0f;
        public float Armor { get; private set; } = 30f;
        public float MoveSpeed { get; private set; } = 5f;
        public float AttackDamage { get; private set; } = 50f;
        public float AttackRange { get; private set; } = 40f;
        public int ScoreValue { get; private set; } = 5000;

        public bool IsAlive => Health > 0;
        public float HealthPercent => Health / MaxHealth;

        // ========== AI STATE ==========
        private enum AIState { Spawning, Idle, Attacking, SpecialAttack, PhaseTransition, Dying }
        private AIState currentState = AIState.Spawning;

        private float lastAttackTime;
        private float lastSpecialTime;
        private float stateTimer;
        private float phaseTransitionTimer;
        private int attackPattern = 0;

        // ========== SPECIAL ABILITIES ==========
        private float specialCooldown = 15f;
        private float minionSummonCooldown = 20f;
        private float lastMinionSummon;
        private bool isEnraged = false;
        private float enrageMultiplier = 1.5f;

        // ========== VISUAL ==========
        private Transform playerTransform;
        private Renderer[] renderers;
        private Light bossLight;
        private ParticleSystem auraParticles;
        private ParticleSystem shieldParticles;
        private Color originalColor;
        private Color phaseColors = Color.red;

        // ========== COMPONENTS ==========
        private Transform head;
        private Transform[] weapons;
        private List<GameObject> activeMinions = new List<GameObject>();

        // ========== LOOT ==========
        private static readonly Dictionary<BossType, LootDrop[]> BossLootTables = new Dictionary<BossType, LootDrop[]>
        {
            { BossType.Overlord, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 50, 100, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 20, 40, 1.0f)
            }},
            { BossType.Behemoth, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 75, 150, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 25, 50, 1.0f)
            }},
            { BossType.Hivemind, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 60, 120, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 30, 60, 1.0f)
            }},
            { BossType.Dreadnought, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 100, 200, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 40, 80, 1.0f)
            }},
            { BossType.Harvester, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 80, 160, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 35, 70, 1.0f)
            }}
        };

        public void Initialize(BossType type, int difficulty = 1)
        {
            Type = type;
            SetupStats(difficulty);
            SetupVisuals();
            SetupEffects();

            currentState = AIState.Spawning;
            stateTimer = 3f; // 3 second dramatic entrance

            playerTransform = Player.instance?.transform;
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0 && renderers[0].material != null)
                originalColor = renderers[0].material.color;

            TurretDefensePlugin.Log.LogWarning($"=== BOSS SPAWNED: {BossName} ===");
            TurretDefensePlugin.Log.LogWarning($"HP: {MaxHealth:F0} | Shield: {MaxShield:F0} | Armor: {Armor:F0}");
        }

        private void SetupStats(int difficulty)
        {
            float diffMult = 1f + (difficulty - 1) * 0.25f;

            switch (Type)
            {
                case BossType.Overlord:
                    BossName = "Alien Overlord";
                    MaxHealth = 5000f * diffMult;
                    MaxShield = 1000f * diffMult;
                    Armor = 20f;
                    MoveSpeed = 8f;
                    AttackDamage = 40f * diffMult;
                    AttackRange = 50f;
                    ScoreValue = 5000;
                    specialCooldown = 12f;
                    minionSummonCooldown = 18f;
                    break;

                case BossType.Behemoth:
                    BossName = "Behemoth Walker";
                    MaxHealth = 10000f * diffMult;
                    MaxShield = 0f;
                    Armor = 50f;
                    MoveSpeed = 3f;
                    AttackDamage = 80f * diffMult;
                    AttackRange = 30f;
                    ScoreValue = 7500;
                    specialCooldown = 8f;
                    minionSummonCooldown = 25f;
                    break;

                case BossType.Hivemind:
                    BossName = "The Hivemind";
                    MaxHealth = 6000f * diffMult;
                    MaxShield = 500f * diffMult;
                    Armor = 10f;
                    MoveSpeed = 5f;
                    AttackDamage = 30f * diffMult;
                    AttackRange = 40f;
                    ScoreValue = 6000;
                    specialCooldown = 10f;
                    minionSummonCooldown = 12f;
                    break;

                case BossType.Dreadnought:
                    BossName = "Dreadnought Carrier";
                    MaxHealth = 8000f * diffMult;
                    MaxShield = 2000f * diffMult;
                    Armor = 35f;
                    MoveSpeed = 4f;
                    AttackDamage = 60f * diffMult;
                    AttackRange = 60f;
                    ScoreValue = 10000;
                    specialCooldown = 15f;
                    minionSummonCooldown = 20f;
                    break;

                case BossType.Harvester:
                    BossName = "The Harvester";
                    MaxHealth = 4000f * diffMult;
                    MaxShield = 800f * diffMult;
                    Armor = 25f;
                    MoveSpeed = 10f;
                    AttackDamage = 25f * diffMult;
                    AttackRange = 35f;
                    ScoreValue = 4000;
                    specialCooldown = 8f;
                    minionSummonCooldown = 15f;
                    break;
            }

            Health = MaxHealth;
            Shield = MaxShield;
        }

        private void SetupVisuals()
        {
            Color bossColor;
            float scale;

            switch (Type)
            {
                case BossType.Overlord:
                    bossColor = new Color(0.6f, 0.2f, 0.8f);
                    scale = 8f;
                    CreateOverlordModel();
                    break;
                case BossType.Behemoth:
                    bossColor = new Color(0.4f, 0.3f, 0.2f);
                    scale = 12f;
                    CreateBehemothModel();
                    break;
                case BossType.Hivemind:
                    bossColor = new Color(0.3f, 0.7f, 0.4f);
                    scale = 10f;
                    CreateHivemindModel();
                    break;
                case BossType.Dreadnought:
                    bossColor = new Color(0.3f, 0.3f, 0.5f);
                    scale = 15f;
                    CreateDreadnoughtModel();
                    break;
                case BossType.Harvester:
                    bossColor = new Color(0.8f, 0.5f, 0.2f);
                    scale = 6f;
                    CreateHarvesterModel();
                    break;
                default:
                    bossColor = Color.red;
                    scale = 8f;
                    CreateOverlordModel();
                    break;
            }

            transform.localScale = Vector3.one * scale;
        }

        private void CreateOverlordModel()
        {
            // Main body - menacing command ship
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(1f, 2f, 1f);
            body.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.15f, 0.4f));

            // Command bridge
            var bridge = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            bridge.transform.SetParent(transform);
            bridge.transform.localPosition = Vector3.up * 0.3f + Vector3.forward * 0.5f;
            bridge.transform.localScale = new Vector3(0.5f, 0.3f, 0.4f);
            bridge.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.8f, 0.2f, 0.9f));
            head = bridge.transform;

            // Wing arrays
            for (int side = -1; side <= 1; side += 2)
            {
                var wing = GameObject.CreatePrimitive(PrimitiveType.Cube);
                wing.transform.SetParent(transform);
                wing.transform.localPosition = new Vector3(side * 0.8f, 0, 0);
                wing.transform.localScale = new Vector3(1f, 0.05f, 0.6f);
                wing.transform.localRotation = Quaternion.Euler(0, 0, side * -10);
                wing.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.2f, 0.1f, 0.3f));

                // Wing weapons
                var cannon = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                cannon.transform.SetParent(wing.transform);
                cannon.transform.localPosition = new Vector3(0.4f * side, 0, 0.3f);
                cannon.transform.localRotation = Quaternion.Euler(90, 0, 0);
                cannon.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                cannon.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.5f, 0.3f, 0.6f));
            }

            // Central energy core
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(transform);
            core.transform.localPosition = Vector3.down * 0.2f;
            core.transform.localScale = Vector3.one * 0.4f;
            core.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.9f, 0.3f, 1f));
            UnityEngine.Object.Destroy(core.GetComponent<Collider>());
        }

        private void CreateBehemothModel()
        {
            // Main body - massive walker
            var body = GameObject.CreatePrimitive(PrimitiveType.Cube);
            body.transform.SetParent(transform);
            body.transform.localPosition = Vector3.up * 0.3f;
            body.transform.localScale = new Vector3(1.2f, 0.8f, 1.5f);
            body.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.35f, 0.3f, 0.25f));

            // Head/sensor array
            var headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.transform.SetParent(transform);
            headObj.transform.localPosition = Vector3.up * 0.6f + Vector3.forward * 0.6f;
            headObj.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            headObj.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.8f, 0.2f, 0.2f));
            head = headObj.transform;

            // Legs (4)
            for (int i = 0; i < 4; i++)
            {
                float xPos = (i < 2 ? -0.5f : 0.5f);
                float zPos = (i % 2 == 0 ? 0.4f : -0.4f);

                var leg = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                leg.transform.SetParent(transform);
                leg.transform.localPosition = new Vector3(xPos, -0.3f, zPos);
                leg.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
                leg.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.25f, 0.2f, 0.15f));
            }

            // Heavy weapons
            var mainGun = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            mainGun.transform.SetParent(transform);
            mainGun.transform.localPosition = Vector3.up * 0.5f + Vector3.forward * 0.8f;
            mainGun.transform.localRotation = Quaternion.Euler(90, 0, 0);
            mainGun.transform.localScale = new Vector3(0.2f, 0.6f, 0.2f);
            mainGun.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.2f, 0.2f, 0.2f));
        }

        private void CreateHivemindModel()
        {
            // Organic mass body
            var core = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            core.transform.SetParent(transform);
            core.transform.localPosition = Vector3.zero;
            core.transform.localScale = new Vector3(1f, 1.2f, 1f);
            core.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.25f, 0.5f, 0.3f));
            head = core.transform;

            // Tentacles/appendages
            for (int i = 0; i < 6; i++)
            {
                float angle = (i / 6f) * Mathf.PI * 2f;
                var tentacle = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                tentacle.transform.SetParent(transform);
                tentacle.transform.localPosition = new Vector3(Mathf.Cos(angle) * 0.6f, -0.3f, Mathf.Sin(angle) * 0.6f);
                tentacle.transform.localRotation = Quaternion.Euler(0, 0, Mathf.Cos(angle) * 30f);
                tentacle.transform.localScale = new Vector3(0.15f, 0.5f, 0.15f);
                tentacle.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.2f, 0.4f, 0.25f));
            }

            // Pulsing eye
            var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            eye.transform.SetParent(transform);
            eye.transform.localPosition = Vector3.up * 0.4f;
            eye.transform.localScale = Vector3.one * 0.3f;
            eye.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.9f, 0.8f, 0.2f));
            UnityEngine.Object.Destroy(eye.GetComponent<Collider>());
        }

        private void CreateDreadnoughtModel()
        {
            // Massive carrier body
            var hull = GameObject.CreatePrimitive(PrimitiveType.Cube);
            hull.transform.SetParent(transform);
            hull.transform.localPosition = Vector3.zero;
            hull.transform.localScale = new Vector3(0.8f, 0.4f, 2f);
            hull.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.25f, 0.25f, 0.35f));

            // Command tower
            var tower = GameObject.CreatePrimitive(PrimitiveType.Cube);
            tower.transform.SetParent(transform);
            tower.transform.localPosition = Vector3.up * 0.4f + Vector3.back * 0.3f;
            tower.transform.localScale = new Vector3(0.3f, 0.5f, 0.4f);
            tower.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.2f, 0.2f, 0.3f));
            head = tower.transform;

            // Flight deck
            var deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
            deck.transform.SetParent(transform);
            deck.transform.localPosition = Vector3.up * 0.25f + Vector3.forward * 0.5f;
            deck.transform.localScale = new Vector3(0.6f, 0.1f, 0.8f);
            deck.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.3f, 0.4f));

            // Weapon batteries
            for (int side = -1; side <= 1; side += 2)
            {
                for (int pos = 0; pos < 3; pos++)
                {
                    var turret = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    turret.transform.SetParent(transform);
                    turret.transform.localPosition = new Vector3(side * 0.35f, 0.25f, -0.5f + pos * 0.5f);
                    turret.transform.localScale = Vector3.one * 0.12f;
                    turret.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.5f, 0.5f, 0.6f));
                }
            }

            // Shield generators
            for (int side = -1; side <= 1; side += 2)
            {
                var gen = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                gen.transform.SetParent(transform);
                gen.transform.localPosition = new Vector3(side * 0.5f, 0.3f, 0);
                gen.transform.localScale = new Vector3(0.1f, 0.15f, 0.1f);
                gen.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.5f, 0.9f));
            }
        }

        private void CreateHarvesterModel()
        {
            // Sleek predator body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(transform);
            body.transform.localPosition = Vector3.zero;
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.transform.localScale = new Vector3(0.6f, 1.5f, 0.6f);
            body.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.7f, 0.45f, 0.2f));

            // Sensor head
            var headObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            headObj.transform.SetParent(transform);
            headObj.transform.localPosition = Vector3.forward * 1f;
            headObj.transform.localScale = new Vector3(0.4f, 0.3f, 0.4f);
            headObj.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.9f, 0.6f, 0.2f));
            head = headObj.transform;

            // Collection arms
            for (int side = -1; side <= 1; side += 2)
            {
                var arm = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                arm.transform.SetParent(transform);
                arm.transform.localPosition = new Vector3(side * 0.5f, 0, 0.3f);
                arm.transform.localScale = new Vector3(0.1f, 0.6f, 0.1f);
                arm.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.5f, 0.3f, 0.15f));

                // Claw
                var claw = GameObject.CreatePrimitive(PrimitiveType.Cube);
                claw.transform.SetParent(arm.transform);
                claw.transform.localPosition = Vector3.down * 0.7f;
                claw.transform.localScale = new Vector3(0.3f, 0.2f, 0.1f);
                claw.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.2f, 0.1f));
            }

            // Storage tanks
            var tank = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            tank.transform.SetParent(transform);
            tank.transform.localPosition = Vector3.back * 0.5f;
            tank.transform.localScale = new Vector3(0.5f, 0.4f, 0.5f);
            tank.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.4f, 0.6f, 0.3f));
        }

        private void SetupEffects()
        {
            // Boss aura light
            var lightObj = new GameObject("BossLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.zero;
            bossLight = lightObj.AddComponent<Light>();
            bossLight.type = LightType.Point;
            bossLight.range = 30f;
            bossLight.intensity = 3f;
            bossLight.color = GetPhaseColor();

            // Aura particles
            var auraObj = new GameObject("BossAura");
            auraObj.transform.SetParent(transform);
            auraObj.transform.localPosition = Vector3.zero;
            auraParticles = auraObj.AddComponent<ParticleSystem>();

            var main = auraParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 2f;
            main.startSize = 0.5f;
            main.startColor = GetPhaseColor();
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = auraParticles.emission;
            emission.rateOverTime = 30;

            var shape = auraParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;

            var renderer = auraObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Shield effect (if applicable)
            if (MaxShield > 0)
            {
                var shieldObj = new GameObject("ShieldEffect");
                shieldObj.transform.SetParent(transform);
                shieldObj.transform.localPosition = Vector3.zero;
                shieldParticles = shieldObj.AddComponent<ParticleSystem>();

                var shieldMain = shieldParticles.main;
                shieldMain.startLifetime = 1f;
                shieldMain.startSpeed = 0.5f;
                shieldMain.startSize = 0.3f;
                shieldMain.startColor = new Color(0.3f, 0.5f, 1f, 0.5f);

                var shieldEmission = shieldParticles.emission;
                shieldEmission.rateOverTime = 50;

                var shieldShape = shieldParticles.shape;
                shieldShape.shapeType = ParticleSystemShapeType.Sphere;
                shieldShape.radius = 3f;

                var shieldRenderer = shieldObj.GetComponent<ParticleSystemRenderer>();
                shieldRenderer.material = TurretDefensePlugin.GetEffectMaterial(new Color(0.3f, 0.5f, 1f, 0.5f));
            }
        }

        private Color GetPhaseColor()
        {
            return CurrentPhase switch
            {
                BossPhase.Phase1 => new Color(0.5f, 0.3f, 0.8f),
                BossPhase.Phase2 => new Color(0.8f, 0.5f, 0.2f),
                BossPhase.Phase3 => new Color(1f, 0.2f, 0.2f),
                _ => Color.white
            };
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (playerTransform == null)
                playerTransform = Player.instance?.transform;

            UpdatePhase();
            UpdateState();
            UpdateEffects();
        }

        private void UpdatePhase()
        {
            BossPhase newPhase = CurrentPhase;

            if (HealthPercent <= 0.33f && CurrentPhase != BossPhase.Phase3)
            {
                newPhase = BossPhase.Phase3;
                TriggerEnrage();
            }
            else if (HealthPercent <= 0.66f && CurrentPhase == BossPhase.Phase1)
            {
                newPhase = BossPhase.Phase2;
            }

            if (newPhase != CurrentPhase)
            {
                StartCoroutine(PhaseTransition(newPhase));
            }
        }

        private IEnumerator PhaseTransition(BossPhase newPhase)
        {
            currentState = AIState.PhaseTransition;
            phaseTransitionTimer = 3f;

            TurretDefensePlugin.Log.LogWarning($"=== {BossName} ENTERING PHASE {(int)newPhase + 1} ===");

            // Dramatic pause
            yield return new WaitForSeconds(0.5f);

            // Visual transition
            float elapsed = 0f;
            Color oldColor = GetPhaseColor();
            CurrentPhase = newPhase;
            Color newColor = GetPhaseColor();

            while (elapsed < 2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 2f;

                // Pulse effect
                float pulse = 1f + Mathf.Sin(elapsed * 10f) * 0.2f;
                transform.localScale = Vector3.one * transform.localScale.magnitude * pulse;

                // Color transition
                if (bossLight != null)
                    bossLight.color = Color.Lerp(oldColor, newColor, t);

                yield return null;
            }

            // Update abilities based on phase
            UpdatePhaseAbilities();

            currentState = AIState.Idle;
        }

        private void TriggerEnrage()
        {
            isEnraged = true;
            TurretDefensePlugin.Log.LogError($"=== {BossName} IS ENRAGED! ===");

            // Boost stats
            AttackDamage *= enrageMultiplier;
            MoveSpeed *= 1.3f;
            specialCooldown *= 0.5f;
            minionSummonCooldown *= 0.5f;
        }

        private void UpdatePhaseAbilities()
        {
            switch (CurrentPhase)
            {
                case BossPhase.Phase2:
                    // Unlock new abilities
                    specialCooldown *= 0.8f;
                    minionSummonCooldown *= 0.8f;
                    break;

                case BossPhase.Phase3:
                    // Maximum aggression
                    if (Type == BossType.Dreadnought)
                        Shield = MaxShield * 0.5f; // Recharge some shield
                    break;
            }
        }

        private void UpdateState()
        {
            stateTimer -= Time.deltaTime;

            switch (currentState)
            {
                case AIState.Spawning:
                    UpdateSpawning();
                    break;
                case AIState.Idle:
                    UpdateIdle();
                    break;
                case AIState.Attacking:
                    UpdateAttacking();
                    break;
                case AIState.SpecialAttack:
                    UpdateSpecialAttack();
                    break;
                case AIState.PhaseTransition:
                    // Waiting
                    break;
            }
        }

        private void UpdateSpawning()
        {
            float progress = 1f - (stateTimer / 3f);

            // Dramatic entrance animation
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1f, progress) * GetBaseScale();

            if (stateTimer <= 0)
            {
                currentState = AIState.Idle;
            }
        }

        private float GetBaseScale()
        {
            return Type switch
            {
                BossType.Behemoth => 12f,
                BossType.Dreadnought => 15f,
                BossType.Hivemind => 10f,
                BossType.Overlord => 8f,
                BossType.Harvester => 6f,
                _ => 8f
            };
        }

        private void UpdateIdle()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Move towards player if too far
            if (distToPlayer > AttackRange * 0.8f)
            {
                MoveTowards(playerTransform.position + Vector3.up * GetHoverHeight());
            }

            // Face player
            LookAt(playerTransform.position);

            // Check for attack opportunity
            if (distToPlayer < AttackRange)
            {
                // Choose attack type
                if (Time.time - lastSpecialTime >= specialCooldown && CurrentPhase != BossPhase.Phase1)
                {
                    currentState = AIState.SpecialAttack;
                    stateTimer = GetSpecialDuration();
                }
                else if (Time.time - lastAttackTime >= GetAttackCooldown())
                {
                    currentState = AIState.Attacking;
                }
            }

            // Summon minions
            if (Time.time - lastMinionSummon >= minionSummonCooldown)
            {
                SummonMinions();
            }
        }

        private float GetHoverHeight()
        {
            return Type switch
            {
                BossType.Behemoth => 0f,  // Ground walker
                BossType.Hivemind => 8f,
                _ => 15f
            };
        }

        private float GetAttackCooldown()
        {
            float baseCooldown = 2f;
            if (isEnraged) baseCooldown *= 0.6f;
            return baseCooldown;
        }

        private float GetSpecialDuration()
        {
            return Type switch
            {
                BossType.Overlord => 4f,
                BossType.Behemoth => 3f,
                BossType.Dreadnought => 5f,
                _ => 3f
            };
        }

        private void UpdateAttacking()
        {
            if (playerTransform == null) return;

            LookAt(playerTransform.position);

            // Execute attack
            ExecuteBasicAttack();
            lastAttackTime = Time.time;

            currentState = AIState.Idle;
        }

        private void UpdateSpecialAttack()
        {
            if (stateTimer <= 0)
            {
                ExecuteSpecialAttack();
                lastSpecialTime = Time.time;
                currentState = AIState.Idle;
            }
            else
            {
                // Charging animation
                float charge = 1f - (stateTimer / GetSpecialDuration());
                if (auraParticles != null)
                {
                    var emission = auraParticles.emission;
                    emission.rateOverTime = 30 + charge * 100;
                }
            }
        }

        private void ExecuteBasicAttack()
        {
            attackPattern = (attackPattern + 1) % 3;

            switch (Type)
            {
                case BossType.Overlord:
                    OverlordBasicAttack();
                    break;
                case BossType.Behemoth:
                    BehemothBasicAttack();
                    break;
                case BossType.Hivemind:
                    HivemindBasicAttack();
                    break;
                case BossType.Dreadnought:
                    DreadnoughtBasicAttack();
                    break;
                case BossType.Harvester:
                    HarvesterBasicAttack();
                    break;
            }
        }

        private void OverlordBasicAttack()
        {
            // Psychic blast
            StartCoroutine(PsychicBlast());
            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{BossName} psychic blast");
        }

        private IEnumerator PsychicBlast()
        {
            var blastObj = new GameObject("PsychicBlast");
            blastObj.transform.position = head != null ? head.position : transform.position;

            var light = blastObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(0.6f, 0.2f, 0.8f);
            light.range = 20f;
            light.intensity = 5f;

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                light.intensity = 5f * (1f - elapsed / 0.3f);
                yield return null;
            }

            UnityEngine.Object.Destroy(blastObj);
        }

        private void BehemothBasicAttack()
        {
            // Ground stomp with shockwave
            StartCoroutine(GroundStomp());
            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{BossName} ground stomp");
        }

        private IEnumerator GroundStomp()
        {
            var shockObj = new GameObject("Shockwave");
            shockObj.transform.position = transform.position - Vector3.up * GetHoverHeight();

            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(shockObj.transform);
            ring.transform.localPosition = Vector3.zero;
            ring.transform.localScale = new Vector3(1f, 0.1f, 1f);
            ring.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.8f, 0.4f, 0.2f, 0.7f));
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, 30f, elapsed);
                ring.transform.localScale = new Vector3(scale, 0.1f * (1f - elapsed), scale);
                yield return null;
            }

            UnityEngine.Object.Destroy(shockObj);
        }

        private void HivemindBasicAttack()
        {
            // Acid spray
            StartCoroutine(AcidSpray());
            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{BossName} acid spray");
        }

        private IEnumerator AcidSpray()
        {
            for (int i = 0; i < 5; i++)
            {
                Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 2f;
                var acid = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                acid.transform.position = spawnPos;
                acid.transform.localScale = Vector3.one * 0.3f;
                acid.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.8f, 0.2f));
                UnityEngine.Object.Destroy(acid.GetComponent<Collider>());

                if (playerTransform != null)
                {
                    Vector3 dir = (playerTransform.position - spawnPos).normalized;
                    var rb = acid.AddComponent<Rigidbody>();
                    rb.useGravity = false;
                    rb.velocity = dir * 20f;
                }

                UnityEngine.Object.Destroy(acid, 2f);
                yield return new WaitForSeconds(0.1f);
            }
        }

        private void DreadnoughtBasicAttack()
        {
            // Missile barrage
            StartCoroutine(MissileBarrage());
            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{BossName} missile barrage");
        }

        private IEnumerator MissileBarrage()
        {
            for (int i = 0; i < 6; i++)
            {
                Vector3 offset = new Vector3((i % 3 - 1) * 2f, 0, 0);
                var missile = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                missile.transform.position = transform.position + offset;
                missile.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
                missile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.5f, 0.5f, 0.6f));
                UnityEngine.Object.Destroy(missile.GetComponent<Collider>());

                StartCoroutine(GuideMissile(missile));
                yield return new WaitForSeconds(0.15f);
            }
        }

        private IEnumerator GuideMissile(GameObject missile)
        {
            float elapsed = 0f;
            Vector3 startPos = missile.transform.position;
            Vector3 targetPos = playerTransform != null ? playerTransform.position : startPos + Vector3.forward * 30f;

            while (elapsed < 1.5f && missile != null)
            {
                elapsed += Time.deltaTime;
                missile.transform.position = Vector3.Lerp(startPos, targetPos, elapsed / 1.5f);
                missile.transform.LookAt(targetPos);
                yield return null;
            }

            if (missile != null)
            {
                // Explosion
                var explosion = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                explosion.transform.position = missile.transform.position;
                explosion.transform.localScale = Vector3.one * 2f;
                explosion.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.5f, 0.2f, 0.7f));
                UnityEngine.Object.Destroy(explosion.GetComponent<Collider>());
                UnityEngine.Object.Destroy(explosion, 0.3f);
                UnityEngine.Object.Destroy(missile);
            }
        }

        private void HarvesterBasicAttack()
        {
            // Energy drain
            TurretDefensePlugin.DamagePlayer(AttackDamage * 0.5f, $"{BossName} energy drain");
            // Also heal self
            Health = Mathf.Min(Health + AttackDamage * 0.25f, MaxHealth);
        }

        private void ExecuteSpecialAttack()
        {
            TurretDefensePlugin.Log.LogWarning($"=== {BossName} SPECIAL ATTACK! ===");

            switch (Type)
            {
                case BossType.Overlord:
                    StartCoroutine(OrbitalStrike());
                    break;
                case BossType.Behemoth:
                    StartCoroutine(LaserSweep());
                    break;
                case BossType.Hivemind:
                    StartCoroutine(CorruptionWave());
                    break;
                case BossType.Dreadnought:
                    StartCoroutine(ShieldOverload());
                    break;
                case BossType.Harvester:
                    StartCoroutine(MassHarvest());
                    break;
            }
        }

        private IEnumerator OrbitalStrike()
        {
            // Orbital bombardment
            Vector3 targetPos = playerTransform != null ? playerTransform.position : transform.position;

            // Warning indicators
            for (int i = 0; i < 5; i++)
            {
                Vector3 strikePos = targetPos + UnityEngine.Random.insideUnitSphere * 15f;
                strikePos.y = targetPos.y;

                var warning = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
                warning.transform.position = strikePos + Vector3.up * 0.1f;
                warning.transform.localScale = new Vector3(3f, 0.05f, 3f);
                warning.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.2f, 0.2f, 0.5f));
                UnityEngine.Object.Destroy(warning.GetComponent<Collider>());

                StartCoroutine(DelayedStrike(strikePos, 1.5f + i * 0.2f));
            }

            yield return new WaitForSeconds(3f);
            TurretDefensePlugin.DamagePlayer(AttackDamage * 2f, $"{BossName} orbital strike");
        }

        private IEnumerator DelayedStrike(Vector3 position, float delay)
        {
            yield return new WaitForSeconds(delay);

            // Strike effect
            var strike = new GameObject("OrbitalStrike");
            strike.transform.position = position;

            var light = strike.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 15f;
            light.intensity = 10f;
            light.color = new Color(1f, 0.5f, 0.2f);

            // Beam from sky
            var beam = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            beam.transform.SetParent(strike.transform);
            beam.transform.position = position + Vector3.up * 25f;
            beam.transform.localScale = new Vector3(2f, 25f, 2f);
            beam.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.3f, 0.1f, 0.8f));
            UnityEngine.Object.Destroy(beam.GetComponent<Collider>());

            yield return new WaitForSeconds(0.5f);
            UnityEngine.Object.Destroy(strike);
        }

        private IEnumerator LaserSweep()
        {
            // 360 degree laser sweep
            var laserObj = new GameObject("LaserSweep");
            laserObj.transform.position = transform.position;

            var laser = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            laser.transform.SetParent(laserObj.transform);
            laser.transform.localPosition = Vector3.forward * 15f;
            laser.transform.localRotation = Quaternion.Euler(90, 0, 0);
            laser.transform.localScale = new Vector3(0.3f, 15f, 0.3f);
            laser.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.2f, 0.2f));
            UnityEngine.Object.Destroy(laser.GetComponent<Collider>());

            float elapsed = 0f;
            while (elapsed < 3f)
            {
                elapsed += Time.deltaTime;
                laserObj.transform.Rotate(Vector3.up, 180f * Time.deltaTime);
                yield return null;
            }

            UnityEngine.Object.Destroy(laserObj);
            TurretDefensePlugin.DamagePlayer(AttackDamage * 1.5f, $"{BossName} laser sweep");
        }

        private IEnumerator CorruptionWave()
        {
            // Spread corruption zones
            for (int i = 0; i < 8; i++)
            {
                float angle = (i / 8f) * Mathf.PI * 2f;
                Vector3 pos = transform.position + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * 20f;
                pos.y = playerTransform != null ? playerTransform.position.y : 0;

                TurretDefensePlugin.SpawnHazardZone(pos, 5f, 10f);
                yield return new WaitForSeconds(0.2f);
            }

            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{BossName} corruption");
        }

        private IEnumerator ShieldOverload()
        {
            // Restore shield and create damaging pulse
            Shield = MaxShield;

            if (shieldParticles != null)
            {
                var emission = shieldParticles.emission;
                emission.rateOverTime = 200;
            }

            yield return new WaitForSeconds(1f);

            // Shield explosion
            var pulseObj = new GameObject("ShieldPulse");
            pulseObj.transform.position = transform.position;

            var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.SetParent(pulseObj.transform);
            sphere.transform.localScale = Vector3.one * 5f;
            sphere.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.5f, 1f, 0.5f));
            UnityEngine.Object.Destroy(sphere.GetComponent<Collider>());

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                sphere.transform.localScale = Vector3.one * Mathf.Lerp(5f, 40f, elapsed);
                yield return null;
            }

            UnityEngine.Object.Destroy(pulseObj);
            TurretDefensePlugin.DamagePlayer(AttackDamage * 1.5f, $"{BossName} shield overload");
        }

        private IEnumerator MassHarvest()
        {
            // Drain multiple sources
            TurretDefensePlugin.Log.LogWarning($"{BossName} is harvesting your resources!");

            for (int i = 0; i < 5; i++)
            {
                TurretDefensePlugin.DamagePlayer(AttackDamage * 0.3f, $"{BossName} mass harvest");
                Health = Mathf.Min(Health + AttackDamage * 0.2f, MaxHealth);
                yield return new WaitForSeconds(0.5f);
            }
        }

        private void SummonMinions()
        {
            lastMinionSummon = Time.time;

            int minionCount = CurrentPhase switch
            {
                BossPhase.Phase1 => 2,
                BossPhase.Phase2 => 4,
                BossPhase.Phase3 => 6,
                _ => 2
            };

            TurretDefensePlugin.Log.LogInfo($"{BossName} summons {minionCount} minions!");

            for (int i = 0; i < minionCount; i++)
            {
                Vector3 spawnPos = transform.position + UnityEngine.Random.insideUnitSphere * 10f;
                spawnPos.y = transform.position.y;

                string minionType = GetMinionType();
                var minion = TurretDefensePlugin.SpawnAlien(minionType, spawnPos, 1);
                activeMinions.Add(minion);
            }
        }

        private string GetMinionType()
        {
            return Type switch
            {
                BossType.Overlord => "AlienFighter",
                BossType.Behemoth => "Robot_Scout",
                BossType.Hivemind => "AlienFighterGreen",
                BossType.Dreadnought => "Robot_Invader",
                BossType.Harvester => "Robot_Scout_HyperX",
                _ => "AlienFighter"
            };
        }

        private void MoveTowards(Vector3 target, float speed = 0)
        {
            if (speed <= 0) speed = MoveSpeed;

            Vector3 direction = (target - transform.position).normalized;
            Vector3 movement = direction * speed * Time.deltaTime;
            transform.position += movement;
        }

        private void LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position);
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 2f);
            }
        }

        private void UpdateEffects()
        {
            // Pulsing aura based on health
            if (bossLight != null)
            {
                float pulse = 1f + Mathf.Sin(Time.time * (isEnraged ? 5f : 2f)) * 0.3f;
                bossLight.intensity = 3f * pulse;
                bossLight.color = GetPhaseColor();
            }

            // Shield particles
            if (shieldParticles != null && Shield > 0)
            {
                var emission = shieldParticles.emission;
                emission.rateOverTime = (Shield / MaxShield) * 50f;
            }
            else if (shieldParticles != null)
            {
                var emission = shieldParticles.emission;
                emission.rateOverTime = 0;
            }
        }

        public void TakeDamage(float damage, bool isCritical)
        {
            // Shield absorbs damage first
            if (Shield > 0)
            {
                float shieldDamage = Mathf.Min(Shield, damage);
                Shield -= shieldDamage;
                damage -= shieldDamage;

                if (Shield <= 0)
                {
                    TurretDefensePlugin.Log.LogInfo($"{BossName}'s shield is down!");
                }
            }

            // Apply armor
            float effectiveDamage = Mathf.Max(1, damage - Armor);
            Health -= effectiveDamage;

            TurretDefensePlugin.SpawnDamageNumber(transform.position + Vector3.up * 3f, effectiveDamage, isCritical);

            // Damage flash
            StartCoroutine(DamageFlash());

            TurretDefensePlugin.LogDebug($"{BossName} took {effectiveDamage:F0} damage, HP: {Health:F0}/{MaxHealth}");

            if (Health <= 0)
            {
                Die();
            }
        }

        private IEnumerator DamageFlash()
        {
            Color flashColor = Color.white;

            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                    r.material.color = flashColor;
            }

            yield return new WaitForSeconds(0.1f);

            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                    r.material.color = originalColor;
            }
        }

        private void Die()
        {
            currentState = AIState.Dying;

            TurretDefensePlugin.Log.LogWarning($"=== {BossName} DEFEATED! ===");
            TurretDefensePlugin.CurrentScore += ScoreValue;

            // Drop massive loot
            if (TurretDefensePlugin.EnableLootDrops.Value)
            {
                DropLoot();
            }

            StartCoroutine(EpicDeathSequence());
        }

        private void DropLoot()
        {
            if (BossLootTables.TryGetValue(Type, out var drops))
            {
                foreach (var drop in drops)
                {
                    if (UnityEngine.Random.value < drop.DropChance)
                    {
                        int amount = UnityEngine.Random.Range(drop.MinAmount, drop.MaxAmount + 1);
                        TurretDefensePlugin.Log.LogInfo($"BOSS LOOT: {amount}x {drop.ItemName}");
                    }
                }
            }
        }

        private IEnumerator EpicDeathSequence()
        {
            // Multiple explosions
            for (int i = 0; i < 10; i++)
            {
                Vector3 explosionPos = transform.position + UnityEngine.Random.insideUnitSphere * 5f;

                var explosion = new GameObject("BossExplosion");
                explosion.transform.position = explosionPos;

                var light = explosion.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 20f;
                light.intensity = 10f;
                light.color = new Color(1f, 0.5f, 0.2f);

                var particles = explosion.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startLifetime = 1f;
                main.startSpeed = 15f;
                main.startSize = 1f;
                main.startColor = new Color(1f, 0.5f, 0.1f);

                var emission = particles.emission;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
                emission.rateOverTime = 0;

                var renderer = explosion.GetComponent<ParticleSystemRenderer>();
                renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

                UnityEngine.Object.Destroy(explosion, 2f);

                yield return new WaitForSeconds(0.3f);
            }

            // Final massive explosion
            yield return new WaitForSeconds(0.5f);

            var finalExplosion = new GameObject("FinalExplosion");
            finalExplosion.transform.position = transform.position;

            var finalLight = finalExplosion.AddComponent<Light>();
            finalLight.type = LightType.Point;
            finalLight.range = 100f;
            finalLight.intensity = 20f;
            finalLight.color = Color.white;

            var finalParticles = finalExplosion.AddComponent<ParticleSystem>();
            var finalMain = finalParticles.main;
            finalMain.startLifetime = 2f;
            finalMain.startSpeed = 30f;
            finalMain.startSize = 2f;
            finalMain.startColor = new Color(1f, 0.8f, 0.4f);

            var finalEmission = finalParticles.emission;
            finalEmission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 200) });
            finalEmission.rateOverTime = 0;

            var finalRenderer = finalExplosion.GetComponent<ParticleSystemRenderer>();
            finalRenderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Destroy remaining minions
            foreach (var minion in activeMinions)
            {
                if (minion != null)
                    UnityEngine.Object.Destroy(minion);
            }

            UnityEngine.Object.Destroy(finalExplosion, 3f);
            UnityEngine.Object.Destroy(gameObject, 0.5f);
        }
    }
}
