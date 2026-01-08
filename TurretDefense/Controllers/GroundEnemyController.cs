using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Ground-based enemy types (Chomper, Spitter, Mimic, Arachnid, Robots)
    /// </summary>
    public enum GroundEnemyType
    {
        Chomper,        // Melee, charges at player, high damage
        Spitter,        // Ranged acid attack, keeps distance
        Mimic,          // Ambush predator, disguises as objects
        Arachnid,       // Fast spider, swarms in groups
        Grenadier,      // Throws explosive projectiles
        Gunner,         // Sustained ranged fire
        MachineGunRobot,// Heavy robot with rapid fire machine gun
        CannonMachine   // Heavy artillery robot with powerful cannon
    }

    /// <summary>
    /// Controller for ground-based alien creatures
    /// </summary>
    public class GroundEnemyController : MonoBehaviour
    {
        // ========== IDENTITY ==========
        public string EnemyName { get; private set; }
        public GroundEnemyType EnemyType { get; private set; }
        public int WaveNumber { get; private set; }

        // ========== STATS ==========
        public float MaxHealth { get; private set; } = 100f;
        public float Health { get; private set; } = 100f;
        public float Armor { get; private set; } = 0f;
        public float MoveSpeed { get; private set; } = 5f;
        public float AttackDamage { get; private set; } = 15f;
        public float AttackRange { get; private set; } = 3f;
        public float AttackCooldown { get; private set; } = 1.5f;
        public int ScoreValue { get; private set; } = 50;

        public bool IsAlive => Health > 0;
        public Vector3 Velocity { get; private set; }

        // ========== AI STATE ==========
        private enum AIState { Spawning, Idle, Pursuing, Attacking, Fleeing, Ambush, Dying }
        private AIState currentState = AIState.Spawning;

        private Vector3 targetPosition;
        private float lastAttackTime;
        private float stateTimer;
        private float ambushRevealDistance = 8f;
        private bool isRevealed = false;

        // ========== ANIMATION ==========
        private Animator animator;
        private float animSpeedMultiplier = 1f;

        // Animation parameter hashes for performance
        private static readonly int AnimIsMoving = Animator.StringToHash("IsMoving");
        private static readonly int AnimMoveSpeed = Animator.StringToHash("MoveSpeed");
        private static readonly int AnimIsAttacking = Animator.StringToHash("IsAttacking");
        private static readonly int AnimIsHurt = Animator.StringToHash("IsHurt");
        private static readonly int AnimIsDying = Animator.StringToHash("IsDying");
        private static readonly int AnimAttackType = Animator.StringToHash("AttackType");
        private static readonly int AnimTriggerAttack = Animator.StringToHash("Attack");
        private static readonly int AnimTriggerHurt = Animator.StringToHash("Hurt");
        private static readonly int AnimTriggerDeath = Animator.StringToHash("Death");
        private static readonly int AnimTriggerSpawn = Animator.StringToHash("Spawn");
        private static readonly int AnimTriggerReveal = Animator.StringToHash("Reveal");
        private static readonly int AnimTriggerRoar = Animator.StringToHash("Roar");

        private float lastMoveSpeed = 0f;
        private bool wasMoving = false;

        // ========== REFERENCES ==========
        private Transform playerTransform;
        private Renderer[] renderers;
        private Color originalColor;
        private Color damageFlashColor = Color.white;

        // ========== LOOT ==========
        private static readonly Dictionary<GroundEnemyType, LootDrop[]> LootTables = new Dictionary<GroundEnemyType, LootDrop[]>
        {
            { GroundEnemyType.Chomper, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 2, 5, 0.6f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.2f)
            }},
            { GroundEnemyType.Spitter, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 1, 3, 0.5f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.25f)
            }},
            { GroundEnemyType.Mimic, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 3, 8, 0.8f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 2, 0.4f)
            }},
            { GroundEnemyType.Arachnid, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 1, 2, 0.4f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 1, 0.1f)
            }},
            { GroundEnemyType.Grenadier, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 4, 10, 0.7f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 3, 0.35f)
            }},
            { GroundEnemyType.Gunner, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 3, 7, 0.65f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 1, 2, 0.3f)
            }},
            { GroundEnemyType.MachineGunRobot, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 8, 15, 0.9f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 2, 4, 0.5f)
            }},
            { GroundEnemyType.CannonMachine, new[] {
                new LootDrop(TurretDefensePlugin.AlienAlloyName, 12, 25, 1.0f),
                new LootDrop(TurretDefensePlugin.AlienCoreName, 3, 6, 0.7f)
            }}
        };

        public void Initialize(string enemyName, GroundEnemyType type, int waveNumber)
        {
            EnemyName = enemyName;
            EnemyType = type;
            WaveNumber = waveNumber;
            SetupStats();
            SetupReferences();

            // Mimic starts in ambush state
            if (EnemyType == GroundEnemyType.Mimic)
            {
                currentState = AIState.Ambush;
                isRevealed = false;
                SetAmbushAppearance(true);
            }
            else
            {
                currentState = AIState.Spawning;
                stateTimer = 0.5f;
            }

            TurretDefensePlugin.LogDebug($"Ground enemy {EnemyName} ({EnemyType}) initialized for wave {WaveNumber}");
        }

        private void SetupStats()
        {
            float waveMultiplier = 1f + (WaveNumber - 1) * (TurretDefensePlugin.DifficultyScaling.Value - 1f);

            switch (EnemyType)
            {
                case GroundEnemyType.Chomper:
                    MaxHealth = 300f * waveMultiplier;
                    Armor = 10f;
                    MoveSpeed = 6f;
                    AttackDamage = 35f * waveMultiplier;
                    AttackRange = 3f;
                    AttackCooldown = 2f;
                    ScoreValue = 150;
                    break;

                case GroundEnemyType.Spitter:
                    MaxHealth = 150f * waveMultiplier;
                    Armor = 3f;
                    MoveSpeed = 4f;
                    AttackDamage = 20f * waveMultiplier;
                    AttackRange = 15f;
                    AttackCooldown = 2.5f;
                    ScoreValue = 100;
                    break;

                case GroundEnemyType.Mimic:
                    MaxHealth = 400f * waveMultiplier;
                    Armor = 15f;
                    MoveSpeed = 8f;
                    AttackDamage = 50f * waveMultiplier; // High ambush damage
                    AttackRange = 4f;
                    AttackCooldown = 1.5f;
                    ScoreValue = 250;
                    ambushRevealDistance = 6f;
                    break;

                case GroundEnemyType.Arachnid:
                    MaxHealth = 80f * waveMultiplier;
                    Armor = 2f;
                    MoveSpeed = 12f; // Very fast
                    AttackDamage = 12f * waveMultiplier;
                    AttackRange = 2f;
                    AttackCooldown = 0.8f;
                    ScoreValue = 40;
                    break;

                case GroundEnemyType.Grenadier:
                    MaxHealth = 250f * waveMultiplier;
                    Armor = 8f;
                    MoveSpeed = 3.5f;
                    AttackDamage = 40f * waveMultiplier;
                    AttackRange = 20f;
                    AttackCooldown = 3f;
                    ScoreValue = 200;
                    break;

                case GroundEnemyType.Gunner:
                    MaxHealth = 200f * waveMultiplier;
                    Armor = 5f;
                    MoveSpeed = 4f;
                    AttackDamage = 8f * waveMultiplier;
                    AttackRange = 18f;
                    AttackCooldown = 0.3f; // Rapid fire
                    ScoreValue = 175;
                    break;

                case GroundEnemyType.MachineGunRobot:
                    MaxHealth = 500f * waveMultiplier;
                    Armor = 25f;
                    MoveSpeed = 3f;
                    AttackDamage = 12f * waveMultiplier;
                    AttackRange = 25f;
                    AttackCooldown = 0.15f; // Very rapid fire
                    ScoreValue = 350;
                    break;

                case GroundEnemyType.CannonMachine:
                    MaxHealth = 800f * waveMultiplier;
                    Armor = 40f;
                    MoveSpeed = 2f;
                    AttackDamage = 60f * waveMultiplier;
                    AttackRange = 35f;
                    AttackCooldown = 4f; // Slow but devastating
                    ScoreValue = 500;
                    break;
            }

            Health = MaxHealth;
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

            // Initialize animator parameters if animator exists
            if (animator != null)
            {
                InitializeAnimatorParameters();
                // Trigger spawn animation
                TriggerAnimation(AnimTriggerSpawn);
            }
        }

        private void InitializeAnimatorParameters()
        {
            if (animator == null) return;

            // Safely try to set initial parameter values
            TrySetAnimatorBool(AnimIsMoving, false);
            TrySetAnimatorFloat(AnimMoveSpeed, 0f);
            TrySetAnimatorBool(AnimIsAttacking, false);
            TrySetAnimatorBool(AnimIsHurt, false);
            TrySetAnimatorBool(AnimIsDying, false);

            // Set attack type based on enemy type
            int attackType = EnemyType switch
            {
                GroundEnemyType.Chomper => 0,    // Bite/claw
                GroundEnemyType.Spitter => 1,    // Ranged spit
                GroundEnemyType.Mimic => 2,      // Ambush strike
                GroundEnemyType.Arachnid => 3,   // Quick bite
                GroundEnemyType.Grenadier => 4,  // Throw
                GroundEnemyType.Gunner => 5,     // Shoot
                GroundEnemyType.MachineGunRobot => 6, // Machine gun
                GroundEnemyType.CannonMachine => 7,   // Cannon
                _ => 0
            };
            TrySetAnimatorInt(AnimAttackType, attackType);
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

        private void TrySetAnimatorInt(int paramHash, int value)
        {
            try { animator.SetInteger(paramHash, value); }
            catch { /* Parameter doesn't exist in this animator */ }
        }

        private void TriggerAnimation(int triggerHash)
        {
            if (animator == null) return;
            try { animator.SetTrigger(triggerHash); }
            catch { /* Trigger doesn't exist in this animator */ }
        }

        private void UpdateAnimationState()
        {
            if (animator == null) return;

            // Calculate current movement state
            float currentSpeed = Velocity.magnitude;
            bool isMoving = currentSpeed > 0.1f;

            // Update movement parameters with smoothing
            if (isMoving != wasMoving)
            {
                TrySetAnimatorBool(AnimIsMoving, isMoving);
                wasMoving = isMoving;
            }

            // Smooth speed transitions
            float targetSpeed = currentSpeed / MoveSpeed;
            lastMoveSpeed = Mathf.Lerp(lastMoveSpeed, targetSpeed, Time.deltaTime * 5f);
            TrySetAnimatorFloat(AnimMoveSpeed, lastMoveSpeed);

            // Update attacking state
            bool isAttacking = currentState == AIState.Attacking && Time.time - lastAttackTime < AttackCooldown * 0.5f;
            TrySetAnimatorBool(AnimIsAttacking, isAttacking);

            // Update dying state
            TrySetAnimatorBool(AnimIsDying, currentState == AIState.Dying);

            // Speed multiplier for animator
            animator.speed = animSpeedMultiplier;
        }

        private void Update()
        {
            if (!IsAlive) return;

            if (playerTransform == null)
                playerTransform = Player.instance?.transform;

            UpdateState();
            UpdateAnimationState();
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
                case AIState.Pursuing:
                    UpdatePursuing();
                    break;
                case AIState.Attacking:
                    UpdateAttacking();
                    break;
                case AIState.Fleeing:
                    UpdateFleeing();
                    break;
                case AIState.Ambush:
                    UpdateAmbush();
                    break;
            }
        }

        private void UpdateSpawning()
        {
            float spawnProgress = 1f - (stateTimer / 0.5f);
            transform.localScale = Vector3.one * Mathf.Lerp(0.1f, 1f, spawnProgress);

            if (stateTimer <= 0)
            {
                currentState = AIState.Pursuing;
                transform.localScale = Vector3.one;
            }
        }

        private void UpdateIdle()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);
            if (distToPlayer < 30f)
            {
                currentState = AIState.Pursuing;
            }
        }

        private void UpdatePursuing()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Move towards player (staying on ground)
            Vector3 direction = (playerTransform.position - transform.position);
            direction.y = 0;
            direction.Normalize();

            MoveInDirection(direction);

            // Face movement direction
            if (direction != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(direction);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 8f);
            }

            // Transition to attack when in range
            if (distToPlayer < AttackRange)
            {
                currentState = AIState.Attacking;
            }

            // Spitter/Grenadier/Gunner attack from range
            if (EnemyType == GroundEnemyType.Spitter ||
                EnemyType == GroundEnemyType.Grenadier ||
                EnemyType == GroundEnemyType.Gunner)
            {
                if (distToPlayer < AttackRange && distToPlayer > AttackRange * 0.3f)
                {
                    currentState = AIState.Attacking;
                }
            }
        }

        private void UpdateAttacking()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            // Face player
            Vector3 lookDir = (playerTransform.position - transform.position);
            lookDir.y = 0;
            if (lookDir != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(lookDir);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }

            // Ranged enemies maintain distance
            if (EnemyType == GroundEnemyType.Spitter ||
                EnemyType == GroundEnemyType.Grenadier ||
                EnemyType == GroundEnemyType.Gunner)
            {
                if (distToPlayer < AttackRange * 0.4f)
                {
                    // Back up
                    Vector3 awayDir = (transform.position - playerTransform.position);
                    awayDir.y = 0;
                    awayDir.Normalize();
                    MoveInDirection(awayDir, MoveSpeed * 0.5f);
                }
                else if (distToPlayer > AttackRange * 1.1f)
                {
                    currentState = AIState.Pursuing;
                    return;
                }
            }
            else
            {
                // Melee enemies need to stay close
                if (distToPlayer > AttackRange * 1.5f)
                {
                    currentState = AIState.Pursuing;
                    return;
                }
            }

            // Attack
            if (Time.time - lastAttackTime >= AttackCooldown)
            {
                Attack();
            }
        }

        private void UpdateFleeing()
        {
            if (playerTransform == null) return;

            Vector3 awayDir = (transform.position - playerTransform.position);
            awayDir.y = 0;
            awayDir.Normalize();

            MoveInDirection(awayDir, MoveSpeed * 1.2f);

            if (stateTimer <= 0)
            {
                currentState = AIState.Pursuing;
            }
        }

        private void UpdateAmbush()
        {
            if (playerTransform == null) return;

            float distToPlayer = Vector3.Distance(transform.position, playerTransform.position);

            if (distToPlayer < ambushRevealDistance)
            {
                // SURPRISE!
                isRevealed = true;
                SetAmbushAppearance(false);

                // Play reveal animation/effect
                TriggerAnimation(AnimTriggerReveal);
                TriggerAnimation(AnimTriggerRoar);  // Roar after reveal
                StartCoroutine(AmbushRevealEffect());

                // Immediately attack
                currentState = AIState.Attacking;
                Attack(); // Free ambush attack
            }
        }

        private void SetAmbushAppearance(bool hidden)
        {
            if (hidden)
            {
                // Look like an innocent object
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        // Darken and make stationary
                        r.material.color = new Color(0.3f, 0.25f, 0.2f); // Brown/rock color
                    }
                }
            }
            else
            {
                // Reveal true form
                foreach (var r in renderers)
                {
                    if (r != null)
                    {
                        r.material.color = originalColor;
                    }
                }
            }
        }

        private IEnumerator AmbushRevealEffect()
        {
            TurretDefensePlugin.Log.LogWarning($"MIMIC AMBUSH!");

            // Dramatic reveal
            var revealObj = new GameObject("MimicReveal");
            revealObj.transform.position = transform.position;

            var particles = revealObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 8f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.5f, 0.3f, 0.1f); // Dirt/debris

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 30) });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var renderer = revealObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            UnityEngine.Object.Destroy(revealObj, 2f);

            yield return null;
        }

        private void MoveInDirection(Vector3 direction, float speed = 0)
        {
            if (speed <= 0) speed = MoveSpeed;

            Vector3 movement = direction * speed * Time.deltaTime;
            Velocity = direction * speed;

            // Stay on ground
            transform.position += movement;

            // Snap to terrain (simple version)
            RaycastHit hit;
            if (Physics.Raycast(transform.position + Vector3.up * 5f, Vector3.down, out hit, 10f))
            {
                Vector3 pos = transform.position;
                pos.y = hit.point.y;
                transform.position = pos;
            }

            // Animation speed
            if (animator != null)
            {
                animator.speed = speed / MoveSpeed;
            }
        }

        private void Attack()
        {
            lastAttackTime = Time.time;

            // Trigger attack animation with proper hash
            TriggerAnimation(AnimTriggerAttack);

            switch (EnemyType)
            {
                case GroundEnemyType.Chomper:
                    StartCoroutine(MeleeAttackEffect());
                    break;
                case GroundEnemyType.Spitter:
                    StartCoroutine(SpitAttackEffect());
                    break;
                case GroundEnemyType.Mimic:
                    StartCoroutine(MeleeAttackEffect());
                    break;
                case GroundEnemyType.Arachnid:
                    StartCoroutine(MeleeAttackEffect());
                    break;
                case GroundEnemyType.Grenadier:
                    StartCoroutine(GrenadeAttackEffect());
                    break;
                case GroundEnemyType.Gunner:
                    StartCoroutine(GunnerAttackEffect());
                    break;
                case GroundEnemyType.MachineGunRobot:
                    StartCoroutine(MachineGunRobotAttackEffect());
                    break;
                case GroundEnemyType.CannonMachine:
                    StartCoroutine(CannonMachineAttackEffect());
                    break;
            }

            TurretDefensePlugin.DamagePlayer(AttackDamage, $"{EnemyName} attack");
        }

        private IEnumerator MeleeAttackEffect()
        {
            // Simple melee visual
            var attackObj = new GameObject("MeleeAttack");
            attackObj.transform.position = transform.position + transform.forward * 1.5f;

            var light = attackObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 3f;
            light.intensity = 2f;
            light.color = Color.red;

            yield return new WaitForSeconds(0.15f);
            UnityEngine.Object.Destroy(attackObj);
        }

        private IEnumerator SpitAttackEffect()
        {
            if (playerTransform == null) yield break;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "AcidSpit";
            projectile.transform.position = transform.position + transform.forward * 1f + Vector3.up * 1f;
            projectile.transform.localScale = Vector3.one * 0.3f;
            projectile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.8f, 0.2f)); // Green acid
            UnityEngine.Object.Destroy(projectile.GetComponent<Collider>());

            float elapsed = 0f;
            Vector3 start = projectile.transform.position;
            Vector3 end = playerTransform.position + Vector3.up * 1f;

            while (elapsed < 0.5f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.5f;
                // Arc trajectory
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 3f;
                projectile.transform.position = pos;
                yield return null;
            }

            // Impact effect
            var impactObj = new GameObject("AcidImpact");
            impactObj.transform.position = projectile.transform.position;

            var particles = impactObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 3f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.3f, 0.8f, 0.2f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 15) });
            emission.rateOverTime = 0;

            var renderer = impactObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            UnityEngine.Object.Destroy(impactObj, 1f);
            UnityEngine.Object.Destroy(projectile);
        }

        private IEnumerator GrenadeAttackEffect()
        {
            if (playerTransform == null) yield break;

            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "Grenade";
            projectile.transform.position = transform.position + transform.forward * 1f + Vector3.up * 2f;
            projectile.transform.localScale = Vector3.one * 0.4f;
            projectile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.3f, 0.3f));
            UnityEngine.Object.Destroy(projectile.GetComponent<Collider>());

            float elapsed = 0f;
            Vector3 start = projectile.transform.position;
            Vector3 end = playerTransform.position;

            while (elapsed < 0.8f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.8f;
                Vector3 pos = Vector3.Lerp(start, end, t);
                pos.y += Mathf.Sin(t * Mathf.PI) * 8f; // High arc
                projectile.transform.position = pos;
                yield return null;
            }

            // Explosion
            var explosionObj = new GameObject("GrenadeExplosion");
            explosionObj.transform.position = projectile.transform.position;

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.8f;
            main.startSpeed = 12f;
            main.startSize = 0.6f;
            main.startColor = new Color(1f, 0.5f, 0.1f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 40) });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            var light = explosionObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 15f;
            light.intensity = 8f;
            light.color = new Color(1f, 0.6f, 0.2f);

            UnityEngine.Object.Destroy(explosionObj, 2f);
            UnityEngine.Object.Destroy(projectile);
        }

        private IEnumerator GunnerAttackEffect()
        {
            if (playerTransform == null) yield break;

            // Rapid fire tracer
            var tracer = new GameObject("GunnerTracer");
            var line = tracer.AddComponent<LineRenderer>();
            line.startWidth = 0.05f;
            line.endWidth = 0.02f;
            line.material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.8f, 0.3f));
            line.positionCount = 2;
            line.SetPosition(0, transform.position + transform.forward * 1f + Vector3.up * 1.5f);
            line.SetPosition(1, playerTransform.position + Vector3.up * 1f);

            yield return new WaitForSeconds(0.05f);
            UnityEngine.Object.Destroy(tracer);
        }

        private IEnumerator MachineGunRobotAttackEffect()
        {
            if (playerTransform == null) yield break;

            // Rapid burst fire with multiple tracers
            for (int i = 0; i < 5; i++)
            {
                var tracer = new GameObject("MachineGunTracer");
                var line = tracer.AddComponent<LineRenderer>();
                line.startWidth = 0.08f;
                line.endWidth = 0.03f;
                line.material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.6f, 0.1f));
                line.positionCount = 2;

                // Slight spread on shots
                Vector3 spread = UnityEngine.Random.insideUnitSphere * 0.5f;
                line.SetPosition(0, transform.position + transform.forward * 1.5f + Vector3.up * 2f);
                line.SetPosition(1, playerTransform.position + Vector3.up * 1f + spread);

                // Muzzle flash
                var flash = new GameObject("MuzzleFlash");
                flash.transform.position = transform.position + transform.forward * 1.5f + Vector3.up * 2f;
                var flashLight = flash.AddComponent<Light>();
                flashLight.type = LightType.Point;
                flashLight.range = 5f;
                flashLight.intensity = 3f;
                flashLight.color = new Color(1f, 0.7f, 0.3f);

                yield return new WaitForSeconds(0.03f);
                UnityEngine.Object.Destroy(tracer);
                UnityEngine.Object.Destroy(flash);
            }
        }

        private IEnumerator CannonMachineAttackEffect()
        {
            if (playerTransform == null) yield break;

            // Charge up effect
            var chargeObj = new GameObject("CannonCharge");
            chargeObj.transform.position = transform.position + transform.forward * 2f + Vector3.up * 1.5f;

            var chargeLight = chargeObj.AddComponent<Light>();
            chargeLight.type = LightType.Point;
            chargeLight.range = 8f;
            chargeLight.color = new Color(1f, 0.2f, 0.2f);

            // Charging animation
            float chargeTime = 0.5f;
            float elapsed = 0f;
            while (elapsed < chargeTime)
            {
                elapsed += Time.deltaTime;
                chargeLight.intensity = Mathf.Lerp(1f, 10f, elapsed / chargeTime);
                yield return null;
            }

            UnityEngine.Object.Destroy(chargeObj);

            // Fire the cannon shot
            var projectile = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            projectile.name = "CannonShot";
            projectile.transform.position = transform.position + transform.forward * 2f + Vector3.up * 1.5f;
            projectile.transform.localScale = Vector3.one * 0.6f;
            projectile.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.3f, 0.1f));
            UnityEngine.Object.Destroy(projectile.GetComponent<Collider>());

            // Trail effect
            var trail = projectile.AddComponent<TrailRenderer>();
            trail.startWidth = 0.4f;
            trail.endWidth = 0.1f;
            trail.time = 0.3f;
            trail.material = TurretDefensePlugin.GetColoredMaterial(new Color(1f, 0.5f, 0.2f));

            // Projectile travel
            Vector3 start = projectile.transform.position;
            Vector3 end = playerTransform.position + Vector3.up * 1f;
            elapsed = 0f;
            float travelTime = 0.4f;

            while (elapsed < travelTime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / travelTime;
                projectile.transform.position = Vector3.Lerp(start, end, t);
                yield return null;
            }

            // Impact explosion
            var explosionObj = new GameObject("CannonExplosion");
            explosionObj.transform.position = projectile.transform.position;

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 15f;
            main.startSize = 0.8f;
            main.startColor = new Color(1f, 0.4f, 0.1f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 50) });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 1f;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            var explosionLight = explosionObj.AddComponent<Light>();
            explosionLight.type = LightType.Point;
            explosionLight.range = 20f;
            explosionLight.intensity = 10f;
            explosionLight.color = new Color(1f, 0.5f, 0.2f);

            UnityEngine.Object.Destroy(explosionObj, 2f);
            UnityEngine.Object.Destroy(projectile);
        }

        public void TakeDamage(float damage, bool isCritical)
        {
            float effectiveDamage = Mathf.Max(1, damage - Armor);
            Health -= effectiveDamage;

            TurretDefensePlugin.LogDebug($"{EnemyName} took {effectiveDamage:F0} damage, HP: {Health:F0}/{MaxHealth}");

            // Trigger hurt animation and visual feedback
            TriggerAnimation(AnimTriggerHurt);
            TrySetAnimatorBool(AnimIsHurt, true);
            StartCoroutine(ResetHurtState());
            StartCoroutine(DamageFlash());

            // Mimic reveals on damage
            if (EnemyType == GroundEnemyType.Mimic && !isRevealed)
            {
                isRevealed = true;
                SetAmbushAppearance(false);
                currentState = AIState.Pursuing;
            }

            // Low health flee chance for some types
            if (Health < MaxHealth * 0.25f &&
                (EnemyType == GroundEnemyType.Spitter || EnemyType == GroundEnemyType.Grenadier))
            {
                if (UnityEngine.Random.value < 0.4f && currentState != AIState.Fleeing)
                {
                    currentState = AIState.Fleeing;
                    stateTimer = 3f;
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
                    r.material.color = isRevealed ? originalColor : new Color(0.3f, 0.25f, 0.2f);
            }
        }

        private void Die()
        {
            currentState = AIState.Dying;

            TurretDefensePlugin.Log.LogInfo($"{EnemyName} destroyed!");
            TurretDefensePlugin.OnGroundEnemyKilled(this);

            if (TurretDefensePlugin.EnableLootDrops.Value)
            {
                DropLoot();
            }

            StartCoroutine(DeathSequence());
        }

        private void DropLoot()
        {
            if (LootTables.TryGetValue(EnemyType, out var drops))
            {
                foreach (var drop in drops)
                {
                    if (UnityEngine.Random.value < drop.DropChance)
                    {
                        int amount = UnityEngine.Random.Range(drop.MinAmount, drop.MaxAmount + 1);
                        TurretDefensePlugin.Log.LogInfo($"LOOT: {amount}x {drop.ItemName}");
                    }
                }
            }
        }

        private IEnumerator DeathSequence()
        {
            Velocity = Vector3.zero;

            // Trigger death animation with new system
            TrySetAnimatorBool(AnimIsDying, true);
            TriggerAnimation(AnimTriggerDeath);

            // Death particles
            var deathObj = new GameObject("GroundEnemyDeath");
            deathObj.transform.position = transform.position;

            var particles = deathObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 1f;
            main.startSpeed = 5f;
            main.startSize = 0.4f;
            main.startColor = EnemyType == GroundEnemyType.Spitter ?
                new Color(0.3f, 0.8f, 0.2f) : new Color(0.5f, 0.1f, 0.1f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] { new ParticleSystem.Burst(0f, 20) });
            emission.rateOverTime = 0;

            var renderer = deathObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            UnityEngine.Object.Destroy(deathObj, 2f);

            yield return new WaitForSeconds(0.5f);

            UnityEngine.Object.Destroy(gameObject);
        }
    }
}
