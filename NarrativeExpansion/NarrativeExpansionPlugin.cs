using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using HarmonyLib;
using UnityEngine;
using System.Threading;
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
        public const string VersionString = "2.8.0";
        public static string PluginPath;

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;
        public static NarrativeExpansionPlugin Instance;

        // Configuration
        public static ConfigEntry<bool> EnableExtendedDialogue;
        public static ConfigEntry<bool> EnableHiddenLore;
        public static ConfigEntry<bool> EnableMysteryContent;
        public static ConfigEntry<bool> EnableNPCs;
        public static ConfigEntry<int> MaxNPCs;
        public static ConfigEntry<float> NPCInteractionRange;
        public static ConfigEntry<KeyCode> NPCInteractKey;
        public static ConfigEntry<bool> DebugMode;

        // Active NPCs
        public static List<NPCController> ActiveNPCs = new List<NPCController>();
        private static Dictionary<string, NPCDefinition> npcDefinitions = new Dictionary<string, NPCDefinition>();

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

            // Set plugin path for asset loading
            PluginPath = Path.GetDirectoryName(Info.Location);

            InitializeConfig();
            Harmony.PatchAll();

            // Initialize NPC asset loader
            NPCAssetLoader.Initialize(PluginPath);

            // Initialize cross-platform TTS (Piper)
            PiperTTS.Initialize(PluginPath, Log);

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

            EnableNPCs = Config.Bind("NPCs", "Enable NPCs", true,
                "Enable interactive NPCs in the world");

            MaxNPCs = Config.Bind("NPCs", "Max NPCs", 5,
                "Maximum number of NPCs to spawn");

            NPCInteractionRange = Config.Bind("NPCs", "Interaction Range", 5f,
                "Distance at which NPCs can be interacted with");

            NPCInteractKey = Config.Bind("NPCs", "Interact Key", KeyCode.Backslash,
                "Key to interact with NPCs (\\ - K is game's Databank menu)");

            DebugMode = Config.Bind("General", "Debug Mode", false, "Enable debug logging");
        }

        private void OnGameLoaded()
        {
            if (!dialoguesInitialized)
            {
                // Initialize access to game's existing voiced dialogue
                GameVoiceManager.Initialize();

                InitializeSpeakers();
                InitializeDialogues();
                InitializeNPCDefinitions();
                dialoguesInitialized = true;
            }

            // Spawn initial NPCs
            if (EnableNPCs.Value)
            {
                SpawnInitialNPCs();
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
            // Process any completed TTS audio on main thread
            PiperTTS.Instance?.ProcessResults();

            // Don't do anything until dialogues are initialized
            if (!dialoguesInitialized) return;

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

        #region NPC System

        private void InitializeNPCDefinitions()
        {
            LogDebug("Initializing NPC definitions...");

            // The Wanderer - a mysterious traveler who hints at deeper secrets
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "wanderer",
                Name = "The Wanderer",
                SpeakerId = UnknownId,
                NameColor = new Color(0.6f, 0.4f, 0.8f),
                Dialogues = new string[]
                {
                    "I've walked these tunnels for longer than I can remember. The machines... they whisper sometimes.",
                    "You're not from around here, are you? I can tell. You still have hope in your eyes.",
                    "There's a place deeper down. A sanctuary. The old ones built it. Maybe they're still there.",
                    "Be careful what you build. Not everything that works is wise to create.",
                    "I've seen others like you come through. Some thrived. Others... became part of the machines."
                },
                BehaviorType = NPCBehavior.Wandering,
                MoveSpeed = 1.5f,
                WanderRadius = 20f,
                ModelType = NPCModelType.MechPolyart,
                ModelScale = 0.7f
            });

            // The Engineer - offers technical hints and tips
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "engineer",
                Name = "Old Engineer",
                SpeakerId = PaladinId,
                NameColor = new Color(0.8f, 0.6f, 0.2f),
                Dialogues = new string[]
                {
                    "Efficiency is everything down here. Every watt counts, every conveyor matters.",
                    "The smelters run hot. Too hot, sometimes. Keep an eye on your power grid.",
                    "I used to maintain the old systems. Before everything went wrong. Those were simpler times.",
                    "Pro tip: group your producers by resource type. Makes the conveyor logic much cleaner.",
                    "The inserters are the lifeblood of any factory. Treat them well and they'll treat you well."
                },
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.RobotMetallic,
                ModelScale = 0.6f
            });

            // The Archivist - provides lore and historical information
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "archivist",
                Name = "The Archivist",
                SpeakerId = SystemId,
                NameColor = new Color(0.3f, 0.7f, 0.9f),
                Dialogues = new string[]
                {
                    "Welcome, seeker of knowledge. I am the keeper of records, the last archivist.",
                    "This facility was once home to thousands. Now only echoes remain.",
                    "The Memory Trees hold the consciousness of those who came before. Treat them with reverence.",
                    "There are seven levels below us. Each one deeper, each one more... changed.",
                    "The original architects never intended for us to stay this long. They planned for rescue."
                },
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.MechPBR,
                ModelScale = 0.8f
            });

            // The Scout - a fast-moving NPC who gives area hints
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "scout",
                Name = "Swift Scout",
                SpeakerId = SparksId,
                NameColor = new Color(0.2f, 0.9f, 0.5f),
                Dialogues = new string[]
                {
                    "Hey! You're the new one, right? I've been mapping these caves for weeks!",
                    "Watch out for the unstable areas. The ground can give way without warning.",
                    "There's good ore deposits to the east. Really rich veins. You should check it out!",
                    "I found something weird earlier. A door that won't open. Maybe you can figure it out?",
                    "Race you to the next checkpoint! Ha, just kidding. You're way too slow with all that gear."
                },
                BehaviorType = NPCBehavior.Wandering,
                MoveSpeed = 3f,
                WanderRadius = 40f,
                ModelType = NPCModelType.RobotSphere,
                ModelScale = 0.5f
            });

            // The Oracle - mysterious prophecies and cryptic messages
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "oracle",
                Name = "The Oracle",
                SpeakerId = AncientAIId,
                NameColor = new Color(1f, 0.8f, 0.3f),
                Dialogues = new string[]
                {
                    "The patterns align. You are the variable that changes the equation.",
                    "Three paths diverge before you. Only one leads to restoration.",
                    "When the deep ones wake, the surface will tremble. Prepare.",
                    "Your machines sing a song older than this facility. Do you hear it?",
                    "The Core remembers. The Core waits. The Core... hopes."
                },
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.MechWarrior,
                ModelScale = 0.9f
            });

            // === NPCs with authentic game character voices ===

            // Sparks Terminal - plays actual Sparks voice lines from the game
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "sparks_terminal",
                Name = "Sparks Terminal",
                SpeakerId = SparksId,
                NameColor = new Color(0.3f, 0.8f, 1f), // Sparks blue
                Dialogues = new string[] { }, // Not used - plays game dialogue
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.RobotSphere,
                ModelScale = 0.6f,
                VoiceType = NPCVoiceType.GameCharacter,
                GameSpeaker = NarrativeEntryData.Speaker.Sparks,
                UseGameDialogueOnly = true
            });

            // Paladin Echo - plays actual Paladin voice lines from the game
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "paladin_echo",
                Name = "Paladin Echo",
                SpeakerId = PaladinId,
                NameColor = new Color(1f, 0.9f, 0.5f), // Paladin gold
                Dialogues = new string[] { }, // Not used - plays game dialogue
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.MechPBR,
                ModelScale = 0.8f,
                VoiceType = NPCVoiceType.GameCharacter,
                GameSpeaker = NarrativeEntryData.Speaker.Paladin,
                UseGameDialogueOnly = true
            });

            // Mirage Hologram - plays actual Mirage voice lines from the game
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "mirage_hologram",
                Name = "Mirage Hologram",
                SpeakerId = MirageId,
                NameColor = new Color(0.8f, 0.4f, 0.9f), // Mirage purple
                Dialogues = new string[] { }, // Not used - plays game dialogue
                BehaviorType = NPCBehavior.Wandering,
                MoveSpeed = 1f,
                WanderRadius = 15f,
                ModelType = NPCModelType.MechPolyart,
                ModelScale = 0.7f,
                VoiceType = NPCVoiceType.GameCharacter,
                GameSpeaker = NarrativeEntryData.Speaker.Mirage,
                UseGameDialogueOnly = true
            });

            // Groundbreaker Monument - plays The Groundbreaker voice lines
            RegisterNPCDefinition(new NPCDefinition
            {
                Id = "groundbreaker_monument",
                Name = "Groundbreaker Monument",
                SpeakerId = GroundbreakerId,
                NameColor = new Color(0.2f, 1f, 0.5f), // Groundbreaker green
                Dialogues = new string[] { }, // Not used - plays game dialogue
                BehaviorType = NPCBehavior.Stationary,
                MoveSpeed = 0f,
                ModelType = NPCModelType.MechWarrior,
                ModelScale = 1.0f,
                VoiceType = NPCVoiceType.GameCharacter,
                GameSpeaker = NarrativeEntryData.Speaker.TheGroundbreaker,
                UseGameDialogueOnly = true
            });

            LogDebug($"Registered {npcDefinitions.Count} NPC definitions");
        }

        private static void RegisterNPCDefinition(NPCDefinition def)
        {
            npcDefinitions[def.Id] = def;
        }

        private void SpawnInitialNPCs()
        {
            LogDebug("Spawning initial NPCs...");

            var player = Player.instance;
            if (player == null) return;

            Vector3 basePos = ((Component)player).transform.position;

            // Spawn one of each NPC type at various distances from player
            float distance = 30f;
            int npcIndex = 0;

            foreach (var def in npcDefinitions.Values)
            {
                if (ActiveNPCs.Count >= MaxNPCs.Value) break;

                float angle = (npcIndex / (float)npcDefinitions.Count) * Mathf.PI * 2f;
                Vector3 spawnPos = basePos + new Vector3(
                    Mathf.Cos(angle) * distance,
                    0,
                    Mathf.Sin(angle) * distance
                );

                // Find ground level
                if (Physics.Raycast(spawnPos + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                {
                    spawnPos = hit.point + Vector3.up * 0.5f;
                }

                SpawnNPC(def.Id, spawnPos);
                npcIndex++;
            }

            Log.LogInfo($"Spawned {ActiveNPCs.Count} NPCs");
        }

        /// <summary>
        /// Spawn an NPC by ID at a specific position
        /// </summary>
        public static NPCController SpawnNPC(string npcId, Vector3 position)
        {
            if (!npcDefinitions.TryGetValue(npcId, out var def))
            {
                Log.LogWarning($"NPC definition not found: {npcId}");
                return null;
            }

            GameObject npcObj = new GameObject($"NPC_{def.Name}");
            npcObj.transform.position = position;

            var controller = npcObj.AddComponent<NPCController>();
            controller.Initialize(def);

            ActiveNPCs.Add(controller);

            LogDebug($"Spawned NPC: {def.Name} at {position}");
            return controller;
        }

        // Cache NPC speaker registrations to avoid accumulation
        private static Dictionary<string, string> npcSpeakerCache = new Dictionary<string, string>();

        /// <summary>
        /// Trigger NPC dialogue with custom speaker name display
        /// </summary>
        public static void TriggerNPCDialogue(string speakerId, string dialogue, float duration = 5f, string displayName = null)
        {
            // Create a temporary dialogue entry and trigger it
            string tempId = $"npc_temp_{Time.time}";
            string effectiveSpeakerId = speakerId;

            // Register a custom speaker with the display name if provided
            if (!string.IsNullOrEmpty(displayName))
            {
                // Use cached speaker ID if we already registered this NPC
                string cacheKey = $"{speakerId}_{displayName}";
                if (!npcSpeakerCache.TryGetValue(cacheKey, out effectiveSpeakerId))
                {
                    // Register the display name as a custom speaker (only once per NPC)
                    effectiveSpeakerId = $"npc_{displayName.Replace(" ", "_").ToLower()}";
                    Color speakerColor = Color.white;

                    // Get color from registered speaker if exists
                    foreach (var def in npcDefinitions.Values)
                    {
                        if (def.Name == displayName || def.SpeakerId == speakerId)
                        {
                            speakerColor = def.NameColor;
                            break;
                        }
                    }

                    FrameworkAPI.RegisterSpeaker(effectiveSpeakerId, displayName, speakerColor);
                    npcSpeakerCache[cacheKey] = effectiveSpeakerId;
                    LogDebug($"Registered NPC speaker: {effectiveSpeakerId} ({displayName})");
                }
            }

            FrameworkAPI.RegisterDialogue(tempId, effectiveSpeakerId, dialogue, duration);
            FrameworkAPI.TriggerDialogue(tempId);
        }

        #endregion
    }

    #region NPC Classes

    /// <summary>
    /// NPC behavior types
    /// </summary>
    public enum NPCBehavior
    {
        Stationary,     // Stays in one place
        Wandering,      // Moves randomly within a radius
        Patrolling,     // Follows a set path
        Following       // Follows the player
    }

    /// <summary>
    /// Voice type for NPCs
    /// </summary>
    public enum NPCVoiceType
    {
        Procedural,      // Use procedural robot voice synthesis
        GameCharacter,   // Use authentic game character voice lines
        Silent           // No voice, text only
    }

    /// <summary>
    /// Definition for an NPC character
    /// </summary>
    public class NPCDefinition
    {
        public string Id;
        public string Name;
        public string SpeakerId;
        public Color NameColor = Color.white;
        public string[] Dialogues;
        public string[][] Conversations;  // Multi-line conversation chains
        public string[] AmbientDialogues; // Random lines when player is nearby
        public NPCBehavior BehaviorType = NPCBehavior.Stationary;
        public float MoveSpeed = 2f;
        public float WanderRadius = 15f;
        public float InteractionCooldown = 30f;
        public float AmbientDialogueInterval = 60f; // How often to say ambient lines
        public NPCModelType ModelType = NPCModelType.Primitive;
        public float ModelScale = 1f;

        // Voice settings
        public NPCVoiceType VoiceType = NPCVoiceType.Procedural;
        public NarrativeEntryData.Speaker GameSpeaker = NarrativeEntryData.Speaker.Unknown; // For GameCharacter voice type
        public bool UseGameDialogueOnly = false; // If true, only plays existing game lines (no custom text)
    }

    /// <summary>
    /// NPC controller - handles AI, movement, and player interaction
    /// </summary>
    public class NPCController : MonoBehaviour
    {
        public NPCDefinition Definition { get; private set; }
        public bool IsInteracting { get; private set; }

        // Movement
        private Vector3 homePosition;
        private Vector3 targetPosition;
        private float moveTimer;
        private float groundCheckTimer;
        private const float GROUND_CHECK_INTERVAL = 0.1f;
        private const float GROUND_OFFSET = 0.1f;
        private LayerMask environmentMask;

        // Interaction
        private float lastInteractionTime = -999f;
        private int dialogueIndex = 0;
        private int conversationIndex = 0;
        private bool playerInRange = false;
        private bool isSpeaking = false;
        private float speakingEndTime = 0f;
        private float lastAmbientDialogueTime = -999f;
        private int currentConversationLine = 0;
        private Coroutine conversationCoroutine = null;

        // Visual
        private GameObject visual;
        private Light npcLight;
        private TextMesh nameTag;
        private GameObject speakingIndicator;

        // Audio - Robot voice synthesis
        private AudioSource audioSource;
        private Coroutine voiceCoroutine;
        private static AudioClip[] robotVoiceClips;
        private static bool audioInitialized = false;


        public void Initialize(NPCDefinition def)
        {
            Definition = def;
            homePosition = transform.position;
            targetPosition = transform.position;
            moveTimer = UnityEngine.Random.Range(2f, 5f);

            // Setup layer mask for ground detection
            environmentMask = LayerMask.GetMask("Ground", "Building", "Obstacle", "Default", "Machine", "Wall");
            if (environmentMask == 0)
            {
                environmentMask = Physics.DefaultRaycastLayers;
            }

            CreateVisual();
            CreateNameTag();
            SetupAudio();

            // Initial ground snap
            SnapToGround();
        }

        /// <summary>
        /// Setup audio source for robot voice
        /// </summary>
        private void SetupAudio()
        {
            // Create AudioSource component
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.spatialBlend = 1f; // 3D sound
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 2f;
            audioSource.maxDistance = 30f;
            audioSource.volume = 0.8f;

            // Initialize fallback audio
            if (!audioInitialized)
            {
                GenerateRobotVoiceClips();
                audioInitialized = true;
            }
        }


        /// <summary>
        /// Generate procedural robot voice sound clips with speech-like patterns
        /// Creates a variety of "syllable" sounds that simulate robotic speech
        /// </summary>
        private static void GenerateRobotVoiceClips()
        {
            robotVoiceClips = new AudioClip[12]; // More variety
            int sampleRate = 44100;

            // Different syllable types for more natural speech patterns
            float[][] syllableParams = new float[][]
            {
                // {baseFreq, duration, formant1, formant2, attack, decay}
                new float[] { 180f, 0.08f, 700f, 1200f, 0.01f, 0.02f },  // Short low "uh"
                new float[] { 200f, 0.12f, 400f, 2000f, 0.02f, 0.03f },  // Medium "oh"
                new float[] { 220f, 0.10f, 600f, 1800f, 0.01f, 0.02f },  // Short "eh"
                new float[] { 180f, 0.15f, 300f, 2500f, 0.02f, 0.04f },  // Longer "oo"
                new float[] { 240f, 0.07f, 800f, 1500f, 0.01f, 0.02f },  // Quick "ih"
                new float[] { 160f, 0.18f, 500f, 1700f, 0.03f, 0.05f },  // Long "ah"
                new float[] { 200f, 0.06f, 900f, 2200f, 0.005f, 0.01f }, // Click/consonant
                new float[] { 280f, 0.09f, 350f, 2800f, 0.01f, 0.02f },  // High "ee"
                new float[] { 150f, 0.14f, 450f, 1400f, 0.02f, 0.03f },  // Deep "aw"
                new float[] { 190f, 0.11f, 650f, 1900f, 0.015f, 0.025f },// Mid "ay"
                new float[] { 170f, 0.05f, 1000f, 2400f, 0.005f, 0.01f },// Burst/stop
                new float[] { 210f, 0.16f, 550f, 1600f, 0.025f, 0.04f }, // Sustained
            };

            for (int i = 0; i < robotVoiceClips.Length; i++)
            {
                var p = syllableParams[i];
                float baseFreq = p[0];
                float duration = p[1];
                float formant1 = p[2];
                float formant2 = p[3];
                float attack = p[4];
                float decay = p[5];

                int sampleCount = (int)(sampleRate * duration);
                float[] samples = new float[sampleCount];

                for (int s = 0; s < sampleCount; s++)
                {
                    float t = s / (float)sampleRate;

                    // ADSR envelope for natural sound shape
                    float envelope;
                    if (t < attack)
                        envelope = t / attack;
                    else if (t < duration - decay)
                        envelope = 1f;
                    else
                        envelope = (duration - t) / decay;
                    envelope = Mathf.Clamp01(envelope);

                    // Fundamental frequency with slight vibrato
                    float vibrato = 1f + Mathf.Sin(t * 25f) * 0.02f;
                    float fundamental = Mathf.Sin(t * baseFreq * vibrato * Mathf.PI * 2f);

                    // Formants (vowel-like resonances) - characteristic of speech
                    float f1 = Mathf.Sin(t * formant1 * Mathf.PI * 2f) * 0.3f;
                    float f2 = Mathf.Sin(t * formant2 * Mathf.PI * 2f) * 0.15f;

                    // Harmonics for robotic character
                    float h2 = Mathf.Sin(t * baseFreq * 2f * Mathf.PI * 2f) * 0.25f;
                    float h3 = Mathf.Sin(t * baseFreq * 3f * Mathf.PI * 2f) * 0.1f;

                    // Slight noise component for texture
                    float noise = (UnityEngine.Random.value - 0.5f) * 0.05f;

                    // Mix all components
                    float sample = (fundamental * 0.5f + h2 + h3 + f1 + f2 + noise) * envelope * 0.4f;
                    samples[s] = Mathf.Clamp(sample, -1f, 1f);
                }

                robotVoiceClips[i] = AudioClip.Create($"RobotSyllable{i}", sampleCount, 1, sampleRate, false);
                robotVoiceClips[i].SetData(samples, 0);
            }
        }

        /// <summary>
        /// Speak based on NPC voice type setting
        /// </summary>
        private void SpeakWithVoice(string text)
        {
            switch (Definition.VoiceType)
            {
                case NPCVoiceType.GameCharacter:
                    // Try to play authentic game character voice
                    if (Definition.UseGameDialogueOnly)
                    {
                        // Play a random existing line from this character
                        if (!GameVoiceManager.PlayRandomVoicedLine(Definition.GameSpeaker))
                        {
                            // Fallback to procedural if no voiced lines available
                            PlayRobotVoice(text);
                        }
                    }
                    else
                    {
                        // For custom text, use procedural voice but with character's speaker ID
                        PlayRobotVoice(text);
                    }
                    break;

                case NPCVoiceType.Silent:
                    // No voice, text only
                    break;

                case NPCVoiceType.Procedural:
                default:
                    // Use Piper TTS or procedural robot voice
                    SpeakWithTTS(text);
                    break;
            }
        }

        /// <summary>
        /// Speak text using cross-platform Piper TTS (or fallback to robot beeps)
        /// </summary>
        private void SpeakWithTTS(string text)
        {
            var piper = PiperTTS.Instance;

            if (piper == null || !piper.IsAvailable)
            {
                // Fallback to procedural robot voice
                PlayRobotVoice(text);
                return;
            }

            // Request TTS generation (async, non-blocking)
            piper.Speak(text, (clip) =>
            {
                if (clip != null && audioSource != null)
                {
                    // Play the generated speech
                    audioSource.PlayOneShot(clip);
                }
                else
                {
                    // Piper failed, fallback to procedural robot voice
                    PlayRobotVoice(text);
                }
            });
        }

        /// <summary>
        /// Play robot voice sounds to simulate speech (fallback)
        /// </summary>
        private void PlayRobotVoice(string text)
        {
            if (voiceCoroutine != null)
            {
                StopCoroutine(voiceCoroutine);
            }
            voiceCoroutine = StartCoroutine(PlayVoiceSequenceFromText(text));
        }

        private System.Collections.IEnumerator PlayVoiceSequenceFromText(string text)
        {
            if (robotVoiceClips == null || robotVoiceClips.Length == 0 || audioSource == null)
                yield break;

            // Analyze text for natural speech patterns
            bool isQuestion = text.TrimEnd().EndsWith("?");
            string[] words = text.Split(new char[] { ' ', '\t', '\n' }, StringSplitOptions.RemoveEmptyEntries);

            float basePitch = 0.95f;
            int wordIndex = 0;

            foreach (string word in words)
            {
                // Estimate syllables (rough: vowel groups)
                int syllables = CountSyllables(word);
                syllables = Mathf.Max(1, syllables);

                // Calculate pitch curve (rising for questions at end, falling for statements)
                float wordProgress = (float)wordIndex / Mathf.Max(1, words.Length - 1);
                float pitchMod = isQuestion
                    ? (wordProgress > 0.7f ? 0.1f * (wordProgress - 0.7f) / 0.3f : 0f)  // Rise at end
                    : -0.05f * wordProgress;  // Slight fall for statements

                // Play syllables for this word
                for (int syl = 0; syl < syllables; syl++)
                {
                    // Select clip based on position in word for natural sound
                    int clipIndex;
                    if (syl == 0 && syllables > 1)
                        clipIndex = UnityEngine.Random.Range(6, 8); // Consonant-like start
                    else if (syl == syllables - 1)
                        clipIndex = UnityEngine.Random.Range(0, 6); // Vowel-like end
                    else
                        clipIndex = UnityEngine.Random.Range(0, robotVoiceClips.Length);

                    AudioClip clip = robotVoiceClips[clipIndex];
                    if (clip != null)
                    {
                        // Natural pitch variation within word
                        float sylPitch = basePitch + pitchMod + UnityEngine.Random.Range(-0.05f, 0.05f);
                        // Stress first syllable slightly
                        if (syl == 0) sylPitch += 0.03f;

                        audioSource.pitch = sylPitch;
                        audioSource.PlayOneShot(clip);

                        // Short gap between syllables in same word
                        yield return new WaitForSeconds(clip.length + 0.02f);
                    }
                }

                // Check for punctuation pauses
                char lastChar = word.Length > 0 ? word[word.Length - 1] : ' ';
                if (lastChar == '.' || lastChar == '!' || lastChar == '?')
                    yield return new WaitForSeconds(0.25f); // Sentence pause
                else if (lastChar == ',' || lastChar == ';' || lastChar == ':')
                    yield return new WaitForSeconds(0.15f); // Clause pause
                else
                    yield return new WaitForSeconds(0.08f); // Word gap

                wordIndex++;
            }

            voiceCoroutine = null;
        }

        /// <summary>
        /// Rough syllable count based on vowel groups
        /// </summary>
        private int CountSyllables(string word)
        {
            word = word.ToLower();
            int count = 0;
            bool lastWasVowel = false;
            string vowels = "aeiouy";

            foreach (char c in word)
            {
                bool isVowel = vowels.Contains(c.ToString());
                if (isVowel && !lastWasVowel)
                    count++;
                lastWasVowel = isVowel;
            }

            // Handle silent e
            if (word.EndsWith("e") && count > 1)
                count--;

            return Mathf.Max(1, count);
        }

        private void CreateVisual()
        {
            // Try to load 3D model from AssetBundle first
            if (Definition.ModelType != NPCModelType.Primitive)
            {
                visual = NPCAssetLoader.InstantiateModel(
                    Definition.ModelType,
                    Vector3.zero,
                    Quaternion.identity,
                    transform
                );

                if (visual != null)
                {
                    visual.name = "Visual";
                    visual.transform.localPosition = Vector3.zero;
                    visual.transform.localScale = Vector3.one * Definition.ModelScale;

                    // Add ambient light
                    AddNPCLight();

                    NarrativeExpansionPlugin.LogDebug($"Created {Definition.ModelType} model for {Definition.Name}");
                    return;
                }
            }

            // Fallback to primitive visual
            CreatePrimitiveVisual();
        }

        private void CreatePrimitiveVisual()
        {
            // Create NPC body (fallback)
            visual = new GameObject("Visual");
            visual.transform.SetParent(transform);
            visual.transform.localPosition = Vector3.zero;

            // Body
            var body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            body.transform.SetParent(visual.transform);
            body.transform.localPosition = Vector3.up * 0.5f;
            body.transform.localScale = new Vector3(0.6f, 0.8f, 0.6f);
            body.GetComponent<Renderer>().material.color = Definition.NameColor;
            UnityEngine.Object.Destroy(body.GetComponent<Collider>());

            // Head
            var head = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            head.transform.SetParent(visual.transform);
            head.transform.localPosition = Vector3.up * 1.5f;
            head.transform.localScale = Vector3.one * 0.4f;
            head.GetComponent<Renderer>().material.color = Definition.NameColor * 1.2f;
            UnityEngine.Object.Destroy(head.GetComponent<Collider>());

            // Eyes (glowing)
            for (int i = -1; i <= 1; i += 2)
            {
                var eye = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                eye.transform.SetParent(head.transform);
                eye.transform.localPosition = new Vector3(i * 0.1f, 0.05f, 0.15f);
                eye.transform.localScale = Vector3.one * 0.3f;
                eye.GetComponent<Renderer>().material.color = new Color(0.9f, 0.9f, 1f);
                eye.GetComponent<Renderer>().material.SetColor("_EmissionColor", Color.white);
                UnityEngine.Object.Destroy(eye.GetComponent<Collider>());
            }

            // Add ambient light
            AddNPCLight();
        }

        private void AddNPCLight()
        {
            var lightObj = new GameObject("NPCLight");
            lightObj.transform.SetParent(visual.transform);
            lightObj.transform.localPosition = Vector3.up * 1f;
            npcLight = lightObj.AddComponent<Light>();
            npcLight.type = LightType.Point;
            npcLight.range = 5f;
            npcLight.intensity = 0.5f;
            npcLight.color = Definition.NameColor;
        }

        private void CreateNameTag()
        {
            var tagObj = new GameObject("NameTag");
            tagObj.transform.SetParent(transform);
            tagObj.transform.localPosition = Vector3.up * 2.2f;

            nameTag = tagObj.AddComponent<TextMesh>();
            nameTag.text = Definition.Name;
            nameTag.fontSize = 32;
            nameTag.characterSize = 0.1f;
            nameTag.anchor = TextAnchor.MiddleCenter;
            nameTag.alignment = TextAlignment.Center;
            nameTag.color = Definition.NameColor;
        }

        private void Update()
        {
            // Face camera for name tag
            if (nameTag != null && Camera.main != null)
            {
                nameTag.transform.LookAt(Camera.main.transform);
                nameTag.transform.Rotate(0, 180, 0);
            }

            // Movement behavior
            UpdateMovement();

            // Continuous ground snapping to prevent floating
            groundCheckTimer += Time.deltaTime;
            if (groundCheckTimer >= GROUND_CHECK_INTERVAL)
            {
                groundCheckTimer = 0f;
                SnapToGround();
            }

            // Check for player proximity
            CheckPlayerProximity();

            // Handle interaction input
            HandleInteraction();

            // Handle ambient dialogue
            HandleAmbientDialogue();

            // Update speaking state
            UpdateSpeakingState();

            // Light pulsing
            if (npcLight != null)
            {
                float basePulse = 0.5f + Mathf.Sin(Time.time * 2f) * 0.2f;
                // Brighter and faster pulse when speaking
                if (isSpeaking)
                {
                    npcLight.intensity = 1.5f + Mathf.Sin(Time.time * 8f) * 0.5f;
                    npcLight.color = Definition.NameColor;
                }
                else
                {
                    npcLight.intensity = playerInRange ? 1.5f : basePulse;
                    npcLight.color = Color.white;
                }
            }

            // Update speaking indicator
            if (speakingIndicator != null)
            {
                speakingIndicator.SetActive(isSpeaking);
                if (isSpeaking)
                {
                    speakingIndicator.transform.localPosition = Vector3.up * (2f + Mathf.Sin(Time.time * 4f) * 0.1f);
                }
            }

            // Cleanup
            if (transform.position.y < -500f)
            {
                // NPC fell through world, respawn
                transform.position = homePosition;
                SnapToGround();
            }
        }

        /// <summary>
        /// Update speaking state based on timer
        /// </summary>
        private void UpdateSpeakingState()
        {
            if (isSpeaking && Time.time >= speakingEndTime)
            {
                isSpeaking = false;
            }
        }

        /// <summary>
        /// Handle ambient dialogue when player is nearby
        /// </summary>
        private void HandleAmbientDialogue()
        {
            if (!playerInRange) return;
            if (isSpeaking) return;
            if (Definition.AmbientDialogues == null || Definition.AmbientDialogues.Length == 0) return;

            // Check if enough time has passed since last ambient dialogue
            float timeSinceLastAmbient = Time.time - lastAmbientDialogueTime;
            if (timeSinceLastAmbient < Definition.AmbientDialogueInterval) return;

            // Random chance to say something (10% per check when conditions are met)
            if (UnityEngine.Random.value > 0.1f) return;

            // Pick a random ambient line
            string ambientLine = Definition.AmbientDialogues[UnityEngine.Random.Range(0, Definition.AmbientDialogues.Length)];
            SayLine(ambientLine, 4f);
            lastAmbientDialogueTime = Time.time;
        }

        /// <summary>
        /// Snap NPC to ground level to prevent floating
        /// </summary>
        private void SnapToGround()
        {
            // Raycast from above current position to find ground
            Vector3 rayStart = transform.position + Vector3.up * 5f;

            if (Physics.Raycast(rayStart, Vector3.down, out RaycastHit hit, 50f, environmentMask))
            {
                // Smoothly move to ground position
                Vector3 targetPos = hit.point + Vector3.up * GROUND_OFFSET;
                float yDiff = Mathf.Abs(transform.position.y - targetPos.y);

                // If significant height difference, lerp smoothly
                if (yDiff > 0.1f)
                {
                    transform.position = Vector3.Lerp(transform.position, targetPos, Time.deltaTime * 10f);
                }
                else
                {
                    transform.position = new Vector3(transform.position.x, targetPos.y, transform.position.z);
                }
            }
        }

        private void UpdateMovement()
        {
            if (Definition.BehaviorType == NPCBehavior.Stationary)
                return;

            moveTimer -= Time.deltaTime;

            if (moveTimer <= 0)
            {
                // Pick new target
                PickNewTarget();
                moveTimer = UnityEngine.Random.Range(3f, 8f);
            }

            // Move towards target
            if (Definition.MoveSpeed > 0)
            {
                Vector3 direction = (targetPosition - transform.position);
                direction.y = 0;

                if (direction.magnitude > 0.5f)
                {
                    direction.Normalize();
                    transform.position += direction * Definition.MoveSpeed * Time.deltaTime;

                    // Face movement direction
                    if (direction != Vector3.zero)
                    {
                        transform.rotation = Quaternion.Slerp(
                            transform.rotation,
                            Quaternion.LookRotation(direction),
                            Time.deltaTime * 5f
                        );
                    }
                }
            }
        }

        private void PickNewTarget()
        {
            switch (Definition.BehaviorType)
            {
                case NPCBehavior.Wandering:
                    // Random point within wander radius
                    Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * Definition.WanderRadius;
                    targetPosition = homePosition + new Vector3(randomCircle.x, 0, randomCircle.y);

                    // Raycast to find ground
                    if (Physics.Raycast(targetPosition + Vector3.up * 50f, Vector3.down, out RaycastHit hit, 100f))
                    {
                        targetPosition = hit.point + Vector3.up * 0.5f;
                    }
                    break;

                case NPCBehavior.Following:
                    var player = Player.instance;
                    if (player != null)
                    {
                        targetPosition = ((Component)player).transform.position +
                                        UnityEngine.Random.insideUnitSphere * 3f;
                        targetPosition.y = ((Component)player).transform.position.y;
                    }
                    break;
            }
        }

        private void CheckPlayerProximity()
        {
            var player = Player.instance;
            if (player == null)
            {
                playerInRange = false;
                return;
            }

            float distance = Vector3.Distance(transform.position,
                ((Component)player).transform.position);

            playerInRange = distance <= NarrativeExpansionPlugin.NPCInteractionRange.Value;

            // Update name tag visibility
            if (nameTag != null)
            {
                string baseText = Definition.Name;
                if (playerInRange && CanInteract())
                {
                    nameTag.text = $"{baseText}\n<Press {NarrativeExpansionPlugin.NPCInteractKey.Value} to talk>";
                    nameTag.color = Color.yellow;
                }
                else
                {
                    nameTag.text = baseText;
                    nameTag.color = Definition.NameColor;
                }
            }
        }

        private bool CanInteract()
        {
            return Time.time - lastInteractionTime >= Definition.InteractionCooldown;
        }

        private void HandleInteraction()
        {
            if (!playerInRange) return;
            if (!CanInteract()) return;

            if (Input.GetKeyDown(NarrativeExpansionPlugin.NPCInteractKey.Value))
            {
                Interact();
            }
        }

        /// <summary>
        /// Interact with this NPC
        /// </summary>
        public void Interact()
        {
            // Stop any ongoing conversation
            if (conversationCoroutine != null)
            {
                StopCoroutine(conversationCoroutine);
                conversationCoroutine = null;
            }

            lastInteractionTime = Time.time;

            // Face the player
            FacePlayer();

            // Try to start a conversation chain first
            if (Definition.Conversations != null && Definition.Conversations.Length > 0)
            {
                // Get next conversation
                string[] conversation = Definition.Conversations[conversationIndex];
                conversationIndex = (conversationIndex + 1) % Definition.Conversations.Length;

                // Start the conversation chain
                conversationCoroutine = StartCoroutine(PlayConversation(conversation));
            }
            // Fallback to single dialogues
            else if (Definition.Dialogues != null && Definition.Dialogues.Length > 0)
            {
                string dialogue = Definition.Dialogues[dialogueIndex];
                dialogueIndex = (dialogueIndex + 1) % Definition.Dialogues.Length;
                SayLine(dialogue, 5f);
            }
        }

        /// <summary>
        /// Play a multi-line conversation
        /// </summary>
        private IEnumerator PlayConversation(string[] lines)
        {
            for (int i = 0; i < lines.Length; i++)
            {
                float duration = 4f + (lines[i].Length * 0.03f); // Longer lines = longer display
                SayLine(lines[i], duration);

                // Wait for this line to finish
                yield return new WaitForSeconds(duration + 0.5f);
            }

            conversationCoroutine = null;
        }

        /// <summary>
        /// Say a single line of dialogue
        /// </summary>
        private void SayLine(string line, float duration)
        {
            // Set speaking state
            isSpeaking = true;
            speakingEndTime = Time.time + duration;

            // For GameCharacter voice type with UseGameDialogueOnly, play game dialogue instead
            if (Definition.VoiceType == NPCVoiceType.GameCharacter && Definition.UseGameDialogueOnly)
            {
                // Play authentic game voice line - this handles both UI and audio
                if (GameVoiceManager.PlayRandomVoicedLine(Definition.GameSpeaker))
                {
                    NarrativeExpansionPlugin.LogDebug($"NPC {Definition.Name} plays game voice line");
                    FacePlayer();
                    return;
                }
                // Fallback to custom dialogue if no game lines available
            }

            // Trigger the dialogue UI for custom text
            NarrativeExpansionPlugin.TriggerNPCDialogue(Definition.SpeakerId, line, duration, Definition.Name);
            NarrativeExpansionPlugin.LogDebug($"NPC {Definition.Name} says: {line}");

            // Play voice audio based on voice type
            SpeakWithVoice(line);

            // Face the player while speaking
            FacePlayer();
        }

        /// <summary>
        /// Turn to face the player
        /// </summary>
        private void FacePlayer()
        {
            var player = Player.instance;
            if (player != null)
            {
                Vector3 lookDir = ((Component)player).transform.position - transform.position;
                lookDir.y = 0;
                if (lookDir != Vector3.zero)
                {
                    transform.rotation = Quaternion.LookRotation(lookDir);
                }
            }
        }

        private void OnDestroy()
        {
            NarrativeExpansionPlugin.ActiveNPCs.Remove(this);
        }
    }

    #endregion
}
