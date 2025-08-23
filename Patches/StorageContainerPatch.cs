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
}
