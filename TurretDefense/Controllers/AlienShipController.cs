using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Alien ship types with different behaviors
    /// </summary>
    public enum AlienBehavior
    {
        Fighter,    // Fast, agile, strafing runs
        Destroyer,  // Heavy, slow, direct assault
        Torpedo,    // Kamikaze, high speed, explodes on contact
        Bomber,     // Drops bombs on structures
        Scout,      // Fast, avoids combat, marks targets
        // Robot variants (from Sci_fi_Drones pack)
        RobotScout,     // Fast harassment, low HP
        RobotInvader,   // Elite enemy, high damage
        RobotGuardian,  // BOSS - massive HP, multi-phase
        RobotCollector  // Harvests resources, flees when damaged
    }

    /// <summary>
    /// Advanced alien ship controller with AI, animations, and combat
    /// </summary>
    public class AlienShipController : MonoBehaviour
    {
        // ========== IDENTITY ==========
        public string ShipType { get; private set; }
        public AlienBehavior Behavior { get; private set; }
        public int WaveNumber { get; private set; }

        // ========== STATS ==========
        public float MaxHealth { get; private set; } = 200f;
        public float Health { get; private set; } = 200f;
        public float Armor { get; private set; } = 0f;
        public float MoveSpeed { get; private set; } = 10f;
        public float AttackDamage { get; private set; } = 20f;
        public float AttackRange { get; private set; } = 15f;
        public float AttackCooldown { get; private set; } = 2f;
        public int ScoreValue { get; private set; } = 100;

        public bool IsAlive => Health > 0;
        public Vector3 Velocity { get; private set; }

        // ========== AI STATE ==========
        private enum AIState { Spawning, Approaching, Attacking, Strafing, Retreating, Dying }
        private AIState currentState = AIState.Spawning;

        private Vector3 targetPosition;
        private Vector3 strafeDirection;
        private float lastAttackTime;
        private float stateTimer;
        private float behaviorTimer;

        // ========== ANIMATION ==========
        private float bobOffset;
        private float bobSpeed = 2f;
        private float bobAmount = 0.5f;
        private float bankAngle = 0f;
        private float engineGlowIntensity = 1f;
        private Vector3 basePosition;

        // Animator support for prefabs with animation
        private Animator animator;
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int AnimIsHurt = Animator.StringToHash("IsHurt");
        private static readonly int AnimIsDying = Animator.StringToHash("IsDying");
        private static readonly int AnimTriggerAttack = Animator.StringToHash("Attack");
        private static readonly int AnimTriggerHurt = Animator.StringToHash("Hurt");
        private static readonly int AnimTriggerDeath = Animator.StringToHash("Death");
        private static readonly int AnimTriggerSpawn = Animator.StringToHash("Spawn");
        private static readonly int AnimTriggerBoost = Animator.StringToHash("Boost");
        private static readonly int AnimBankAngle = Animator.StringToHash("BankAngle");

        // ========== REFERENCES ==========
        private Transform playerTransform;
        private Renderer[] renderers;
        private Light engineLight;
        private ParticleSystem engineTrail;
        private Color originalColor;
        private Color damageFlashColor = Color.white;

        // ========== LOOT ==========
        private static readonly Dictionary<string, LootDrop[]> LootTables = new Dictionary<string, LootDrop[]>
        {
            { "Fighter", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 1, 3, 0.5f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.15f)
            }},
            { "Destroyer", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 3, 8, 0.7f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 2, 0.4f)
            }},
            { "Torpedo", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 1, 2, 0.3f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.2f)
            }},
            // Robot enemies (from Sci_fi_Drones pack)
            { "RobotScout", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 1, 2, 0.4f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.1f)
            }},
            { "RobotInvader", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 5, 12, 0.8f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 2, 4, 0.5f)
            }},
            { "RobotGuardian", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 15, 30, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 5, 10, 1.0f)
            }},
            { "RobotCollector", new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 3, 8, 0.9f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 3, 0.3f)
            }}
        };

        public void Initialize(string shipType, int waveNumber)
        {
            ShipType = shipType;
            WaveNumber = waveNumber;
            SetupStats();
            SetupBehavior();
            SetupReferences();
            SetupEffects();

            currentState = AIState.Spawning;
            stateTimer = 1f; // 1 second spawn animation
            basePosition = transform.position;

            TurretDefensePlugin.LogDebug($"Alien {ShipType} initialized for wave {WaveNumber}");
        }

        private void SetupStats()
        {
            float waveMultiplier = 1f + (WaveNumber - 1) * (TurretDefensePlugin.DifficultyScaling.Value - 1f);

            if (ShipType.Contains("Destroyer"))
            {
                Behavior = AlienBehavior.Destroyer;
                MaxHealth = 500f * waveMultiplier;
                Armor = 10f;
                MoveSpeed = 6f;
                AttackDamage = 40f * waveMultiplier;
                AttackRange = 20f;
                AttackCooldown = 3f;
                ScoreValue = 250;
            }
            else if (ShipType.Contains("Torpedo"))
            {
                Behavior = AlienBehavior.Torpedo;
                MaxHealth = 80f * waveMultiplier;
                Armor = 0f;
                MoveSpeed = 20f;
                AttackDamage = 100f * waveMultiplier;
                AttackRange = 2f; // Explodes on contact
                AttackCooldown = 0f;
                ScoreValue = 150;
            }
            // Robot variants (from Sci_fi_Drones pack)
            else if (ShipType.Contains("Robot_Guardian"))
            {
                // BOSS ENEMY - massive stats, multi-phase potential
                Behavior = AlienBehavior.RobotGuardian;
                MaxHealth = 2000f * waveMultiplier;
                Armor = 25f;
                MoveSpeed = 4f;
                AttackDamage = 60f * waveMultiplier;
                AttackRange = 30f;
                AttackCooldown = 2f;
                ScoreValue = 1000;
            }
            else if (ShipType.Contains("Robot_Invader"))
            {
                // Elite enemy - high damage, moderately tanky
                Behavior = AlienBehavior.RobotInvader;
                MaxHealth = 400f * waveMultiplier;
                Armor = 15f;
                MoveSpeed = 8f;
                AttackDamage = 45f * waveMultiplier;
                AttackRange = 18f;
                AttackCooldown = 1.5f;
                ScoreValue = 400;
            }
            else if (ShipType.Contains("Robot_Scout") || ShipType.Contains("Robot_Rockie"))
            {
                // Fast harassment unit
                Behavior = AlienBehavior.RobotScout;
                MaxHealth = 100f * waveMultiplier;
                Armor = 2f;
                MoveSpeed = 18f;
                AttackDamage = 10f * waveMultiplier;
                AttackRange = 10f;
                AttackCooldown = 0.8f;
                ScoreValue = 75;
            }
            else if (ShipType.Contains("Robot_Collector"))
            {
                // Resource harvester - flees when damaged
                Behavior = AlienBehavior.RobotCollector;
                MaxHealth = 200f * waveMultiplier;
                Armor = 8f;
                MoveSpeed = 10f;
                AttackDamage = 5f * waveMultiplier;
                AttackRange = 8f;
                AttackCooldown = 2f;
                ScoreValue = 200;
            }
            else
            {
                Behavior = AlienBehavior.Fighter;
                MaxHealth = 150f * waveMultiplier;
                Armor = 5f;
                MoveSpeed = 12f;
                AttackDamage = 15f * waveMultiplier;
                AttackRange = 12f;
                AttackCooldown = 1.5f;
                ScoreValue = 100;
            }

            Health = MaxHealth;

            // Color variant bonuses
            if (ShipType.Contains("Green"))
            {
                MaxHealth *= 1.2f;
                Health = MaxHealth;
                Armor += 5f;
                ScoreValue = (int)(ScoreValue * 1.5f);
            }
            else if (ShipType.Contains("White"))
            {
                MoveSpeed *= 1.3f;
                AttackDamage *= 1.2f;
                ScoreValue = (int)(ScoreValue * 1.3f);
            }
        }

        private void SetupBehavior()
        {
            switch (Behavior)
            {
                case AlienBehavior.Fighter:
                    bobSpeed = 3f;
                    bobAmount = 0.3f;
                    break;
                case AlienBehavior.Destroyer:
                    bobSpeed = 1.5f;
                    bobAmount = 0.2f;
                    break;
                case AlienBehavior.Torpedo:
                    bobSpeed = 0f;
                    bobAmount = 0f;
                    break;
            }
        }

        private void SetupReferences()
        {
            playerTransform = Player.instance?.transform;
            renderers = GetComponentsInChildren<Renderer>();
            animator = GetComponent<Animator>() ?? GetComponentInChildren<Animator>();

            if (renderers.Length > 0 && renderers[0].material != null)
            {
                originalColor = renderers[0].material.color;
            }

            // Initialize animator if present
            if (animator != null)
            {
                InitializeAnimatorParameters();
                TriggerAnimation(AnimTriggerSpawn);
            }
        }

        private void InitializeAnimatorParameters()
        {
            if (animator == null) return;

            TrySetAnimatorBool(AnimIsMoving, false);
            TrySetAnimatorFloat(AnimMoveSpeed, 0f);
            TrySetAnimatorBool(AnimIsAttacking, false);
            TrySetAnimatorBool(AnimIsHurt, false);
            TrySetAnimatorBool(AnimIsDying, false);
            TrySetAnimatorFloat(AnimBankAngle, 0f);
        }

        private void TrySetAnimatorBool(int paramHash, bool value)
        {
            try { animator.SetBool(paramHash, value); }
            catch { /* Parameter doesn't exist in this animator */ }
        }

        private void TrySetAnimatorFloat(int paramHash, float value)
        {
            try { animator.SetFloat(paramHash, value); }
            catch { /* Parameter doesn't exist in this animator */ }
        }

        private void TriggerAnimation(int triggerHash)
        {
            if (animator == null) return;
            try { animator.SetTrigger(triggerHash); }
            catch { /* Trigger doesn't exist in this animator */ }
        }

        private void UpdateAnimatorState()
        {
            if (animator == null) return;

            // Update movement state
            float speed = Velocity.magnitude / MoveSpeed;
            TrySetAnimatorFloat(AnimMoveSpeed, speed);
            TrySetAnimatorBool(AnimIsMoving, speed > 0.1f);

            // Update bank angle for strafing
            TrySetAnimatorFloat(AnimBankAngle, bankAngle / 30f); // Normalize to -1 to 1

            // Update attacking state
            bool isAttacking = currentState == AIState.Attacking && Time.time - lastAttackTime < AttackCooldown * 0.3f;
            TrySetAnimatorBool(AnimIsAttacking, isAttacking);

            // Update dying state
            TrySetAnimatorBool(AnimIsDying, currentState == AIState.Dying);
        }

        private void SetupEffects()
        {
            // Engine light
            var lightObj = new GameObject("EngineLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.back * 1f;
            engineLight = lightObj.AddComponent<Light>();
            engineLight.type = LightType.Point;
            engineLight.range = 3f;
            engineLight.intensity = 1f;
            engineLight.color = new Color(0.3f, 0.5f, 1f);

            // Engine trail
            var trailObj = new GameObject("EngineTrail");
            trailObj.transform.SetParent(transform);
            trailObj.transform.localPosition = Vector3.back * 1.2f;
            engineTrail = trailObj.AddComponent<ParticleSystem>();

            var main = engineTrail.main;
            main.startLifetime = 0.5f;
            main.startSpeed = -2f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.3f, 0.5f, 1f, 0.7f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = engineTrail.emission;
            emission.rateOverTime = 20;

            var shape = engineTrail.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 10f;
            shape.radius = 0.1f;

            var colorOverLife = engineTrail.colorOverLifetime;
            colorOverLife.enabled = true;
            Gradient grad = new Gradient();
            grad.SetKeys(
                new GradientColorKey[] {
                    new GradientColorKey(new Color(0.3f, 0.5f, 1f), 0f),
                    new GradientColorKey(new Color(0.2f, 0.3f, 0.8f), 1f)
                },
                new GradientAlphaKey[] {
                    new GradientAlphaKey(0.8f, 0f),
                    new GradientAlphaKey(0f, 1f)
                }
            );
            colorOverLife.color = new ParticleSystem.MinMaxGradient(grad);

            var renderer = trailObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
        }

        private void Update()
        {
            if (!IsAlive) return;

            // Update player reference
            if (playerTransform == null)
                playerTransform = Player.instance?.transform;

            // State machine
            UpdateState();

            // Animation
            UpdateAnimation();

            // Effects
            UpdateEffects();
        }

        private void UpdateState()
        {
            stateTimer -= Time.deltaTime;
            behaviorTimer -= Time.deltaTime;

            switch (currentState)
            {
                case AIState.Spawning:
                    UpdateSpawning();
                    break;
                case AIState.Approaching:
                    UpdateApproaching();
                    break;
                case AIState.Attacking:
                    UpdateAttacking();
                    break;
                case AIState.Strafing:
                    UpdateStrafing();
                    break;
                case AIState.Retreating:
                    UpdateRetreating();
                    break;
            }
        }

        private void UpdateSpawning()
        {
            // Spawn animation - descend/materialize
            float spawnProgress = 1f - (stateTimer / 1f);
            transform.localScale = Vector3.one * 3f * Mathf.Lerp(0.1f, 1f, spawnProgress);

            if (stateTimer <= 0)
            {
                currentState = AIState.Approaching;
                transform.localScale = Vector3.one * 3f;
            }
        }

        private void UpdateApproaching()
        {
            if (playerTransform == null) return;

            // Move towards player
            Vector3 targetPos = playerTransform.position + Vector3.up * 10f;
            MoveTowards(targetPos);

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Transition to attack when in range
            if (distToPlayer < AttackRange)
            {
                if (Behavior == AlienBehavior.Torpedo)
                {
                    // Torpedoes commit to attack run
                    currentState = AIState.Attacking;
                }
                else if (Behavior == AlienBehavior.Fighter)
                {
                    // Fighters strafe
                    StartStrafe();
                }
                else
                {
                    currentState = AIState.Attacking;
                }
            }
        }

        private void UpdateAttacking()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (Behavior == AlienBehavior.Torpedo)
            {
                // Kamikaze run
                MoveTowards(playerTransform.position, MoveSpeed * 1.5f);

                if (distToPlayer < AttackRange)
                {
                    Explode();
                    return;
                }
            }
            else
            {
                // Maintain distance while attacking
                if (distToPlayer > AttackRange * 1.2f)
                {
                    MoveTowards(playerTransform.position + Vector3.up * 5f);
                }
                else if (distToPlayer < AttackRange * 0.5f)
                {
                    // Too close, back off
                    Vector3 awayDir = (transform.position - playerTransform.position).normalized;
                    MoveTowards(transform.position + awayDir * 10f);
                }

                // Face player
                LookAt(playerTransform.position);

                // Attack
                if (Time.time - lastAttackTime >= AttackCooldown)
                {
                    Attack();
                }

                // Fighter transitions to strafe after attack
                if (Behavior == AlienBehavior.Fighter && behaviorTimer <= 0)
                {
                    StartStrafe();
                }
            }
        }

        private void StartStrafe()
        {
            currentState = AIState.Strafing;
            strafeDirection = UnityEngine.Random.value > 0.5f ? transform.right : -transform.right;
            stateTimer = UnityEngine.Random.Range(2f, 4f);
        }

        private void UpdateStrafing()
        {
            if (playerTransform == null) return;

            // Strafe around player
            Vector3 toPlayer = (playerTransform.position - transform.position).normalized;
            Vector3 strafeTarget = transform.position + strafeDirection * MoveSpeed + toPlayer * 2f;
            strafeTarget.y = playerTransform.position.y + UnityEngine.Random.Range(5f, 15f);

            MoveTowards(strafeTarget, MoveSpeed * 1.2f);
            LookAt(playerTransform.position);

            // Attack while strafing
            if (Time.time - lastAttackTime >= AttackCooldown)
            {
                Attack();
            }

            // End strafe
            if (stateTimer <= 0)
            {
                currentState = AIState.Approaching;
            }
        }

        private void UpdateRetreating()
        {
            if (playerTransform == null) return;

            // Fly away
            Vector3 awayDir = (transform.position - playerTransform.position).normalized;
            Vector3 retreatTarget = transform.position + awayDir * 50f + Vector3.up * 20f;

            MoveTowards(retreatTarget, MoveSpeed * 1.3f);

            // Return to attacking after retreat
            if (stateTimer <= 0 || Vector3.Distance(transform.position, playerTransform.position) > 80f)
            {
                currentState = AIState.Approaching;
            }
        }

        private void MoveTowards(Vector3 target, float speed = 0)
        {
            if (speed <= 0) speed = MoveSpeed;

            Vector3 direction = (target - transform.position).normalized;
            Vector3 movement = direction * speed * Time.deltaTime;
            Velocity = direction * speed;

            transform.position += movement;

            // Bank during turns
            float turnAmount = Vector3.Dot(transform.right, direction);
            bankAngle = Mathf.Lerp(bankAngle, -turnAmount * 30f, Time.deltaTime * 3f);
        }

        private void LookAt(Vector3 target)
        {
            Vector3 direction = (target - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 5f);
            }
        }

        private void Attack()
        {
            lastAttackTime = Time.time;
            behaviorTimer = UnityEngine.Random.Range(3f, 6f);

            // Visual attack effect
            StartCoroutine(AttackEffect());

            TurretDefensePlugin.Log.LogWarning($"{ShipType} attacks!");

            // Apply damage to player
            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{ShipType} attack");

            // Also check for nearby buildings to damage
            var nearbyMachines = TurretDefensePlugin.GetNearbyMachines(transform.position, AttackRange * 1.5f);
            if (nearbyMachines.Count > 0 && UnityEngine.Random.value < 0.3f)
            {
                // 30% chance to target buildings instead of player
                var targetMachine = nearbyMachines[0];
                TurretDefensePlugin.Log.LogInfo($"{ShipType} attacking machine at {targetMachine.position}");
            }
        }

        private IEnumerator AttackEffect()
        {
            // Muzzle flash / projectile
            var attackObj = new GameObject("AlienAttack");
            attackObj.transform.position = transform.position + transform.forward * 2f;

            Color projectileColor = ShipType.Contains("Green") ? Color.green : new Color(1f, 0.3f, 0.8f);

            var light = attackObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 5f;
            light.intensity = 3f;
            light.color = projectileColor;

            // Projectile towards player
            if (playerTransform != null)
            {
                var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                projectile.transform.position = attackObj.transform.position;
                projectile.transform.localScale = Vector3.one * 0.3f;
                projectile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(projectileColor);
                UnityEngine.Object.Destroy(projectile.GetComponent<Collider>());

                float elapsed = 0f;
                Vector3 start = projectile.transform.position;
                Vector3 end = playerTransform.position;

                while (elapsed < 0.3f)
                {
                    elapsed += Time.deltaTime;
                    projectile.transform.position = Vector3.Lerp(start, end, elapsed / 0.3f);
                    yield return null;
                }

                UnityEngine.Object.Destroy(projectile);
            }

            yield return new WaitForSeconds(0.1f);
            UnityEngine.Object.Destroy(attackObj);
        }

        private void Explode()
        {
            // Torpedo explosion
            TurretDefensePlugin.Log.LogWarning($"{ShipType} EXPLODES!");

            // Create explosion effect
            var explosionObj = new GameObject("TorpedoExplosion");
            explosionObj.transform.position = transform.position;

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 15f;
            main.startSize = 1f;
            main.startColor = new Color(1f, 0.5f, 0.1f);

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
            light.range = 20f;
            light.intensity = 10f;
            light.color = new Color(1f, 0.6f, 0.2f);

            UnityEngine.Object.Destroy(explosionObj, 2f);

            // Damage to player (large radius)
            // TODO: Apply AOE damage

            Die();
        }

        private void UpdateAnimation()
        {
            // Hover bobbing (procedural animation)
            if (bobAmount > 0)
            {
                bobOffset += Time.deltaTime * bobSpeed;
                Vector3 bob = Vector3.up * Mathf.Sin(bobOffset) * bobAmount;
                // Apply bob offset relative to base movement
            }

            // Banking (procedural animation)
            Vector3 currentRot = transform.eulerAngles;
            currentRot.z = bankAngle;
            transform.eulerAngles = currentRot;

            // Update animator state for prefabs with animation
            UpdateAnimatorState();
        }

        private void UpdateEffects()
        {
            // Engine glow based on speed
            float speedRatio = Velocity.magnitude / MoveSpeed;
            engineGlowIntensity = Mathf.Lerp(engineGlowIntensity, 0.5f + speedRatio * 1.5f, Time.deltaTime * 3f);

            if (engineLight != null)
                engineLight.intensity = engineGlowIntensity;

            if (engineTrail != null)
            {
                var emission = engineTrail.emission;
                emission.rateOverTime = 10 + speedRatio * 30f;
            }
        }

        public void TakeDamage(float damage, bool isCritical)
        {
            // Apply armor
            float effectiveDamage = Mathf.Max(1, damage - Armor);

            Health -= effectiveDamage;

            TurretDefensePlugin.LogDebug($"{ShipType} took {effectiveDamage:F0} damage (armor blocked {Armor}), HP: {Health:F0}/{MaxHealth}");

            // Trigger hurt animation
            TriggerAnimation(AnimTriggerHurt);
            TrySetAnimatorBool(AnimIsHurt, true);
            StartCoroutine(ResetHurtState());

            // Damage flash
            StartCoroutine(DamageFlash());

            // Hit recoil
            Vector3 knockback = -transform.forward * 0.5f;
            transform.position += knockback;

            // Low health behavior
            if (Health < MaxHealth * 0.3f && Behavior != AlienBehavior.Torpedo)
            {
                // Chance to retreat
                if (UnityEngine.Random.value < 0.3f && currentState != AIState.Retreating)
                {
                    currentState = AIState.Retreating;
                    stateTimer = 5f;
                }

                // Smoke effect when damaged
                if (engineTrail != null)
                {
                    var main = engineTrail.main;
                    main.startColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
                }
            }

            if (Health <= 0)
            {
                Die();
            }
        }

        private IEnumerator ResetHurtState()
        {
            yield return new WaitForSeconds(0.3f);
            TrySetAnimatorBool(AnimIsHurt, false);
        }

        private IEnumerator DamageFlash()
        {
            foreach (var r in renderers)
            {
                if (r != null && r.material != null)
                    r.material.color = damageFlashColor;
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

            // Trigger death animation
            TrySetAnimatorBool(AnimIsDying, true);
            TriggerAnimation(AnimTriggerDeath);

            TurretDefensePlugin.Log.LogInfo($"{ShipType} destroyed!");
            TurretDefensePlugin.OnAlienKilled(this);

            // Drop loot
            if (TurretDefensePlugin.EnableLootDrops.Value)
            {
                DropLoot();
            }

            // Death explosion
            StartCoroutine(DeathSequence());
        }

        private void DropLoot()
        {
            // Determine loot table key based on enemy type
            string lootKey;
            if (ShipType.Contains("Robot_Guardian"))
                lootKey = "RobotGuardian";
            else if (ShipType.Contains("Robot_Invader"))
                lootKey = "RobotInvader";
            else if (ShipType.Contains("Robot_Scout") || ShipType.Contains("Robot_Rockie") || ShipType.Contains("Robot_HyperX"))
                lootKey = "RobotScout";
            else if (ShipType.Contains("Robot_Collector"))
                lootKey = "RobotCollector";
            else if (ShipType.Contains("Destroyer"))
                lootKey = "Destroyer";
            else if (ShipType.Contains("Torpedo"))
                lootKey = "Torpedo";
            else
                lootKey = "Fighter";

            if (LootTables.TryGetValue(lootKey, out var drops))
            {
                foreach (var drop in drops)
                {
                    if (UnityEngine.Random.value < drop.DropChance)
                    {
                        int amount = UnityEngine.Random.Range(drop.MinAmount, drop.MaxAmount + 1);
                        TurretDefensePlugin.Log.LogInfo($"LOOT: {amount}x {drop.ItemName}");
                        // TODO: Actually add to player inventory or spawn pickup
                    }
                }
            }
        }

        private IEnumerator DeathSequence()
        {
            // Stop moving
            Velocity = Vector3.zero;

            // Sparks and smoke
            if (engineTrail != null)
            {
                var main = engineTrail.main;
                main.startColor = new Color(1f, 0.3f, 0.1f, 0.8f);
                var emission = engineTrail.emission;
                emission.rateOverTime = 50;
            }

            // Spin out
            float spinTime = 0.5f;
            float elapsed = 0f;
            Vector3 spinAxis = UnityEngine.Random.insideUnitSphere.normalized;

            while (elapsed < spinTime)
            {
                elapsed += Time.deltaTime;
                transform.Rotate(spinAxis, 500f * Time.deltaTime);
                transform.position += Vector3.down * 10f * Time.deltaTime;
                yield return null;
            }

            // Final explosion
            var explosionObj = new GameObject("DeathExplosion");
            explosionObj.transform.position = transform.position;

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main2 = particles.main;
            main2.startLifetime = 0.8f;
            main2.startSpeed = 10f;
            main2.startSize = 0.5f;
            main2.startColor = new Color(1f, 0.5f, 0.1f);

            var emission2 = particles.emission;
            emission2.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
            emission2.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            var light = explosionObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 15f;
            light.intensity = 5f;
            light.color = new Color(1f, 0.6f, 0.2f);

            UnityEngine.Object.Destroy(explosionObj, 2f);

            // Remove from list and destroy
            UnityEngine.Object.Destroy(gameObject);
        }

        void OnDrawGizmosSelected()
        {
            // Draw attack range
            Gizmos.color = new Color(1, 0, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, AttackRange);

            // Draw velocity
            if (Velocity != Vector3.zero)
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(transform.position, transform.position + Velocity);
            }
        }
    }

    /// <summary>
    /// Loot drop definition
    /// </summary>
    public struct LootDrop
    {
        public string ItemName;
        public int MinAmount;
        public int MaxAmount;
        public float DropChance;

        public LootDrop(string item, int min, int max, float chance)
        {
            ItemName = item;
            MinAmount = min;
            MaxAmount = max;
            DropChance = chance;
        }
    }
}
