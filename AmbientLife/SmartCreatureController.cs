using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientLife
{
    /// <summary>
    /// Smart creature movement controller with terrain following, obstacle avoidance,
    /// and proper collision detection to prevent clipping
    /// </summary>
    public class SmartCreatureController : MonoBehaviour
    {
        public enum MovementMode
        {
            Flying,      // Birds, butterflies - avoids obstacles in 3D space
            Hovering,    // Fireflies, moths - low altitude flying
            Crawling,    // Beetles, spiders - follows ground surface
            WallCrawling // Spiders - can climb walls and ceilings
        }

        // Configuration
        public MovementMode Mode { get; private set; }
        public float MoveSpeed { get; set; } = 2f;
        public float TurnSpeed { get; set; } = 3f;
        public float TargetChangeInterval { get; set; } = 3f;
        public float WanderRadius { get; set; } = 10f;
        public float PreferredHeight { get; set; } = 2f;
        public float MinHeight { get; set; } = 0.5f;
        public float MaxHeight { get; set; } = 20f;

        // Internal state
        private Vector3 targetPosition;
        private Vector3 currentVelocity;
        private Vector3 surfaceNormal = Vector3.up;
        private float targetChangeTimer;
        private float obstacleCheckTimer;
        private bool isGrounded;
        private bool isOnWall;
        private bool isOnCeiling;

        // Animation
        private Animator animator;
        private Animation legacyAnimation;
        private bool isMoving;
        private bool wasMoving;
        private string currentAnimClip = "";
        private bool animInitialized = false;
        private static readonly int SpeedHash = Animator.StringToHash("Speed");
        private static readonly int IsWalkingHash = Animator.StringToHash("IsWalking");
        private static readonly int IsRunningHash = Animator.StringToHash("IsRunning");
        private static readonly int IsMovingHash = Animator.StringToHash("IsMoving");
        private static readonly int VelocityHash = Animator.StringToHash("Velocity");
        private static readonly int WalkHash = Animator.StringToHash("Walk");
        private static readonly int RunHash = Animator.StringToHash("Run");
        private static readonly int IdleHash = Animator.StringToHash("Idle");

        // Animation clip names found for this creature
        private string walkClip = null;
        private string idleClip = null;
        private string runClip = null;

        // Common animation clip names used by different asset packs
        private static readonly string[] WalkNames = { "Walk", "walk", "Walking", "walking", "walk_1", "walk_01", "Walk_1", "WalkCycle" };
        private static readonly string[] IdleNames = { "Idle", "idle", "Idle1", "idle1", "idle_1", "idle_01", "Idle_1", "IdleCycle" };
        private static readonly string[] RunNames = { "Run", "run", "Running", "running", "run_1", "run_01", "Run_1", "WalkFast" };

        // Collision detection
        private const float OBSTACLE_CHECK_INTERVAL = 0.1f;
        private const float GROUND_CHECK_DIST = 0.5f;
        private const float OBSTACLE_AVOID_DIST = 2f;
        private const float SURFACE_FOLLOW_DIST = 0.3f;

        // Layer masks
        private int groundLayer;
        private int buildingLayer;
        private int obstacleLayer;
        private LayerMask environmentMask;

        private void Awake()
        {
            // Setup layer masks for collision detection
            groundLayer = LayerMask.NameToLayer("Ground");
            buildingLayer = LayerMask.NameToLayer("Building");
            obstacleLayer = LayerMask.NameToLayer("Obstacle");

            // Combined mask for all solid objects
            environmentMask = LayerMask.GetMask("Ground", "Building", "Obstacle", "Default", "Machine", "Wall");
            if (environmentMask == 0)
            {
                // Fallback if named layers don't exist - use default physics
                environmentMask = Physics.DefaultRaycastLayers;
            }

            // Find animation components
            animator = GetComponent<Animator>();
            if (animator == null)
            {
                animator = GetComponentInChildren<Animator>();
            }

            // Also check for legacy Animation component
            legacyAnimation = GetComponent<Animation>();
            if (legacyAnimation == null)
            {
                legacyAnimation = GetComponentInChildren<Animation>();
            }
        }

        public void Initialize(MovementMode mode, float speed, float wanderRadius)
        {
            Mode = mode;
            MoveSpeed = speed;
            WanderRadius = wanderRadius;

            SetupModeParameters();
            PickNewTarget();
        }

        private void SetupModeParameters()
        {
            switch (Mode)
            {
                case MovementMode.Flying:
                    PreferredHeight = UnityEngine.Random.Range(8f, 15f);
                    MinHeight = 5f;
                    MaxHeight = 25f;
                    TurnSpeed = 2f;
                    break;

                case MovementMode.Hovering:
                    PreferredHeight = UnityEngine.Random.Range(1f, 3f);
                    MinHeight = 0.3f;
                    MaxHeight = 5f;
                    TurnSpeed = 4f;
                    break;

                case MovementMode.Crawling:
                    PreferredHeight = 0f;
                    MinHeight = 0f;
                    MaxHeight = 0.5f;
                    TurnSpeed = 5f;
                    break;

                case MovementMode.WallCrawling:
                    PreferredHeight = 0f;
                    MinHeight = 0f;
                    MaxHeight = 0f;
                    TurnSpeed = 4f;
                    break;
            }
        }

        private void Update()
        {
            // Update timers
            targetChangeTimer += Time.deltaTime;
            obstacleCheckTimer += Time.deltaTime;

            // Check for obstacles periodically
            if (obstacleCheckTimer >= OBSTACLE_CHECK_INTERVAL)
            {
                obstacleCheckTimer = 0f;
                CheckEnvironment();
            }

            // Change target periodically
            if (targetChangeTimer >= TargetChangeInterval)
            {
                targetChangeTimer = 0f;
                PickNewTarget();
            }

            // Move based on mode
            switch (Mode)
            {
                case MovementMode.Flying:
                    UpdateFlyingMovement();
                    break;
                case MovementMode.Hovering:
                    UpdateHoveringMovement();
                    break;
                case MovementMode.Crawling:
                    UpdateCrawlingMovement();
                    break;
                case MovementMode.WallCrawling:
                    UpdateWallCrawlingMovement();
                    break;
            }

            // Update animations based on movement
            UpdateAnimation();
        }

        private void CheckEnvironment()
        {
            // Check ground beneath
            isGrounded = Physics.Raycast(transform.position, -surfaceNormal, out RaycastHit groundHit,
                GROUND_CHECK_DIST, environmentMask);

            if (Mode == MovementMode.WallCrawling)
            {
                CheckForSurfaces();
            }
        }

        private void CheckForSurfaces()
        {
            // Check multiple directions for wall crawling
            Vector3[] directions = new Vector3[]
            {
                -surfaceNormal,      // Current "down"
                Vector3.down,        // World down
                Vector3.up,          // Ceiling
                transform.forward,   // Forward wall
                -transform.forward,  // Back wall
                transform.right,     // Right wall
                -transform.right     // Left wall
            };

            float closestDist = float.MaxValue;
            Vector3 closestNormal = Vector3.up;
            bool foundSurface = false;

            foreach (var dir in directions)
            {
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit, SURFACE_FOLLOW_DIST * 2f, environmentMask))
                {
                    if (hit.distance < closestDist)
                    {
                        closestDist = hit.distance;
                        closestNormal = hit.normal;
                        foundSurface = true;
                    }
                }
            }

            if (foundSurface)
            {
                surfaceNormal = Vector3.Lerp(surfaceNormal, closestNormal, Time.deltaTime * 5f);
                isOnWall = Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) < 0.5f;
                isOnCeiling = Vector3.Dot(surfaceNormal, Vector3.up) < -0.5f;
            }
        }

        private void PickNewTarget()
        {
            Vector3 basePos = transform.position;

            // Keep near player if they exist
            if (Player.instance != null)
            {
                float distToPlayer = Vector3.Distance(transform.position, Player.instance.transform.position);
                if (distToPlayer > AmbientLifePlugin.SpawnRadius.Value)
                {
                    // Move back towards player
                    basePos = Vector3.Lerp(transform.position, Player.instance.transform.position, 0.5f);
                }
            }

            // Random direction
            Vector3 randomDir = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                0f,
                UnityEngine.Random.Range(-1f, 1f)
            ).normalized;

            float dist = UnityEngine.Random.Range(WanderRadius * 0.3f, WanderRadius);
            targetPosition = basePos + randomDir * dist;

            // Adjust height based on mode
            switch (Mode)
            {
                case MovementMode.Flying:
                case MovementMode.Hovering:
                    targetPosition.y = GetSafeHeight(targetPosition);
                    break;
                case MovementMode.Crawling:
                case MovementMode.WallCrawling:
                    targetPosition = GetGroundPosition(targetPosition);
                    break;
            }

            // Validate target is reachable
            if (!IsPathClear(transform.position, targetPosition))
            {
                // Find alternate path
                targetPosition = FindAlternatePath();
            }
        }

        private float GetSafeHeight(Vector3 position)
        {
            float targetHeight = PreferredHeight + UnityEngine.Random.Range(-2f, 2f);

            // Check for ground
            if (Physics.Raycast(new Vector3(position.x, position.y + 100f, position.z),
                Vector3.down, out RaycastHit groundHit, 200f, environmentMask))
            {
                float groundY = groundHit.point.y;
                targetHeight = Mathf.Max(groundY + MinHeight,
                    Mathf.Min(groundY + MaxHeight, groundY + targetHeight));
            }

            // Check for ceiling/obstacles above
            if (Physics.Raycast(position, Vector3.up, out RaycastHit ceilingHit,
                MaxHeight, environmentMask))
            {
                targetHeight = Mathf.Min(targetHeight, ceilingHit.point.y - 0.5f);
            }

            return targetHeight;
        }

        private Vector3 GetGroundPosition(Vector3 position)
        {
            // Raycast down to find ground
            Vector3 rayStart = new Vector3(position.x, position.y + 10f, position.z);
            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 100f, environmentMask))
            {
                return hit.point + hit.normal * SURFACE_FOLLOW_DIST;
            }
            return position;
        }

        private bool IsPathClear(Vector3 from, Vector3 to)
        {
            Vector3 direction = to - from;
            float distance = direction.magnitude;

            return !Physics.Raycast(from, direction.normalized, distance, environmentMask);
        }

        private Vector3 FindAlternatePath()
        {
            // Try several random directions
            for (int i = 0; i < 8; i++)
            {
                float angle = i * 45f * Mathf.Deg2Rad;
                Vector3 dir = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                Vector3 testPos = transform.position + dir * WanderRadius * 0.5f;

                if (Mode == MovementMode.Flying || Mode == MovementMode.Hovering)
                {
                    testPos.y = GetSafeHeight(testPos);
                }
                else
                {
                    testPos = GetGroundPosition(testPos);
                }

                if (IsPathClear(transform.position, testPos))
                {
                    return testPos;
                }
            }

            // Fallback - stay near current position
            return transform.position + Vector3.up * 0.5f;
        }

        private void UpdateFlyingMovement()
        {
            Vector3 toTarget = targetPosition - transform.position;
            Vector3 desiredDirection = toTarget.normalized;

            // Obstacle avoidance
            Vector3 avoidance = GetObstacleAvoidance();
            if (avoidance.sqrMagnitude > 0.01f)
            {
                desiredDirection = (desiredDirection + avoidance * 2f).normalized;
            }

            // Smooth steering
            currentVelocity = Vector3.Lerp(currentVelocity, desiredDirection * MoveSpeed, Time.deltaTime * TurnSpeed);

            // Apply movement
            transform.position += currentVelocity * Time.deltaTime;

            // Face movement direction
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                Quaternion targetRot = Quaternion.LookRotation(currentVelocity.normalized);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSpeed);
            }

            // Height correction
            float currentHeight = GetHeightAboveGround();
            if (currentHeight < MinHeight)
            {
                transform.position += Vector3.up * (MinHeight - currentHeight) * Time.deltaTime * 5f;
            }
            else if (currentHeight > MaxHeight)
            {
                transform.position -= Vector3.up * (currentHeight - MaxHeight) * Time.deltaTime * 5f;
            }
        }

        private void UpdateHoveringMovement()
        {
            Vector3 toTarget = targetPosition - transform.position;
            Vector3 desiredDirection = toTarget.normalized;

            // Add hover wobble
            float wobbleX = Mathf.Sin(Time.time * 3f + transform.position.x) * 0.3f;
            float wobbleY = Mathf.Sin(Time.time * 2f + transform.position.z) * 0.2f;

            // Obstacle avoidance
            Vector3 avoidance = GetObstacleAvoidance();
            desiredDirection += avoidance;
            desiredDirection.Normalize();

            // Smooth movement with wobble
            currentVelocity = Vector3.Lerp(currentVelocity, desiredDirection * MoveSpeed, Time.deltaTime * TurnSpeed);
            Vector3 movement = currentVelocity + new Vector3(wobbleX, wobbleY, 0f);

            transform.position += movement * Time.deltaTime;

            // Soft rotation
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 lookDir = currentVelocity;
                lookDir.y *= 0.3f; // Reduce pitch
                if (lookDir.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(lookDir.normalized);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSpeed);
                }
            }

            // Maintain height above ground
            MaintainHeight();
        }

        private void UpdateCrawlingMovement()
        {
            // Find ground beneath current position and target
            Vector3 groundPos = GetGroundPosition(transform.position);
            Vector3 targetGroundPos = GetGroundPosition(targetPosition);

            Vector3 toTarget = targetGroundPos - groundPos;
            toTarget.y = 0; // Move along ground plane
            Vector3 desiredDirection = toTarget.normalized;

            // Obstacle avoidance (horizontal only)
            Vector3 avoidance = GetObstacleAvoidance();
            avoidance.y = 0;
            if (avoidance.sqrMagnitude > 0.01f)
            {
                desiredDirection = (desiredDirection + avoidance).normalized;
            }

            // Smooth steering
            currentVelocity = Vector3.Lerp(currentVelocity, desiredDirection * MoveSpeed, Time.deltaTime * TurnSpeed);
            currentVelocity.y = 0;

            // Move along ground
            Vector3 newPos = transform.position + currentVelocity * Time.deltaTime;
            newPos = GetGroundPosition(newPos);

            // Smooth transition to new position
            transform.position = Vector3.Lerp(transform.position, newPos, Time.deltaTime * 10f);

            // Align to surface
            if (Physics.Raycast(transform.position + Vector3.up * 0.5f, Vector3.down,
                out RaycastHit hit, 2f, environmentMask))
            {
                // Orient to ground normal
                Vector3 forward = Vector3.Cross(transform.right, hit.normal);
                if (forward.sqrMagnitude > 0.01f)
                {
                    Quaternion targetRot = Quaternion.LookRotation(forward, hit.normal);
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSpeed);
                }
            }
        }

        private void UpdateWallCrawlingMovement()
        {
            // Find closest surface
            Vector3 surfacePoint = transform.position;
            Vector3 surfaceNormalLocal = surfaceNormal;

            if (Physics.Raycast(transform.position, -surfaceNormal, out RaycastHit hit,
                SURFACE_FOLLOW_DIST * 2f, environmentMask))
            {
                surfacePoint = hit.point;
                surfaceNormalLocal = hit.normal;
            }

            // Project target onto surface plane
            Vector3 toTarget = targetPosition - transform.position;
            Vector3 tangent = Vector3.ProjectOnPlane(toTarget, surfaceNormalLocal).normalized;

            // Obstacle avoidance along surface
            Vector3 avoidance = GetSurfaceObstacleAvoidance(surfaceNormalLocal);
            if (avoidance.sqrMagnitude > 0.01f)
            {
                tangent = (tangent + avoidance).normalized;
            }

            // Smooth steering
            currentVelocity = Vector3.Lerp(currentVelocity, tangent * MoveSpeed, Time.deltaTime * TurnSpeed);

            // Move along surface
            Vector3 newPos = transform.position + currentVelocity * Time.deltaTime;

            // Re-project to surface
            if (Physics.Raycast(newPos + surfaceNormalLocal * 0.5f, -surfaceNormalLocal,
                out RaycastHit newHit, 1f, environmentMask))
            {
                newPos = newHit.point + newHit.normal * SURFACE_FOLLOW_DIST;
                surfaceNormal = newHit.normal;
            }

            transform.position = newPos;

            // Orient to surface
            if (currentVelocity.sqrMagnitude > 0.01f)
            {
                Vector3 forward = currentVelocity.normalized;
                Quaternion targetRot = Quaternion.LookRotation(forward, surfaceNormal);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * TurnSpeed);
            }
        }

        private Vector3 GetObstacleAvoidance()
        {
            Vector3 avoidance = Vector3.zero;
            int rayCount = 8;

            for (int i = 0; i < rayCount; i++)
            {
                float angle = (i / (float)rayCount) * 360f * Mathf.Deg2Rad;
                Vector3 rayDir = transform.TransformDirection(new Vector3(
                    Mathf.Cos(angle), 0.2f, Mathf.Sin(angle)
                )).normalized;

                if (Physics.Raycast(transform.position, rayDir, out RaycastHit hit,
                    OBSTACLE_AVOID_DIST, environmentMask))
                {
                    // Push away from obstacle
                    float strength = 1f - (hit.distance / OBSTACLE_AVOID_DIST);
                    avoidance -= rayDir * strength;
                }
            }

            // Also check directly ahead
            if (Physics.Raycast(transform.position, transform.forward, out RaycastHit frontHit,
                OBSTACLE_AVOID_DIST * 1.5f, environmentMask))
            {
                float strength = 1f - (frontHit.distance / (OBSTACLE_AVOID_DIST * 1.5f));
                avoidance -= transform.forward * strength * 2f;
                avoidance += frontHit.normal * strength;
            }

            // Check above and below
            if (Physics.Raycast(transform.position, Vector3.up, out RaycastHit upHit,
                OBSTACLE_AVOID_DIST, environmentMask))
            {
                avoidance -= Vector3.up * (1f - upHit.distance / OBSTACLE_AVOID_DIST);
            }
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit downHit,
                MinHeight, environmentMask))
            {
                avoidance += Vector3.up * (1f - downHit.distance / MinHeight);
            }

            return avoidance;
        }

        private Vector3 GetSurfaceObstacleAvoidance(Vector3 normal)
        {
            Vector3 avoidance = Vector3.zero;
            Vector3 right = Vector3.Cross(normal, transform.forward).normalized;
            Vector3 forward = Vector3.Cross(right, normal).normalized;

            // Check tangent directions
            Vector3[] dirs = { forward, -forward, right, -right };
            foreach (var dir in dirs)
            {
                if (Physics.Raycast(transform.position, dir, out RaycastHit hit,
                    OBSTACLE_AVOID_DIST * 0.5f, environmentMask))
                {
                    float strength = 1f - (hit.distance / (OBSTACLE_AVOID_DIST * 0.5f));
                    avoidance -= dir * strength;
                }
            }

            return Vector3.ProjectOnPlane(avoidance, normal);
        }

        private void MaintainHeight()
        {
            float heightAboveGround = GetHeightAboveGround();

            if (heightAboveGround < MinHeight)
            {
                transform.position += Vector3.up * (MinHeight - heightAboveGround + 0.1f);
            }
            else if (heightAboveGround > MaxHeight)
            {
                transform.position -= Vector3.up * (heightAboveGround - MaxHeight) * Time.deltaTime * 3f;
            }
        }

        private float GetHeightAboveGround()
        {
            if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, 100f, environmentMask))
            {
                return hit.distance;
            }
            return PreferredHeight; // Default if no ground found
        }

        private void UpdateAnimation()
        {
            // Initialize animation system on first call
            if (!animInitialized)
            {
                InitializeAnimations();
                animInitialized = true;
            }

            // Calculate speed for animation parameters
            float speed = currentVelocity.magnitude;
            isMoving = speed > 0.1f;

            // Only update if movement state changed
            if (isMoving == wasMoving && currentAnimClip != "") return;
            wasMoving = isMoving;

            // Determine which clip to play
            string targetClip = isMoving ? (speed > MoveSpeed * 0.7f && runClip != null ? runClip : walkClip) : idleClip;
            if (targetClip == null || targetClip == currentAnimClip) return;

            // Try legacy Animation first
            if (legacyAnimation != null)
            {
                try
                {
                    if (legacyAnimation[targetClip] != null)
                    {
                        legacyAnimation.CrossFade(targetClip, 0.2f);
                        currentAnimClip = targetClip;
                        return;
                    }
                }
                catch { }
            }

            // Try Animator - play state directly
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                try
                {
                    // Try cross-fade to state by name
                    animator.CrossFade(targetClip, 0.2f, 0);
                    currentAnimClip = targetClip;
                }
                catch
                {
                    // Fall back to parameter-based animation
                    TryParameterAnimation(speed);
                }
            }
        }

        private void InitializeAnimations()
        {
            // Search for animation clips in legacy Animation component
            if (legacyAnimation != null)
            {
                foreach (AnimationState state in legacyAnimation)
                {
                    string clipName = state.name;
                    CheckClipName(clipName);
                }
            }

            // Search for clips in Animator
            if (animator != null && animator.runtimeAnimatorController != null)
            {
                try
                {
                    var clips = animator.runtimeAnimatorController.animationClips;
                    foreach (var clip in clips)
                    {
                        CheckClipName(clip.name);
                    }
                }
                catch { }
            }

            // If no clips found, try searching child objects for Animation clips
            if (walkClip == null && idleClip == null)
            {
                var childAnims = GetComponentsInChildren<Animation>();
                foreach (var anim in childAnims)
                {
                    foreach (AnimationState state in anim)
                    {
                        CheckClipName(state.name);
                    }
                    if (legacyAnimation == null)
                        legacyAnimation = anim;
                }
            }

            // Default to common names if nothing found
            if (walkClip == null) walkClip = "Walk";
            if (idleClip == null) idleClip = "Idle";
        }

        private void CheckClipName(string clipName)
        {
            string lower = clipName.ToLowerInvariant();

            // Check for walk
            if (walkClip == null)
            {
                foreach (string name in WalkNames)
                {
                    if (lower.Contains(name.ToLowerInvariant()))
                    {
                        walkClip = clipName;
                        break;
                    }
                }
            }

            // Check for idle
            if (idleClip == null)
            {
                foreach (string name in IdleNames)
                {
                    if (lower.Contains(name.ToLowerInvariant()))
                    {
                        idleClip = clipName;
                        break;
                    }
                }
            }

            // Check for run
            if (runClip == null)
            {
                foreach (string name in RunNames)
                {
                    if (lower.Contains(name.ToLowerInvariant()))
                    {
                        runClip = clipName;
                        break;
                    }
                }
            }
        }

        private void TryParameterAnimation(float speed)
        {
            if (animator == null) return;

            try
            {
                // Set speed-based parameters
                if (HasParameter(SpeedHash))
                    animator.SetFloat(SpeedHash, speed);
                if (HasParameter(VelocityHash))
                    animator.SetFloat(VelocityHash, speed);

                // Set boolean movement states
                if (HasParameter(IsMovingHash))
                    animator.SetBool(IsMovingHash, isMoving);
                if (HasParameter(IsWalkingHash))
                    animator.SetBool(IsWalkingHash, isMoving);
                if (HasParameter(IsRunningHash))
                    animator.SetBool(IsRunningHash, isMoving && speed >= MoveSpeed * 0.8f);
            }
            catch { }
        }

        private bool HasParameter(int hash)
        {
            if (animator == null || animator.parameterCount == 0) return false;

            try
            {
                foreach (AnimatorControllerParameter param in animator.parameters)
                {
                    if (param.nameHash == hash)
                        return true;
                }
            }
            catch { }
            return false;
        }

        // Public methods for external control
        public void SetTarget(Vector3 position)
        {
            targetPosition = position;
            targetChangeTimer = 0f;
        }

        public void Flee(Vector3 threatPosition, float fleeDistance)
        {
            Vector3 fleeDir = (transform.position - threatPosition).normalized;
            targetPosition = transform.position + fleeDir * fleeDistance;

            if (Mode == MovementMode.Flying || Mode == MovementMode.Hovering)
            {
                targetPosition.y = GetSafeHeight(targetPosition);
            }
            else
            {
                targetPosition = GetGroundPosition(targetPosition);
            }

            targetChangeTimer = 0f;
            TargetChangeInterval = 5f; // Stay fled for a while
        }

        public bool IsNearPlayer(float distance)
        {
            if (Player.instance == null) return false;
            return Vector3.Distance(transform.position, Player.instance.transform.position) < distance;
        }
    }
}
