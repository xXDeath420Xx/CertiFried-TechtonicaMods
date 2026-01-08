using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEngine;

namespace TurretDefense
{
    /// <summary>
    /// Manages icon loading and caching for TurretDefense mod items.
    /// Supports loading from asset bundles, embedded resources, or procedural generation.
    /// </summary>
    public static class IconManager
    {
        // Icon cache to avoid reloading
        private static Dictionary<string, Sprite> iconCache = new Dictionary<string, Sprite>();

        // Bundle containing icons (loaded on demand)
        private static AssetBundle iconBundle;
        private static bool bundleLoadAttempted = false;

        // Icon categories for organization
        public static class Categories
        {
            public const string Turrets = "turrets";
            public const string Enemies = "enemies";
            public const string Bosses = "bosses";
            public const string Artillery = "artillery";
            public const string Resources = "resources";
            public const string Structures = "structures";
        }

        /// <summary>
        /// Get an icon by name, loading from cache if available.
        /// </summary>
        public static Sprite GetIcon(string iconName, string category = null)
        {
            string cacheKey = category != null ? $"{category}/{iconName}" : iconName;

            // Check cache first
            if (iconCache.TryGetValue(cacheKey, out var cached))
            {
                return cached;
            }

            // Try to load from bundle
            Sprite icon = LoadFromBundle(iconName, category);
            if (icon != null)
            {
                iconCache[cacheKey] = icon;
                return icon;
            }

            // Generate procedural icon as fallback
            icon = GenerateProceduralIcon(iconName, category);
            if (icon != null)
            {
                iconCache[cacheKey] = icon;
            }

            return icon;
        }

