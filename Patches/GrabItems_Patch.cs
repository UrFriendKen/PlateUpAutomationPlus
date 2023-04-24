using HarmonyLib;
using Kitchen;
using Unity.Entities;

namespace KitchenAutomationPlus.Patches
{
    [HarmonyPatch]
    static class GrabItems_Patch
    {
        [HarmonyPatch(typeof(GrabItems), "AttemptGrabHolder")]
        [HarmonyPrefix]
        static bool AttemptGrabHolder_Prefix(Entity target, ref bool __result)
        {
            if (GrabItemsReversible.RequireCConveyPushItemsReversible(target, out CConveyPushItemsReversible push) && push.State != CConveyPushItems.ConveyState.None)
            {
                __result = false;
                return false;
            }
            return true;
        }
    }
}
