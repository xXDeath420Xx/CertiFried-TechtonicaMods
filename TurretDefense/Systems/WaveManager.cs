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
        public int Fighters;
        public int Destroyers;
        public int Torpedoes;
        // Robot enemies
        public int RobotScouts;
        public int RobotInvaders;
        public int RobotCollectors;
        public int RobotGuardians; // Mini-boss
        public float SpawnDelay;
        public bool IsBossWave;
        public BossType? BossToSpawn; // Epic boss encounter

        public int TotalAliens => Fighters + Destroyers + Torpedoes + RobotScouts + RobotInvaders + RobotCollectors + RobotGuardians;
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
            // Wave 1-5: Tutorial waves (organic aliens only)
            new WaveComposition { Fighters = 3, Destroyers = 0, Torpedoes = 0, SpawnDelay = 1f },
            new WaveComposition { Fighters = 5, Destroyers = 0, Torpedoes = 0, SpawnDelay = 0.8f },
            new WaveComposition { Fighters = 4, Destroyers = 1, Torpedoes = 0, SpawnDelay = 0.8f },
            new WaveComposition { Fighters = 6, Destroyers = 1, Torpedoes = 1, SpawnDelay = 0.7f },
            new WaveComposition { Fighters = 5, Destroyers = 2, Torpedoes = 2, SpawnDelay = 0.6f, IsBossWave = true, BossToSpawn = BossType.Overlord },

            // Wave 6-10: Robot scouts introduced
            new WaveComposition { Fighters = 6, Destroyers = 2, Torpedoes = 2, RobotScouts = 3, SpawnDelay = 0.5f },
            new WaveComposition { Fighters = 5, Destroyers = 3, Torpedoes = 3, RobotScouts = 4, SpawnDelay = 0.5f },
            new WaveComposition { Fighters = 8, Destroyers = 2, Torpedoes = 4, RobotScouts = 5, RobotCollectors = 2, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 6, Destroyers = 4, Torpedoes = 3, RobotScouts = 6, RobotInvaders = 1, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 8, Destroyers = 5, Torpedoes = 5, RobotScouts = 4, RobotInvaders = 2, RobotGuardians = 1, SpawnDelay = 0.3f, IsBossWave = true, BossToSpawn = BossType.Behemoth },

            // Wave 11-15: Full robot invasion
            new WaveComposition { Fighters = 10, Destroyers = 4, Torpedoes = 4, RobotScouts = 8, RobotInvaders = 2, SpawnDelay = 0.4f },
            new WaveComposition { Fighters = 8, Destroyers = 5, Torpedoes = 6, RobotScouts = 10, RobotInvaders = 3, RobotCollectors = 4, SpawnDelay = 0.35f },
            new WaveComposition { Fighters = 12, Destroyers = 6, Torpedoes = 5, RobotScouts = 8, RobotInvaders = 4, SpawnDelay = 0.3f },
            new WaveComposition { Fighters = 10, Destroyers = 8, Torpedoes = 6, RobotScouts = 12, RobotInvaders = 4, RobotCollectors = 5, SpawnDelay = 0.25f },
            new WaveComposition { Fighters = 12, Destroyers = 8, Torpedoes = 8, RobotScouts = 10, RobotInvaders = 5, RobotGuardians = 2, SpawnDelay = 0.2f, IsBossWave = true, BossToSpawn = BossType.Dreadnought },

            // Wave 16-20: Hivemind invasion
            new WaveComposition { Fighters = 15, Destroyers = 8, Torpedoes = 6, RobotScouts = 12, RobotInvaders = 4, SpawnDelay = 0.25f },
            new WaveComposition { Fighters = 12, Destroyers = 10, Torpedoes = 8, RobotScouts = 14, RobotInvaders = 5, RobotCollectors = 6, SpawnDelay = 0.2f },
            new WaveComposition { Fighters = 18, Destroyers = 10, Torpedoes = 8, RobotScouts = 15, RobotInvaders = 6, SpawnDelay = 0.2f },
            new WaveComposition { Fighters = 15, Destroyers = 12, Torpedoes = 10, RobotScouts = 16, RobotInvaders = 6, RobotCollectors = 8, SpawnDelay = 0.15f },
            new WaveComposition { Fighters = 20, Destroyers = 12, Torpedoes = 12, RobotScouts = 15, RobotInvaders = 8, RobotGuardians = 3, SpawnDelay = 0.15f, IsBossWave = true, BossToSpawn = BossType.Hivemind },

            // Wave 21-25: Harvester assault
            new WaveComposition { Fighters = 20, Destroyers = 12, Torpedoes = 10, RobotScouts = 18, RobotInvaders = 8, SpawnDelay = 0.15f },
            new WaveComposition { Fighters = 18, Destroyers = 14, Torpedoes = 12, RobotScouts = 20, RobotInvaders = 10, RobotCollectors = 10, SpawnDelay = 0.12f },
            new WaveComposition { Fighters = 22, Destroyers = 14, Torpedoes = 12, RobotScouts = 20, RobotInvaders = 10, SpawnDelay = 0.12f },
            new WaveComposition { Fighters = 20, Destroyers = 16, Torpedoes = 14, RobotScouts = 22, RobotInvaders = 12, RobotCollectors = 12, SpawnDelay = 0.1f },
            new WaveComposition { Fighters = 25, Destroyers = 16, Torpedoes = 15, RobotScouts = 20, RobotInvaders = 12, RobotGuardians = 4, SpawnDelay = 0.1f, IsBossWave = true, BossToSpawn = BossType.Harvester },
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
            int totalAliens = Mathf.RoundToInt(baseCount * waveMultiplier);

            bool isBoss = wave % 5 == 0;

            // As waves progress, robot enemies become more common
            float robotRatio = Mathf.Min(0.5f, (wave - 10) * 0.03f); // Max 50% robots after wave 26
            int robotCount = Mathf.RoundToInt(totalAliens * robotRatio);
            int organicCount = totalAliens - robotCount;

            return new WaveComposition
            {
                Fighters = Mathf.RoundToInt(organicCount * 0.5f),
                Destroyers = Mathf.RoundToInt(organicCount * 0.3f),
                Torpedoes = Mathf.RoundToInt(organicCount * 0.2f),
                RobotScouts = Mathf.RoundToInt(robotCount * 0.5f),
                RobotInvaders = Mathf.RoundToInt(robotCount * 0.3f),
                RobotCollectors = Mathf.RoundToInt(robotCount * 0.15f),
                RobotGuardians = isBoss ? Mathf.Max(1, wave / 10) : 0, // More guardians in later boss waves
                SpawnDelay = Mathf.Max(0.15f, 0.4f - (wave * 0.015f)),
                IsBossWave = isBoss
            };
        }

        private IEnumerator SpawnWave(WaveComposition composition)
        {
            // Create spawn order
            List<string> spawnOrder = new List<string>();

            // Add fighters (40% regular, 35% green/tanky, 25% white/fast)
            for (int i = 0; i < composition.Fighters; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "AlienFighter" :
                                roll < 0.75f ? "AlienFighterGreen" : "AlienFighterWhite";
                spawnOrder.Add(variant);
            }

            // Add destroyers (40% regular, 35% green/tanky, 25% white/fast)
            for (int i = 0; i < composition.Destroyers; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "AlienDestroyer" :
                                roll < 0.75f ? "AlienDestroyerGreen" : "AlienDestroyerWhite";
                spawnOrder.Add(variant);
            }

            // Add torpedoes (40% regular, 30% green, 30% white)
            for (int i = 0; i < composition.Torpedoes; i++)
            {
                float roll = UnityEngine.Random.value;
                string variant = roll < 0.4f ? "BioTorpedo" :
                                roll < 0.7f ? "BioTorpedoGreen" : "BioTorpedoWhite";
                spawnOrder.Add(variant);
            }

            // ========== ROBOT ENEMIES (from Sci_fi_Drones) ==========

            // Add robot scouts (various color variants)
            string[] scoutVariants = { "Robot_Scout", "Robot_Scout_HyperX", "Robot_Scout_HyperX_Red", "Robot_Scout_Rockie" };
            for (int i = 0; i < composition.RobotScouts; i++)
            {
                string variant = scoutVariants[UnityEngine.Random.Range(0, scoutVariants.Length)];
                spawnOrder.Add(variant);
            }

            // Add robot invaders (elite enemy)
            for (int i = 0; i < composition.RobotInvaders; i++)
            {
                spawnOrder.Add("Robot_Invader");
            }

            // Add robot collectors
            for (int i = 0; i < composition.RobotCollectors; i++)
            {
                spawnOrder.Add("Robot_Collector");
            }

            // Add robot guardians (BOSS - spawn last, don't shuffle them)
            List<string> guardians = new List<string>();
            for (int i = 0; i < composition.RobotGuardians; i++)
            {
                guardians.Add("Robot_Guardian");
            }

            // Shuffle spawn order (but bosses spawn last)
            ShuffleList(spawnOrder);

            // Add guardians (bosses) at the end so they spawn last
            spawnOrder.AddRange(guardians);

            TurretDefensePlugin.LogDebug($"Spawning {spawnOrder.Count} aliens ({guardians.Count} mini-bosses) for wave {CurrentWave}");

            // Spawn with delays
            spawnIndex = 0;
            foreach (var alienType in spawnOrder)
            {
                // Get spawn position
                Vector3 spawnPos = TurretDefensePlugin.GetSpawnPosition(spawnIndex, UnityEngine.Random.Range(50f, 80f));
                spawnIndex++;

                // Spawn alien
                TurretDefensePlugin.SpawnAlien(alienType, spawnPos, CurrentWave);

                yield return new WaitForSeconds(composition.SpawnDelay);
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
