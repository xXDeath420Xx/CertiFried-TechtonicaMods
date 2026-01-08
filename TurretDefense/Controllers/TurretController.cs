using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Advanced turret controller with animations, targeting, and weapon effects
    /// </summary>
    public class TurretController : MonoBehaviour
    {
        // ========== TURRET TYPE ==========
        public string TurretType { get; private set; } = "Gatling";
        public int UpgradeLevel { get; private set; } = 0;

        // ========== STATS ==========
        public float Damage { get; private set; } = 25f;
        public float Range { get; private set; } = 35f;
        public float FireRate { get; private set; } = 4f; // Shots per second
        public float RotationSpeed { get; private set; } = 8f;
        public float CritChance { get; private set; } = 0.1f;
        public float CritMultiplier { get; private set; } = 2f;
        public int ChainCount { get; private set; } = 0; // For lightning turret

        // ========== STATE ==========
        private Transform target;
        private float lastFireTime;
        private float chargeProgress; // For railgun
        private float overheat; // For gatling
        private bool isCharging;
        private bool isFiring;

        // ========== REFERENCES ==========
        private Transform turretBody;
        private Transform barrelRoot;
        private Transform muzzlePoint;
        private List<Transform> barrels = new List<Transform>();

        // ========== ANIMATION ==========
        private float idleScanAngle = 0f;
        private float barrelSpinSpeed = 0f;
        private float recoilOffset = 0f;
        private bool isScanning = true;

        // ========== EFFECTS ==========
        private LineRenderer laserBeam;
        private LineRenderer[] lightningArcs;
        private ParticleSystem muzzleFlash;
        private ParticleSystem chargeEffect;
        private Light muzzleLight;

        public void Initialize(string turretType)
        {
            TurretType = turretType;
            SetupStats();
            SetupReferences();
            SetupEffects();
        }

        private void SetupStats()
        {
            float baseDamage = TurretDefensePlugin.TurretDamage.Value;
            float baseRange = TurretDefensePlugin.TurretRange.Value;

            switch (TurretType)
            {
                case "Gatling":
                    Damage = baseDamage * 0.6f;
                    Range = baseRange * 0.8f;
                    FireRate = 12f;
                    RotationSpeed = 10f;
                    break;
                case "Rocket":
                    Damage = baseDamage * 4f;
                    Range = baseRange * 1.2f;
                    FireRate = 0.5f;
                    RotationSpeed = 5f;
                    CritChance = 0.15f;
                    break;
                case "Laser":
                    Damage = baseDamage * 0.3f; // DPS style
                    Range = baseRange * 1f;
                    FireRate = 20f; // Continuous
                    RotationSpeed = 12f;
                    break;
                case "Railgun":
                    Damage = baseDamage * 8f;
                    Range = baseRange * 2f;
                    FireRate = 0.25f;
                    RotationSpeed = 4f;
                    CritChance = 0.25f;
                    CritMultiplier = 3f;
                    break;
                case "Lightning":
                    Damage = baseDamage * 1.5f;
                    Range = baseRange * 0.9f;
                    FireRate = 1f;
                    RotationSpeed = 8f;
                    ChainCount = 3;
                    break;

                // ========== TDTK Turrets ==========
                case "cannon":
                case "Cannon":
                    Damage = baseDamage * 3f;
                    Range = baseRange * 1.1f;
                    FireRate = 0.8f;
                    RotationSpeed = 6f;
                    break;
                case "cannon2":
                case "Cannon2":
                    Damage = baseDamage * 4f;
                    Range = baseRange * 1.3f;
                    FireRate = 0.6f;
                    RotationSpeed = 5f;
                    CritChance = 0.12f;
                    break;
                case "mg":
                case "MG":
                    Damage = baseDamage * 0.5f;
                    Range = baseRange * 0.85f;
                    FireRate = 15f;
                    RotationSpeed = 12f;
                    break;
                case "mg2":
                case "MG2":
                    Damage = baseDamage * 0.6f;
                    Range = baseRange * 0.95f;
                    FireRate = 18f;
                    RotationSpeed = 14f;
                    break;
                case "missile":
                case "Missile":
                    Damage = baseDamage * 5f;
                    Range = baseRange * 1.4f;
                    FireRate = 0.4f;
                    RotationSpeed = 4f;
                    CritChance = 0.18f;
                    break;
                case "missile2":
                case "Missile2":
                    Damage = baseDamage * 6f;
                    Range = baseRange * 1.6f;
                    FireRate = 0.35f;
                    RotationSpeed = 3.5f;
                    CritChance = 0.2f;
                    break;
                case "beamlaser":
                case "BeamLaser":
                    Damage = baseDamage * 0.4f;
                    Range = baseRange * 1.2f;
                    FireRate = 20f; // Continuous beam
                    RotationSpeed = 10f;
                    break;
                case "beamlaser2":
                case "BeamLaser2":
                    Damage = baseDamage * 0.5f;
                    Range = baseRange * 1.4f;
                    FireRate = 22f;
                    RotationSpeed = 11f;
                    break;
                case "aoe":
                case "AOE":
                    Damage = baseDamage * 2f;
                    Range = baseRange * 0.7f;
                    FireRate = 0.5f;
                    RotationSpeed = 8f;
                    break;
                case "aoe2":
                case "AOE2":
                    Damage = baseDamage * 2.5f;
                    Range = baseRange * 0.85f;
                    FireRate = 0.45f;
                    RotationSpeed = 8f;
                    break;
                case "support":
                case "Support":
                    Damage = 0f; // Support doesn't deal damage, buffs allies
                    Range = baseRange * 1.5f;
                    FireRate = 0f;
                    RotationSpeed = 6f;
                    break;
                case "support2":
                case "Support2":
                    Damage = 0f;
                    Range = baseRange * 2f;
                    FireRate = 0f;
                    RotationSpeed = 6f;
                    break;

                // ========== Special Turrets ==========
                case "nuke":
                case "Nuke":
                    Damage = baseDamage * 25f;
                    Range = baseRange * 2.5f;
                    FireRate = 0.1f;
                    RotationSpeed = 2f;
                    CritChance = 0.3f;
                    CritMultiplier = 4f;
                    break;
                case "flamethrower":
                case "Flamethrower":
                    Damage = baseDamage * 0.8f;
                    Range = baseRange * 0.5f;
                    FireRate = 25f; // Continuous fire damage
                    RotationSpeed = 15f;
                    break;

                // Default fallback
                default:
                    Damage = baseDamage;
                    Range = baseRange;
                    FireRate = 2f;
                    RotationSpeed = 8f;
                    break;
            }
        }

        private void SetupReferences()
        {
            // Find turret parts
            turretBody = transform.Find("TurretBody") ?? FindChildContaining("Body") ?? FindChildContaining("Head");
            if (turretBody == null)
            {
                // Search in children
                foreach (Transform child in transform)
                {
                    if (child.GetComponent<Collider>() != null)
                    {
                        turretBody = child;
                        break;
                    }
                }
            }
            if (turretBody == null) turretBody = transform;

            barrelRoot = turretBody.Find("BarrelRoot") ?? turretBody.Find("Barrel") ?? turretBody;
            muzzlePoint = FindChildRecursive(transform, "MuzzlePoint") ?? FindChildRecursive(transform, "Muzzle");

            if (muzzlePoint == null)
            {
                // Create muzzle point
                var muzzle = new GameObject("MuzzlePoint");
                muzzle.transform.SetParent(barrelRoot);
                muzzle.transform.localPosition = Vector3.forward * 1.5f;
                muzzlePoint = muzzle.transform;
            }

            // Find barrels
            foreach (Transform child in barrelRoot)
            {
                if (child.name.Contains("Barrel"))
                    barrels.Add(child);
            }
        }

        private void SetupEffects()
        {
            // Setup laser beam for laser/railgun
            if (TurretType == "Laser" || TurretType == "Railgun")
            {
                var laserObj = new GameObject("LaserBeam");
                laserObj.transform.SetParent(muzzlePoint);
                laserObj.transform.localPosition = Vector3.zero;

                laserBeam = laserObj.AddComponent<LineRenderer>();
                laserBeam.startWidth = TurretType == "Railgun" ? 0.3f : 0.1f;
                laserBeam.endWidth = TurretType == "Railgun" ? 0.2f : 0.05f;
                laserBeam.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
                laserBeam.positionCount = 2;

                if (TurretType == "Laser")
                {
                    laserBeam.startColor = new Color(1f, 0.2f, 0.2f, 0.9f);
                    laserBeam.endColor = new Color(1f, 0.4f, 0.1f, 0.6f);
                }
                else // Railgun
                {
                    laserBeam.startColor = new Color(0.3f, 0.5f, 1f, 1f);
                    laserBeam.endColor = new Color(0.6f, 0.8f, 1f, 0.8f);
                }
                laserBeam.enabled = false;
            }

            // Setup lightning arcs
            if (TurretType == "Lightning")
            {
                lightningArcs = new LineRenderer[ChainCount + 1];
                for (int i = 0; i <= ChainCount; i++)
                {
                    var arcObj = new GameObject($"LightningArc_{i}");
                    arcObj.transform.SetParent(transform);

                    var arc = arcObj.AddComponent<LineRenderer>();
                    arc.startWidth = 0.15f;
                    arc.endWidth = 0.08f;
                    arc.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
                    arc.startColor = new Color(0.5f, 0.7f, 1f, 1f);
                    arc.endColor = new Color(0.8f, 0.9f, 1f, 0.6f);
                    arc.positionCount = 8; // Jagged line
                    arc.enabled = false;
                    lightningArcs[i] = arc;
                }
            }

            // Setup muzzle flash
            var flashObj = new GameObject("MuzzleFlash");
            flashObj.transform.SetParent(muzzlePoint);
            flashObj.transform.localPosition = Vector3.zero;

            muzzleFlash = flashObj.AddComponent<ParticleSystem>();
            var main = muzzleFlash.main;
            main.startLifetime = 0.1f;
            main.startSpeed = 5f;
            main.startSize = 0.3f;
            main.maxParticles = 20;
            main.playOnAwake = false;

            var emission = muzzleFlash.emission;
            emission.rateOverTime = 0;

            var shape = muzzleFlash.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;
            shape.radius = 0.1f;

            Color flashColor = TurretType switch
            {
                "Laser" => new Color(1f, 0.3f, 0.3f),
                "Railgun" => new Color(0.4f, 0.6f, 1f),
                "Lightning" => new Color(0.6f, 0.8f, 1f),
                _ => new Color(1f, 0.8f, 0.3f)
            };
            main.startColor = flashColor;

            var renderer = flashObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Muzzle light
            var lightObj = new GameObject("MuzzleLight");
            lightObj.transform.SetParent(muzzlePoint);
            lightObj.transform.localPosition = Vector3.zero;
            muzzleLight = lightObj.AddComponent<Light>();
            muzzleLight.type = LightType.Point;
            muzzleLight.range = 5f;
            muzzleLight.intensity = 0f;
            muzzleLight.color = flashColor;

            // Charge effect for railgun
            if (TurretType == "Railgun")
            {
                var chargeObj = new GameObject("ChargeEffect");
                chargeObj.transform.SetParent(muzzlePoint);
                chargeObj.transform.localPosition = Vector3.zero;

                chargeEffect = chargeObj.AddComponent<ParticleSystem>();
                var chargeMain = chargeEffect.main;
                chargeMain.startLifetime = 0.5f;
                chargeMain.startSpeed = -2f; // Inward
                chargeMain.startSize = 0.2f;
                chargeMain.maxParticles = 50;
                chargeMain.simulationSpace = ParticleSystemSimulationSpace.Local;
                chargeMain.startColor = new Color(0.4f, 0.6f, 1f, 0.8f);

                var chargeEmission = chargeEffect.emission;
                chargeEmission.rateOverTime = 30;

                var chargeShape = chargeEffect.shape;
                chargeShape.shapeType = ParticleSystemShapeType.Sphere;
                chargeShape.radius = 1f;

                var chargeRenderer = chargeObj.GetComponent<ParticleSystemRenderer>();
                chargeRenderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

                chargeEffect.Stop();
            }
        }

        private void Update()
        {
            // Find/validate target
            if (target == null || !IsValidTarget(target))
            {
                FindTarget();
            }

            if (target != null)
            {
                isScanning = false;
                TrackTarget();
                TryFire();
            }
            else
            {
                isScanning = true;
                IdleScan();
                StopFiring();
            }

            // Animations
            UpdateAnimations();

            // Overheat management for gatling
            if (TurretType == "Gatling")
            {
                if (!isFiring)
                    overheat = Mathf.Max(0, overheat - Time.deltaTime * 20f);
            }
        }

        private void FindTarget()
        {
            float closestDist = float.MaxValue;
            target = null;

            // Check flying aliens
            foreach (var alien in TurretDefensePlugin.ActiveAliens)
            {
                if (alien == null || !alien.IsAlive) continue;
                CheckPotentialTarget(alien.transform, ref closestDist);
            }

            // Check ground enemies
            foreach (var ground in TurretDefensePlugin.ActiveGroundEnemies)
            {
                if (ground == null || !ground.IsAlive) continue;
                CheckPotentialTarget(ground.transform, ref closestDist);
            }

            // Check hives (lower priority - only target if no mobile enemies)
            if (target == null)
            {
                foreach (var hive in TurretDefensePlugin.ActiveHives)
                {
                    if (hive == null || !hive.IsAlive) continue;
                    CheckPotentialTarget(hive.transform, ref closestDist);
                }
            }
        }

        private void CheckPotentialTarget(Transform potentialTarget, ref float closestDist)
        {
            float dist = Vector3.Distance(transform.position, potentialTarget.position);
            if (dist < Range && dist < closestDist)
            {
                // Line of sight check
                Vector3 dir = (potentialTarget.position - muzzlePoint.position).normalized;
                if (!Physics.Raycast(muzzlePoint.position, dir, dist * 0.9f, LayerMask.GetMask("Default")))
                {
                    closestDist = dist;
                    target = potentialTarget;
                }
            }
        }

        private bool IsValidTarget(Transform t)
        {
            if (t == null) return false;

            // Check if it's any valid enemy type
            var alien = t.GetComponent<AlienShipController>();
            if (alien != null && alien.IsAlive)
                return Vector3.Distance(transform.position, t.position) < Range * 1.1f;

            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null && ground.IsAlive)
                return Vector3.Distance(transform.position, t.position) < Range * 1.1f;

            var hive = t.GetComponent<AlienHiveController>();
            if (hive != null && hive.IsAlive)
                return Vector3.Distance(transform.position, t.position) < Range * 1.1f;

            return false;
        }

        /// <summary>
        /// Get velocity from any enemy type for target leading
        /// </summary>
        private Vector3 GetTargetVelocity(Transform t)
        {
            if (t == null) return Vector3.zero;

            var alien = t.GetComponent<AlienShipController>();
            if (alien != null) return alien.Velocity;

            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null) return ground.Velocity;

            // Hives don't move
            return Vector3.zero;
        }

        /// <summary>
        /// Deal damage to any enemy type
        /// </summary>
        private void DealDamageToTarget(Transform t, float damage, bool isCritical)
        {
            if (t == null) return;

            var alien = t.GetComponent<AlienShipController>();
            if (alien != null)
            {
                alien.TakeDamage(damage, isCritical);
                TurretDefensePlugin.SpawnDamageNumber(t.position, damage, isCritical);
                return;
            }

            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null)
            {
                ground.TakeDamage(damage, isCritical);
                TurretDefensePlugin.SpawnDamageNumber(t.position, damage, isCritical);
                return;
            }

            var hive = t.GetComponent<AlienHiveController>();
            if (hive != null)
            {
                hive.TakeDamage(damage, isCritical);
                TurretDefensePlugin.SpawnDamageNumber(t.position, damage, isCritical);
                return;
            }
        }

        private void TrackTarget()
        {
            if (turretBody == null || target == null) return;

            Vector3 targetPos = target.position;

            // Lead the target based on projectile travel time (for rocket/railgun)
            if (TurretType == "Rocket" || TurretType == "Railgun")
            {
                Vector3 velocity = GetTargetVelocity(target);
                if (velocity != Vector3.zero)
                {
                    float distance = Vector3.Distance(muzzlePoint.position, targetPos);
                    float projectileSpeed = TurretType == "Rocket" ? 30f : 100f;
                    float travelTime = distance / projectileSpeed;
                    targetPos += velocity * travelTime;
                }
            }

            // Calculate rotation
            Vector3 direction = (targetPos - turretBody.position).normalized;
            direction.y *= 0.5f; // Reduce vertical tracking speed

            Quaternion targetRotation = Quaternion.LookRotation(direction);
            turretBody.rotation = Quaternion.Slerp(turretBody.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        private void IdleScan()
        {
            if (turretBody == null) return;

            idleScanAngle += Time.deltaTime * 30f;
            float scanOffset = Mathf.Sin(idleScanAngle * Mathf.Deg2Rad) * 45f;

            Quaternion scanRotation = Quaternion.Euler(0, transform.eulerAngles.y + scanOffset, 0);
            turretBody.rotation = Quaternion.Slerp(turretBody.rotation, scanRotation, 2f * Time.deltaTime);
        }

        private void TryFire()
        {
            float timeSinceFire = Time.time - lastFireTime;
            float fireInterval = 1f / FireRate;

            // Railgun charging
            if (TurretType == "Railgun")
            {
                if (!isCharging && timeSinceFire >= fireInterval)
                {
                    StartCharge();
                }
                else if (isCharging)
                {
                    chargeProgress += Time.deltaTime;
                    if (chargeProgress >= 1.5f) // 1.5 second charge
                    {
                        Fire();
                        isCharging = false;
                        chargeProgress = 0;
                        chargeEffect?.Stop();
                    }
                }
                return;
            }

            // Gatling overheat check
            if (TurretType == "Gatling" && overheat >= 100f)
            {
                StopFiring();
                return;
            }

            // Standard firing
            if (timeSinceFire >= fireInterval)
            {
                Fire();
            }

            // Continuous laser
            if (TurretType == "Laser" && isFiring)
            {
                UpdateLaserBeam();
                ApplyContinuousDamage();
            }
        }

        private void StartCharge()
        {
            isCharging = true;
            chargeProgress = 0;
            chargeEffect?.Play();
            TurretDefensePlugin.LogDebug($"{TurretType} turret charging...");
        }

        private void Fire()
        {
            lastFireTime = Time.time;
            isFiring = true;

            // Calculate damage
            float finalDamage = Damage;
            bool isCritical = UnityEngine.Random.value < CritChance;
            if (isCritical)
            {
                finalDamage *= CritMultiplier;
            }

            // Apply upgrade bonus
            finalDamage *= (1f + UpgradeLevel * 0.2f);

            switch (TurretType)
            {
                case "Gatling":
                    FireGatling(finalDamage, isCritical);
                    break;
                case "Rocket":
                    FireRocket(finalDamage, isCritical);
                    break;
                case "Laser":
                    StartLaser();
                    break;
                case "Railgun":
                    FireRailgun(finalDamage, isCritical);
                    break;
                case "Lightning":
                    FireLightning(finalDamage, isCritical);
                    break;
            }

            // Effects
            if (TurretType != "Laser")
            {
                muzzleFlash?.Emit(5);
                StartCoroutine(MuzzleLightFlash());
            }
        }

        private void FireGatling(float damage, bool isCritical)
        {
            if (target == null) return;

            DealDamageToTarget(target, damage, isCritical);

            overheat += 2f;
            barrelSpinSpeed = 1000f;

            // Tracer effect
            StartCoroutine(TracerEffect(muzzlePoint.position, target.position));
        }

        private void FireRocket(float damage, bool isCritical)
        {
            if (target == null) return;

            // Spawn rocket projectile
            var rocket = new GameObject("Rocket");
            rocket.transform.position = muzzlePoint.position;
            rocket.transform.rotation = muzzlePoint.rotation;

            var proj = rocket.AddComponent<RocketProjectile>();
            proj.Initialize(target, damage, isCritical, 30f);

            // Recoil
            recoilOffset = -0.3f;
        }

        private void StartLaser()
        {
            if (laserBeam != null)
            {
                laserBeam.enabled = true;
                muzzleLight.intensity = 2f;
            }
        }

        private void UpdateLaserBeam()
        {
            if (laserBeam == null || target == null) return;

            laserBeam.SetPosition(0, muzzlePoint.position);
            laserBeam.SetPosition(1, target.position);

            // Beam wobble
            float wobble = Mathf.Sin(Time.time * 50f) * 0.02f;
            laserBeam.startWidth = 0.1f + wobble;
        }

        private void ApplyContinuousDamage()
        {
            if (target == null) return;

            float dps = Damage * (1f + UpgradeLevel * 0.2f);
            DealDamageToTarget(target, dps * Time.deltaTime, false);
        }

        private void FireRailgun(float damage, bool isCritical)
        {
            if (target == null) return;

            DealDamageToTarget(target, damage, isCritical);

            // Beam effect
            StartCoroutine(RailgunBeamEffect(target.position));

            // Heavy recoil
            recoilOffset = -0.5f;
        }

        private void FireLightning(float damage, bool isCritical)
        {
            if (target == null) return;

            // Track all hit targets by transform
            HashSet<Transform> hitTargets = new HashSet<Transform>();

            // Primary target damage
            DealDamageToTarget(target, damage, isCritical);
            TurretDefensePlugin.SpawnDamageNumber(target.position, damage, isCritical);
            hitTargets.Add(target);

            // Draw primary arc
            DrawLightningArc(0, muzzlePoint.position, target.position);

            // Chain to nearby targets (any enemy type)
            Vector3 lastPos = target.position;
            float chainDamage = damage * 0.7f;
            float chainRange = Range * 0.5f;

            for (int i = 0; i < ChainCount && hitTargets.Count < ChainCount + 1; i++)
            {
                Transform nearestTarget = null;
                float nearestDist = chainRange;

                // Check flying aliens
                foreach (var alien in TurretDefensePlugin.ActiveAliens)
                {
                    if (alien == null || !alien.IsAlive || hitTargets.Contains(alien.transform)) continue;
                    float dist = Vector3.Distance(lastPos, alien.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestTarget = alien.transform;
                    }
                }

                // Check ground enemies
                foreach (var ground in TurretDefensePlugin.ActiveGroundEnemies)
                {
                    if (ground == null || !ground.IsAlive || hitTargets.Contains(ground.transform)) continue;
                    float dist = Vector3.Distance(lastPos, ground.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestTarget = ground.transform;
                    }
                }

                // Check hives
                foreach (var hive in TurretDefensePlugin.ActiveHives)
                {
                    if (hive == null || !hive.IsAlive || hitTargets.Contains(hive.transform)) continue;
                    float dist = Vector3.Distance(lastPos, hive.transform.position);
                    if (dist < nearestDist)
                    {
                        nearestDist = dist;
                        nearestTarget = hive.transform;
                    }
                }

                if (nearestTarget != null)
                {
                    DealDamageToTarget(nearestTarget, chainDamage, false);
                    TurretDefensePlugin.SpawnDamageNumber(nearestTarget.position, chainDamage, false);
                    DrawLightningArc(i + 1, lastPos, nearestTarget.position);
                    lastPos = nearestTarget.position;
                    hitTargets.Add(nearestTarget);
                    chainDamage *= 0.7f;
                }
                else
                {
                    break; // No more targets in range
                }
            }

            StartCoroutine(DisableLightningArcs(0.15f));
        }

        private void DrawLightningArc(int index, Vector3 start, Vector3 end)
        {
            if (lightningArcs == null || index >= lightningArcs.Length) return;

            var arc = lightningArcs[index];
            arc.enabled = true;

            Vector3[] points = new Vector3[8];
            for (int i = 0; i < 8; i++)
            {
                float t = i / 7f;
                Vector3 basePos = Vector3.Lerp(start, end, t);

                // Add jagged offset (except endpoints)
                if (i > 0 && i < 7)
                {
                    basePos += UnityEngine.Random.insideUnitSphere * 0.5f;
                }
                points[i] = basePos;
            }
            arc.SetPositions(points);
        }

        private void StopFiring()
        {
            isFiring = false;

            if (laserBeam != null)
                laserBeam.enabled = false;

            if (muzzleLight != null)
                muzzleLight.intensity = 0f;
        }

        private void UpdateAnimations()
        {
            // Barrel spin for gatling
            if (TurretType == "Gatling" && barrelRoot != null)
            {
                barrelRoot.Rotate(Vector3.forward, barrelSpinSpeed * Time.deltaTime);
                barrelSpinSpeed = Mathf.Lerp(barrelSpinSpeed, isFiring ? 1000f : 0f, Time.deltaTime * 5f);
            }

            // Recoil recovery
            if (barrelRoot != null && recoilOffset != 0)
            {
                recoilOffset = Mathf.Lerp(recoilOffset, 0f, Time.deltaTime * 10f);
                barrelRoot.localPosition = new Vector3(0, 0, recoilOffset);
            }
        }

        private IEnumerator TracerEffect(Vector3 start, Vector3 end)
        {
            var tracerObj = new GameObject("Tracer");
            var tracer = tracerObj.AddComponent<LineRenderer>();
            tracer.startWidth = 0.05f;
            tracer.endWidth = 0.02f;
            tracer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
            tracer.startColor = new Color(1f, 0.9f, 0.3f, 1f);
            tracer.endColor = new Color(1f, 0.5f, 0.1f, 0f);
            tracer.positionCount = 2;
            tracer.SetPosition(0, start);
            tracer.SetPosition(1, end);

            yield return new WaitForSeconds(0.05f);
            UnityEngine.Object.Destroy(tracerObj);
        }

        private IEnumerator MuzzleLightFlash()
        {
            muzzleLight.intensity = 3f;
            yield return new WaitForSeconds(0.05f);
            muzzleLight.intensity = 0f;
        }

        private IEnumerator RailgunBeamEffect(Vector3 targetPos)
        {
            if (laserBeam == null) yield break;

            laserBeam.enabled = true;
            laserBeam.SetPosition(0, muzzlePoint.position);
            laserBeam.SetPosition(1, targetPos);

            // Bright flash
            laserBeam.startWidth = 0.5f;
            muzzleLight.intensity = 5f;

            yield return new WaitForSeconds(0.1f);

            // Fade out
            float elapsed = 0f;
            while (elapsed < 0.2f)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / 0.2f;
                laserBeam.startWidth = Mathf.Lerp(0.5f, 0f, t);
                muzzleLight.intensity = Mathf.Lerp(5f, 0f, t);
                yield return null;
            }

            laserBeam.enabled = false;
            muzzleLight.intensity = 0f;
        }

        private IEnumerator DisableLightningArcs(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (lightningArcs != null)
            {
                foreach (var arc in lightningArcs)
                {
                    if (arc != null) arc.enabled = false;
                }
            }
        }

        public void Upgrade()
        {
            UpgradeLevel++;
            Range *= 1.1f;
            FireRate *= 1.15f;
            TurretDefensePlugin.Log.LogInfo($"{TurretType} turret upgraded to level {UpgradeLevel}!");
        }

        private Transform FindChildContaining(string namePart)
        {
            foreach (Transform child in transform)
            {
                if (child.name.ToLower().Contains(namePart.ToLower()))
                    return child;
            }
            return null;
        }

        private Transform FindChildRecursive(Transform parent, string name)
        {
            foreach (Transform child in parent)
            {
                if (child.name.ToLower().Contains(name.ToLower()))
                    return child;

                var found = FindChildRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        void OnDrawGizmosSelected()
        {
            // Draw range
            Gizmos.color = new Color(0, 1, 0, 0.2f);
            Gizmos.DrawWireSphere(transform.position, Range);

            // Draw target line
            if (target != null)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(transform.position, target.position);
            }
        }
    }

    /// <summary>
    /// Rocket projectile with tracking
    /// </summary>
    public class RocketProjectile : MonoBehaviour
    {
        private Transform target;
        private float damage;
        private bool isCritical;
        private float speed;
        private float lifeTime = 5f;
        private float elapsed = 0f;

        private TrailRenderer trail;
        private ParticleSystem smoke;

        public void Initialize(Transform target, float damage, bool isCritical, float speed)
        {
            this.target = target;
            this.damage = damage;
            this.isCritical = isCritical;
            this.speed = speed;

            // Create visual
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(transform);
            body.transform.localScale = new Vector3(0.1f, 0.3f, 0.1f);
            body.transform.localRotation = Quaternion.Euler(90, 0, 0);
            body.GetComponent<Renderer>().material = TurretDefensePlugin.GetColoredMaterial(new Color(0.3f, 0.3f, 0.3f));
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Trail
            trail = gameObject.AddComponent<TrailRenderer>();
            trail.startWidth = 0.15f;
            trail.endWidth = 0f;
            trail.time = 0.3f;
            trail.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
            trail.startColor = new Color(1f, 0.5f, 0.1f, 1f);
            trail.endColor = new Color(1f, 0.2f, 0.1f, 0f);

            // Smoke particles
            var smokeObj = new GameObject("Smoke");
            smokeObj.transform.SetParent(transform);
            smokeObj.transform.localPosition = Vector3.back * 0.2f;
            smoke = smokeObj.AddComponent<ParticleSystem>();

            var main = smoke.main;
            main.startLifetime = 0.5f;
            main.startSpeed = -1f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);

            var emission = smoke.emission;
            emission.rateOverTime = 30;

            var renderer = smokeObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);
        }

        void Update()
        {
            elapsed += Time.deltaTime;
            if (elapsed > lifeTime)
            {
                Destroy(gameObject);
                return;
            }

            // Track target
            if (target != null)
            {
                Vector3 dir = (target.position - transform.position).normalized;
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(dir), 5f * Time.deltaTime);
            }

            transform.position += transform.forward * speed * Time.deltaTime;

            // Check for hit
            if (target != null)
            {
                float dist = Vector3.Distance(transform.position, target.position);
                if (dist < 1f)
                {
                    Explode();
                }
            }
        }

        private void Explode()
        {
            // Damage primary target (any type)
            if (target != null)
            {
                DealDamageToTarget(target, damage, isCritical);
                TurretDefensePlugin.SpawnDamageNumber(target.position, damage, isCritical);
            }

            // AOE damage to all nearby enemies
            float aoeRadius = 3f;
            float aoeDamage = damage * 0.5f;
            HashSet<Transform> hitTargets = new HashSet<Transform>();
            if (target != null) hitTargets.Add(target);

            // Flying aliens
            foreach (var alien in TurretDefensePlugin.ActiveAliens)
            {
                if (alien == null || !alien.IsAlive || hitTargets.Contains(alien.transform)) continue;
                float dist = Vector3.Distance(transform.position, alien.transform.position);
                if (dist < aoeRadius)
                {
                    float falloff = 1f - (dist / aoeRadius);
                    alien.TakeDamage(aoeDamage * falloff, false);
                }
            }

            // Ground enemies
            foreach (var ground in TurretDefensePlugin.ActiveGroundEnemies)
            {
                if (ground == null || !ground.IsAlive || hitTargets.Contains(ground.transform)) continue;
                float dist = Vector3.Distance(transform.position, ground.transform.position);
                if (dist < aoeRadius)
                {
                    float falloff = 1f - (dist / aoeRadius);
                    ground.TakeDamage(aoeDamage * falloff, false);
                }
            }

            // Hives
            foreach (var hive in TurretDefensePlugin.ActiveHives)
            {
                if (hive == null || !hive.IsAlive || hitTargets.Contains(hive.transform)) continue;
                float dist = Vector3.Distance(transform.position, hive.transform.position);
                if (dist < aoeRadius)
                {
                    float falloff = 1f - (dist / aoeRadius);
                    hive.TakeDamage(aoeDamage * falloff, false);
                }
            }

            // Explosion effect
            SpawnExplosion(transform.position);

            Destroy(gameObject);
        }

        private void DealDamageToTarget(Transform t, float dmg, bool crit)
        {
            var alien = t.GetComponent<AlienShipController>();
            if (alien != null) { alien.TakeDamage(dmg, crit); return; }
            var ground = t.GetComponent<GroundEnemyController>();
            if (ground != null) { ground.TakeDamage(dmg, crit); return; }
            var hive = t.GetComponent<AlienHiveController>();
            if (hive != null) { hive.TakeDamage(dmg, crit); return; }
        }

        private void SpawnExplosion(Vector3 position)
        {
            var explosionObj = new GameObject("Explosion");
            explosionObj.transform.position = position;

            var particles = explosionObj.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.startLifetime = 0.5f;
            main.startSpeed = 8f;
            main.startSize = 0.5f;
            main.startColor = new Color(1f, 0.5f, 0.1f);

            var emission = particles.emission;
            emission.SetBursts(new ParticleSystem.Burst[] {
                new ParticleSystem.Burst(0f, 30)
            });
            emission.rateOverTime = 0;

            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.5f;

            var renderer = explosionObj.GetComponent<ParticleSystemRenderer>();
            renderer.material = TurretDefensePlugin.GetEffectMaterial(Color.white);

            // Light flash
            var light = explosionObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.range = 10f;
            light.intensity = 5f;
            light.color = new Color(1f, 0.6f, 0.2f);

            Destroy(explosionObj, 1f);
        }
    }
}
