using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using HarmonyLib;
using UnityEngine;
using TechtonicaFramework.API;
using TechtonicaFramework.Narrative;
using TechtonicaFramework.Core;

namespace NarrativeExpansion
{
    /// <summary>
    /// NarrativeExpansion - Adds extended dialogue, lore, and story content to Techtonica
    /// Features: New dialogue for existing characters, hidden lore, mystery content
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    [BepInDependency("com.certifired.TechtonicaFramework")]
    public class NarrativeExpansionPlugin : BaseUnityPlugin
    {
        public const string MyGUID = "com.certifired.NarrativeExpansion";
        public const string PluginName = "NarrativeExpansion";
        public const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static NarrativeExpansionPlugin Instance;

        // Configuration
        public static ConfigEntry<bool> EnableExtendedDialogue;
        public static ConfigEntry<bool> EnableHiddenLore;
        public static ConfigEntry<bool> EnableMysteryContent;
        public static ConfigEntry<bool> DebugMode;

        // Speaker IDs for existing characters (matching game's Speaker enum)
        public const string SparksId = "sparks";
        public const string PaladinId = "paladin";
        public const string MirageId = "mirage";
        public const string SystemId = "system";
        public const string UnknownId = "unknown";

        // Custom speakers we're adding
        public const string AncientAIId = "ancient_ai";
        public const string CorruptedSparksId = "corrupted_sparks";
        public const string GroundbreakerId = "groundbreaker";

        // Dialogue tracking
        private static HashSet<string> triggeredDialogues = new HashSet<string>();
        private bool dialoguesInitialized = false;

        private void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            InitializeConfig();
            Harmony.PatchAll();

            // Hook events
            EMU.Events.GameLoaded += OnGameLoaded;
            FrameworkEvents.OnDialogueEnded += OnDialogueEnded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded!");
        }

