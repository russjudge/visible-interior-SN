using BepInEx;
using HarmonyLib;
using System.Reflection;


namespace VisibleLockerInterior
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {

        public Harmony Harmony { get; } = new Harmony(MyPluginInfo.PLUGIN_GUID);

        private void OnEnable()
        {
            Harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        private void OnDisable()
        {
            Harmony.UnpatchSelf();
        }
    }
}
