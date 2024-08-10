using HarmonyLib;
using Kitchen;
using KitchenData;
using KitchenMods;
using System.Reflection;
using Unity.Entities;

namespace KitchenAutomationPlus.Patches
{
    [HarmonyPatch]
    static class ApplianceComponentHelpers_Patch
    {
        [HarmonyTargetMethod]
        static MethodBase ApplianceComponentSetDynamic_TargetMethod()
        {
            ;
            return AccessTools.FirstMethod(typeof(ApplianceComponentHelpers), method => method.Name.Contains("SetDynamic") && method.IsGenericMethod).MakeGenericMethod(typeof(IApplianceProperty));
        }

        [HarmonyPrefix]
        [HarmonyPriority(int.MinValue)]
        static bool ApplianceComponentSetDynamic_Prefix(bool __runOriginal, EntityContext ctx, Entity e, IApplianceProperty component)
        {
            if (!__runOriginal ||
                !(component is IModComponent))
                return true;
            ctx.Set(e, (dynamic)component);
            return false;
        }
    }

    [HarmonyPatch]
    static class ItemComponentHelpers_Patch
    {
        [HarmonyTargetMethod]
        static MethodBase ItemComponentSetDynamic_TargetMethod()
        {
            ;
            return AccessTools.FirstMethod(typeof(ItemComponentHelpers), method => method.Name.Contains("SetDynamic") && method.IsGenericMethod).MakeGenericMethod(typeof(IItemProperty));
        }

        [HarmonyPrefix]
        [HarmonyPriority(int.MinValue)]
        static bool ItemComponentSetDynamic_Prefix(bool __runOriginal, EntityContext ctx, Entity e, IItemProperty component)
        {
            if (!__runOriginal ||
                !(component is IModComponent))
                return true;
            ctx.Set(e, (dynamic)component);
            return false;
        }
    }
}
