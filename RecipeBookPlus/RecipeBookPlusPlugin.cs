using System;
using System.Collections.Generic;
using System.Linq;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using EquinoxsModUtils;
using HarmonyLib;
using UnityEngine;

namespace RecipeBookPlus
{
    /// <summary>
    /// RecipeBookPlus - A collapsible recipe browser for Techtonica
    /// Replaces Equinox-RecipeBook with collapsible panels
    /// </summary>
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    [BepInDependency("com.equinox.EquinoxsModUtils")]
    public class RecipeBookPlusPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.RecipeBookPlus";
        private const string PluginName = "RecipeBookPlus";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        // Config
        public static ConfigEntry<bool> FilterUnknown;
        public static ConfigEntry<KeyCode> ToggleKey;
        public static ConfigEntry<bool> OpenWithInventory;
        public static ConfigEntry<int> ItemImageSize;
        public static ConfigEntry<int> DefaultMachineMk;
        public static ConfigEntry<bool> StartCollapsed;

        // State
        private static bool isOpen = false;
        private static bool itemsPanelCollapsed = false;
        private static bool recipesPanelCollapsed = true;
        private static string searchQuery = "";
        private static Vector2 itemsScrollPos = Vector2.zero;
        private static Vector2 recipesScrollPos = Vector2.zero;
        private static List<ResourceInfo> filteredItems = new List<ResourceInfo>();
        private static ResourceInfo selectedResource = null;
        private static bool showingRecipes = true; // true = recipes for, false = uses of
        private static int machineMk = 1;

        // GUI Styles
        private static GUIStyle tabStyle;
        private static GUIStyle headerStyle;
        private static GUIStyle itemButtonStyle;
        private static GUIStyle selectedItemStyle;
        private static bool stylesInitialized = false;

        // Panel dimensions
        private const int ITEMS_PANEL_WIDTH = 380;
        private const int ITEMS_PANEL_HEIGHT = 500;
        private const int RECIPES_PANEL_WIDTH = 420;
        private const int RECIPES_PANEL_HEIGHT = 600;
        private const int TAB_WIDTH = 30;
        private const int TAB_HEIGHT = 100;

        private void Awake()
        {
            Log = Logger;
            Log.LogInfo($"{PluginName} v{VersionString} loading...");

            FilterUnknown = Config.Bind("General", "Filter Unknown", true,
                "Hide items and recipes you haven't discovered yet");
            ToggleKey = Config.Bind("General", "Toggle Key", KeyCode.F3,
                "Key to toggle the Recipe Book");
            OpenWithInventory = Config.Bind("General", "Open With Inventory", true,
                "Automatically open when inventory opens");
            ItemImageSize = Config.Bind("Display", "Item Image Size", 40,
                new ConfigDescription("Size of item icons", new AcceptableValueRange<int>(24, 64)));
            DefaultMachineMk = Config.Bind("Display", "Default Machine Mk", 1,
                new ConfigDescription("Default machine tier for rate calculations", new AcceptableValueRange<int>(1, 3)));
            StartCollapsed = Config.Bind("Display", "Start Collapsed", true,
                "Start with panels collapsed");

            machineMk = DefaultMachineMk.Value;
            itemsPanelCollapsed = StartCollapsed.Value;

            Harmony.PatchAll();

            EMU.Events.TechTreeStateLoaded += OnTechTreeStateLoaded;

            Log.LogInfo($"{PluginName} v{VersionString} loaded! Press {ToggleKey.Value} to toggle.");
        }

        private void OnTechTreeStateLoaded()
        {
            RefreshItemList();
        }

        private void Update()
        {
            if (Input.GetKeyDown(ToggleKey.Value))
            {
                if (UIManager.instance != null && !UIManager.instance.anyMenuOpen)
                {
                    isOpen = !isOpen;
                    if (isOpen)
                    {
                        RefreshItemList();
                    }
                }
                else if (isOpen)
                {
                    isOpen = false;
                }
            }

            // Close with Escape
            if (isOpen && Input.GetKeyDown(KeyCode.Escape))
            {
                isOpen = false;
            }
        }