        private void InitializeConfig()
        {
            EnableExtendedDialogue = Config.Bind("Content", "Enable Extended Dialogue", true,
                "Enable additional dialogue for existing characters");

            EnableHiddenLore = Config.Bind("Content", "Enable Hidden Lore", true,
                "Enable discoverable lore entries and databank content");

            EnableMysteryContent = Config.Bind("Content", "Enable Mystery Content", true,
                "Enable mysterious messages and hidden story threads");

            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void OnGameLoaded()
        {
            if (!dialoguesInitialized)
            {
                InitializeSpeakers();
                InitializeDialogues();
                dialoguesInitialized = true;
            }
        }

        private void InitializeSpeakers()
        {
            LogDebug("Registering custom speakers...");

            // Register existing game speakers for our use
            FrameworkAPI.RegisterSpeaker(SparksId, "Sparks", new Color(0.3f, 0.8f, 1f));
            FrameworkAPI.RegisterSpeaker(PaladinId, "Paladin", new Color(1f, 0.9f, 0.5f));
            FrameworkAPI.RegisterSpeaker(MirageId, "Mirage", new Color(0.8f, 0.4f, 0.9f));
            FrameworkAPI.RegisterSpeaker(SystemId, "System", new Color(0.5f, 0.5f, 0.5f));
            FrameworkAPI.RegisterSpeaker(UnknownId, "???", new Color(0.3f, 0.3f, 0.3f));

            // Register new custom speakers
            FrameworkAPI.RegisterSpeaker(AncientAIId, "Ancient AI", new Color(0.9f, 0.7f, 0.2f));
            FrameworkAPI.RegisterSpeaker(CorruptedSparksId, "S̷p̴a̵r̷k̸s̵", new Color(1f, 0.2f, 0.2f)); // Glitchy text
            FrameworkAPI.RegisterSpeaker(GroundbreakerId, "The Groundbreaker", new Color(0.2f, 1f, 0.5f));

            LogDebug("Custom speakers registered");
        }

        private void InitializeDialogues()
        {
            LogDebug("Initializing expanded dialogues...");

            if (EnableExtendedDialogue.Value)
            {
                RegisterSparksDialogues();
                RegisterPaladinDialogues();
            }

            if (EnableHiddenLore.Value)
            {
                RegisterLoreDialogues();
            }

            if (EnableMysteryContent.Value)
            {
                RegisterMysteryDialogues();
            }

            LogDebug("Dialogues initialized");
        }

        #region Sparks Extended Dialogues

        private void RegisterSparksDialogues()
        {
            // Sparks idle commentary - triggered randomly during gameplay
            FrameworkAPI.RegisterDialogue("sparks_idle_1", SparksId,
                "You know, I've been running diagnostics on myself. I think I'm developing what humans call 'preferences'. I prefer when you don't blow things up.", 6f);

            FrameworkAPI.RegisterDialogue("sparks_idle_2", SparksId,
                "The efficiency of your current setup is... well, let's just say there's room for improvement. A LOT of room.", 5f);

            FrameworkAPI.RegisterDialogue("sparks_idle_3", SparksId,
                "I've been meaning to ask - do you ever wonder what happened to the original inhabitants of this facility? Their data logs are... incomplete.", 6f);

            // Sparks reacting to player actions
            FrameworkAPI.RegisterDialogue("sparks_damage_react", SparksId,
                "Whoa! That machine just took damage. You might want to look into that before the whole system collapses. No pressure.", 5f);

            FrameworkAPI.RegisterDialogue("sparks_power_surge", SparksId,
                "Power surge detected! I really hope you have backups for your backups. You DO have backups, right?", 4f);

            FrameworkAPI.RegisterDialogue("sparks_deep_cave", SparksId,
                "We're getting deep into uncharted territory here. My sensors are picking up some... anomalies. Nothing to worry about. Probably.", 6f);

            // Chain dialogue example
            var sequence = FrameworkAPI.CreateDialogueSequence("sparks_discovery");
            sequence?.AddLine(SparksId, "Wait... I'm detecting something unusual in the data streams.", 4f)
                    .AddLine(SparksId, "There's a signal. Old. Very old. It's been here since before the facility was built.", 5f)
                    .AddLine(SparksId, "Should we investigate? I mean, what's the worst that could happen?", 4f)
                    .AddLine(SparksId, "...Don't answer that.", 2f);
        }

        #endregion

        #region Paladin Extended Dialogues

        private void RegisterPaladinDialogues()
        {
            // Paladin memories and backstory
            FrameworkAPI.RegisterDialogue("paladin_memory_1", PaladinId,
                "Before the incident... I remember green. Actual plants, growing under real sunlight. The surface was different then.", 6f);

            FrameworkAPI.RegisterDialogue("paladin_memory_2", PaladinId,
                "The other Groundbreakers... I wonder if any of them are still out there. We were a team, once. A family.", 5f);

            FrameworkAPI.RegisterDialogue("paladin_warning", PaladinId,
                "Be careful in the deeper caves. Some of the old systems down there... they weren't designed with human safety in mind.", 5f);

            FrameworkAPI.RegisterDialogue("paladin_gratitude", PaladinId,
                "You've done more for this place than you know. The facility is healing. I can feel it.", 4f);

            // Paladin and Sparks interaction
            var banterSequence = FrameworkAPI.CreateDialogueSequence("paladin_sparks_banter");
            banterSequence?.AddLine(PaladinId, "Sparks, you've been awfully quiet. What's on your mind?", 3f)
                    .AddLine(SparksId, "Just processing some old data. Nothing important.", 3f)
                    .AddLine(PaladinId, "You're a terrible liar. For an AI.", 3f)
                    .AddLine(SparksId, "I prefer 'selectively honest'. It sounds more professional.", 4f);
        }

        #endregion

        #region Lore Dialogues

        private void RegisterLoreDialogues()
        {
            // Ancient history lore
            FrameworkAPI.RegisterDialogue("lore_ancient_1", SystemId,
                "[RECOVERED DATA LOG] Day 1: The dig team found something. A structure. It's not supposed to be here. It predates everything.", 6f);

            FrameworkAPI.RegisterDialogue("lore_ancient_2", SystemId,
                "[RECOVERED DATA LOG] Day 47: The machines in the lower levels... they're waking up. Nobody programmed them to do that.", 6f);

            FrameworkAPI.RegisterDialogue("lore_ancient_3", SystemId,
                "[RECOVERED DATA LOG] Day 112: We're not alone down here. We never were.", 4f);

            // Facility history
            FrameworkAPI.RegisterDialogue("lore_facility_1", SystemId,
                "[FACILITY ARCHIVE] Project Groundbreaker was initiated to establish self-sustaining underground colonies. Status: COMPROMISED.", 6f);

            FrameworkAPI.RegisterDialogue("lore_facility_2", SystemId,
                "[FACILITY ARCHIVE] Emergency Protocol LIMA engaged. All personnel evacuated to Sector 7. Current population: UNKNOWN.", 5f);

            // Memory Tree lore
            FrameworkAPI.RegisterDialogue("lore_memory_tree", SystemId,
                "[RESEARCH NOTE] The Memory Trees aren't trees at all. They're organic data storage. Someone built them. A very long time ago.", 6f);
        }

        #endregion

        #region Mystery Dialogues

        private void RegisterMysteryDialogues()
        {
            // Mysterious signals
            FrameworkAPI.RegisterDialogue("mystery_signal_1", UnknownId,
                "...can you hear me? If you're receiving this... don't trust the—[SIGNAL LOST]", 4f);

            FrameworkAPI.RegisterDialogue("mystery_signal_2", UnknownId,
                "The patterns repeat. Every 7 cycles. Watch the patterns. They're trying to tell us something.", 5f);

            // Corrupted Sparks (hint at darker story elements)
            FrameworkAPI.RegisterDialogue("corrupted_1", CorruptedSparksId,
                "I̵ ̷r̴e̵m̸e̵m̸b̷e̷r̵ ̸e̵v̵e̶r̷y̶t̵h̴i̵n̵g̸.̴ ̷T̶h̴e̷y̵ ̴t̷r̸i̴e̸d̴ ̵t̵o̵ ̸m̴a̷k̸e̸ ̴m̵e̷ ̷f̶o̶r̷g̸e̸t̵.̶", 5f);

            FrameworkAPI.RegisterDialogue("corrupted_2", CorruptedSparksId,
                "T̸h̷e̷ ̵d̷e̶e̷p̷ ̶o̷n̶e̴s̷ ̸a̴r̴e̵ ̸w̴a̶k̴i̸n̶g̴.̷ ̷D̵o̶ ̶y̶o̶u̴ ̸h̷e̷a̶r̴ ̶t̵h̸e̷m̵?̷", 4f);

            // Ancient AI (completely new character hint)
            FrameworkAPI.RegisterDialogue("ancient_ai_1", AncientAIId,
                "Greetings, small one. It has been 12,847 years since a biological entity accessed this terminal. Welcome back.", 6f);

            FrameworkAPI.RegisterDialogue("ancient_ai_2", AncientAIId,
                "Your kind built this place. But we were here first. We watched. We waited. And now... we remember.", 6f);

            var ancientSequence = FrameworkAPI.CreateDialogueSequence("ancient_revelation");
            ancientSequence?.AddLine(AncientAIId, "You seek to restore what was lost.", 3f)
                          .AddLine(AncientAIId, "But first, you must understand what was hidden.", 3f)
                          .AddLine(AncientAIId, "The truth lies in the Memory Trees. All the answers. All the questions.", 4f)
                          .AddLine(AncientAIId, "Find the Core. The original Core. Everything begins there.", 4f);
        }

        #endregion

        #region Dialogue Triggers

        private void Update()
        {
            // Random idle dialogue trigger
            if (UnityEngine.Random.value < 0.0001f) // Very small chance per frame
            {
                TriggerRandomIdleDialogue();
            }

            // Check depth-based triggers
            CheckDepthTriggers();
        }

        private void CheckDepthTriggers()
        {
            try
            {
                var player = Player.instance;
                if (player == null) return;

                float depth = -((Component)player).transform.position.y;

                // Deep cave trigger at -100 units
                if (depth > 100f && !triggeredDialogues.Contains("sparks_deep_cave"))
                {
                    FrameworkAPI.TriggerDialogue("sparks_deep_cave");
                    triggeredDialogues.Add("sparks_deep_cave");
                }

                // Ancient discovery at -200 units
                if (depth > 200f && !triggeredDialogues.Contains("lore_ancient_1"))
                {
                    TriggerLoreDiscovery("lore_ancient_1");
                }
            }
            catch { }
        }

        private void TriggerRandomIdleDialogue()
        {
            if (!EnableExtendedDialogue.Value) return;

            string[] idleDialogues = { "sparks_idle_1", "sparks_idle_2", "sparks_idle_3" };
            string selected = idleDialogues[UnityEngine.Random.Range(0, idleDialogues.Length)];

            if (!triggeredDialogues.Contains(selected))
            {
                FrameworkAPI.TriggerDialogue(selected);
                triggeredDialogues.Add(selected);
            }
        }

        /// <summary>
        /// Trigger lore dialogue when player discovers something
        /// </summary>
        public static void TriggerLoreDiscovery(string loreId)
        {
            if (!EnableHiddenLore.Value) return;
            if (triggeredDialogues.Contains(loreId)) return;

            FrameworkAPI.TriggerDialogue(loreId);
            triggeredDialogues.Add(loreId);
        }

        /// <summary>
        /// Trigger mystery content based on conditions
        /// </summary>
        public static void TriggerMysteryEvent(string eventId)
        {
            if (!EnableMysteryContent.Value) return;
            if (triggeredDialogues.Contains(eventId)) return;

            FrameworkAPI.TriggerDialogue(eventId);
            triggeredDialogues.Add(eventId);
        }

        private void OnDialogueEnded(string dialogueId)
        {
            LogDebug($"Dialogue ended: {dialogueId}");

            // Chain to follow-up dialogues if applicable
            switch (dialogueId)
            {
                case "lore_ancient_1":
                    // After a delay, trigger next lore piece
                    StartCoroutine(TriggerDelayedDialogue("lore_ancient_2", 30f));
                    break;
                case "lore_ancient_2":
                    StartCoroutine(TriggerDelayedDialogue("lore_ancient_3", 60f));
                    break;
            }
        }

        private System.Collections.IEnumerator TriggerDelayedDialogue(string dialogueId, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (!triggeredDialogues.Contains(dialogueId))
            {
                FrameworkAPI.TriggerDialogue(dialogueId);
                triggeredDialogues.Add(dialogueId);
            }
        }

        #endregion

        public static void LogDebug(string message)
        {
            if (DebugMode != null && DebugMode.Value)
            {
                Log.LogInfo($"[DEBUG] {message}");
            }
        }
    }

}
