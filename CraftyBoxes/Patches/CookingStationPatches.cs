using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using CraftyBoxes.Utils;
using HarmonyLib;

namespace CraftyBoxes.Patches;

[HarmonyPatch(typeof(CookingStation), nameof(CookingStation.OnAddFuelSwitch))]
static class CookingStationOnAddFuelSwitchPatch
{
    static bool Prefix(CookingStation __instance, ref bool __result, Humanoid user, ItemDrop.ItemData item,
        ZNetView ___m_nview)
    {
        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"(CookingStationOnAddFuelSwitchPatch) Looking for fuel");

        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey() || item != null ||
            __instance.GetFuel() > __instance.m_maxFuel - 1 ||
            user.GetInventory().HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name))
            return true;

        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
            $"(CookingStationOnAddFuelSwitchPatch) Missing fuel in player inventory");


        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);

        foreach (Container c in nearbyContainers)
        {
            ItemDrop.ItemData fuelItem = c.GetInventory().GetItem(__instance.m_fuelItem.m_itemData.m_shared.m_name);
            if (fuelItem == null) continue;
            if (((IList)CraftyBoxesPlugin.fuelDisallowTypes.Value.Split(',')).Contains(fuelItem.m_dropPrefab.name))
            {
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"(CookingStationOnAddFuelSwitchPatch) Container at {c.transform.position} has {fuelItem.m_stack} {fuelItem.m_dropPrefab.name} but it's forbidden by config");
                continue;
            }

            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                $"(CookingStationOnAddFuelSwitchPatch) Container at {c.transform.position} has {fuelItem.m_stack} {fuelItem.m_dropPrefab.name}, taking one");
            c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, 1);
            c.Save();
            //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
            user.Message(MessageHud.MessageType.Center,
                "$msg_added " + __instance.m_fuelItem.m_itemData.m_shared.m_name);
            ___m_nview.InvokeRPC("AddFuel", Array.Empty<object>());
            __result = true;
            return false;
        }

        return true;
    }
}

[HarmonyPatch(typeof(CookingStation), nameof(CookingStation.FindCookableItem))]
static class CookingStationFindCookableItemPatch
{
    static void Postfix(CookingStation __instance, ref ItemDrop.ItemData __result)
    {
        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"(CookingStationFindCookableItemPatch) Looking for cookable");

        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey() || __result != null ||
            (__instance.m_requireFire && !__instance.IsFireLit() || __instance.GetFreeSlot() == -1))
            return;

        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
            $"(CookingStationFindCookableItemPatch) Missing cookable in player inventory");


        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);

        foreach (CookingStation.ItemConversion itemConversion in __instance.m_conversion)
        {
            foreach (Container c in nearbyContainers)
            {
                ItemDrop.ItemData item = c.GetInventory().GetItem(itemConversion.m_from.m_itemData.m_shared.m_name);
                if (item == null) continue;
                if (((IList)CraftyBoxesPlugin.oreDisallowTypes.Value.Split(',')).Contains(item.m_dropPrefab.name))
                {
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"(CookingStationFindCookableItemPatch) Container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name} but it's forbidden by config");
                    continue;
                }

                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"(CookingStationFindCookableItemPatch) Container at {c.transform.position} has {item.m_stack} {item.m_dropPrefab.name}, taking one");
                __result = item;
                c.GetInventory().RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, 1);
                c.Save();
                //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });
                return;
            }
        }
    }
}