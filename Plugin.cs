using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System.Reflection;


namespace VisibleLockerInterior
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public static void Log(LogLevel level, string message) =>
            MainLogger?.Log(level, message ?? "NULL");

        private static BepInEx.Logging.ManualLogSource? MainLogger;

        //public static ConfigEntry<bool> modEnabled;
        public Harmony Harmony { get; } = new Harmony(MyPluginInfo.PLUGIN_GUID);

        private void OnEnable()
        {
            MainLogger = this.Logger;

            //modEnabled = Config.Bind<bool>("General", "Enabled", true, "Enable this mod");
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
            Log(LogLevel.Info, $"{MyPluginInfo.PLUGIN_NAME} {MyPluginInfo.PLUGIN_VERSION} is loaded!");
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
        }
    }
}