        private void OnGUI()
        {
            if (!isOpen) return;
            if (TechTreeState.instance == null) return;

            InitializeStyles();

            // Draw collapsed tabs on right edge
            float screenRight = Screen.width;
            float tabY = Screen.height * 0.3f;

            // Items panel tab (always visible)
            Rect itemsTabRect = new Rect(
                itemsPanelCollapsed ? screenRight - TAB_WIDTH : screenRight - ITEMS_PANEL_WIDTH - TAB_WIDTH,
                tabY,
                TAB_WIDTH,
                TAB_HEIGHT
            );

            GUI.backgroundColor = itemsPanelCollapsed ? new Color(0.3f, 0.5f, 0.7f) : new Color(0.2f, 0.4f, 0.6f);
            if (GUI.Button(itemsTabRect, itemsPanelCollapsed ? ">" : "<", tabStyle))
            {
                itemsPanelCollapsed = !itemsPanelCollapsed;
            }

            // Items panel (when expanded)
            if (!itemsPanelCollapsed)
            {
                Rect itemsPanelRect = new Rect(
                    screenRight - ITEMS_PANEL_WIDTH,
                    tabY,
                    ITEMS_PANEL_WIDTH,
                    ITEMS_PANEL_HEIGHT
                );
                GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
                GUI.Box(itemsPanelRect, "");
                GUILayout.BeginArea(itemsPanelRect);
                DrawItemsPanel();
                GUILayout.EndArea();
            }

            // Recipes panel tab (only visible when item selected)
            if (selectedResource != null)
            {
                float recipeTabY = tabY + TAB_HEIGHT + 20;
                Rect recipesTabRect = new Rect(
                    recipesPanelCollapsed ? screenRight - TAB_WIDTH : screenRight - RECIPES_PANEL_WIDTH - TAB_WIDTH,
                    recipeTabY,
                    TAB_WIDTH,
                    TAB_HEIGHT
                );

                GUI.backgroundColor = recipesPanelCollapsed ? new Color(0.5f, 0.3f, 0.5f) : new Color(0.4f, 0.2f, 0.4f);
                if (GUI.Button(recipesTabRect, recipesPanelCollapsed ? ">" : "<", tabStyle))
                {
                    recipesPanelCollapsed = !recipesPanelCollapsed;
                }

                // Recipes panel (when expanded)
                if (!recipesPanelCollapsed)
                {
                    Rect recipesPanelRect = new Rect(
                        screenRight - RECIPES_PANEL_WIDTH,
                        recipeTabY,
                        RECIPES_PANEL_WIDTH,
                        RECIPES_PANEL_HEIGHT
                    );
                    GUI.backgroundColor = new Color(0.15f, 0.15f, 0.2f, 0.95f);
                    GUI.Box(recipesPanelRect, "");
                    GUILayout.BeginArea(recipesPanelRect);
                    DrawRecipesPanel();
                    GUILayout.EndArea();
                }
            }

            GUI.backgroundColor = Color.white;
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            tabStyle = new GUIStyle(GUI.skin.button)
            {
                fontSize = 16,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };

            headerStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
            headerStyle.normal.textColor = Color.white;

            itemButtonStyle = new GUIStyle(GUI.skin.button)
            {
                alignment = TextAnchor.MiddleCenter,
                imagePosition = ImagePosition.ImageAbove
            };

            selectedItemStyle = new GUIStyle(itemButtonStyle);
            selectedItemStyle.normal.background = MakeTex(2, 2, new Color(0.3f, 0.5f, 0.7f, 0.8f));

            stylesInitialized = true;
        }

