using HarmonyLib;

namespace VisibleLockerInterior
{
    [HarmonyPatch(typeof(StorageContainer), nameof(StorageContainer.Awake))]
    internal class PatchStorageContainerAwake
    {
        [HarmonyPostfix]
        public static void Postfix(StorageContainer __instance) =>
            Controller.UpdateInterior(__instance);
    }

    //As of August 2025 update, StorageContainer.OnClose appears to be no longer available.
    //Therefore, it is likely this code can be safely removed.
    [HarmonyPatch(typeof(StorageContainer), "OnClose")]
    internal class PatchCloseAction
    {
        [HarmonyPostfix]
        public static void Postfix(StorageContainer __instance)
        {
            //TODO: add logging to show whether or not this gets called in game.

            Controller.UpdateInterior(__instance);
        }
    }
}
