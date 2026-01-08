using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Wave composition definition
    /// </summary>
    public class WaveComposition
    {
        // Flying aliens
        public int Fighters;
        public int Destroyers;
        public int Torpedoes;
        // Flying robot drones
        public int RobotScouts;
        public int RobotInvaders;
        public int RobotCollectors;
        public int RobotGuardians; // Mini-boss
        // Ground enemies (creatures)
        public int Chompers;
        public int Spitters;
        public int Arachnids;
        public int Mimics;
        // Ground robots (heavy mechs)
        public int MachineGunRobots;  // Heavy rapid-fire ground units
        public int CannonMachines;     // Artillery ground units
        // Wave settings
        public float SpawnDelay;
        public bool IsBossWave;
        public BossType? BossToSpawn; // Epic boss encounter

        public int TotalFlying => Fighters + Destroyers + Torpedoes + RobotScouts + RobotInvaders + RobotCollectors + RobotGuardians;
        public int TotalGround => Chompers + Spitters + Arachnids + Mimics + MachineGunRobots + CannonMachines;
        public int TotalAliens => TotalFlying + TotalGround;
    }

    /// <summary>
    /// Manages alien wave spawning, difficulty scaling, and wave announcements
    /// </summary>
    public class WaveManager : MonoBehaviour
    {
        // ========== WAVE STATE ==========
        public int CurrentWave { get; private set; } = 0;
        public bool IsWaveActive { get; private set; } = false;
        public int AliensRemaining { get; private set; } = 0;
        public float TimeUntilNextWave { get; private set; } = 0f;

        // ========== TIMING ==========
        private float waveTimer = 0f;
        private float intermissionTimer = 0f;
        private bool systemActive = false;
        private bool inIntermission = true;

        // ========== SPAWNING ==========
        private int spawnIndex = 0;
        private Coroutine spawnCoroutine;

        // ========== UI ==========
        private WaveUI waveUI;

        // ========== WAVE DEFINITIONS ==========
        private static readonly WaveComposition[] WaveDefinitions = new WaveComposition[]
        {
            // Wave 1-5: Tutorial waves (flying organic aliens only)
            new WaveComposition { Fighters = 3, Destroyers = 0, Torpedoes = 0, SpawnDelay = 1f },
            new WaveComposition { Fighters = 5, Destroyers = 0, Torpedoes = 0, SpawnDelay = 0.8f },
            new WaveComposition { Fighters = 4, Destroyers = 1, Torpedoes = 0, SpawnDelay = 0.8f },
            new WaveComposition { Fighters = 6, Destroyers = 1, Torpedoes = 1, SpawnDelay = 0.7f },
            new WaveComposition { Fighters = 5, Destroyers = 2, Torpedoes = 2, SpawnDelay = 0.6f, IsBossWave = true, BossToSpawn = BossType.Overlord },

            // Wave 6-10: Ground creatures introduced + robot scouts
            new WaveComposition { Fighters = 6, Destroyers = 2, Torpedoes = 2, RobotScouts = 3, Chompers = 2, SpawnDelay = 0.5f },
            new WaveComposition { Fighters = 5, Destroyers = 3, Torpedoes = 3, RobotScouts = 4, Chompers = 3, Spitters = 1, SpawnDelay = 0.5f },
            new WaveComposition { Fighters = 8, Destroyers = 2, Torpedoes = 4, RobotScouts = 5, RobotCollectors = 2, Chompers = 4, Spitters = 2, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 6, Destroyers = 4, Torpedoes = 3, RobotScouts = 6, RobotInvaders = 1, Chompers = 5, Spitters = 2, Arachnids = 1, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 8, Destroyers = 5, Torpedoes = 5, RobotScouts = 4, RobotInvaders = 2, RobotGuardians = 1, Chompers = 6, Spitters = 3, Arachnids = 2, SpawnDelay = 0.3f, IsBossWave = true, BossToSpawn = BossType.Behemoth },

            // Wave 11-15: Full robot invasion + mimics + machine gun robots
            new WaveComposition { Fighters = 10, Destroyers = 4, Torpedoes = 4, RobotScouts = 8, RobotInvaders = 2, Chompers = 6, Spitters = 3, Arachnids = 2, Mimics = 1, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 8, Destroyers = 5, Torpedoes = 6, RobotScouts = 10, RobotInvaders = 3, RobotCollectors = 4, Chompers = 8, Spitters = 4, Arachnids = 3, Mimics = 2, MachineGunRobots = 1, SpawnDelay = 0.35f },
            new WaveComposition { Fighters = 12, Destroyers = 6, Torpedoes = 5, RobotScouts = 8, RobotInvaders = 4, Chompers = 8, Spitters = 4, Arachnids = 4, Mimics = 2, MachineGunRobots = 2, SpawnDelay = 0.3f },
            new WaveComposition { Fighters = 10, Destroyers = 8, Torpedoes = 6, RobotScouts = 12, RobotInvaders = 4, RobotCollectors = 5, Chompers = 10, Spitters = 5, Arachnids = 4, Mimics = 3, MachineGunRobots = 2, SpawnDelay = 0.25f },
            new WaveComposition { Fighters = 12, Destroyers = 8, Torpedoes = 8, RobotScouts = 10, RobotInvaders = 5, RobotGuardians = 2, Chompers = 10, Spitters = 5, Arachnids = 5, Mimics = 3, MachineGunRobots = 3, CannonMachines = 1, SpawnDelay = 0.2f, IsBossWave = true, BossToSpawn = BossType.Dreadnought },

            // Wave 16-20: Hivemind invasion - heavy ground robots
            new WaveComposition { Fighters = 15, Destroyers = 8, Torpedoes = 6, RobotScouts = 12, RobotInvaders = 4, Chompers = 12, Spitters = 6, Arachnids = 5, Mimics = 4, MachineGunRobots = 3, CannonMachines = 1, SpawnDelay = 0.25f },
            new WaveComposition { Fighters = 12, Destroyers = 10, Torpedoes = 8, RobotScouts = 14, RobotInvaders = 5, RobotCollectors = 6, Chompers = 14, Spitters = 7, Arachnids = 6, Mimics = 4, MachineGunRobots = 4, CannonMachines = 2, SpawnDelay = 0.2f },
            new WaveComposition { Fighters = 18, Destroyers = 10, Torpedoes = 8, RobotScouts = 15, RobotInvaders = 6, Chompers = 15, Spitters = 8, Arachnids = 6, Mimics = 5, MachineGunRobots = 4, CannonMachines = 2, SpawnDelay = 0.2f },
            new WaveComposition { Fighters = 15, Destroyers = 12, Torpedoes = 10, RobotScouts = 16, RobotInvaders = 6, RobotCollectors = 8, Chompers = 16, Spitters = 8, Arachnids = 7, Mimics = 5, MachineGunRobots = 5, CannonMachines = 3, SpawnDelay = 0.15f },
            new WaveComposition { Fighters = 20, Destroyers = 12, Torpedoes = 12, RobotScouts = 15, RobotInvaders = 8, RobotGuardians = 3, Chompers = 18, Spitters = 10, Arachnids = 8, Mimics = 6, MachineGunRobots = 5, CannonMachines = 3, SpawnDelay = 0.15f, IsBossWave = true, BossToSpawn = BossType.Hivemind },

            // Wave 21-25: Harvester assault - maximum ground forces
            new WaveComposition { Fighters = 20, Destroyers = 12, Torpedoes = 10, RobotScouts = 18, RobotInvaders = 8, Chompers = 20, Spitters = 10, Arachnids = 8, Mimics = 6, MachineGunRobots = 6, CannonMachines = 3, SpawnDelay = 0.15f },
            new WaveComposition { Fighters = 18, Destroyers = 14, Torpedoes = 12, RobotScouts = 20, RobotInvaders = 10, RobotCollectors = 10, Chompers = 22, Spitters = 12, Arachnids = 10, Mimics = 7, MachineGunRobots = 6, CannonMachines = 4, SpawnDelay = 0.12f },
            new WaveComposition { Fighters = 22, Destroyers = 14, Torpedoes = 12, RobotScouts = 20, RobotInvaders = 10, Chompers = 24, Spitters = 12, Arachnids = 10, Mimics = 8, MachineGunRobots = 7, CannonMachines = 4, SpawnDelay = 0.12f },
            new WaveComposition { Fighters = 20, Destroyers = 16, Torpedoes = 14, RobotScouts = 22, RobotInvaders = 12, RobotCollectors = 12, Chompers = 26, Spitters = 14, Arachnids = 12, Mimics = 8, MachineGunRobots = 8, CannonMachines = 5, SpawnDelay = 0.1f },
            new WaveComposition { Fighters = 25, Destroyers = 16, Torpedoes = 15, RobotScouts = 20, RobotInvaders = 12, RobotGuardians = 4, Chompers = 28, Spitters = 15, Arachnids = 12, Mimics = 10, MachineGunRobots = 8, CannonMachines = 5, SpawnDelay = 0.1f, IsBossWave = true, BossToSpawn = BossType.Harvester },
        };

        public void StartWaveSystem(float initialDelay = -1f)
        {
            systemActive = true;
            inIntermission = true;

            // Use provided delay or default to half interval
            intermissionTimer = initialDelay > 0 ? initialDelay : TurretDefensePlugin.WaveInterval.Value * 0.5f;

            // Create wave UI
            var uiObj = new GameObject("WaveUI");
            uiObj.transform.SetParent(transform);
            waveUI = uiObj.AddComponent<WaveUI>();

            TurretDefensePlugin.Log.LogInfo($"Wave system started! First wave in {intermissionTimer:F0} seconds.");
        }

        public void StopWaveSystem()
        {
            systemActive = false;
            if (spawnCoroutine != null)
                StopCoroutine(spawnCoroutine);
        }

        private void Update()
        {
            if (!systemActive) return;

            if (inIntermission)
            {
                intermissionTimer -= Time.deltaTime;
                TimeUntilNextWave = intermissionTimer;

                if (intermissionTimer <= 0)
                {
                    StartNextWave();
                }
            }
            else if (IsWaveActive)
            {
                // Check if wave is complete (all aliens AND bosses dead)
                AliensRemaining = TurretDefensePlugin.ActiveAliens.Count +
                                  TurretDefensePlugin.ActiveGroundEnemies.Count +
                                  TurretDefensePlugin.ActiveBosses.Count;

                if (AliensRemaining == 0 && spawnCoroutine == null)
                {
                    EndWave();
                }
            }
        }

        public void ForceSpawnWave()
        {
            if (IsWaveActive)
            {
                TurretDefensePlugin.Log.LogWarning("Wave already in progress!");
                return;
            }

            // Start system if not yet active
            if (!systemActive)
            {
                StartWaveSystem(0f); // Start immediately
                return;
            }

            intermissionTimer = 0;
        }

        private void StartNextWave()
        {
            CurrentWave++;
            IsWaveActive = true;
            inIntermission = false;

            TurretDefensePlugin.Log.LogWarning($"=== WAVE {CurrentWave} STARTING! ===");

            // Get wave composition
            WaveComposition composition = GetWaveComposition(CurrentWave);

            if (composition.IsBossWave)
            {
                TurretDefensePlugin.Log.LogError($"!!! BOSS WAVE {CurrentWave} !!!");
            }

            // UI announcement
            waveUI?.ShowWaveStart(CurrentWave, composition.IsBossWave);

            // Start spawning
            spawnCoroutine = StartCoroutine(SpawnWave(composition));
        }

        private WaveComposition GetWaveComposition(int wave)
        {
            // Use predefined waves if available
            if (wave <= WaveDefinitions.Length)
            {
                return WaveDefinitions[wave - 1];
            }

            // Generate procedural wave for endless mode
            float scaling = TurretDefensePlugin.DifficultyScaling.Value;
            float waveMultiplier = Mathf.Pow(scaling, wave - WaveDefinitions.Length);

            int baseCount = TurretDefensePlugin.BaseAliensPerWave.Value;
            int totalEnemies = Mathf.RoundToInt(baseCount * waveMultiplier);

            bool isBoss = wave % 5 == 0;

            // Distribution: 40% flying, 35% ground creatures, 25% ground robots
            float flyingRatio = 0.40f;
            float groundCreatureRatio = 0.35f;
            float groundRobotRatio = 0.25f;

            int flyingCount = Mathf.RoundToInt(totalEnemies * flyingRatio);
            int groundCreatureCount = Mathf.RoundToInt(totalEnemies * groundCreatureRatio);
            int groundRobotCount = Mathf.RoundToInt(totalEnemies * groundRobotRatio);

            // Flying breakdown: 40% organic aliens, 60% robot drones
            int organicFlying = Mathf.RoundToInt(flyingCount * 0.4f);
            int robotFlying = flyingCount - organicFlying;

            return new WaveComposition
            {
                // Flying organic aliens
                Fighters = Mathf.RoundToInt(organicFlying * 0.5f),
                Destroyers = Mathf.RoundToInt(organicFlying * 0.3f),
                Torpedoes = Mathf.RoundToInt(organicFlying * 0.2f),
                // Flying robot drones
                RobotScouts = Mathf.RoundToInt(robotFlying * 0.5f),
                RobotInvaders = Mathf.RoundToInt(robotFlying * 0.3f),
                RobotCollectors = Mathf.RoundToInt(robotFlying * 0.15f),
                RobotGuardians = isBoss ? Mathf.Max(1, wave / 10) : 0, // More guardians in later boss waves
                // Ground creatures
                Chompers = Mathf.RoundToInt(groundCreatureCount * 0.35f),
                Spitters = Mathf.RoundToInt(groundCreatureCount * 0.25f),
                Arachnids = Mathf.RoundToInt(groundCreatureCount * 0.20f),
                Mimics = Mathf.RoundToInt(groundCreatureCount * 0.20f),
                // Ground robots (heavy mechs)
                MachineGunRobots = Mathf.RoundToInt(groundRobotCount * 0.6f),
                CannonMachines = Mathf.RoundToInt(groundRobotCount * 0.4f),
                // Settings
                SpawnDelay = Mathf.Max(0.08f, 0.4f - (wave * 0.012f)),
                IsBossWave = isBoss
            };
        }

        private IEnumerator SpawnWave(WaveComposition composition)
        {
            // ========== FLYING ENEMIES ==========
            List<string> flyingSpawnOrder = new List<string>();

            // Add fighters (40% regular, 35% green/tanky, 25% white/fast)
            for (int i = 0; i < composition.Fighters; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "AlienFighter" :
                                roll < 0.75f ? "AlienFighterGreen" : "AlienFighterWhite";
                flyingSpawnOrder.Add(variant);
            }

            // Add destroyers (40% regular, 35% green/tanky, 25% white/fast)
            for (int i = 0; i < composition.Destroyers; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "AlienDestroyer" :
                                roll < 0.75f ? "AlienDestroyerGreen" : "AlienDestroyerWhite";
                flyingSpawnOrder.Add(variant);
            }

            // Add torpedoes (40% regular, 30% green, 30% white)
            for (int i = 0; i < composition.Torpedoes; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "BioTorpedo" :
                                roll < 0.7f ? "BioTorpedoGreen" : "BioTorpedoWhite";
                flyingSpawnOrder.Add(variant);
            }

            // Add robot scouts (various color variants)
            string[] scoutVariants = { "Robot_Scout", "Robot_Scout_HyperX", "Robot_Scout_HyperX_Red", "Robot_Scout_Rockie" };
            for (int i = 0; i < composition.RobotScouts; i++)
            {
                string variant = scoutVariants[UnityEngine.Random.Range(0, scoutVariants.Length)];
                flyingSpawnOrder.Add(variant);
            }

            // Add robot invaders (elite enemy)
            for (int i = 0; i < composition.RobotInvaders; i++)
            {
                flyingSpawnOrder.Add("Robot_Invader");
            }

            // Add robot collectors
            for (int i = 0; i < composition.RobotCollectors; i++)
            {
                flyingSpawnOrder.Add("Robot_Collector");
            }

            // Robot guardians (mini-boss - spawn last among flying)
            List<string> guardians = new List<string>();
            for (int i = 0; i < composition.RobotGuardians; i++)
            {
                guardians.Add("Robot_Guardian");
            }

            // ========== GROUND ENEMIES ==========
            List<GroundEnemyType> groundSpawnOrder = new List<GroundEnemyType>();

            // Ground creatures
            for (int i = 0; i < composition.Chompers; i++)
                groundSpawnOrder.Add(GroundEnemyType.Chomper);
            for (int i = 0; i < composition.Spitters; i++)
                groundSpawnOrder.Add(GroundEnemyType.Spitter);
            for (int i = 0; i < composition.Arachnids; i++)
                groundSpawnOrder.Add(GroundEnemyType.Arachnid);
            for (int i = 0; i < composition.Mimics; i++)
                groundSpawnOrder.Add(GroundEnemyType.Mimic);

            // Ground robots (spawn last among ground enemies - they're heavy units)
            List<GroundEnemyType> groundRobots = new List<GroundEnemyType>();
            for (int i = 0; i < composition.MachineGunRobots; i++)
                groundRobots.Add(GroundEnemyType.MachineGunRobot);
            for (int i = 0; i < composition.CannonMachines; i++)
                groundRobots.Add(GroundEnemyType.CannonMachine);

            // Shuffle both lists
            ShuffleList(flyingSpawnOrder);
            ShuffleList(groundSpawnOrder);

            // Add mini-bosses at end of flying
            flyingSpawnOrder.AddRange(guardians);
            // Add heavy robots at end of ground
            groundSpawnOrder.AddRange(groundRobots);

            TurretDefensePlugin.LogDebug($"Wave {CurrentWave}: Spawning {flyingSpawnOrder.Count} flying ({guardians.Count} mini-bosses), {groundSpawnOrder.Count} ground ({groundRobots.Count} heavy mechs)");

            // ========== INTERLEAVED SPAWNING ==========
            // Spawn flying and ground enemies in alternating fashion
            spawnIndex = 0;
            int flyingIdx = 0;
            int groundIdx = 0;

            while (flyingIdx < flyingSpawnOrder.Count || groundIdx < groundSpawnOrder.Count)
            {
                // Spawn 2 flying, then 1 ground (keeps the sky active)
                for (int f = 0; f < 2 && flyingIdx < flyingSpawnOrder.Count; f++)
                {
                    Vector3 spawnPos = TurretDefensePlugin.GetSpawnPosition(spawnIndex, UnityEngine.Random.Range(50f, 80f));
                    spawnIndex++;
                    TurretDefensePlugin.SpawnAlien(flyingSpawnOrder[flyingIdx], spawnPos, CurrentWave);
                    flyingIdx++;
                    yield return new WaitForSeconds(composition.SpawnDelay);
                }

                // Spawn 1 ground enemy
                if (groundIdx < groundSpawnOrder.Count)
                {
                    Vector3 groundSpawnPos = TurretDefensePlugin.GetGroundSpawnPosition(spawnIndex);
                    spawnIndex++;
                    TurretDefensePlugin.SpawnGroundEnemy(groundSpawnOrder[groundIdx], groundSpawnPos, CurrentWave);
                    groundIdx++;
                    yield return new WaitForSeconds(composition.SpawnDelay * 1.5f); // Ground spawns slightly slower
                }
            }

            // Spawn epic boss last if this is a boss wave
            if (composition.BossToSpawn.HasValue)
            {
                yield return new WaitForSeconds(2f); // Dramatic pause

                var player = Player.instance;
                if (player != null)
                {
                    Vector3 bossSpawnPos = player.transform.position + player.transform.forward * 60f + Vector3.up * 30f;
                    TurretDefensePlugin.SpawnBoss(composition.BossToSpawn.Value, bossSpawnPos, CurrentWave);
                }
            }

            spawnCoroutine = null;
            TurretDefensePlugin.LogDebug($"Wave {CurrentWave} spawning complete");
        }

        private void EndWave()
        {
            IsWaveActive = false;

            TurretDefensePlugin.Log.LogInfo($"=== WAVE {CurrentWave} COMPLETE! ===");
            TurretDefensePlugin.Log.LogInfo($"Total Kills: {TurretDefensePlugin.TotalKills}, Score: {TurretDefensePlugin.CurrentScore}");

            // UI
            waveUI?.ShowWaveComplete(CurrentWave, TurretDefensePlugin.CurrentScore);

            // Start intermission
            inIntermission = true;
            intermissionTimer = TurretDefensePlugin.WaveInterval.Value;

            // Bonus time reduction for fast clear
            // TODO: Track wave clear time
        }

        private void ShuffleList<T>(List<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = UnityEngine.Random.Range(0, n + 1);
                T value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }
    }

    /// <summary>
    /// Wave announcement and status UI
    /// </summary>
    public class WaveUI : MonoBehaviour
    {
        private GUIStyle titleStyle;
        private GUIStyle subtitleStyle;
        private GUIStyle statusStyle;

        private string currentMessage = "";
        private string currentSubMessage = "";
        private float messageTimer = 0f;
        private float messageAlpha = 0f;
        private bool isBossWave = false;

        void Awake()
        {
            titleStyle = new GUIStyle();
            subtitleStyle = new GUIStyle();
            statusStyle = new GUIStyle();
        }

        void OnGUI()
        {
            SetupStyles();

            // Wave announcement
            if (messageTimer > 0)
            {
                DrawWaveAnnouncement();
            }

            // Status bar
            DrawStatusBar();
        }

        void Update()
        {
            if (messageTimer > 0)
            {
                messageTimer -= Time.deltaTime;

                // Fade in/out
                if (messageTimer > 2.5f)
                    messageAlpha = Mathf.Lerp(messageAlpha, 1f, Time.deltaTime * 5f);
                else if (messageTimer < 0.5f)
                    messageAlpha = Mathf.Lerp(messageAlpha, 0f, Time.deltaTime * 5f);
            }
        }

        public void ShowWaveStart(int wave, bool boss)
        {
            currentMessage = boss ? $"!!! BOSS WAVE {wave} !!!" : $"WAVE {wave}";
            currentSubMessage = "Incoming hostiles!";
            messageTimer = 3f;
            messageAlpha = 0f;
            isBossWave = boss;
        }

        public void ShowWaveComplete(int wave, int score)
        {
            currentMessage = "WAVE CLEARED!";
            currentSubMessage = $"Score: {score}";
            messageTimer = 3f;
            messageAlpha = 0f;
            isBossWave = false;
        }

        private void SetupStyles()
        {
            titleStyle.fontSize = 48;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;

            subtitleStyle.fontSize = 24;
            subtitleStyle.alignment = TextAnchor.MiddleCenter;

            statusStyle.fontSize = 16;
            statusStyle.alignment = TextAnchor.MiddleLeft;
            statusStyle.normal.textColor = Color.white;
        }

        private void DrawWaveAnnouncement()
        {
            Color titleColor = isBossWave ? new Color(1f, 0.3f, 0.3f, messageAlpha) : new Color(1f, 0.8f, 0.2f, messageAlpha);
            titleStyle.normal.textColor = titleColor;
            subtitleStyle.normal.textColor = new Color(1f, 1f, 1f, messageAlpha);

            float centerX = Screen.width / 2f;
            float centerY = Screen.height / 3f;

            // Shadow
            GUI.color = new Color(0, 0, 0, messageAlpha * 0.5f);
            GUI.Label(new Rect(centerX - 200 + 2, centerY - 30 + 2, 400, 60), currentMessage, titleStyle);
            GUI.Label(new Rect(centerX - 200 + 1, centerY + 30 + 1, 400, 40), currentSubMessage, subtitleStyle);

            // Main text
            GUI.color = Color.white;
            GUI.Label(new Rect(centerX - 200, centerY - 30, 400, 60), currentMessage, titleStyle);
            GUI.Label(new Rect(centerX - 200, centerY + 30, 400, 40), currentSubMessage, subtitleStyle);
        }

        private void DrawStatusBar()
        {
            var waveManager = TurretDefensePlugin.WaveSystem;
            if (waveManager == null) return;

            float x = Screen.width - 220;
            float y = 10;

            // Background
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(x - 10, y - 5, 220, 90), "");
            GUI.color = Color.white;

            // Wave info
            GUI.Label(new Rect(x, y, 200, 20), $"Wave: {waveManager.CurrentWave}", statusStyle);
            y += 20;

            if (waveManager.IsWaveActive)
            {
                GUI.Label(new Rect(x, y, 200, 20), $"Enemies: {waveManager.AliensRemaining}", statusStyle);
            }
            else
            {
                float timeLeft = waveManager.TimeUntilNextWave;
                GUI.Label(new Rect(x, y, 200, 20), $"Next wave: {timeLeft:F0}s", statusStyle);
            }
            y += 20;

            GUI.Label(new Rect(x, y, 200, 20), $"Kills: {TurretDefensePlugin.TotalKills}", statusStyle);
            y += 20;

            GUI.Label(new Rect(x, y, 200, 20), $"Score: {TurretDefensePlugin.CurrentScore}", statusStyle);
        }
    }
}
