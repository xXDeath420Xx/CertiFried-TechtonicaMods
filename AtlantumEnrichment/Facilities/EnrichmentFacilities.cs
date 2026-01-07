using UnityEngine;
using System.Collections.Generic;

namespace AtlantumEnrichment
{
    /// <summary>
    /// Centrifuge - First stage of enrichment, separates isotopes
    /// Input: Raw Atlantum
    /// Output: Concentrated Atlantum + Atlantum Waste
    /// Risk: Low base explosion risk
    /// </summary>
    public class CentrifugeController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Processing
        public float RawAtlantumStored { get; private set; }
        public float MaxRawAtlantum = 100f;
        public float ConcentratedAtlantumStored { get; private set; }
        public float MaxConcentrated = 50f;
        public float WasteStored { get; private set; }
        public float MaxWaste = 50f;

        // Conversion rates
        public float ProcessingRate = 2f; // raw per second
        public float ConcentrationYield = 0.4f; // 40% becomes concentrated
        public float WasteYield = 0.5f; // 50% becomes waste

        // Health & Risk
        public float Health { get; private set; } = 100f;
        public float MaxHealth = 100f;
        public float ExplosionRiskMultiplier = 0.5f; // Lower risk than enrichment chamber
        public int SafetyModulesInstalled { get; private set; }
        public int MaxSafetyModules = 2;

        // State
        public bool IsProcessing => RawAtlantumStored > 0 && ConcentratedAtlantumStored < MaxConcentrated;
        public float SpinSpeed { get; private set; }
        private float targetSpinSpeed;

        // Visual
        private Transform drum;
        private Light statusLight;
        private ParticleSystem processParticles;

