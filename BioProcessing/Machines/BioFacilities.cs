using UnityEngine;
using System.Collections.Generic;

namespace BioProcessing
{
    /// <summary>
    /// Algae Vat - Produces raw algae from water and light
    /// Output: Raw Algae → Algae Press → Bio-Oil → Biofuel
    /// </summary>
    public class AlgaeVatController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Production
        public float AlgaeStored { get; private set; }
        public float MaxAlgae = 100f;
        public float ProductionRate = 1f; // per second base

        // Requirements
        public bool HasWater { get; set; } = true; // Simplified - assume water available
        public bool HasLight { get; set; } = true;

        // State
        public bool IsProducing => HasWater && HasLight && AlgaeStored < MaxAlgae;

        // Visual
        private Renderer tankRenderer;
        private Color emptyColor = new Color(0.2f, 0.3f, 0.2f, 0.5f);
        private Color fullColor = new Color(0.1f, 0.8f, 0.2f, 0.8f);

        public void Initialize()
        {
            FacilityId = nextId++;

            // Find tank renderer for visual feedback
            var tank = transform.Find("Tank");
            if (tank != null)
            {
                tankRenderer = tank.GetComponent<Renderer>();
            }
        }

        void Update()
        {
            if (IsProducing)
            {
                float rate = ProductionRate * BioProcessingPlugin.AlgaeGrowthRate.Value;
                AlgaeStored = Mathf.Min(MaxAlgae, AlgaeStored + rate * Time.deltaTime);
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (tankRenderer != null)
            {
                float fill = AlgaeStored / MaxAlgae;
                tankRenderer.material.color = Color.Lerp(emptyColor, fullColor, fill);
            }
        }

        public float Harvest(float amount)
        {
            float harvested = Mathf.Min(amount, AlgaeStored);
            AlgaeStored -= harvested;

            if (harvested > 0)
            {
                BioProcessingPlugin.TotalPlantsHarvested++;
            }

            return harvested;
        }

        public float HarvestAll()
        {
            return Harvest(AlgaeStored);
        }

        void OnDestroy()
        {
            BioProcessingPlugin.ActiveAlgaeVats.Remove(this);
        }
    }

    /// <summary>
    /// Mushroom Farm - Grows mushrooms in dark conditions with fertilizer
    /// Output: Mushrooms, Spores
    /// </summary>
    public class MushroomFarmController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Production
        public int MushroomsReady { get; private set; }
        public int MaxMushrooms = 20;
        public float GrowthProgress { get; private set; }
        public float GrowthTime = 30f; // seconds to grow one mushroom

        // Requirements
        public float FertilizerLevel { get; private set; }
        public float MaxFertilizer = 50f;
        public float FertilizerPerMushroom = 2f;

        // State
        public bool CanGrow => FertilizerLevel >= FertilizerPerMushroom && MushroomsReady < MaxMushrooms;

        // Visuals
        private List<Transform> mushroomVisuals = new List<Transform>();

        public void Initialize()
        {
            FacilityId = nextId++;

            // Find mushroom visual children
            foreach (Transform child in transform)
            {
                if (child.name.StartsWith("Mushroom"))
                {
                    mushroomVisuals.Add(child);
                    child.gameObject.SetActive(false);
                }
            }
        }

        void Update()
        {
            if (CanGrow)
            {
                float rate = 1f / GrowthTime * BioProcessingPlugin.MushroomGrowthRate.Value;
                GrowthProgress += rate * Time.deltaTime;

                if (GrowthProgress >= 1f)
                {
                    GrowthProgress = 0f;
                    MushroomsReady++;
                    FertilizerLevel -= FertilizerPerMushroom;
                    UpdateMushroomVisuals();
                }
            }
        }

        private void UpdateMushroomVisuals()
        {
            for (int i = 0; i < mushroomVisuals.Count; i++)
            {
                mushroomVisuals[i].gameObject.SetActive(i < MushroomsReady);
            }
        }

        public void AddFertilizer(float amount)
        {
            FertilizerLevel = Mathf.Min(MaxFertilizer, FertilizerLevel + amount);
        }

        /// <summary>
        /// Add compost from Recycler as fertilizer
        /// </summary>
        public void AddCompost(int compostAmount)
        {
            float fertilizerFromCompost = compostAmount * BioProcessingPlugin.CompostEfficiency.Value;
            AddFertilizer(fertilizerFromCompost);
            BioProcessingPlugin.TotalCompostProduced += compostAmount;
        }

        public int Harvest()
        {
            int harvested = MushroomsReady;
            MushroomsReady = 0;
            UpdateMushroomVisuals();

            if (harvested > 0)
            {
                BioProcessingPlugin.TotalPlantsHarvested += harvested;
            }

            return harvested;
        }

