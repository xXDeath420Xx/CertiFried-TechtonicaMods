using System;
using System.Collections;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Floating health bar that follows an enemy
    /// </summary>
    public class FloatingHealthBar : MonoBehaviour
    {
        private AlienShipController alienTarget;
        private GroundEnemyController groundTarget;
        private AlienHiveController hiveTarget;
        private BossController bossTarget;
        private Camera mainCamera;

        // UI textures
        private Texture2D backgroundTex;
        private Texture2D healthTex;
        private Texture2D armorTex;
        private Texture2D shieldTex;

        // Settings (base values at 1080p)
        private float baseBarWidth = 80f;
        private float baseBarHeight = 8f;
        private float barWidth = 80f;
        private float barHeight = 8f;
        private float verticalOffset = 3f;

        // Resolution scaling
        private const float BaseScreenHeight = 1080f;

        // Animation
        private float displayedHealth;
        private float displayedShield;
        private float smoothSpeed = 5f;

        // Target abstraction
        private float TargetHealth => alienTarget?.Health ?? groundTarget?.Health ?? hiveTarget?.Health ?? bossTarget?.Health ?? 0f;
        private float TargetMaxHealth => alienTarget?.MaxHealth ?? groundTarget?.MaxHealth ?? hiveTarget?.MaxHealth ?? bossTarget?.MaxHealth ?? 1f;
        private float TargetArmor => alienTarget?.Armor ?? groundTarget?.Armor ?? hiveTarget?.Armor ?? bossTarget?.Armor ?? 0f;
        private float TargetShield => bossTarget?.Shield ?? 0f;
        private float TargetMaxShield => bossTarget?.MaxShield ?? 0f;
        private bool TargetIsAlive => alienTarget?.IsAlive ?? groundTarget?.IsAlive ?? hiveTarget?.IsAlive ?? bossTarget?.IsAlive ?? false;
        private Transform TargetTransform => alienTarget?.transform ?? groundTarget?.transform ?? hiveTarget?.transform ?? bossTarget?.transform;
        private string TargetName => alienTarget?.ShipType ?? groundTarget?.EnemyName ?? hiveTarget?.HiveName ?? bossTarget?.BossName ?? "";
        private bool IsLargeEnemy => TargetName.Contains("Destroyer") || TargetName.Contains("Guardian") ||
                                     TargetName.Contains("Chomper") || hiveTarget != null || bossTarget != null;

        private void ApplyResolutionScaling()
        {
            float scale = Mathf.Clamp(Screen.height / BaseScreenHeight, 0.75f, 2f);
            barWidth = baseBarWidth * scale;
            barHeight = baseBarHeight * scale;
        }

        public void Initialize(AlienShipController alien)
        {
            alienTarget = alien;
            mainCamera = Camera.main;
            displayedHealth = alien.Health;
            baseBarWidth = 80f;
            baseBarHeight = 8f;
            ApplyResolutionScaling();
            CreateTextures();
        }

        public void Initialize(GroundEnemyController ground)
        {
            groundTarget = ground;
            mainCamera = Camera.main;
            displayedHealth = ground.Health;
            baseBarWidth = 80f;
            baseBarHeight = 8f;
            ApplyResolutionScaling();
            CreateTextures();
        }

        public void Initialize(AlienHiveController hive)
        {
            hiveTarget = hive;
            mainCamera = Camera.main;
            displayedHealth = hive.Health;
            baseBarWidth = 120f;
            baseBarHeight = 12f;
            verticalOffset = 5f;
            ApplyResolutionScaling();
            CreateTextures();
        }

        public void Initialize(BossController boss)
        {
            bossTarget = boss;
            mainCamera = Camera.main;
            displayedHealth = boss.Health;
            displayedShield = boss.Shield;
            baseBarWidth = 200f;
            baseBarHeight = 16f;
            verticalOffset = 10f;
            ApplyResolutionScaling();
            CreateTextures();
        }

        private void CreateTextures()
        {
            backgroundTex = MakeTexture(2, 2, new Color(0.1f, 0.1f, 0.1f, 0.8f));
            healthTex = MakeTexture(2, 2, new Color(0.8f, 0.2f, 0.2f, 1f));
            armorTex = MakeTexture(2, 2, new Color(0.4f, 0.4f, 0.5f, 1f));
            shieldTex = MakeTexture(2, 2, new Color(0.3f, 0.5f, 1f, 0.8f));
        }

        void Update()
        {
            if (!TargetIsAlive || TargetTransform == null)
            {
                Destroy(gameObject);
                return;
            }

            // Smooth health display
            displayedHealth = Mathf.Lerp(displayedHealth, TargetHealth, Time.deltaTime * smoothSpeed);

            // Smooth shield display (for bosses)
            if (bossTarget != null)
            {
                displayedShield = Mathf.Lerp(displayedShield, TargetShield, Time.deltaTime * smoothSpeed);
            }
        }

        void OnGUI()
        {
            if (TargetTransform == null || mainCamera == null) return;

            // Don't show if behind camera
            Vector3 screenPos = mainCamera.WorldToScreenPoint(TargetTransform.position + Vector3.up * verticalOffset);
            if (screenPos.z < 0) return;

            // Convert to GUI coordinates (Y is flipped)
            float x = screenPos.x - barWidth / 2;
            float y = Screen.height - screenPos.y - barHeight / 2;

            // Distance-based scaling
            float distance = Vector3.Distance(mainCamera.transform.position, TargetTransform.position);
            float scale = Mathf.Clamp(1f - (distance - 20f) / 60f, 0.5f, 1f);
            float scaledWidth = barWidth * scale;
            float scaledHeight = barHeight * scale;
            x = screenPos.x - scaledWidth / 2;

            // Don't draw if too far or too small
            if (scale < 0.5f) return;

            // Background
            GUI.DrawTexture(new Rect(x - 1, y - 1, scaledWidth + 2, scaledHeight + 2), backgroundTex);

            // Armor bar (if any)
            if (TargetArmor > 0)
            {
                float armorWidth = Mathf.Min(scaledWidth * 0.3f, TargetArmor);
                GUI.DrawTexture(new Rect(x, y, armorWidth, scaledHeight), armorTex);
            }

            // Health bar
            float healthPercent = displayedHealth / TargetMaxHealth;
            Color healthColor = GetHealthColor(healthPercent);
            healthTex = MakeTexture(2, 2, healthColor);
            GUI.DrawTexture(new Rect(x, y, scaledWidth * healthPercent, scaledHeight), healthTex);

            // Shield bar (above health for bosses)
            if (bossTarget != null && TargetMaxShield > 0)
            {
                float shieldPercent = displayedShield / TargetMaxShield;
                if (shieldPercent > 0)
                {
                    float shieldY = y - scaledHeight - 4;
                    GUI.DrawTexture(new Rect(x - 1, shieldY - 1, scaledWidth + 2, scaledHeight + 2), backgroundTex);
                    GUI.DrawTexture(new Rect(x, shieldY, scaledWidth * shieldPercent, scaledHeight), shieldTex);
                }
            }

            // Health text for large enemies and hives
            if (IsLargeEnemy && scale > 0.7f)
            {
                GUIStyle style = new GUIStyle();
                style.normal.textColor = Color.white;
                style.fontSize = Mathf.RoundToInt(10 * scale);
                style.alignment = TextAnchor.MiddleCenter;

                string healthText = $"{Mathf.RoundToInt(displayedHealth)}/{Mathf.RoundToInt(TargetMaxHealth)}";
                GUI.Label(new Rect(x, y + scaledHeight + 2, scaledWidth, 15), healthText, style);
            }
        }

        private Color GetHealthColor(float percent)
        {
            if (percent > 0.6f)
                return new Color(0.3f, 0.8f, 0.3f, 1f); // Green
            else if (percent > 0.3f)
                return new Color(0.9f, 0.8f, 0.2f, 1f); // Yellow
            else
                return new Color(0.9f, 0.2f, 0.2f, 1f); // Red
        }

        private Texture2D MakeTexture(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = color;

            Texture2D tex = new Texture2D(width, height);
            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        void OnDestroy()
        {
            if (backgroundTex != null) UnityEngine.Object.Destroy(backgroundTex);
            if (healthTex != null) UnityEngine.Object.Destroy(healthTex);
            if (armorTex != null) UnityEngine.Object.Destroy(armorTex);
            if (shieldTex != null) UnityEngine.Object.Destroy(shieldTex);
        }
    }

    /// <summary>
    /// Floating damage number that pops up and fades
    /// </summary>
    public class DamageNumber : MonoBehaviour
    {
        private float damage;
        private bool isCritical;
        private Camera mainCamera;

        // Animation
        private float lifetime = 1.5f;
        private float elapsed = 0f;
        private Vector3 startPosition;
        private Vector3 velocity;

        // Style
        private GUIStyle style;
        private Color textColor;

        // Resolution scaling
        private const float BaseScreenHeight = 1080f;
        private int baseFontSize;
        private float uiScale;

        public void Initialize(float damage, bool isCritical)
        {
            this.damage = damage;
            this.isCritical = isCritical;
            mainCamera = Camera.main;
            startPosition = transform.position;

            // Resolution scaling
            uiScale = Mathf.Clamp(Screen.height / BaseScreenHeight, 0.75f, 2f);
            baseFontSize = isCritical ? 24 : 18;

            // Random upward velocity
            velocity = new Vector3(
                UnityEngine.Random.Range(-1f, 1f),
                UnityEngine.Random.Range(2f, 4f),
                UnityEngine.Random.Range(-1f, 1f)
            );

            // Style
            style = new GUIStyle();
            style.alignment = TextAnchor.MiddleCenter;
            style.fontStyle = isCritical ? FontStyle.Bold : FontStyle.Normal;
            style.fontSize = Mathf.RoundToInt(baseFontSize * uiScale);

            textColor = isCritical ? new Color(1f, 0.8f, 0.2f) : new Color(1f, 0.4f, 0.4f);
        }

        void Update()
        {
            elapsed += Time.deltaTime;

            if (elapsed >= lifetime)
            {
                Destroy(gameObject);
                return;
            }

            // Move upward
            transform.position += velocity * Time.deltaTime;
            velocity *= 0.95f; // Slow down
        }

        void OnGUI()
        {
            if (mainCamera == null) return;

            Vector3 screenPos = mainCamera.WorldToScreenPoint(transform.position);
            if (screenPos.z < 0) return;

            float y = Screen.height - screenPos.y;

            // Fade out
            float alpha = 1f - (elapsed / lifetime);
            textColor.a = alpha;
            style.normal.textColor = textColor;

            // Scale effect for crits
            float scale = 1f;
            if (isCritical && elapsed < 0.2f)
            {
                scale = 1f + (0.2f - elapsed) * 2f;
            }
            style.fontSize = Mathf.RoundToInt(baseFontSize * uiScale * scale);

            // Label dimensions scaled for resolution
            float labelWidth = 100 * uiScale;
            float labelHeight = 30 * uiScale;

            // Shadow
            GUI.color = new Color(0, 0, 0, alpha * 0.5f);
            GUI.Label(new Rect(screenPos.x - labelWidth / 2 + 1, y - labelHeight / 2 + 1, labelWidth, labelHeight), FormatDamage(), style);

            // Text
            GUI.color = Color.white;
            GUI.Label(new Rect(screenPos.x - labelWidth / 2, y - labelHeight / 2, labelWidth, labelHeight), FormatDamage(), style);
        }

        private string FormatDamage()
        {
            if (isCritical)
                return $"CRIT! {damage:F0}";
            return $"{damage:F0}";
        }
    }

    /// <summary>
    /// Turret range indicator when placing/selecting turrets
    /// </summary>
    public class TurretRangeIndicator : MonoBehaviour
    {
        private float range;
        private LineRenderer rangeCircle;
        private int segments = 64;

        public void Initialize(float range)
        {
            this.range = range;
            CreateRangeCircle();
        }

        private void CreateRangeCircle()
        {
            var circleObj = new GameObject("RangeCircle");
            circleObj.transform.SetParent(transform);
            circleObj.transform.localPosition = Vector3.up * 0.1f;

            rangeCircle = circleObj.AddComponent<LineRenderer>();
            rangeCircle.positionCount = segments + 1;
            rangeCircle.useWorldSpace = false;
            rangeCircle.startWidth = 0.1f;
            rangeCircle.endWidth = 0.1f;
            rangeCircle.material = TurretDefensePlugin.GetEffectMaterial(new Color(0, 1, 0, 0.5f));
            rangeCircle.startColor = new Color(0, 1, 0, 0.5f);
            rangeCircle.endColor = new Color(0, 1, 0, 0.5f);
            rangeCircle.loop = true;

            // Draw circle
            float angleStep = 360f / segments;
            for (int i = 0; i <= segments; i++)
            {
                float angle = i * angleStep * Mathf.Deg2Rad;
                float x = Mathf.Cos(angle) * range;
                float z = Mathf.Sin(angle) * range;
                rangeCircle.SetPosition(i, new Vector3(x, 0, z));
            }
        }

        public void SetColor(Color color)
        {
            if (rangeCircle != null)
            {
                rangeCircle.startColor = color;
                rangeCircle.endColor = color;
            }
        }

        public void UpdateRange(float newRange)
        {
            range = newRange;
            if (rangeCircle != null)
            {
                float angleStep = 360f / segments;
                for (int i = 0; i <= segments; i++)
                {
                    float angle = i * angleStep * Mathf.Deg2Rad;
                    float x = Mathf.Cos(angle) * range;
                    float z = Mathf.Sin(angle) * range;
                    rangeCircle.SetPosition(i, new Vector3(x, 0, z));
                }
            }
        }
    }

    /// <summary>
    /// Combat stats display UI
    /// </summary>
    public class CombatStatsUI : MonoBehaviour
    {
        private GUIStyle headerStyle;
        private GUIStyle statStyle;
        private GUIStyle valueStyle;

        private bool isVisible = true;

        // Resolution scaling - base sizes at 1080p
        private const float BaseScreenHeight = 1080f;
        private float uiScale = 1f;

        void Awake()
        {
            UpdateStyles();
        }

        private void UpdateStyles()
        {
            // Scale based on screen height (1080p is baseline)
            uiScale = Screen.height / BaseScreenHeight;
            uiScale = Mathf.Clamp(uiScale, 0.75f, 2f);

            headerStyle = new GUIStyle();
            headerStyle.fontSize = Mathf.RoundToInt(18 * uiScale);
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(1f, 0.8f, 0.2f);

            statStyle = new GUIStyle();
            statStyle.fontSize = Mathf.RoundToInt(14 * uiScale);
            statStyle.normal.textColor = Color.white;

            valueStyle = new GUIStyle();
            valueStyle.fontSize = Mathf.RoundToInt(14 * uiScale);
            valueStyle.alignment = TextAnchor.MiddleRight;
            valueStyle.normal.textColor = new Color(0.7f, 1f, 0.7f);
        }

        void Update()
        {
            // Toggle with Tab
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                isVisible = !isVisible;
            }
        }

        void OnGUI()
        {
            if (!isVisible) return;

            // Update scale if resolution changed
            float currentScale = Screen.height / BaseScreenHeight;
            if (Mathf.Abs(currentScale - uiScale) > 0.01f)
            {
                UpdateStyles();
            }

            float x = 10 * uiScale;
            float y = 100 * uiScale;
            float width = 180 * uiScale;
            float height = 200 * uiScale;

            // Background
            GUI.color = new Color(0, 0, 0, 0.7f);
            GUI.Box(new Rect(x - 5, y - 5, width + 10, height + 10), "");
            GUI.color = Color.white;

            float lineHeight = 20 * uiScale;
            float lineSpacing = 22 * uiScale;

            // Header
            GUI.Label(new Rect(x, y, width, 25 * uiScale), "DEFENSE STATUS", headerStyle);
            y += 30 * uiScale;

            // Active turrets
            GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Turrets:", statStyle);
            GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                TurretDefensePlugin.ActiveTurrets.Count.ToString(), valueStyle);
            y += lineSpacing;

            // Active enemies (ships + ground)
            int totalEnemies = TurretDefensePlugin.ActiveAliens.Count + TurretDefensePlugin.ActiveGroundEnemies.Count;
            GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Enemies:", statStyle);
            GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                totalEnemies.ToString(), valueStyle);
            y += lineSpacing;

            // Active hives
            if (TurretDefensePlugin.ActiveHives.Count > 0)
            {
                GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Hives:", statStyle);
                GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                    TurretDefensePlugin.ActiveHives.Count.ToString(), valueStyle);
                y += lineSpacing;
            }

            // Kills
            GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Total Kills:", statStyle);
            GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                TurretDefensePlugin.TotalKills.ToString(), valueStyle);
            y += lineSpacing;

            // Score
            GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Score:", statStyle);
            GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                TurretDefensePlugin.CurrentScore.ToString(), valueStyle);
            y += 30 * uiScale;

            // Wave info
            var wave = TurretDefensePlugin.WaveSystem;
            if (wave != null)
            {
                GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Current Wave:", statStyle);
                GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                    wave.CurrentWave.ToString(), valueStyle);
                y += lineSpacing;

                if (wave.IsWaveActive)
                {
                    GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Remaining:", statStyle);
                    GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                        wave.AliensRemaining.ToString(), valueStyle);
                }
                else
                {
                    GUI.Label(new Rect(x, y, width * 0.6f, lineHeight), "Next Wave:", statStyle);
                    GUI.Label(new Rect(x + width * 0.6f, y, width * 0.4f, lineHeight),
                        $"{wave.TimeUntilNextWave:F0}s", valueStyle);
                }
            }

            // Controls hint
            y = height + 90 * uiScale;
            GUIStyle hintStyle = new GUIStyle();
            hintStyle.fontSize = Mathf.RoundToInt(10 * uiScale);
            hintStyle.normal.textColor = new Color(0.6f, 0.6f, 0.6f);
            GUI.Label(new Rect(x, y, width, 15 * uiScale), "[Tab] Toggle Stats", hintStyle);
        }
    }
}
