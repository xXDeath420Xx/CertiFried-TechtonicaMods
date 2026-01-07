using UnityEngine;
using System.Collections.Generic;

namespace DroneLogistics
{
    /// <summary>
    /// Drone landing pad - spawns, charges, and manages drones
    /// </summary>
    public class DronePadController : MonoBehaviour
    {
        public int PadId { get; private set; }
        private static int nextPadId = 0;

        // Configuration
        public int MaxDrones => DroneLogisticsPlugin.MaxDronesPerPad.Value;

        // Managed drones
        private List<DroneController> drones = new List<DroneController>();
        public IReadOnlyList<DroneController> Drones => drones;
        public int DroneCount => drones.Count;
        public int AvailableDroneCount => drones.FindAll(d => d != null && d.IsAvailable).Count;

        // Queued requests
        private Queue<DeliveryRequest> requestQueue = new Queue<DeliveryRequest>();
        public int QueuedRequests => requestQueue.Count;

        // Visual
        private Light padLight;
        private LineRenderer rangeIndicator;

        public void Initialize()
        {
            PadId = nextPadId++;

            SetupVisuals();

            DroneLogisticsPlugin.Log($"Drone Pad {PadId} initialized");
        }

        private void SetupVisuals()
        {
            // Add landing light
            var lightObj = new GameObject("PadLight");
            lightObj.transform.SetParent(transform);
            lightObj.transform.localPosition = Vector3.up * 0.5f;

            padLight = lightObj.AddComponent<Light>();
            padLight.type = LightType.Point;
            padLight.range = 10f;
            padLight.intensity = 2f;
            padLight.color = new Color(0.3f, 0.5f, 1f);

            // Add range indicator
            CreateRangeIndicator();
        }

        private void CreateRangeIndicator()
        {
            var rangeObj = new GameObject("RangeIndicator");
            rangeObj.transform.SetParent(transform);
            rangeObj.transform.localPosition = Vector3.up * 0.1f;

            rangeIndicator = rangeObj.AddComponent<LineRenderer>();
            rangeIndicator.useWorldSpace = false;
            rangeIndicator.startWidth = 0.1f;
            rangeIndicator.endWidth = 0.1f;
            rangeIndicator.material = DroneLogisticsPlugin.GetEffectMaterial(new Color(0.3f, 0.5f, 1f, 0.3f));
            rangeIndicator.loop = true;

            // Draw circle
            int segments = 64;
            rangeIndicator.positionCount = segments + 1;
            float range = DroneLogisticsPlugin.DroneRange.Value;

            for (int i = 0; i <= segments; i++)
            {
                float angle = i * (360f / segments) * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * range;
                float z = Mathf.Sin(angle) * range;
                rangeIndicator.SetPosition(i, new Vector3(x, 0, z));
            }

            // Start hidden
            rangeIndicator.enabled = false;
        }

        void Update()
        {
            // Clean up destroyed drones
            drones.RemoveAll(d => d == null);

            // Process queued requests
            ProcessQueue();

            // Update visuals
            UpdateVisuals();
        }

        private void ProcessQueue()
        {
            while (requestQueue.Count > 0)
            {
                // Find available drone
                var available = drones.Find(d => d != null && d.IsAvailable);

                if (available == null)
                    break; // No drones available

                var request = requestQueue.Peek();

                if (available.AssignDelivery(request))
                {
                    requestQueue.Dequeue();
                }
                else
                {
                    break; // Request couldn't be assigned (too far, etc.)
                }
            }
        }

        private void UpdateVisuals()
        {
            if (padLight != null)
            {
                // Pulse when drones are charging
                bool hasCharging = drones.Exists(d => d != null && d.Charge < 1f);
                if (hasCharging)
                {
                    padLight.intensity = 1.5f + Mathf.Sin(Time.time * 3f) * 0.5f;
                    padLight.color = new Color(1f, 0.8f, 0.3f);
                }
                else
                {
                    padLight.intensity = 2f;
                    padLight.color = new Color(0.3f, 0.5f, 1f);
                }
            }
        }

        #region Drone Management

        public DroneController SpawnDrone(DroneType type)
        {
            if (DroneCount >= MaxDrones)
            {
                DroneLogisticsPlugin.LogWarning($"Pad {PadId} at max capacity ({MaxDrones} drones)");
                return null;
            }

            Vector3 spawnPos = transform.position + Vector3.up * 2f;
            spawnPos += Random.insideUnitSphere * 1f;
            spawnPos.y = transform.position.y + 2f;

            var drone = DroneLogisticsPlugin.SpawnDrone(type, spawnPos, this);
            if (drone != null)
            {
                drones.Add(drone);
                DroneLogisticsPlugin.Log($"Pad {PadId} spawned drone (total: {DroneCount})");
            }

            return drone;
        }

        public void RemoveDrone(DroneController drone)
        {
            if (drones.Contains(drone))
            {
                drones.Remove(drone);
                if (drone != null)
                {
                    Destroy(drone.gameObject);
                }
            }
        }

        public void RecallAllDrones()
        {
            foreach (var drone in drones)
            {
                if (drone != null)
                {
                    drone.RecallToBase();
                }
            }
        }

        #endregion

        #region Delivery Requests

        public bool RequestDelivery(DeliveryRequest request)
        {
            // Check if within range
            float pickupDist = Vector3.Distance(transform.position, request.PickupLocation);
            float deliveryDist = Vector3.Distance(request.PickupLocation, request.DeliveryLocation);

            if (pickupDist > DroneLogisticsPlugin.DroneRange.Value ||
                deliveryDist > DroneLogisticsPlugin.DroneRange.Value)
            {
                return false; // Out of range
            }

            // Try to assign immediately
            var available = drones.Find(d => d != null && d.IsAvailable);
            if (available != null && available.AssignDelivery(request))
            {
                return true;
            }

            // Queue for later
            requestQueue.Enqueue(request);
            DroneLogisticsPlugin.Log($"Pad {PadId} queued delivery (queue: {QueuedRequests})");
            return true;
        }

        public void CancelRequest(DeliveryRequest request)
        {
            // Convert to list, remove, convert back
            var list = new List<DeliveryRequest>(requestQueue);
            list.Remove(request);
            requestQueue = new Queue<DeliveryRequest>(list);
        }

        public void ClearQueue()
        {
            requestQueue.Clear();
        }

        #endregion

        #region UI Helpers

        public void ShowRange(bool show)
        {
            if (rangeIndicator != null)
            {
                rangeIndicator.enabled = show;
            }
        }

        public string GetStatusText()
        {
            int available = AvailableDroneCount;
            int charging = drones.FindAll(d => d != null && d.Charge < 1f).Count;
            int working = DroneCount - available - charging;

            return $"Pad {PadId}: {available}/{DroneCount} available, {working} working, {charging} charging, {QueuedRequests} queued";
        }

        #endregion

        void OnDestroy()
        {
            // Recall all drones before destroying
            foreach (var drone in drones)
            {
                if (drone != null)
                {
                    drone.HomePad = null;
                    drone.RecallToBase();
                }
            }

            DroneLogisticsPlugin.ActivePads.Remove(this);
        }
    }
}
