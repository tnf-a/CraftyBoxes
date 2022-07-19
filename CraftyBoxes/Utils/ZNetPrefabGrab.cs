using HarmonyLib;
using UnityEngine;

namespace CraftyBoxes.Utils;

// Aedenthorn was getting this prefab almost every frame after iterating through all gameobjects...not sure why, but this should improve performance greatly in the UpdatePlacementGhost patch.
[HarmonyPatch(typeof(ZNetScene), nameof(ZNetScene.Awake))]
static class ZNetSceneAwakePatch
{
    static void Postfix(ZNetScene __instance)
    {
        CraftyBoxesPlugin.connectionVfxPrefab = __instance.GetPrefab("vfx_ExtensionConnection");
        if (CraftyBoxesPlugin.connectionVfxPrefab != null) return;
        foreach (GameObject go in (Resources.FindObjectsOfTypeAll(typeof(GameObject)) as GameObject[])!)
        {
            if (go.name != "vfx_ExtensionConnection") continue;
            CraftyBoxesPlugin.connectionVfxPrefab = go;
        }
    }
}