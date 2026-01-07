using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace TurretDefense
{
    /// <summary>
    /// Artillery turret controller - long range, high damage, slow fire rate
    /// Requires ammunition (integration with DroneLogistics for resupply)
    /// </summary>
    public class ArtilleryController : MonoBehaviour
    {
        // Identity
        public string ArtilleryType { get; private set; }
        public int TurretId { get; private set; }
        private static int nextTurretId = 0;

        // Stats
        public float Damage { get; private set; }
        public float Range { get; private set; }
        public float FireRate { get; private set; }
        public float BlastRadius { get; private set; }
        public float ArmorPenetration { get; private set; }
        public int UpgradeLevel = 0;

        // Ammunition
        public int CurrentAmmo { get; private set; }
        public int MaxAmmo { get; private set; }
        public bool HasAmmo => CurrentAmmo > 0;
        public bool NeedsResupply => CurrentAmmo < MaxAmmo * 0.3f;

        // State
        private Transform target;
        private float fireCooldown;
        private bool isReloading;
        private float reloadTimer;

        // Components
        private Transform turretBase;
        private Transform barrel;
        private Transform muzzlePoint;
        private Light muzzleLight;
        private ParticleSystem muzzleFlash;

        // Audio (placeholder for future)
        private float lastFireTime;

        // Rotation
        private float rotationSpeed = 15f;
        private float elevationSpeed = 10f;
        private float minElevation = -5f;
        private float maxElevation = 60f;
        private float currentElevation;

        // Shell tracking
        private List<ArtilleryShell> activeShells = new List<ArtilleryShell>();

        public void Initialize(string type)
        {
            ArtilleryType = type;
            TurretId = nextTurretId++;

            SetStats(type);
            SetupComponents();

            // Start with full ammo
            CurrentAmmo = MaxAmmo;

            TurretDefensePlugin.Log.LogInfo($"Artillery {TurretId} ({type}) initialized");
        }

        private void SetStats(string type)
        {
            switch (type)
            {
                case "Light":
                    Damage = 150f;
                    Range = 80f;
                    FireRate = 0.5f;
                    BlastRadius = 3f;
                    ArmorPenetration = 0.5f;
                    MaxAmmo = 50;
                    rotationSpeed = 30f;
                    break;

                case "Medium":
                    Damage = 400f;
                    Range = 120f;
                    FireRate = 0.25f;
                    BlastRadius = 5f;
                    ArmorPenetration = 0.75f;
                    MaxAmmo = 30;
                    rotationSpeed = 20f;
                    break;

                case "Heavy":
                    Damage = 1000f;
                    Range = 200f;
                    FireRate = 0.1f;
                    BlastRadius = 10f;
                    ArmorPenetration = 1f;
                    MaxAmmo = 15;
                    rotationSpeed = 10f;
                    break;

                case "Auto":
                    Damage = 75f;
                    Range = 60f;
                    FireRate = 4f;
                    BlastRadius = 1f;
                    ArmorPenetration = 0.3f;
                    MaxAmmo = 200;
                    rotationSpeed = 60f;
                    break;

                default:
                    Damage = 200f;
                    Range = 100f;
                    FireRate = 0.3f;
                    BlastRadius = 4f;
                    ArmorPenetration = 0.5f;
                    MaxAmmo = 40;
                    break;
            }
        }

        private void SetupComponents()
        {
            // Find or create turret parts
            turretBase = transform.Find("Base") ?? transform;
            barrel = transform.Find("Barrel") ?? transform.Find("barrel") ?? turretBase;

            // Find muzzle point
            muzzlePoint = transform.Find("MuzzlePoint");
            if (muzzlePoint == null)
            {
                var muzzleObj = new GameObject("MuzzlePoint");
                muzzleObj.transform.SetParent(barrel);
                muzzleObj.transform.localPosition = Vector3.forward * 2f;
                muzzlePoint = muzzleObj.transform;
            }

            // Add muzzle light
            var lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(muzzlePoint);
            lightObj.transform.localPosition = Vector3.zero;

            muzzleLight = lightObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 15f;
            muzzleLight.intensity = 0f;
            muzzleLight.color = new Color(1f, 0.7f, 0.3f);

            // Add muzzle flash particles
            SetupMuzzleFlash();
        }

        private void SetupMuzzleFlash()
        {
            var flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.SetParent(muzzlePoint);
            flashObj.transform.localPosition = Vector3.zero;

            muzzleFlash = flashObj.AddComponent<ParticleSystem>();
            var main = muzzleFlash.main;
            main.startLifetime = 0.3f;
            main.startSpeed = 15f;
            main.startSize = 1f;
            main.startColor = new Color(1f, 0.8f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = muzzleFlash.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, 20)
            });

            var shape = muzzleFlash.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25f;
            shape.radius = 0.2f;

            var renderer = flashObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            muzzleFlash.Stop();
        }

        void Update()
        {
            // Update cooldown
            if (fireCooldown > 0)
                fireCooldown -= Time.deltaTime;

            // Update reload
            if (isReloading)
            {
                reloadTimer -= Time.deltaTime;
                if (reloadTimer <= 0)
                {
                    isReloading = false;
                }
            }

            // Find target
            if (target == null || !IsValidTarget(target))
            {
                FindTarget();
            }

            // Track target
            if (target != null)
            {
                TrackTarget();

                if (CanFire())
                {
                    Fire();
                }
            }

            // Update active shells
            activeShells.RemoveAll(s => s == null);

            // Update muzzle light
            if (muzzleLight != null && muzzleLight.intensity > 0)
            {
                muzzleLight.intensity = Mathf.Lerp(muzzleLight.intensity, 0, Time.deltaTime * 10f);
            }
        }

        #region Targeting

        private void FindTarget()
        {
            target = null;
            float closestDist = Range * (1f + UpgradeLevel * 0.1f);

            // Prioritize hives (high value targets)
            foreach (var hive in TurretDefensePlugin.ActiveHives)
            {
                if (hive == null || !hive.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, hive.transform.position);
                if (dist < closestDist)
                {
                    closestDist = dist;
                    target = hive.transform;
                }
            }

            // Then ground enemies (easier to hit with artillery)
            if (target == null)
            {
                foreach (var ground in TurretDefensePlugin.ActiveGroundEnemies)
                {
                    if (ground == null || !ground.IsAlive) continue;

                    float dist = Vector3.Distance(transform.position, ground.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        target = ground.transform;
                    }
                }
            }

            // Finally flying targets (harder to hit)
            if (target == null)
            {
                foreach (var alien in TurretDefensePlugin.ActiveAliens)
                {
                    if (alien == null || !alien.IsAlive) continue;

                    float dist = Vector3.Distance(transform.position, alien.transform.position);
                    if (dist < closestDist)
                    {
                        closestDist = dist;
                        target = alien.transform;
                    }
                }
            }
        }

        private bool IsValidTarget(Transform t)
        {
            if (t == null) return false;

            float dist = Vector3.Distance(transform.position, t.position);
            if (dist > Range * (1f + UpgradeLevel * 0.1f)) return false;

            // Check if target is still alive
            var alien = t.GetComponent<AlienShipController>();
            if (alien != null) return alien.IsAlive;

            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null) return ground.IsAlive;

            var hive = t.GetComponent<AlienHiveController>();
            if (hive != null) return hive.IsAlive;

            return false;
        }

        private void TrackTarget()
        {
            if (target == null) return;

            // Predict target position based on shell travel time
            Vector3 targetPos = PredictTargetPosition(target);

            // Horizontal rotation
            Vector3 dirToTarget = targetPos - turretBase.position;
            dirToTarget.y = 0;

            if (dirToTarget.sqrMagnitude > 0.1f)
            {
                Quaternion targetRot = Quaternion.LookRotation(dirToTarget);
                turretBase.rotation = Quaternion.RotateTowards(
                    turretBase.rotation,
                    targetRot,
                    rotationSpeed * Time.deltaTime
                );
            }

            // Elevation
            float distance = Vector3.Distance(transform.position, targetPos);
            float heightDiff = targetPos.y - muzzlePoint.position.y;

            // Simple ballistic calculation
            float targetElevation = Mathf.Atan2(heightDiff, distance) * Mathf.Rad2Deg;
            targetElevation += distance * 0.1f; // Add arc compensation

            targetElevation = Mathf.Clamp(targetElevation, minElevation, maxElevation);

            currentElevation = Mathf.MoveTowards(currentElevation, targetElevation, elevationSpeed * Time.deltaTime);

            if (barrel != null && barrel != turretBase)
            {
                barrel.localEulerAngles = new Vector3(-currentElevation, 0, 0);
            }
        }

        private Vector3 PredictTargetPosition(Transform t)
        {
            Vector3 targetPos = t.position;
            Vector3 velocity = Vector3.zero;

            // Get velocity if available
            var alien = t.GetComponent<AlienShipController>();
            if (alien != null) velocity = alien.Velocity;

            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null) velocity = ground.Velocity;

            // Estimate shell travel time
            float distance = Vector3.Distance(muzzlePoint.position, targetPos);
            float shellSpeed = 50f; // Approximate
            float travelTime = distance / shellSpeed;

            // Predict position
            return targetPos + velocity * travelTime;
        }

        #endregion

        #region Firing

        private bool CanFire()
        {
            if (fireCooldown > 0) return false;
            if (isReloading) return false;
            if (!HasAmmo) return false;
            if (target == null) return false;

            // Check if barrel is pointed at target
            Vector3 toTarget = target.position - muzzlePoint.position;
            float angle = Vector3.Angle(muzzlePoint.forward, toTarget);

            return angle < 10f;
        }

        private void Fire()
        {
            // Consume ammo
            CurrentAmmo--;

            // Set cooldown
            fireCooldown = 1f / FireRate;

            // Calculate damage with upgrades
            float finalDamage = Damage * (1f + UpgradeLevel * 0.15f);
            bool isCritical = Random.value < (0.1f + UpgradeLevel * 0.02f);
            if (isCritical) finalDamage *= 2f;

            // Spawn shell projectile
            SpawnShell(finalDamage, isCritical);

            // Visual effects
            if (muzzleFlash != null)
            {
                muzzleFlash.Play();
            }

            if (muzzleLight != null)
            {
                muzzleLight.intensity = 5f;
            }

            lastFireTime = Time.time;

            // Apply recoil (visual)
            StartCoroutine(RecoilAnimation());
        }

        private void SpawnShell(float damage, bool isCritical)
        {
            var shellObj = new GameObject($"ArtilleryShell_{TurretId}");
            shellObj.transform.position = muzzlePoint.position;
            shellObj.transform.rotation = muzzlePoint.rotation;

            var shell = shellObj.AddComponent<ArtilleryShell>();
            shell.Initialize(target, damage, isCritical, BlastRadius, ArmorPenetration, ArtilleryType);

            activeShells.Add(shell);
        }

        private IEnumerator RecoilAnimation()
        {
            if (barrel == null) yield break;

            Vector3 originalPos = barrel.localPosition;
            float recoilDistance = ArtilleryType == "Heavy" ? -0.5f : -0.2f;

            barrel.localPosition = originalPos + Vector3.forward * recoilDistance;

            float elapsed = 0f;
            float recoilTime = 0.3f;

            while (elapsed < recoilTime)
            {
                elapsed += Time.deltaTime;
                barrel.localPosition = Vector3.Lerp(
                    originalPos + Vector3.forward * recoilDistance,
                    originalPos,
                    elapsed / recoilTime
                );
                yield return null;
            }

            barrel.localPosition = originalPos;
        }

        #endregion

        #region Ammunition

        public int Resupply(int amount)
        {
            int needed = MaxAmmo - CurrentAmmo;
            int toAdd = Mathf.Min(amount, needed);

            CurrentAmmo += toAdd;

            TurretDefensePlugin.Log.LogInfo($"Artillery {TurretId} resupplied: +{toAdd} ammo ({CurrentAmmo}/{MaxAmmo})");

            return toAdd;
        }

        public void SetAmmo(int amount)
        {
            CurrentAmmo = Mathf.Clamp(amount, 0, MaxAmmo);
        }

        #endregion

        void OnDestroy()
        {
            // Clean up shells
            foreach (var shell in activeShells)
            {
                if (shell != null)
                {
                    Destroy(shell.gameObject);
                }
            }
        }
    }

    /// <summary>
    /// Artillery shell projectile - arcs toward target, explodes on impact
    /// </summary>
    public class ArtilleryShell : MonoBehaviour
    {
        private Transform target;
        private Vector3 targetPosition;
        private float damage;
        private bool isCritical;
        private float blastRadius;
        private float armorPen;
        private string artilleryType;

        // Flight
        private Vector3 velocity;
        private float gravity = 15f;
        private float speed = 50f;
        private bool hasLaunched;

        // Trail
        private TrailRenderer trail;

        public void Initialize(Transform target, float damage, bool isCritical, float blastRadius, float armorPen, string type)
        {
            this.target = target;
            this.targetPosition = target?.position ?? transform.position + transform.forward * 50f;
            this.damage = damage;
            this.isCritical = isCritical;
            this.blastRadius = blastRadius;
            this.armorPen = armorPen;
            this.artilleryType = type;

            // Calculate initial velocity for arc
            CalculateLaunchVelocity();

            // Add trail
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.time = 0.5f;
            trail.startWidth = 0.2f;
            trail.endWidth = 0.05f;
            trail.material = TurretDefensePlugin.GetEffectMaterial(new Color(1f, 0.6f, 0.2f, 0.5f));

            hasLaunched = true;

            // Self-destruct after max flight time
            Destroy(gameObject, 10f);
        }

        private void CalculateLaunchVelocity()
        {
            Vector3 toTarget = targetPosition - transform.position;
            float horizontalDist = new Vector3(toTarget.x, 0, toTarget.z).magnitude;
            float verticalDist = toTarget.y;

            // Simple arc calculation
            float time = horizontalDist / speed;
            float verticalSpeed = (verticalDist + 0.5f * gravity * time * time) / time;

            Vector3 horizontalDir = new Vector3(toTarget.x, 0, toTarget.z).normalized;
            velocity = horizontalDir * speed + Vector3.up * verticalSpeed;
        }

        void Update()
        {
            if (!hasLaunched) return;

            // Apply gravity
            velocity += Vector3.down * gravity * Time.deltaTime;

            // Move
            transform.position += velocity * Time.deltaTime;

            // Face velocity direction
            if (velocity.sqrMagnitude > 0.1f)
            {
                transform.rotation = Quaternion.LookRotation(velocity);
            }

            // Check for ground impact
            if (transform.position.y < 0.5f)
            {
                Explode();
            }

            // Check for target proximity
            if (target != null)
            {
                float distToTarget = Vector3.Distance(transform.position, target.position);
                if (distToTarget < 2f)
                {
                    Explode();
                }
            }
        }

        private void Explode()
        {
            // AOE damage
            DealAOEDamage();

            // Visual explosion
            SpawnExplosion();

            Destroy(gameObject);
        }

        private void DealAOEDamage()
        {
            HashSet<Transform> hitTargets = new HashSet<Transform>();

            // Flying aliens
            foreach (var alien in TurretDefensePlugin.ActiveAliens)
            {
                if (alien == null || !alien.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, alien.transform.position);
                if (dist < blastRadius)
                {
                    float falloff = 1f - (dist / blastRadius);
                    float effectiveDamage = damage * falloff;

                    // Apply armor penetration
                    effectiveDamage = effectiveDamage * (1f - alien.Armor * (1f - armorPen));

                    alien.TakeDamage(effectiveDamage, isCritical && dist < blastRadius * 0.3f);
                    TurretDefensePlugin.SpawnDamageNumber(alien.transform.position, effectiveDamage, isCritical);
                }
            }

            // Ground enemies
            foreach (var ground in TurretDefensePlugin.ActiveGroundEnemies)
            {
                if (ground == null || !ground.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, ground.transform.position);
                if (dist < blastRadius)
                {
                    float falloff = 1f - (dist / blastRadius);
                    float effectiveDamage = damage * falloff;

                    effectiveDamage = effectiveDamage * (1f - ground.Armor * (1f - armorPen));

                    ground.TakeDamage(effectiveDamage, isCritical && dist < blastRadius * 0.3f);
                    TurretDefensePlugin.SpawnDamageNumber(ground.transform.position, effectiveDamage, isCritical);
                }
            }

            // Hives
            foreach (var hive in TurretDefensePlugin.ActiveHives)
            {
                if (hive == null || !hive.IsAlive) continue;

                float dist = Vector3.Distance(transform.position, hive.transform.position);
                if (dist < blastRadius)
                {
                    float falloff = 1f - (dist / blastRadius);
                    float effectiveDamage = damage * falloff;

                    effectiveDamage = effectiveDamage * (1f - hive.Armor * (1f - armorPen));

                    hive.TakeDamage(effectiveDamage, isCritical && dist < blastRadius * 0.3f);
                    TurretDefensePlugin.SpawnDamageNumber(hive.transform.position, effectiveDamage, isCritical);
                }
            }
        }

        private void SpawnExplosion()
        {
            var explosionObj = new GameObject("ArtilleryExplosion");
            explosionObj.transform.position = transform.position;

            // Scale explosion based on artillery type
            float scale = artilleryType == "Heavy" ? 2f : (artilleryType == "Medium" ? 1.5f : 1f);

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.8f * scale;
            main.startSpeed = 12f * scale;
            main.startSize = 0.8f * scale;
            main.startColor = new Color(1f, 0.5f, 0.1f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[]
            {
                new ParticleSystem.Burst(0f, (short)(40 * scale))
            });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f * scale;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Light flash
            var light = explosionObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 15f * scale;
            light.intensity = 8f;
            light.color = new Color(1f, 0.6f, 0.2f);

            Destroy(explosionObj, 2f);
        }
    }
}