        private void DrawItemsPanel()
        {
            GUILayout.BeginVertical();

            // Header
            GUILayout.Space(5);
            GUILayout.Label("Recipe Book", headerStyle);
            GUILayout.Space(5);

            // Search bar
            GUILayout.BeginHorizontal();
            GUILayout.Label("Search:", GUILayout.Width(50));
            string newQuery = GUILayout.TextField(searchQuery, GUILayout.ExpandWidth(true));
            if (newQuery != searchQuery)
            {
                searchQuery = newQuery;
                RefreshItemList();
            }
            if (GUILayout.Button("X", GUILayout.Width(25)))
            {
                searchQuery = "";
                RefreshItemList();
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.Label($"Items: {filteredItems.Count}", GUILayout.Height(20));

            // Items grid
            itemsScrollPos = GUILayout.BeginScrollView(itemsScrollPos, GUILayout.ExpandHeight(true));

            int itemSize = ItemImageSize.Value;
            int itemsPerRow = Mathf.Max(1, (ITEMS_PANEL_WIDTH - 40) / (itemSize + 8));
            int itemIndex = 0;

            GUILayout.BeginHorizontal();
            foreach (var resource in filteredItems)
            {
                if (itemIndex > 0 && itemIndex % itemsPerRow == 0)
                {
                    GUILayout.EndHorizontal();
                    GUILayout.BeginHorizontal();
                }

                bool isSelected = selectedResource == resource;
                GUIStyle style = isSelected ? selectedItemStyle : itemButtonStyle;

                GUIContent content = new GUIContent();
                if (resource.sprite != null)
                {
                    content.image = resource.sprite.texture;
                }
                content.tooltip = resource.displayName;

                if (GUILayout.Button(content, style, GUILayout.Width(itemSize), GUILayout.Height(itemSize)))
                {
                    // Left click = recipes for, right click = uses of
                    if (Event.current.button == 0)
                    {
                        selectedResource = resource;
                        showingRecipes = true;
                        recipesPanelCollapsed = false;
                    }
                    else if (Event.current.button == 1)
                    {
                        selectedResource = resource;
                        showingRecipes = false;
                        recipesPanelCollapsed = false;
                    }
                }

                itemIndex++;
            }
            GUILayout.EndHorizontal();

            GUILayout.EndScrollView();

            // Footer
            GUILayout.Space(5);
            GUILayout.BeginHorizontal();
            FilterUnknown.Value = GUILayout.Toggle(FilterUnknown.Value, " Hide Unknown");
            if (GUI.changed)
            {
                RefreshItemList();
            }
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("Close", GUILayout.Width(60)))
            {
                isOpen = false;
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private void DrawRecipesPanel()
        {
            if (selectedResource == null) return;

            GUILayout.BeginVertical();

            // Header
            GUILayout.Space(5);
            string headerText = showingRecipes
                ? $"Recipes for {selectedResource.displayName}"
                : $"Uses for {selectedResource.displayName}";
            GUILayout.Label(headerText, headerStyle);

            // Toggle buttons
            GUILayout.BeginHorizontal();
            GUI.backgroundColor = showingRecipes ? Color.cyan : Color.gray;
            if (GUILayout.Button("Recipes", GUILayout.Height(25)))
            {
                showingRecipes = true;
            }
            GUI.backgroundColor = !showingRecipes ? Color.cyan : Color.gray;
            if (GUILayout.Button("Uses", GUILayout.Height(25)))
            {
                showingRecipes = false;
            }
            GUI.backgroundColor = Color.white;
            GUILayout.EndHorizontal();

            // Machine Mk selector
            GUILayout.BeginHorizontal();
            GUILayout.Label("Machine Mk:", GUILayout.Width(80));
            for (int mk = 1; mk <= 3; mk++)
            {
                GUI.backgroundColor = (machineMk == mk) ? Color.cyan : Color.gray;
                if (GUILayout.Button($"Mk{mk}", GUILayout.Width(50)))
                {
                    machineMk = mk;
                }
            }
            GUI.backgroundColor = Color.white;
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.Space(5);

            // Get recipes
            var recipes = GetRecipesForResource(selectedResource, showingRecipes);

            GUILayout.Label($"Found: {recipes.Count} recipes");

            // Recipes list
            recipesScrollPos = GUILayout.BeginScrollView(recipesScrollPos, GUILayout.ExpandHeight(true));

            foreach (var recipe in recipes)
            {
                DrawRecipeEntry(recipe);
                GUILayout.Space(5);
            }

            if (recipes.Count == 0)
            {
                GUILayout.Label("No recipes found.", headerStyle);
            }

            GUILayout.EndScrollView();

            GUILayout.Space(5);
            GUILayout.EndVertical();
        }

        private void DrawRecipeEntry(SchematicsRecipeData recipe)
        {
            GUILayout.BeginVertical(GUI.skin.box);

            // Machine type
            GUILayout.Label($"Made in: {recipe.craftingMethod}", new GUIStyle(GUI.skin.label) { fontStyle = FontStyle.Bold });

            // Inputs
            GUILayout.BeginHorizontal();
            GUILayout.Label("In:", GUILayout.Width(30));
            int ingIndex = 0;
            foreach (var ing in recipe.ingTypes)
            {
                int qty = (ingIndex < recipe.ingQuantities.Length) ? recipe.ingQuantities[ingIndex] : 1;
                string rate = GetRateString(recipe, ingIndex, true);

                GUIContent content = new GUIContent();
                if (ing != null && ing.sprite != null)
                {
                    content.image = ing.sprite.texture;
                }
                content.tooltip = $"{(ing != null ? ing.displayName : "?")} x{qty}\n{rate}";

                if (GUILayout.Button(content, GUILayout.Width(36), GUILayout.Height(36)))
                {
                    if (ing != null) selectedResource = ing;
                    showingRecipes = true;
                }
                GUILayout.Label($"x{qty}", GUILayout.Width(30));
                ingIndex++;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            // Arrow
            GUILayout.Label("  -->", GUILayout.Height(15));

            // Outputs
            GUILayout.BeginHorizontal();
            GUILayout.Label("Out:", GUILayout.Width(30));
            int outIndex = 0;
            foreach (var output in recipe.outputTypes)
            {
                int qty = (outIndex < recipe.outputQuantities.Length) ? recipe.outputQuantities[outIndex] : 1;
                string rate = GetRateString(recipe, outIndex, false);

                GUIContent content = new GUIContent();
                if (output != null && output.sprite != null)
                {
                    content.image = output.sprite.texture;
                }
                content.tooltip = $"{(output != null ? output.displayName : "?")} x{qty}\n{rate}";

                if (GUILayout.Button(content, GUILayout.Width(36), GUILayout.Height(36)))
                {
                    if (output != null) selectedResource = output;
                    showingRecipes = true;
                }
                GUILayout.Label($"x{qty}", GUILayout.Width(30));
                outIndex++;
            }
            GUILayout.FlexibleSpace();
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
        }

        private List<SchematicsRecipeData> GetRecipesForResource(ResourceInfo resource, bool recipesFor)
        {
            if (GameDefines.instance == null) return new List<SchematicsRecipeData>();

            var allRecipes = GameDefines.instance.schematicsRecipeEntries;
            IEnumerable<SchematicsRecipeData> filtered;

            if (recipesFor)
            {
                filtered = allRecipes.Where(r => r.outputTypes.Contains(resource));
            }
            else
            {
                filtered = allRecipes.Where(r => r.ingTypes.Contains(resource));
            }

            if (FilterUnknown.Value)
            {
                filtered = filtered.Where(r => TechTreeState.instance.IsRecipeKnown(r));
            }

            return filtered.ToList();
        }

        private string GetRateString(SchematicsRecipeData recipe, int index, bool isInput)
        {
            float qty = isInput ? recipe.ingQuantities[index] : recipe.outputQuantities[index];
            float rate = qty / recipe.duration * 60f;

            // Apply machine efficiency based on Mk level
            float efficiency = 1f;
            if (machineMk >= 2) efficiency *= 1.5f;
            if (machineMk >= 3) efficiency *= 1.5f;

            rate *= efficiency;

            return $"{rate:F1}/min";
        }

        private void RefreshItemList()
        {
            filteredItems.Clear();

            if (GameDefines.instance == null) return;

            var allResources = GameDefines.instance.resources;
            if (allResources == null) return;

            foreach (var resource in allResources)
            {
                if (resource == null) continue;

                // Filter by search
                if (!string.IsNullOrEmpty(searchQuery))
                {
                    if (!resource.displayName.ToLower().Contains(searchQuery.ToLower()))
                        continue;
                }

                // Filter unknown
                if (FilterUnknown.Value && TechTreeState.instance != null)
                {
                    if (!TechTreeState.instance.IsResourceKnown(resource))
                        continue;
                }

                filteredItems.Add(resource);
            }

            // Sort alphabetically
            filteredItems = filteredItems.OrderBy(r => r.displayName).ToList();
        }

        private static Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; i++)
                pix[i] = color;
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }
    }

    /// <summary>
    /// Patch to open Recipe Book with inventory
    /// </summary>
    [HarmonyPatch(typeof(InventoryAndCraftingUI))]
    internal static class InventoryPatch
    {
        [HarmonyPatch("Open")]
        [HarmonyPostfix]
        private static void OnInventoryOpen()
        {
            // The plugin's OnGUI will handle display when inventory is open
            // This is just a hook point if needed
        }
    }
}
