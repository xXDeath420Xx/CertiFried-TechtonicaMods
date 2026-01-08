using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Types of alien structures that can spawn
    /// </summary>
    public enum HiveType
    {
        Birther,     // Main spawner - produces ground enemies
        Brain,       // Command structure - buffs nearby enemies
        Stomach,     // Resource processor - heals enemies
        Terraformer, // Spreads corruption/hazards
        Claw,        // Defensive turret
        Crystal,     // Power source - increases spawn rates
        Idol         // Boss spawner - summons epic bosses (Cthulhu Idol visuals)
    }

    /// <summary>
    /// Controller for alien hive structures that spawn enemies
    /// </summary>
    public class AlienHiveController : MonoBehaviour
    {
        // ========== IDENTITY ==========
        public string HiveName { get; private set; }
        public HiveType Type { get; private set; }
        public int ThreatLevel { get; private set; } = 1;

        // ========== STATS ==========
        public float MaxHealth { get; private set; } = 1000f;
        public float Health { get; private set; } = 1000f;
        public float Armor { get; private set; } = 20f;
        public float SpawnInterval { get; private set; } = 15f;
        public int MaxSpawns { get; private set; } = 10;
        public float EffectRadius { get; private set; } = 20f;
        public int ScoreValue { get; private set; } = 500;

        public bool IsAlive => Health > 0;
        public bool IsActive { get; private set; } = true;

        // ========== SPAWNING ==========
        private float lastSpawnTime;
        private int currentSpawnCount = 0;
        private List<GameObject> spawnedEnemies = new List<GameObject>();

        // ========== EFFECTS ==========
        private float pulseTimer;
        private float pulseInterval = 3f;
        private ParticleSystem ambientParticles;
        private Light hiveLight;
        private Renderer[] renderers;
        private Color originalColor;

        // ========== REFERENCES ==========
        private Transform playerTransform;

        public void Initialize(string name, HiveType type, int threatLevel)
        {
            HiveName = name;
            Type = type;
            ThreatLevel = threatLevel;

            SetupStats();
            SetupEffects();
            SetupReferences();

            TurretDefensePlugin.Log.LogInfo($"Alien Hive {HiveName} ({Type}) initialized - Threat Level {ThreatLevel}");
        }

        private void SetupStats()
        {
            float threatMultiplier = 1f + (ThreatLevel - 1) * 0.5f;

            switch (Type)
            {
                case HiveType.Birther:
                    MaxHealth = 1500f * threatMultiplier;
                    Armor = 15f;
                    SpawnInterval = 12f / threatMultiplier;
                    MaxSpawns = 8 + ThreatLevel * 2;
                    EffectRadius = 30f;
                    ScoreValue = 750;
                    break;

                case HiveType.Brain:
                    MaxHealth = 800f * threatMultiplier;
                    Armor = 10f;
                    SpawnInterval = 0f; // Doesn't spawn
                    MaxSpawns = 0;
                    EffectRadius = 40f; // Large buff radius
                    ScoreValue = 600;
                    break;

                case HiveType.Stomach:
                    MaxHealth = 1200f * threatMultiplier;
                    Armor = 20f;
                    SpawnInterval = 0f;
                    MaxSpawns = 0;
                    EffectRadius = 25f;
                    ScoreValue = 500;
                    break;

                case HiveType.Terraformer:
                    MaxHealth = 600f * threatMultiplier;
                    Armor = 5f;
                    SpawnInterval = 20f;
                    MaxSpawns = 3; // Spawns hazard zones
                    EffectRadius = 50f;
                    ScoreValue = 400;
                    break;

                case HiveType.Claw:
                    MaxHealth = 500f * threatMultiplier;
                    Armor = 25f;
                    SpawnInterval = 0f;
                    MaxSpawns = 0;
                    EffectRadius = 35f; // Attack range
                    ScoreValue = 350;
                    break;

                case HiveType.Crystal:
                    MaxHealth = 400f * threatMultiplier;
                    Armor = 8f;
                    SpawnInterval = 0f;
                    MaxSpawns = 0;
                    EffectRadius = 60f; // Large power radius
                    ScoreValue = 450;
                    break;

                case HiveType.Idol:
                    // Boss spawner - extremely tough, spawns epic bosses
                    MaxHealth = 3000f * threatMultiplier;
                    Armor = 30f;
                    SpawnInterval = 60f / threatMultiplier; // Slow boss spawn rate
                    MaxSpawns = ThreatLevel; // Spawns bosses based on threat level
                    EffectRadius = 80f; // Large influence
                    ScoreValue = 2000;
                    break;
            }

            Health = MaxHealth;
        }

        private void SetupEffects()
        {
            // Ambient glow
            var lightObj = new GameObject("HiveGlow");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 2f;
            hiveLight = lightObj.AddComponent<Light>();
            hiveLight.type = LightType.Point;
            hiveLight.range = EffectRadius * 0.5f;
            hiveLight.intensity = 2f;

            switch (Type)
            {
                case HiveType.Birther:
                    hiveLight.color = new Color(1f, 0.3f, 0.5f); // Pink/organic
                    break;
                case HiveType.Brain:
                    hiveLight.color = new Color(0.8f, 0.2f, 1f); // Purple
                    break;
                case HiveType.Stomach:
                    hiveLight.color = new Color(0.3f, 0.8f, 0.2f); // Green
                    break;
                case HiveType.Terraformer:
                    hiveLight.color = new Color(0.5f, 0.3f, 0.1f); // Brown
                    break;
                case HiveType.Claw:
                    hiveLight.color = new Color(1f, 0.2f, 0.2f); // Red
                    break;
                case HiveType.Crystal:
                    hiveLight.color = new Color(0.3f, 0.5f, 1f); // Blue
                    break;
                case HiveType.Idol:
                    hiveLight.color = new Color(0.1f, 0.6f, 0.4f); // Eerie teal/eldritch green
                    hiveLight.intensity = 4f; // Extra bright for boss spawner
                    break;
            }

            // Ambient particles
            var particleObj = new GameObject("HiveParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.up * 1f;
            ambientParticles = particleObj.AddComponent<ParticleSystem>();

            var main = ambientParticles.main;
            main.startLifetime = 3f;
            main.startSpeed = 1f;
            main.startSize = 0.3f;
            main.startColor = hiveLight.color;
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = ambientParticles.emission;
            emission.rateOverTime = 10;

            var shape = ambientParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 2f;

            var colorOverLife = ambientParticles.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(hiveLight.color, 0f),
                    new GradientColorKey(hiveLight.color * 0.5f, 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.6f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = particleObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Get renderers for damage flash
            renderers = GetComponentsInChildren<Renderer>();
            if (renderers.Length > 0 && renderers[0].material != null)
            {
                originalColor = renderers[0].material.color;
            }
        }

        private void SetupReferences()
        {
            playerTransform = Player.instance?.transform;
        }

        private void Update()
        {
            if (!IsAlive || !IsActive) return;

            if (playerTransform == null)
                playerTransform = Player.instance?.transform;

            // Update effects
            UpdateEffects();

            // Type-specific behavior
            switch (Type)
            {
                case HiveType.Birther:
                    UpdateBirtherSpawning();
                    break;
                case HiveType.Brain:
                    UpdateBrainBuffs();
                    break;
                case HiveType.Stomach:
                    UpdateStomachHealing();
                    break;
                case HiveType.Terraformer:
                    UpdateTerraformerCorruption();
                    break;
                case HiveType.Claw:
                    UpdateClawAttack();
                    break;
                case HiveType.Crystal:
                    UpdateCrystalPower();
                    break;
            }
        }

        private void UpdateEffects()
        {
            // Pulsing glow
            pulseTimer += Time.deltaTime;
            float pulse = 1f + Mathf.Sin(pulseTimer * 2f) * 0.3f;

            if (hiveLight != null)
            {
                hiveLight.intensity = 2f * pulse;
            }

            // Health-based effects
            float healthRatio = Health / MaxHealth;
            if (healthRatio < 0.5f)
            {
                // Damaged - flickering
                if (hiveLight != null)
                {
                    hiveLight.intensity *= UnityEngine.Random.Range(0.5f, 1f);
                }

                // Smoke particles
                var emission = ambientParticles.emission;
                emission.rateOverTime = 20 + (1f - healthRatio) * 30f;
            }
        }

        private void UpdateBirtherSpawning()
        {
            // Clean up dead spawns
            spawnedEnemies.RemoveAll(e => e == null);

            if (Time.time - lastSpawnTime < SpawnInterval) return;
            if (currentSpawnCount >= MaxSpawns) return;
            if (spawnedEnemies.Count >= MaxSpawns / 2) return; // Don't oversaturate

            SpawnEnemy();
        }

        private void SpawnEnemy()
        {
            lastSpawnTime = Time.time;
            currentSpawnCount++;

            // Spawn effect
            StartCoroutine(SpawnEffect());

            // Spawn position around hive
            Vector3 spawnOffset = UnityEngine.Random.insideUnitSphere * 5f;
            spawnOffset.y = 0;
            Vector3 spawnPos = transform.position + spawnOffset;

            // Idol type spawns bosses instead of regular enemies
            if (Type == HiveType.Idol)
            {
                SpawnBoss(spawnPos);
                return;
            }

            // Determine what to spawn based on threat level
            GroundEnemyType[] possibleTypes;

            if (ThreatLevel >= 3)
            {
                possibleTypes = new[] {
                    GroundEnemyType.Chomper,
                    GroundEnemyType.Spitter,
                    GroundEnemyType.Arachnid,
                    GroundEnemyType.Mimic,
                    GroundEnemyType.MachineGunRobot
                };
            }
            else if (ThreatLevel >= 2)
            {
                possibleTypes = new[] {
                    GroundEnemyType.Chomper,
                    GroundEnemyType.Spitter,
                    GroundEnemyType.Arachnid,
                    GroundEnemyType.MachineGunRobot
                };
            }
            else
            {
                possibleTypes = new[] {
                    GroundEnemyType.Arachnid,
                    GroundEnemyType.Chomper
                };
            }

            var enemyType = possibleTypes[UnityEngine.Random.Range(0, possibleTypes.Length)];

            // Actually spawn the enemy
            var enemy = TurretDefensePlugin.SpawnGroundEnemy(enemyType, spawnPos, ThreatLevel);
            if (enemy != null)
            {
                spawnedEnemies.Add(enemy);
            }
        }

        private void SpawnBoss(Vector3 spawnPos)
        {
            // Determine boss type based on threat level
            BossType[] possibleBosses;

            if (ThreatLevel >= 4)
            {
                possibleBosses = new[] {
                    BossType.Overlord,
                    BossType.Behemoth,
                    BossType.Dreadnought,
                    BossType.Hivemind
                };
            }
            else if (ThreatLevel >= 3)
            {
                possibleBosses = new[] {
                    BossType.Overlord,
                    BossType.Behemoth,
                    BossType.Dreadnought
                };
            }
            else if (ThreatLevel >= 2)
            {
                possibleBosses = new[] {
                    BossType.Overlord,
                    BossType.Behemoth
                };
            }
            else
            {
                possibleBosses = new[] { BossType.Overlord };
            }

            var bossType = possibleBosses[UnityEngine.Random.Range(0, possibleBosses.Length)];

            // Spawn boss above the idol
            Vector3 bossSpawnPos = spawnPos + Vector3.up * 20f;
            TurretDefensePlugin.SpawnBoss(bossType, bossSpawnPos, ThreatLevel);

            TurretDefensePlugin.Log.LogError($"=== IDOL SPAWNED BOSS: {bossType} (Threat {ThreatLevel}) ===");
        }

        private IEnumerator SpawnEffect()
        {
            var spawnObj = new GameObject("HiveSpawn");
            spawnObj.transform.position = transform.position;

            var particles = spawnObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 5f;
            main.startSize = 0.5f;
            main.startColor = hiveLight.color;

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.5f;

            var renderer = spawnObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            yield return new WaitForSeconds(2f);
            UnityEngine.Object.Destroy(spawnObj);
        }

        private void UpdateBrainBuffs()
        {
            pulseTimer += Time.deltaTime;
            if (pulseTimer < pulseInterval) return;
            pulseTimer = 0f;

            // Find and buff nearby enemies
            var nearbyEnemies = TurretDefensePlugin.GetNearbyEnemies(transform.position, EffectRadius);

            foreach (var enemy in nearbyEnemies)
            {
                // Apply buff effect
                // TODO: Implement buff system
            }

            // Visual pulse
            StartCoroutine(BuffPulseEffect());
        }

        private IEnumerator BuffPulseEffect()
        {
            var pulseObj = new GameObject("BrainPulse");
            pulseObj.transform.position = transform.position;

            // Expanding ring effect
            var ring = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            ring.transform.SetParent(pulseObj.transform);
            ring.transform.localScale = new Vector3(1f, 0.1f, 1f);
            ring.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.8f, 0.2f, 1f, 0.5f));
            UnityEngine.Object.Destroy(ring.GetComponent<Collider>());

            float elapsed = 0f;
            while (elapsed < 1f)
            {
                elapsed += Time.deltaTime;
                float scale = Mathf.Lerp(1f, EffectRadius, elapsed / 1f);
                ring.transform.localScale = new Vector3(scale, 0.1f, scale);

                var mat = ring.GetComponent<Renderer>().material;
                mat.color = new Color(0.8f, 0.2f, 1f, 0.5f * (1f - elapsed));

                yield return null;
            }

            UnityEngine.Object.Destroy(pulseObj);
        }

        private void UpdateStomachHealing()
        {
            pulseTimer += Time.deltaTime;
            if (pulseTimer < 2f) return;
            pulseTimer = 0f;

            // Heal nearby enemies
            var nearbyEnemies = TurretDefensePlugin.GetNearbyEnemies(transform.position, EffectRadius);

            foreach (var enemy in nearbyEnemies)
            {
                // TODO: Heal enemy
            }
        }

        private void UpdateTerraformerCorruption()
        {
            // Slowly spreads hazardous terrain
            if (Time.time - lastSpawnTime < SpawnInterval) return;
            if (currentSpawnCount >= MaxSpawns) return;

            lastSpawnTime = Time.time;
            currentSpawnCount++;

            // Spawn hazard zone
            Vector3 hazardPos = transform.position + UnityEngine.Random.insideUnitSphere * EffectRadius * 0.5f;
            hazardPos.y = transform.position.y;

            TurretDefensePlugin.SpawnHazardZone(hazardPos, 5f, 10f); // Radius 5, duration 10s
        }

        private void UpdateClawAttack()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer > EffectRadius) return;

            // Attack cooldown
            if (Time.time - lastSpawnTime < 2f) return;
            lastSpawnTime = Time.time;

            // Claw attack
            StartCoroutine(ClawAttackEffect());
            TurretDefensePlugin.DamagePlayer(30f, "Hive Claw");
        }

        private IEnumerator ClawAttackEffect()
        {
            if (playerTransform == null) yield break;

            // Projectile towards player
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "ClawSpike";
            projectile.transform.position = transform.position + Vector3.up * 3f;
            projectile.transform.localScale = Vector3.one * 0.5f;
            projectile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.5f, 0.1f, 0.1f));
            UnityEngine.Object.Destroy(projectile.GetComponent<Collider>());

            float elapsed = 0f;
            Vector3 start = projectile.transform.position;
            Vector3 end = playerTransform.position + Vector3.up;

            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                projectile.transform.position = Vector3.Lerp(start, end, elapsed / 0.3f);
                yield return null;
            }

            UnityEngine.Object.Destroy(projectile);
        }

        private void UpdateCrystalPower()
        {
            // Passive effect - nearby birthing hives spawn faster
            // This is checked by other hives
        }

        public bool IsNearCrystal()
        {
            // Check if there's a crystal hive nearby
            var hives = TurretDefensePlugin.GetNearbyHives(transform.position, 60f);
            foreach (var hive in hives)
            {
                if (hive != this && hive.Type == HiveType.Crystal && hive.IsAlive)
                    return true;
            }
            return false;
        }

        public void TakeDamage(float damage, bool isCritical)
        {
            float effectiveDamage = Mathf.Max(1, damage - Armor);
            Health -= effectiveDamage;

            TurretDefensePlugin.LogDebug($"Hive {HiveName} took {effectiveDamage:F0} damage, HP: {Health:F0}/{MaxHealth}");

            StartCoroutine(DamageFlash());

            if (Health <= 0)
            {
                Die();
            }
        }

        private IEnumerator DamageFlash()
        {
            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                    r.material.color = Color.white;
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
            IsActive = false;

            TurretDefensePlugin.Log.LogInfo($"HIVE DESTROYED: {HiveName}!");
            TurretDefensePlugin.OnHiveDestroyed(this);

            // Massive explosion
            StartCoroutine(DeathSequence());
        }

        private IEnumerator DeathSequence()
        {
            // Chain reaction - damage nearby enemies
            var nearbyEnemies = TurretDefensePlugin.GetNearbyEnemies(transform.position, 15f);
            foreach (var enemy in nearbyEnemies)
            {
                var controller = enemy.GetComponent<GroundEnemyController>();
                if (controller != null)
                {
                    controller.TakeDamage(100f, false);
                }
            }

            // Multi-stage explosion
            for (int i = 0; i < 5; i++)
            {
                Vector3 explosionPos = transform.position + UnityEngine.Random.insideUnitSphere * 3f;

                var explosionObj = new GameObject("HiveExplosion");
                explosionObj.transform.position = explosionPos;

                var particles = explosionObj.AddComponent<ParticleSystem>();
                var main = particles.main;
                main.startLifetime = 1.5f;
                main.startSpeed = 15f;
                main.startSize = 1f;
                main.startColor = hiveLight.color;

                var emission = particles.emission;
                emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
                emission.rateOverTime = 0;

                var shape = particles.shape;
                shape.shapeType = ParticleSystemShapeType.Sphere;
                shape.radius = 1f;

                var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
                renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

                var light = explosionObj.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 25f;
                light.intensity = 10f;
                light.color = hiveLight.color;

                UnityEngine.Object.Destroy(explosionObj, 3f);

                yield return new WaitForSeconds(0.2f);
            }

            // Final destruction
            yield return new WaitForSeconds(0.5f);
            UnityEngine.Object.Destroy(gameObject);
        }

        public float GetSpawnRateMultiplier()
        {
            float multiplier = 1f;

            // Crystal nearby boosts spawn rate
            if (IsNearCrystal())
            {
                multiplier *= 1.5f;
            }

            // Damaged hives spawn slower
            float healthRatio = Health / MaxHealth;
            if (healthRatio < 0.5f)
            {
                multiplier *= healthRatio + 0.5f;
            }

            return multiplier;
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1, 0.5f, 0, 0.3f);
            Gizmos.DrawWireSphere(transform.position, EffectRadius);

            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, 5f); // Spawn radius
        }
    }
}
