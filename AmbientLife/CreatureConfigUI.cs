using System;
using System.Collections.Generic;
using UnityEngine;

namespace AmbientLife
{
    /// <summary>
    /// In-game UI for configuring creature spawning - toggles and density per creature type
    /// </summary>
    public class CreatureConfigUI : MonoBehaviour
    {
        private bool showUI = false;
        private Rect windowRect = new Rect(20, 20, 400, 500);
        private Vector2 scrollPosition = Vector2.zero;

        // UI style cache
        private GUIStyle windowStyle;
        private GUIStyle headerStyle;
        private GUIStyle toggleStyle;
        private GUIStyle sliderLabelStyle;
        private GUIStyle buttonStyle;
        private bool stylesInitialized = false;

        // Keybind (F7 to avoid conflict with DevTools which uses F8)
        private KeyCode toggleKey = KeyCode.F7;

        private void Update()
        {
            // Toggle UI with keybind
            if (Input.GetKeyDown(toggleKey))
            {
                showUI = !showUI;
            }
        }

        private void OnGUI()
        {
            if (!showUI) return;

            InitStyles();

            windowRect = GUI.Window(9876, windowRect, DrawWindow, "Ambient Life Settings", windowStyle);
        }

        private void InitStyles()
        {
            if (stylesInitialized) return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.normal.background = MakeTex(2, 2, new Color(0.15f, 0.15f, 0.18f, 0.95f));
            windowStyle.padding = new RectOffset(10, 10, 25, 10);

            headerStyle = new GUIStyle(GUI.skin.label);
            headerStyle.fontSize = 14;
            headerStyle.fontStyle = FontStyle.Bold;
            headerStyle.normal.textColor = new Color(0.9f, 0.85f, 0.6f);

            toggleStyle = new GUIStyle(GUI.skin.toggle);
            toggleStyle.normal.textColor = Color.white;
            toggleStyle.fontSize = 12;

            sliderLabelStyle = new GUIStyle(GUI.skin.label);
            sliderLabelStyle.fontSize = 11;
            sliderLabelStyle.normal.textColor = new Color(0.8f, 0.8f, 0.8f);

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.3f, 0.35f));
            buttonStyle.hover.background = MakeTex(2, 2, new Color(0.4f, 0.4f, 0.45f));
            buttonStyle.normal.textColor = Color.white;

            stylesInitialized = true;
        }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = col;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void DrawWindow(int windowID)
        {
            GUILayout.BeginVertical();

            // Header
            GUILayout.Label($"Press {toggleKey} to toggle this window", sliderLabelStyle);
            GUILayout.Space(5);

            // Master controls
            GUILayout.Label("General Settings", headerStyle);
            AmbientLifePlugin.EnableAmbientLife.Value = GUILayout.Toggle(
                AmbientLifePlugin.EnableAmbientLife.Value,
                " Enable Ambient Life", toggleStyle);

            GUILayout.Space(5);

            // Max creatures slider
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Max Creatures: {AmbientLifePlugin.MaxCreatures.Value}", sliderLabelStyle, GUILayout.Width(120));
            AmbientLifePlugin.MaxCreatures.Value = (int)GUILayout.HorizontalSlider(
                AmbientLifePlugin.MaxCreatures.Value, 5, 100, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            // Spawn radius slider
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Spawn Radius: {AmbientLifePlugin.SpawnRadius.Value:F0}m", sliderLabelStyle, GUILayout.Width(120));
            AmbientLifePlugin.SpawnRadius.Value = GUILayout.HorizontalSlider(
                AmbientLifePlugin.SpawnRadius.Value, 20f, 100f, GUILayout.Width(200));
            GUILayout.EndHorizontal();

            GUILayout.Space(15);

            // Scrollable creature list
            GUILayout.Label("Creature Types", headerStyle);
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.Height(280));

            DrawCreatureCategory("Flying Insects", new[] {
                ("Butterflies", AmbientLifePlugin.EnableButterflies),
                ("Fireflies", AmbientLifePlugin.EnableFireflies)
            });

            DrawCreatureCategory("Beetles & Bugs", new[] {
                ("Flying Beetles", AmbientLifePlugin.EnableBeetles),
                ("Nightmare Beetles", AmbientLifePlugin.EnableNightmareBeetles)
            });

            DrawCreatureCategory("Spiders", new[] {
                ("Brown Spiders", AmbientLifePlugin.EnableBrownSpiders),
                ("Green Spiders", AmbientLifePlugin.EnableGreenSpiders),
                ("Black Spiders", AmbientLifePlugin.EnableBlackSpiders)
            });

            DrawCreatureCategory("Ground Animals", new[] {
                ("Dogs", AmbientLifePlugin.EnableDogs),
                ("Cats", AmbientLifePlugin.EnableCats),
                ("Chickens", AmbientLifePlugin.EnableChickens),
                ("Deer", AmbientLifePlugin.EnableDeer)
            });

            DrawCreatureCategory("Other", new[] {
                ("Wolves", AmbientLifePlugin.EnableWolves),
                ("Penguins", AmbientLifePlugin.EnablePenguins)
            });

            GUILayout.EndScrollView();

            GUILayout.Space(10);

            // Density per creature type
            GUILayout.Label("Spawn Weights (Relative Density)", headerStyle);

            DrawDensitySlider("Flying Insects", ref AmbientLifePlugin.DensityFlying);
            DrawDensitySlider("Ground Crawlers", ref AmbientLifePlugin.DensityCrawling);
            DrawDensitySlider("Ground Animals", ref AmbientLifePlugin.DensityAnimals);

            GUILayout.Space(10);

            // Status info
            GUILayout.BeginHorizontal();
            GUILayout.Label($"Active Creatures: {AmbientLifePlugin.ActiveCreatures.Count}", sliderLabelStyle);
            GUILayout.Label($"Loaded Prefabs: {CreatureAssetLoader.LoadedPrefabCount}", sliderLabelStyle);
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Buttons
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Reload Assets", buttonStyle))
            {
                CreatureAssetLoader.Unload();
                CreatureAssetLoader.Initialize();
            }
            if (GUILayout.Button("Clear All Creatures", buttonStyle))
            {
                AmbientLifePlugin.Instance.ClearAllCreatures();
            }
            if (GUILayout.Button("Close", buttonStyle))
            {
                showUI = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();

            // Make window draggable
            GUI.DragWindow(new Rect(0, 0, windowRect.width, 25));
        }

        private void DrawCreatureCategory(string categoryName, (string name, BepInEx.Configuration.ConfigEntry<bool> config)[] creatures)
        {
            GUILayout.BeginVertical(GUI.skin.box);
            GUILayout.Label(categoryName, sliderLabelStyle);

            foreach (var creature in creatures)
            {
                creature.config.Value = GUILayout.Toggle(creature.config.Value, $" {creature.name}", toggleStyle);
            }

            GUILayout.EndVertical();
            GUILayout.Space(5);
        }

        private void DrawDensitySlider(string label, ref float density)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label($"{label}: {density:P0}", sliderLabelStyle, GUILayout.Width(150));
            density = GUILayout.HorizontalSlider(density, 0f, 1f, GUILayout.Width(170));
            GUILayout.EndHorizontal();
        }
    }
}
