using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using UnityEngine;

namespace SoundSampler
{
    /// <summary>
    /// SoundSampler_Patched - Dev utility for testing and sampling game sounds.
    /// Press F12 to open the sound sampler GUI.
    ///
    /// ORIGINAL AUTHOR: Equinox
    /// ORIGINAL MOD: https://thunderstore.io/c/techtonica/p/Equinox/SoundSampler/
    /// LICENSE: Not specified (original)
    ///
    /// PATCHED BY: CertiFried
    /// PATCH REASON: Removed deprecated ModUtils.LoadTexture2DFromFile dependency
    /// CHANGES: See CHANGELOG.md for full list of changes
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class SoundSamplerPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.SoundSampler_Patched";
        private const string PluginName = "SoundSampler_Patched";
        private const string VersionString = "1.1.0";

        public static ManualLogSource Log;

        // Configuration
        public static ConfigEntry<KeyCode> ToggleKey;

        // GUI state
        private bool showGUI = false;
        private Vector2 scrollPosition = Vector2.zero;
        private string searchFilter = "";
        private List<SoundEntry> soundEntries = new List<SoundEntry>();
        private bool initialized = false;

        private struct SoundEntry
        {
            public string name;
            public string category;
            public FieldInfo field;
            public object source;
        }

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");

            ToggleKey = Config.Bind("General", "Toggle Key", KeyCode.F12,
                "Key to toggle the sound sampler GUI");

            Logger.LogInfo($"{PluginName} v{VersionString} loaded! Press {ToggleKey.Value} to toggle.");
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey.Value))
            {
                showGUI = !showGUI;
                if (showGUI && !initialized)
                {
                    InitializeSoundList();
                }
            }
        }

        private void InitializeSoundList()
        {
            soundEntries.Clear();

            try
            {
                // Get sounds from Player.instance.audio
                if (Player.instance?.audio != null)
                {
                    var playerAudio = Player.instance.audio;
                    CollectSoundsFromObject(playerAudio, "Player");
                }

                // Get sounds from UIManager
                if (UIManager.instance != null)
                {
                    var uiAudio = typeof(UIManager).GetField("audio", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                    if (uiAudio != null)
                    {
                        var audioObj = uiAudio.GetValue(UIManager.instance);
                        if (audioObj != null)
                        {
                            CollectSoundsFromObject(audioObj, "UI");
                        }
                    }
                }

                Log.LogInfo($"Found {soundEntries.Count} sounds");
                initialized = true;
            }
            catch (Exception ex)
            {
                Log.LogError($"Error initializing sound list: {ex.Message}");
            }
        }

        private void CollectSoundsFromObject(object obj, string category)
        {
            if (obj == null) return;

            var type = obj.GetType();
            var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);

            foreach (var field in fields)
            {
                var fieldType = field.FieldType;

                // Check for GameAudioClip or similar audio types
                if (fieldType.Name.Contains("Audio") || fieldType.Name.Contains("Sound") ||
                    fieldType.Name.Contains("FMOD") || fieldType.Name == "GameAudioClip")
                {
                    soundEntries.Add(new SoundEntry
                    {
                        name = field.Name,
                        category = category,
                        field = field,
                        source = obj
                    });
                }
            }
        }

        private void OnGUI()
        {
            if (!showGUI) return;

            GUI.skin = GUI.skin;

            // Main window
            GUILayout.BeginArea(new Rect(20, 20, 400, 600), GUI.skin.box);

            GUILayout.Label($"Sound Sampler v{VersionString}", GUI.skin.label);
            GUILayout.Label($"Press {ToggleKey.Value} to close", GUI.skin.label);

            GUILayout.Space(10);

            // Search filter
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(60));
            searchFilter = GUILayout.TextField(searchFilter);
            if (GUILayout.Button("Clear", GUILayout.Width(50)))
            {
                searchFilter = "";
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            // Sound list
            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            string currentCategory = "";

            foreach (var entry in soundEntries)
            {
                // Filter by search
                if (!string.IsNullOrEmpty(searchFilter) &&
                    !entry.name.ToLower().Contains(searchFilter.ToLower()))
                {
                    continue;
                }

                // Category header
                if (entry.category != currentCategory)
                {
                    currentCategory = entry.category;
                    GUILayout.Space(5);
                    GUILayout.Label($"=== {currentCategory} ===", GUI.skin.label);
                    GUILayout.Space(5);
                }

                // Sound button
                GUILayout.BeginHorizontal();
                if (GUILayout.Button(entry.name, GUILayout.Width(250)))
                {
                    PlaySound(entry);
                }
                if (GUILayout.Button("Copy", GUILayout.Width(50)))
                {
                    GUIUtility.systemCopyBuffer = entry.name;
                    Log.LogInfo($"Copied: {entry.name}");
                }
                GUILayout.EndHorizontal();
            }

            GUILayout.EndScrollView();

            GUILayout.Space(10);
            GUILayout.Label($"Total sounds: {soundEntries.Count}");

            if (GUILayout.Button("Refresh List"))
            {
                initialized = false;
                InitializeSoundList();
            }

            GUILayout.EndArea();
        }

        private void PlaySound(SoundEntry entry)
        {
            try
            {
                var value = entry.field.GetValue(entry.source);
                if (value == null)
                {
                    Log.LogWarning($"Sound {entry.name} is null");
                    return;
                }

                // Try to play using various methods
                var playMethod = value.GetType().GetMethod("Play", BindingFlags.Public | BindingFlags.Instance);
                if (playMethod != null)
                {
                    playMethod.Invoke(value, null);
                    Log.LogInfo($"Playing: {entry.name}");
                    return;
                }

                var playRandomMethod = value.GetType().GetMethod("PlayRandomClip", BindingFlags.Public | BindingFlags.Instance);
                if (playRandomMethod != null)
                {
                    playRandomMethod.Invoke(value, null);
                    Log.LogInfo($"Playing: {entry.name}");
                    return;
                }

                Log.LogWarning($"Could not find play method for {entry.name}");
            }
            catch (Exception ex)
            {
                Log.LogError($"Error playing sound {entry.name}: {ex.Message}");
            }
        }
    }
}
