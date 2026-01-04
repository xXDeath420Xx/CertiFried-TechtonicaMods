using System.Reflection;
using BepInEx;
using BepInEx.Logging;
using HarmonyLib;

namespace BeltImmunity
{
    [BepInPlugin(MyGUID, PluginName, VersionString)]
    public class BeltImmunityPlugin : BaseUnityPlugin
    {
        private const string MyGUID = "com.certifired.BeltImmunity";
        private const string PluginName = "BeltImmunity";
        private const string VersionString = "1.0.0";

        private static readonly Harmony Harmony = new Harmony(MyGUID);
        public static ManualLogSource Log;

        private void Awake()
        {
            Log = Logger;
            Logger.LogInfo($"{PluginName} v{VersionString} is loading...");
            Harmony.PatchAll(typeof(PlayerFirstPersonControllerPatch));
            Logger.LogInfo($"{PluginName} v{VersionString} loaded - You are now immune to conveyor belt movement!");
        }
    }

    [HarmonyPatch(typeof(PlayerFirstPersonController))]
    public static class PlayerFirstPersonControllerPatch
    {
        private static FieldInfo ridingBeltField;

        [HarmonyPatch("CalculateBeltMovement")]
        [HarmonyPrefix]
        static bool SetRidingBeltFalse(PlayerFirstPersonController __instance)
        {
            // Cache the field info for performance
            if (ridingBeltField == null)
            {
                ridingBeltField = typeof(PlayerFirstPersonController).GetField("ridingBelt",
                    BindingFlags.NonPublic | BindingFlags.Instance);
            }

            if (ridingBeltField != null)
            {
                ridingBeltField.SetValue(__instance, false);
            }

            // Skip the original method entirely
            return false;
        }
    }
}