        /// <summary>
        /// Load icon from asset bundle.
        /// </summary>
        private static Sprite LoadFromBundle(string iconName, string category)
        {
            if (!bundleLoadAttempted)
            {
                LoadIconBundle();
            }

            if (iconBundle == null) return null;

            try
            {
                // Try different path patterns
                string[] searchPaths = {
                    iconName,
                    $"icon_{iconName}",
                    $"{category}_{iconName}",
                    $"icons/{iconName}",
                    $"icons/{category}/{iconName}"
                };

                foreach (var path in searchPaths)
                {
                    var sprite = iconBundle.LoadAsset<Sprite>(path);
                    if (sprite != null) return sprite;

                    var texture = iconBundle.LoadAsset<Texture2D>(path);
                    if (texture != null)
                    {
                        return CreateSpriteFromTexture(texture);
                    }
                }
            }
            catch (Exception ex)
            {
                TurretDefensePlugin.LogDebug($"Failed to load icon {iconName}: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Load the icon bundle from mod folder.
        /// </summary>
        private static void LoadIconBundle()
        {
            bundleLoadAttempted = true;

            string bundlePath = Path.Combine(TurretDefensePlugin.PluginPath, "Bundles", "mod_icons");
            if (File.Exists(bundlePath))
            {
                try
                {
                    iconBundle = AssetBundle.LoadFromFile(bundlePath);
                    if (iconBundle != null)
                    {
                        TurretDefensePlugin.Log.LogInfo($"Loaded icon bundle with {iconBundle.GetAllAssetNames().Length} assets");
                    }
                }
                catch (Exception ex)
                {
                    TurretDefensePlugin.Log.LogWarning($"Failed to load icon bundle: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Generate a procedural icon when no bundle icon is available.
        /// </summary>
        private static Sprite GenerateProceduralIcon(string iconName, string category)
        {
            int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);

            // Base color based on category
            Color baseColor = GetCategoryColor(category);
            Color accentColor = GetAccentColor(iconName);

            // Fill background with gradient
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float gradientT = y / (float)size;
                    Color bgColor = Color.Lerp(baseColor * 0.5f, baseColor, gradientT);

                    // Add border
                    bool isBorder = x < 4 || x >= size - 4 || y < 4 || y >= size - 4;
                    if (isBorder)
                    {
                        bgColor = accentColor;
                    }

                    // Add corner cuts for sci-fi look
                    int cornerSize = 12;
                    if ((x < cornerSize && y < cornerSize - x) ||
                        (x >= size - cornerSize && y < x - (size - cornerSize - 1)) ||
                        (x < cornerSize && y >= size - cornerSize + x) ||
                        (x >= size - cornerSize && y >= size - (x - (size - cornerSize))))
                    {
                        bgColor = Color.clear;
                    }

                    texture.SetPixel(x, y, bgColor);
                }
            }

            // Draw category-specific symbol
            DrawCategorySymbol(texture, category, accentColor);

            texture.Apply();

            return Sprite.Create(
                texture,
                new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        private static Color GetCategoryColor(string category)
        {
            return category switch
            {
                Categories.Turrets => new Color(0.2f, 0.4f, 0.6f),      // Blue
                Categories.Enemies => new Color(0.6f, 0.2f, 0.2f),     // Red
                Categories.Bosses => new Color(0.5f, 0.1f, 0.5f),      // Purple
                Categories.Artillery => new Color(0.4f, 0.5f, 0.3f),   // Olive
                Categories.Resources => new Color(0.3f, 0.5f, 0.3f),   // Green
                Categories.Structures => new Color(0.4f, 0.4f, 0.5f),  // Steel blue
                _ => new Color(0.3f, 0.3f, 0.35f)                      // Gray
            };
        }

        private static Color GetAccentColor(string iconName)
        {
            // Create consistent accent color from name hash
            int hash = iconName.GetHashCode();
            float hue = (hash & 0xFF) / 255f;
            return Color.HSVToRGB(hue, 0.6f, 0.9f);
        }

        private static void DrawCategorySymbol(Texture2D texture, string category, Color color)
        {
            int center = texture.width / 2;
            int symbolSize = texture.width / 3;

            switch (category)
            {
                case Categories.Turrets:
                    // Draw turret shape (barrel + base)
                    DrawRect(texture, center - 5, center - symbolSize / 2, 10, symbolSize, color);
                    DrawRect(texture, center - symbolSize / 3, center - 10, symbolSize * 2 / 3, 20, color);
                    break;

                case Categories.Enemies:
                    // Draw skull/alien shape
                    DrawCircle(texture, center, center, symbolSize / 2, color);
                    DrawCircle(texture, center - 10, center + 5, 6, Color.black);
                    DrawCircle(texture, center + 10, center + 5, 6, Color.black);
                    break;

                case Categories.Bosses:
                    // Draw crown/star
                    for (int i = 0; i < 5; i++)
                    {
                        float angle = i * 72f * Mathf.Deg2Rad - Mathf.PI / 2;
                        int px = center + (int)(Mathf.Cos(angle) * symbolSize / 2);
                        int py = center + (int)(Mathf.Sin(angle) * symbolSize / 2);
                        DrawLine(texture, center, center, px, py, color, 3);
                    }
                    break;

                case Categories.Artillery:
                    // Draw cannon shape
                    DrawRect(texture, center - symbolSize / 2, center - 8, symbolSize, 16, color);
                    DrawCircle(texture, center - symbolSize / 3, center, 12, color);
                    break;

                case Categories.Resources:
                    // Draw cube/crystal
                    int cubeSize = symbolSize / 2;
                    DrawRect(texture, center - cubeSize / 2, center - cubeSize / 2, cubeSize, cubeSize, color);
                    break;

                case Categories.Structures:
                    // Draw building shape
                    DrawRect(texture, center - symbolSize / 3, center - symbolSize / 3, symbolSize * 2 / 3, symbolSize * 2 / 3, color);
                    DrawRect(texture, center - 6, center + symbolSize / 6, 12, symbolSize / 3, Color.black);
                    break;

                default:
                    // Draw question mark
                    DrawCircle(texture, center, center, symbolSize / 3, color);
                    break;
            }
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color)
        {
            for (int py = y; py < y + height && py < texture.height; py++)
            {
                for (int px = x; px < x + width && px < texture.width; px++)
                {
                    if (px >= 0 && py >= 0)
                    {
                        texture.SetPixel(px, py, color);
                    }
                }
            }
        }

        private static void DrawCircle(Texture2D texture, int cx, int cy, int radius, Color color)
        {
            for (int y = cy - radius; y <= cy + radius; y++)
            {
                for (int x = cx - radius; x <= cx + radius; x++)
                {
                    if (x >= 0 && x < texture.width && y >= 0 && y < texture.height)
                    {
                        if ((x - cx) * (x - cx) + (y - cy) * (y - cy) <= radius * radius)
                        {
                            texture.SetPixel(x, y, color);
                        }
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, int x1, int y1, int x2, int y2, Color color, int thickness)
        {
            int dx = Mathf.Abs(x2 - x1);
            int dy = Mathf.Abs(y2 - y1);
            int sx = x1 < x2 ? 1 : -1;
            int sy = y1 < y2 ? 1 : -1;
            int err = dx - dy;

            while (true)
            {
                DrawCircle(texture, x1, y1, thickness / 2, color);

                if (x1 == x2 && y1 == y2) break;

                int e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x1 += sx;
                }
                if (e2 < dx)
                {
                    err += dx;
                    y1 += sy;
                }
            }
        }

        private static Sprite CreateSpriteFromTexture(Texture2D texture)
        {
            return Sprite.Create(
                texture,
                new Rect(0, 0, texture.width, texture.height),
                new Vector2(0.5f, 0.5f),
                100f
            );
        }

        /// <summary>
        /// Preload common icons for faster access.
        /// </summary>
        public static void PreloadCommonIcons()
        {
            // Turret icons
            string[] turretNames = { "cannon", "mg", "missile", "beamlaser", "aoe", "support", "nuke", "flamethrower" };
            foreach (var name in turretNames)
            {
                GetIcon(name, Categories.Turrets);
            }

            // Enemy icons
            string[] enemyNames = { "fighter", "destroyer", "torpedo", "chomper", "spitter", "mimic", "arachnid" };
            foreach (var name in enemyNames)
            {
                GetIcon(name, Categories.Enemies);
            }

            // Resource icons
            string[] resourceNames = { "alien_alloy", "alien_core" };
            foreach (var name in resourceNames)
            {
                GetIcon(name, Categories.Resources);
            }

            TurretDefensePlugin.Log.LogInfo($"Preloaded {iconCache.Count} icons");
        }

        /// <summary>
        /// Clear icon cache to free memory.
        /// </summary>
        public static void ClearCache()
        {
            iconCache.Clear();
        }

        /// <summary>
        /// Unload icon bundle.
        /// </summary>
        public static void Cleanup()
        {
            ClearCache();
            if (iconBundle != null)
            {
                iconBundle.Unload(true);
                iconBundle = null;
            }
            bundleLoadAttempted = false;
        }
    }
}
