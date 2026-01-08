using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NarrativeExpansion
{
    /// <summary>
    /// Manages access to the game's existing voiced dialogue lines.
    /// Allows NPCs to play authentic character voices from Sparks, Paladin, Mirage, etc.
    /// </summary>
    public static class GameVoiceManager
    {
        private static bool _initialized = false;
        private static Dictionary<NarrativeEntryData.Speaker, List<NarrativeEntryData>> _voicedDialoguesBySpeaker;

        /// <summary>
        /// Initialize the voice manager by indexing all voiced dialogue entries
        /// </summary>
        public static void Initialize()
        {
            if (_initialized) return;

            try
            {
                _voicedDialoguesBySpeaker = new Dictionary<NarrativeEntryData.Speaker, List<NarrativeEntryData>>();

                // Get all narrative entries from the game
                var narrativeEntries = GameDefines.instance?.narrativeEntries;
                if (narrativeEntries == null || narrativeEntries.Count == 0)
                {
                    NarrativeExpansionPlugin.Log?.LogWarning("GameVoiceManager: No narrative entries found");
                    return;
                }

                // Index entries by speaker, only those with voice keys
                foreach (var entry in narrativeEntries)
                {
                    if (entry == null) continue;
                    if (string.IsNullOrEmpty(entry.shortTextVOKey)) continue; // No voice

                    if (!_voicedDialoguesBySpeaker.ContainsKey(entry.speaker))
                    {
                        _voicedDialoguesBySpeaker[entry.speaker] = new List<NarrativeEntryData>();
                    }

                    _voicedDialoguesBySpeaker[entry.speaker].Add(entry);
                }

                _initialized = true;

                // Log available voices
                foreach (var kvp in _voicedDialoguesBySpeaker)
                {
                    NarrativeExpansionPlugin.LogDebug($"GameVoiceManager: {kvp.Key} has {kvp.Value.Count} voiced lines");
                }

                NarrativeExpansionPlugin.Log?.LogInfo($"GameVoiceManager: Indexed {_voicedDialoguesBySpeaker.Values.Sum(l => l.Count)} voiced dialogue entries");
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.Log?.LogError($"GameVoiceManager: Init failed - {ex.Message}");
            }
        }

        /// <summary>
        /// Check if a speaker has voiced dialogue available
        /// </summary>
        public static bool HasVoicedDialogue(NarrativeEntryData.Speaker speaker)
        {
            if (!_initialized) return false;
            return _voicedDialoguesBySpeaker.ContainsKey(speaker) &&
                   _voicedDialoguesBySpeaker[speaker].Count > 0;
        }

        /// <summary>
        /// Get a random voiced dialogue entry for a speaker
        /// </summary>
        public static NarrativeEntryData GetRandomVoicedDialogue(NarrativeEntryData.Speaker speaker)
        {
            if (!_initialized) return null;
            if (!_voicedDialoguesBySpeaker.TryGetValue(speaker, out var dialogues)) return null;
            if (dialogues.Count == 0) return null;

            return dialogues[UnityEngine.Random.Range(0, dialogues.Count)];
        }

        /// <summary>
        /// Get voiced dialogue entries matching a text filter
        /// </summary>
        public static List<NarrativeEntryData> FindVoicedDialogue(NarrativeEntryData.Speaker speaker, string textFilter)
        {
            if (!_initialized) return new List<NarrativeEntryData>();
            if (!_voicedDialoguesBySpeaker.TryGetValue(speaker, out var dialogues)) return new List<NarrativeEntryData>();

            return dialogues.Where(d =>
                d.shortText != null &&
                d.shortText.IndexOf(textFilter, StringComparison.OrdinalIgnoreCase) >= 0
            ).ToList();
        }

        /// <summary>
        /// Play a random voiced line from a character using the game's dialogue system
        /// </summary>
        public static bool PlayRandomVoicedLine(NarrativeEntryData.Speaker speaker)
        {
            var entry = GetRandomVoicedDialogue(speaker);
            if (entry == null) return false;

            try
            {
                // Use the game's dialogue popup system to play the voice
                UIManager.instance?.dialoguePopup?.OnTriggerNarrativeEntry(entry);
                return true;
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.Log?.LogWarning($"GameVoiceManager: Failed to play voice - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Play a specific narrative entry with voice
        /// </summary>
        public static bool PlayVoicedEntry(NarrativeEntryData entry)
        {
            if (entry == null) return false;

            try
            {
                UIManager.instance?.dialoguePopup?.OnTriggerNarrativeEntry(entry);
                return true;
            }
            catch (Exception ex)
            {
                NarrativeExpansionPlugin.Log?.LogWarning($"GameVoiceManager: Failed to play entry - {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get count of voiced lines for a speaker
        /// </summary>
        public static int GetVoicedLineCount(NarrativeEntryData.Speaker speaker)
        {
            if (!_initialized) return 0;
            if (!_voicedDialoguesBySpeaker.TryGetValue(speaker, out var dialogues)) return 0;
            return dialogues.Count;
        }

        /// <summary>
        /// Get all available speakers with voiced content
        /// </summary>
        public static NarrativeEntryData.Speaker[] GetAvailableSpeakers()
        {
            if (!_initialized) return new NarrativeEntryData.Speaker[0];
            return _voicedDialoguesBySpeaker.Keys.ToArray();
        }
    }
}