        void OnDestroy()
        {
            BioProcessingPlugin.ActiveMushroomFarms.Remove(this);
        }
    }

    /// <summary>
    /// Bio Reactor - Converts organic matter into Biogas/Biofuel
    /// Input: Raw Algae, Mushrooms, Organic Waste
    /// Output: Biofuel (for DroneLogistics Bio-Drones), Biogas (for power)
    /// </summary>
    public class BioReactorController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Input storage
        public float OrganicMatter { get; private set; }
        public float MaxOrganicMatter = 200f;

        // Output storage
        public float BiofuelStored { get; private set; }
        public float MaxBiofuel = 100f;
        public float BiogasStored { get; private set; }
        public float MaxBiogas = 100f;

        // Conversion rates
        public float OrganicToBiofuel = 0.3f; // 30% conversion to biofuel
        public float OrganicToBiogas = 0.5f;  // 50% conversion to biogas
        public float ProcessingRate = 5f;     // organic matter processed per second

        // State
        public bool IsProcessing => OrganicMatter > 0 && (BiofuelStored < MaxBiofuel || BiogasStored < MaxBiogas);

        // Visual
        private Light reactorLight;
        private ParticleSystem steamParticles;

        public void Initialize()
        {
            FacilityId = nextId++;
            SetupVisuals();
        }

        private void SetupVisuals()
        {
            // Add reactor glow
            var lightObj = new GameObject("ReactorLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 1.5f;

            reactorLight = lightObj.AddComponent<Light>();
            reactorLight.type = LightType.Point;
            reactorLight.range = 5f;
            reactorLight.intensity = 0f;
            reactorLight.color = new Color(0.5f, 0.8f, 0.3f);

            // Add steam particles
            var steamObj = new GameObject("Steam");
            steamObj.transform.SetParent(transform);
            steamObj.transform.localPosition = Vector3.up * 3f;

            steamParticles = steamObj.AddComponent<ParticleSystem>();
            var main = steamParticles.main;
            main.startLifetime = 2f;
            main.startSpeed = 1f;
            main.startSize = 0.5f;
            main.startColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);

            var emission = steamParticles.emission;
            emission.rateOverTime = 0f;

            var shape = steamParticles.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius = 0.3f;
        }

        void Update()
        {
            if (IsProcessing)
            {
                float processed = Mathf.Min(OrganicMatter, ProcessingRate * Time.deltaTime);
                OrganicMatter -= processed;

                float biofuelProduced = processed * OrganicToBiofuel * BioProcessingPlugin.BiofuelYield.Value;
                float biogasProduced = processed * OrganicToBiogas;

                BiofuelStored = Mathf.Min(MaxBiofuel, BiofuelStored + biofuelProduced);
                BiogasStored = Mathf.Min(MaxBiogas, BiogasStored + biogasProduced);

                BioProcessingPlugin.TotalBiofuelProduced += biofuelProduced;
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            if (reactorLight != null)
            {
                reactorLight.intensity = IsProcessing ? 2f : 0.5f;
            }

            if (steamParticles != null)
            {
                var emission = steamParticles.emission;
                emission.rateOverTime = IsProcessing ? 20f : 0f;
            }
        }

        public void AddOrganicMatter(float amount)
        {
            OrganicMatter = Mathf.Min(MaxOrganicMatter, OrganicMatter + amount);
        }

        public void AddAlgae(float algaeAmount)
        {
            AddOrganicMatter(algaeAmount);
        }

        public void AddMushrooms(int mushroomCount)
        {
            AddOrganicMatter(mushroomCount * 5f); // Each mushroom = 5 organic matter
        }

        public float TakeBiofuel(float amount)
        {
            float taken = Mathf.Min(amount, BiofuelStored);
            BiofuelStored -= taken;
            return taken;
        }

        public float TakeBiogas(float amount)
        {
            float taken = Mathf.Min(amount, BiogasStored);
            BiogasStored -= taken;
            return taken;
        }

        void OnDestroy()
        {
            BioProcessingPlugin.ActiveBioReactors.Remove(this);
        }
    }

    /// <summary>
    /// Composter - Converts organic waste into fertilizer
    /// Integration with Recycler: receives organic waste, produces fertilizer for farms
    /// </summary>
    public class ComposterController : MonoBehaviour
    {
        public int FacilityId { get; private set; }
        private static int nextId = 0;

        // Input
        public float OrganicWaste { get; private set; }
        public float MaxWaste = 100f;

        // Output
        public float FertilizerReady { get; private set; }
        public float MaxFertilizer = 50f;

        // Processing
        public float WasteToFertilizer = 0.5f; // 50% conversion
        public float ProcessingTime = 60f; // seconds for full batch
        private float processingProgress;

        // State
        public bool IsProcessing => OrganicWaste >= 10f && FertilizerReady < MaxFertilizer;

        public void Initialize()
        {
            FacilityId = nextId++;
        }

        void Update()
        {
            if (IsProcessing)
            {
                float rate = BioProcessingPlugin.CompostEfficiency.Value / ProcessingTime;
                processingProgress += rate * Time.deltaTime;

                if (processingProgress >= 1f)
                {
                    processingProgress = 0f;

                    float converted = Mathf.Min(10f, OrganicWaste);
                    OrganicWaste -= converted;

                    float fertilizerProduced = converted * WasteToFertilizer;
                    FertilizerReady = Mathf.Min(MaxFertilizer, FertilizerReady + fertilizerProduced);

                    BioProcessingPlugin.TotalCompostProduced += fertilizerProduced;
                }
            }

            UpdateVisuals();
        }

        private void UpdateVisuals()
        {
            // Update organic matter visual based on fill level
            var organic = transform.Find("OrganicMatter");
            if (organic != null)
            {
                float fill = OrganicWaste / MaxWaste;
                organic.localScale = new Vector3(1.8f, 0.6f * fill + 0.1f, 1.8f);
            }
        }

        /// <summary>
        /// Add organic waste from Recycler
        /// </summary>
        public void AddWaste(float amount)
        {
            OrganicWaste = Mathf.Min(MaxWaste, OrganicWaste + amount);
        }

        public float TakeFertilizer(float amount)
        {
            float taken = Mathf.Min(amount, FertilizerReady);
            FertilizerReady -= taken;
            return taken;
        }

        public float TakeAllFertilizer()
        {
            return TakeFertilizer(FertilizerReady);
        }

        void OnDestroy()
        {
            BioProcessingPlugin.ActiveComposters.Remove(this);
        }
    }
}