        public void Initialize()
        {
            FacilityId = nextId++;
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            drum = transform.Find("Drum");

            // Status light
            var lightObj = new GameObject("StatusLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 3.5f;

            statusLight = lightObj.AddComponent<Light>();
            statusLight.type = LightType.Point;
            statusLight.range = 3f;
            statusLight.intensity = 1f;
            statusLight.color = Color.green;

            // Process particles
            var particleObj = new GameObject("ProcessParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.up * 1.5f;

            processParticles = particleObj.AddComponent<ParticleSystem>();
            var main = processParticles.main;
            main.startLifetime = 1f;
            main.startSpeed = 2f;
            main.startSize = 0.1f;
            main.startColor = new Color(0.2f, 0.8f, 1f, 0.5f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = processParticles.emission;
            emission.rateOverTime = 0f;

            var shape = processParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.5f;
        }

        void Update()
        {
            if (IsProcessing)
            {
                ProcessAtlantum();
                CheckExplosionRisk();
            }

            UpdateVisuals();
        }

        private void ProcessAtlantum()
        {
            float processed = Mathf.Min(RawAtlantumStored, ProcessingRate * Time.deltaTime);
            RawAtlantumStored -= processed;

            float concentrated = processed * ConcentrationYield;
            float waste = processed * WasteYield;

            ConcentratedAtlantumStored = Mathf.Min(MaxConcentrated, ConcentratedAtlantumStored + concentrated);
            WasteStored = Mathf.Min(MaxWaste, WasteStored + waste);

            AtlantumEnrichmentPlugin.TotalEnrichedAtlantum += concentrated;
            AtlantumEnrichmentPlugin.TotalWasteProduced += waste;
        }

        private void CheckExplosionRisk()
        {
            float baseRisk = AtlantumEnrichmentPlugin.BaseExplosionRisk.Value * ExplosionRiskMultiplier;
            float safetyReduction = SafetyModulesInstalled * AtlantumEnrichmentPlugin.SafetyModuleReduction.Value;
            float finalRisk = Mathf.Max(0.01f, baseRisk - safetyReduction);

            // Very small chance per frame, scales with processing
            float riskPerFrame = finalRisk * Time.deltaTime * 0.001f;

            if (Random.value < riskPerFrame)
            {
                TriggerMalfunction();
            }
        }

        private void TriggerMalfunction()
        {
            AtlantumEnrichmentPlugin.LogWarning($"Centrifuge {FacilityId} malfunction!");

            // Smaller explosion than enrichment chamber
            AtlantumEnrichmentPlugin.TriggerExplosion(
                transform.position,
                AtlantumEnrichmentPlugin.RadiationRadius.Value * 0.5f,
                50f
            );

            TakeDamage(30f);
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            AtlantumEnrichmentPlugin.LogWarning($"Centrifuge {FacilityId} took {damage} damage. Health: {Health}/{MaxHealth}");

            if (Health <= 0)
            {
                Explode();
            }
        }

        private void Explode()
        {
            AtlantumEnrichmentPlugin.LogError($"Centrifuge {FacilityId} DESTROYED!");

            // Major explosion with all stored material
            float explosionIntensity = (RawAtlantumStored + ConcentratedAtlantumStored) / 100f;
            AtlantumEnrichmentPlugin.TriggerExplosion(
                transform.position,
                AtlantumEnrichmentPlugin.RadiationRadius.Value * explosionIntensity,
                100f * explosionIntensity
            );

            Destroy(gameObject);
        }

        private void UpdateVisuals()
        {
            // Spin drum when processing
            targetSpinSpeed = IsProcessing ? 360f : 0f;
            SpinSpeed = Mathf.Lerp(SpinSpeed, targetSpinSpeed, Time.deltaTime * 2f);

            if (drum != null)
            {
                drum.Rotate(Vector3.up, SpinSpeed * Time.deltaTime);
            }

            // Status light color
            if (statusLight != null)
            {
                if (Health < MaxHealth * 0.3f)
                    statusLight.color = Color.red;
                else if (IsProcessing)
                    statusLight.color = new Color(0.2f, 0.8f, 1f); // Cyan
                else
                    statusLight.color = Color.green;
            }

            // Particles when processing
            if (processParticles != null)
            {
                var emission = processParticles.emission;
                emission.rateOverTime = IsProcessing ? 30f : 0f;
            }
        }

        public void AddRawAtlantum(float amount)
        {
            RawAtlantumStored = Mathf.Min(MaxRawAtlantum, RawAtlantumStored + amount);
        }

        public float TakeConcentrated(float amount)
        {
            float taken = Mathf.Min(amount, ConcentratedAtlantumStored);
            ConcentratedAtlantumStored -= taken;
            return taken;
        }

        public float TakeWaste(float amount)
        {
            float taken = Mathf.Min(amount, WasteStored);
            WasteStored -= taken;
            return taken;
        }

        public bool InstallSafetyModule()
        {
            if (SafetyModulesInstalled >= MaxSafetyModules)
                return false;

            SafetyModulesInstalled++;
            AtlantumEnrichmentPlugin.LogInfo($"Centrifuge {FacilityId} safety module installed ({SafetyModulesInstalled}/{MaxSafetyModules})");
            return true;
        }

        public void Repair(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        void OnDestroy()
        {
            AtlantumEnrichmentPlugin.ActiveCentrifuges.Remove(this);
        }
    }

    /// <summary>
    /// Enrichment Chamber - Main enrichment process, high risk
    /// Input: Concentrated Atlantum
    /// Output: Enriched Atlantum + More Waste
    /// Risk: HIGH explosion risk without safety modules
    /// </summary>
    public class EnrichmentChamberController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Processing
        public float ConcentratedAtlantumStored { get; private set; }
        public float MaxConcentrated = 50f;
        public float EnrichedAtlantumStored { get; private set; }
        public float MaxEnriched = 20f;
        public float WasteStored { get; private set; }
        public float MaxWaste = 100f;

        // Conversion - slower but more valuable output
        public float ProcessingRate = 0.5f; // concentrated per second
        public float EnrichmentYield = 0.2f; // 20% becomes enriched (valuable!)
        public float WasteYield = 0.7f; // 70% becomes waste

        // Health & Risk
        public float Health { get; private set; } = 100f;
        public float MaxHealth = 100f;
        public float ExplosionRiskMultiplier = 1.5f; // HIGH risk
        public int SafetyModulesInstalled { get; private set; }
        public int MaxSafetyModules = 4;

        // Containment - critical safety
        public float ContainmentIntegrity { get; private set; } = 100f;
        public float MaxContainment = 100f;
        private float containmentDecayRate = 0.1f; // per second when processing

        // State
        public bool IsProcessing => ConcentratedAtlantumStored > 0 && EnrichedAtlantumStored < MaxEnriched;
        public bool IsCritical => ContainmentIntegrity < 30f;

        // Visual
        private Light coreLight;
        private ParticleSystem coreParticles;
        private List<Transform> safetyModuleVisuals = new List<Transform>();

        public void Initialize()
        {
            FacilityId = nextId++;
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Core glow light
            var lightObj = new GameObject("CoreLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 1.5f;

            coreLight = lightObj.AddComponent<Light>();
            coreLight.type = LightType.Point;
            coreLight.range = 8f;
            coreLight.intensity = 2f;
            coreLight.color = new Color(0.1f, 0.9f, 1f);

            // Core particles
            var particleObj = new GameObject("CoreParticles");
            particleObj.transform.SetParent(transform);
            particleObj.transform.localPosition = Vector3.up * 1.5f;

            coreParticles = particleObj.AddComponent<ParticleSystem>();
            var main = coreParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 0.5f;
            main.startSize = 0.2f;
            main.startColor = new Color(0.2f, 1f, 0.8f, 0.6f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;

            var emission = coreParticles.emission;
            emission.rateOverTime = 0f;

            var shape = coreParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = 0.3f;

            // Find safety module slots
            for (int i = 0; i < 4; i++)
            {
                var slot = transform.Find($"SafetySlot_{i}");
                if (slot != null)
                    safetyModuleVisuals.Add(slot);
            }
        }

        void Update()
        {
            if (IsProcessing)
            {
                ProcessAtlantum();
                DegradeContainment();
                CheckExplosionRisk();
            }

            UpdateVisuals();
        }

        private void ProcessAtlantum()
        {
            float processed = Mathf.Min(ConcentratedAtlantumStored, ProcessingRate * Time.deltaTime);
            ConcentratedAtlantumStored -= processed;

            float enriched = processed * EnrichmentYield;
            float waste = processed * WasteYield;

            EnrichedAtlantumStored = Mathf.Min(MaxEnriched, EnrichedAtlantumStored + enriched);
            WasteStored = Mathf.Min(MaxWaste, WasteStored + waste);

            AtlantumEnrichmentPlugin.TotalEnrichedAtlantum += enriched;
            AtlantumEnrichmentPlugin.TotalWasteProduced += waste;
        }

        private void DegradeContainment()
        {
            float degradeRate = containmentDecayRate;

            // Safety modules slow degradation
            degradeRate *= (1f - SafetyModulesInstalled * 0.2f);

            ContainmentIntegrity -= degradeRate * Time.deltaTime;

            if (ContainmentIntegrity <= 0)
            {
                ContainmentBreach();
            }
        }

        private void CheckExplosionRisk()
        {
            float baseRisk = AtlantumEnrichmentPlugin.BaseExplosionRisk.Value * ExplosionRiskMultiplier;
            float safetyReduction = SafetyModulesInstalled * AtlantumEnrichmentPlugin.SafetyModuleReduction.Value;

            // Containment affects risk dramatically
            float containmentFactor = 1f + (1f - ContainmentIntegrity / MaxContainment) * 2f;

            float finalRisk = Mathf.Max(0.01f, (baseRisk - safetyReduction) * containmentFactor);

            float riskPerFrame = finalRisk * Time.deltaTime * 0.001f;

            if (Random.value < riskPerFrame)
            {
                TriggerMalfunction();
            }
        }

        private void ContainmentBreach()
        {
            AtlantumEnrichmentPlugin.LogError($"CONTAINMENT BREACH at Enrichment Chamber {FacilityId}!");

            // Create radiation zone immediately
            AtlantumEnrichmentPlugin.CreateRadiationZone(transform.position, 1.5f);

            // Severe damage
            TakeDamage(50f);

            // Reset containment (emergency protocols)
            ContainmentIntegrity = 20f;
        }

        private void TriggerMalfunction()
        {
            AtlantumEnrichmentPlugin.LogError($"Enrichment Chamber {FacilityId} CRITICAL MALFUNCTION!");

            // Major explosion
            AtlantumEnrichmentPlugin.TriggerExplosion(
                transform.position,
                AtlantumEnrichmentPlugin.RadiationRadius.Value,
                100f
            );

            TakeDamage(50f);
            ContainmentIntegrity -= 30f;
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;
            AtlantumEnrichmentPlugin.LogWarning($"Enrichment Chamber {FacilityId} took {damage} damage. Health: {Health}/{MaxHealth}");

            if (Health <= 0)
            {
                Meltdown();
            }
        }

        private void Meltdown()
        {
            AtlantumEnrichmentPlugin.LogError($"MELTDOWN at Enrichment Chamber {FacilityId}!");

            // Catastrophic explosion
            float intensity = 2f + (EnrichedAtlantumStored / MaxEnriched);
            AtlantumEnrichmentPlugin.TriggerExplosion(
                transform.position,
                AtlantumEnrichmentPlugin.RadiationRadius.Value * intensity,
                200f
            );

            // Long-lasting radiation
            AtlantumEnrichmentPlugin.CreateRadiationZone(transform.position, 2f);

            Destroy(gameObject);
        }

        private void UpdateVisuals()
        {
            // Core light intensity based on processing and containment
            if (coreLight != null)
            {
                float baseIntensity = IsProcessing ? 4f : 1f;
                float containmentFactor = 1f + (1f - ContainmentIntegrity / MaxContainment);

                coreLight.intensity = baseIntensity * containmentFactor;

                // Color shifts to red when critical
                if (IsCritical)
                    coreLight.color = Color.Lerp(new Color(0.1f, 0.9f, 1f), Color.red, 1f - ContainmentIntegrity / 30f);
                else
                    coreLight.color = new Color(0.1f, 0.9f, 1f);
            }

            // Particles intensity
            if (coreParticles != null)
            {
                var emission = coreParticles.emission;
                emission.rateOverTime = IsProcessing ? 50f : 10f;
            }

            // Safety module visuals
            for (int i = 0; i < safetyModuleVisuals.Count; i++)
            {
                var renderer = safetyModuleVisuals[i].GetComponent<Renderer>();
                if (renderer != null)
                {
                    renderer.material.color = i < SafetyModulesInstalled
                        ? new Color(0.2f, 1f, 0.3f) // Green = installed
                        : new Color(0.2f, 0.2f, 0.25f); // Dark = empty
                }
            }
        }

        public void AddConcentratedAtlantum(float amount)
        {
            ConcentratedAtlantumStored = Mathf.Min(MaxConcentrated, ConcentratedAtlantumStored + amount);
        }

        public float TakeEnriched(float amount)
        {
            float taken = Mathf.Min(amount, EnrichedAtlantumStored);
            EnrichedAtlantumStored -= taken;
            return taken;
        }

        public float TakeWaste(float amount)
        {
            float taken = Mathf.Min(amount, WasteStored);
            WasteStored -= taken;
            return taken;
        }

        public bool InstallSafetyModule()
        {
            if (SafetyModulesInstalled >= MaxSafetyModules)
                return false;

            SafetyModulesInstalled++;
            AtlantumEnrichmentPlugin.LogInfo($"Enrichment Chamber {FacilityId} safety module installed ({SafetyModulesInstalled}/{MaxSafetyModules})");
            return true;
        }

        public void RepairContainment(float amount)
        {
            ContainmentIntegrity = Mathf.Min(MaxContainment, ContainmentIntegrity + amount);
        }

        public void Repair(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        void OnDestroy()
        {
            AtlantumEnrichmentPlugin.ActiveChambers.Remove(this);
        }
    }

    /// <summary>
    /// Waste Processor - Handles radioactive waste
    /// Input: Atlantum Waste from Centrifuge/Chamber
    /// Output: Depleted Atlantum (safe) + Small amount of recoverable material
    /// Integration: Waste can go to BioProcessing for bioremediation
    /// </summary>
    public class WasteProcessorController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Input
        public float WasteStored { get; private set; }
        public float MaxWaste = 200f;

        // Output
        public float DepletedAtlantumStored { get; private set; }
        public float MaxDepleted = 100f;
        public float RecoveredMaterialStored { get; private set; }
        public float MaxRecovered = 10f;

        // Processing
        public float ProcessingRate = 1f; // waste per second
        public float DepletionYield = 0.8f; // 80% becomes depleted (safe)
        public float RecoveryYield = 0.05f; // 5% recoverable

        // Health
        public float Health { get; private set; } = 100f;
        public float MaxHealth = 100f;

        // State
        public bool IsProcessing => WasteStored > 0 && DepletedAtlantumStored < MaxDepleted;

        // Visual
        private ParticleSystem steamParticles;
        private Light processLight;

        public void Initialize()
        {
            FacilityId = nextId++;
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Steam particles
            var steamObj = new GameObject("Steam");
            steamObj.transform.SetParent(transform);
            steamObj.transform.localPosition = Vector3.up * 3f;

            steamParticles = steamObj.AddComponent<ParticleSystem>();
            var main = steamParticles.main;
            main.startLifetime = 3f;
            main.startSpeed = 1.5f;
            main.startSize = 0.4f;
            main.startColor = new Color(0.7f, 0.7f, 0.5f, 0.4f);

            var emission = steamParticles.emission;
            emission.rateOverTime = 0f;

            var shape = steamParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 15f;

            // Process light
            var lightObj = new GameObject("ProcessLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 1.5f;

            processLight = lightObj.AddComponent<Light>();
            processLight.type = LightType.Point;
            processLight.range = 4f;
            processLight.intensity = 1f;
            processLight.color = new Color(1f, 1f, 0.5f);
        }

        void Update()
        {
            if (IsProcessing)
            {
                ProcessWaste();
            }

            UpdateVisuals();
        }

        private void ProcessWaste()
        {
            float processed = Mathf.Min(WasteStored, ProcessingRate * Time.deltaTime);
            WasteStored -= processed;

            float depleted = processed * DepletionYield;
            float recovered = processed * RecoveryYield;

            DepletedAtlantumStored = Mathf.Min(MaxDepleted, DepletedAtlantumStored + depleted);
            RecoveredMaterialStored = Mathf.Min(MaxRecovered, RecoveredMaterialStored + recovered);
        }

        private void UpdateVisuals()
        {
            if (steamParticles != null)
            {
                var emission = steamParticles.emission;
                emission.rateOverTime = IsProcessing ? 15f : 0f;
            }

            if (processLight != null)
            {
                processLight.intensity = IsProcessing ? 2f : 0.5f;
            }
        }

        public void AddWaste(float amount)
        {
            WasteStored = Mathf.Min(MaxWaste, WasteStored + amount);
        }

        public float TakeDepleted(float amount)
        {
            float taken = Mathf.Min(amount, DepletedAtlantumStored);
            DepletedAtlantumStored -= taken;
            return taken;
        }

        public float TakeRecovered(float amount)
        {
            float taken = Mathf.Min(amount, RecoveredMaterialStored);
            RecoveredMaterialStored -= taken;
            return taken;
        }

        public void TakeDamage(float damage)
        {
            Health -= damage;

            if (Health <= 0)
            {
                // Waste processor failure releases contamination
                AtlantumEnrichmentPlugin.CreateRadiationZone(transform.position, 0.5f);
                Destroy(gameObject);
            }
        }

        public void Repair(float amount)
        {
            Health = Mathf.Min(MaxHealth, Health + amount);
        }

        void OnDestroy()
        {
            AtlantumEnrichmentPlugin.ActiveWasteProcessors.Remove(this);
        }
    }

    /// <summary>
    /// Radiation Zone - Hazardous area that damages entities
    /// Created by explosions, leaks, and containment breaches
    /// Decays over time
    /// Integration: BioProcessing can accelerate cleanup
    /// </summary>
    public class RadiationZone : MonoBehaviour
    {
        public int ZoneId { get; private set; }
        private static int nextId = 0;

        // Zone properties
        public float Intensity { get; private set; } = 1f;
        public float Radius { get; private set; }
        public float TimeRemaining { get; private set; }

        // Damage
        private float damageTickRate = 1f; // seconds between damage ticks
        private float lastDamageTick;

        // Visual
        private ParticleSystem radiationParticles;
        private Light radiationLight;
        private SphereCollider zoneCollider;

        public void Initialize(float intensity)
        {
            ZoneId = nextId++;
            Intensity = intensity;
            Radius = AtlantumEnrichmentPlugin.RadiationRadius.Value * intensity;
            TimeRemaining = AtlantumEnrichmentPlugin.RadiationDecayTime.Value * intensity;

            SetupVisuals();
            SetupCollider();
        }

        private void SetupVisuals()
        {
            // Radiation particles
            radiationParticles = gameObject.AddComponent<ParticleSystem>();
            var main = radiationParticles.main;
            main.startLifetime = 3f;
            main.startSpeed = 0.5f;
            main.startSize = 0.3f;
            main.startColor = new Color(0.2f, 1f, 0.3f, 0.3f);
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.maxParticles = 500;

            var emission = radiationParticles.emission;
            emission.rateOverTime = 50f * Intensity;

            var shape = radiationParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = Radius;

            // Radiation glow
            var lightObj = new GameObject("RadiationGlow");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.5f;

            radiationLight = lightObj.AddComponent<Light>();
            radiationLight.type = LightType.Point;
            radiationLight.range = Radius * 1.5f;
            radiationLight.intensity = 2f * Intensity;
            radiationLight.color = new Color(0.3f, 1f, 0.4f);
        }

        private void SetupCollider()
        {
            zoneCollider = gameObject.AddComponent<SphereCollider>();
            zoneCollider.radius = Radius;
            zoneCollider.isTrigger = true;
        }

        public void UpdateZone()
        {
            // Decay over time
            TimeRemaining -= Time.deltaTime;

            if (TimeRemaining <= 0)
            {
                AtlantumEnrichmentPlugin.LogInfo($"Radiation zone {ZoneId} has decayed");
                Destroy(gameObject);
                return;
            }

            // Update intensity based on remaining time
            float decayFactor = TimeRemaining / (AtlantumEnrichmentPlugin.RadiationDecayTime.Value * Intensity);
            UpdateIntensity(Intensity * decayFactor);

            // Apply damage to nearby entities
            if (Time.time - lastDamageTick >= damageTickRate)
            {
                ApplyRadiationDamage();
                lastDamageTick = Time.time;
            }
        }

        private void UpdateIntensity(float newIntensity)
        {
            float normalizedIntensity = newIntensity / Intensity;

            if (radiationParticles != null)
            {
                var emission = radiationParticles.emission;
                emission.rateOverTime = 50f * normalizedIntensity;
            }

            if (radiationLight != null)
            {
                radiationLight.intensity = 2f * normalizedIntensity;
            }
        }

        private void ApplyRadiationDamage()
        {
            float damage = AtlantumEnrichmentPlugin.RadiationDamage.Value * Intensity;

            // Damage nearby centrifuges
            foreach (var centrifuge in AtlantumEnrichmentPlugin.ActiveCentrifuges)
            {
                if (centrifuge == null) continue;
                if (Vector3.Distance(transform.position, centrifuge.transform.position) < Radius)
                {
                    centrifuge.TakeDamage(damage * 0.5f);
                }
            }

            // Damage nearby enrichment chambers
            foreach (var chamber in AtlantumEnrichmentPlugin.ActiveChambers)
            {
                if (chamber == null) continue;
                if (Vector3.Distance(transform.position, chamber.transform.position) < Radius)
                {
                    chamber.TakeDamage(damage * 0.5f);
                    chamber.RepairContainment(-damage * 0.2f); // Radiation degrades containment
                }
            }

            // Damage waste processors
            foreach (var processor in AtlantumEnrichmentPlugin.ActiveWasteProcessors)
            {
                if (processor == null) continue;
                if (Vector3.Distance(transform.position, processor.transform.position) < Radius)
                {
                    processor.TakeDamage(damage * 0.3f);
                }
            }

            // Note: Player damage would integrate with TechtonicaFramework health system
        }

        /// <summary>
        /// Accelerate radiation decay (for BioProcessing integration)
        /// </summary>
        public void Remediate(float amount)
        {
            TimeRemaining -= amount;
            AtlantumEnrichmentPlugin.LogInfo($"Radiation zone {ZoneId} remediated. Time remaining: {TimeRemaining:F1}s");
        }

        /// <summary>
        /// Immediately clear the radiation zone
        /// </summary>
        public void Cleanse()
        {
            AtlantumEnrichmentPlugin.LogInfo($"Radiation zone {ZoneId} cleansed!");
            Destroy(gameObject);
        }

        void OnDestroy()
        {
            AtlantumEnrichmentPlugin.ActiveRadiationZones.Remove(this);
        }
    }
}
