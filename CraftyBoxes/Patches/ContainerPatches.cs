using HarmonyLib;

namespace CraftyBoxes.Patches;

[HarmonyPatch(typeof(Container), nameof(Container.Awake))]
static class ContainerAwakePatch
{
    static void Postfix(Container __instance, ZNetView ___m_nview)
    {
       Utils.Functions.AddContainer(__instance, ___m_nview);
    }
}

[HarmonyPatch(typeof(Container), nameof(Container.OnDestroyed))]
static class ContainerOnDestroyedPatch
{
    static void Prefix(Container __instance)
    {
        CraftyBoxesPlugin.ContainerList.Remove(__instance);
    }
}