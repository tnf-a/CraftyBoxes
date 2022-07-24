using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Configuration;
using CraftyBoxes.Utils;
using HarmonyLib;
using UnityEngine;

namespace CraftyBoxes.Patches;

[HarmonyPatch(typeof(Smelter), nameof(Smelter.UpdateHoverTexts))]
static class SmelterUpdateHoverTextsPatch
{
    static void Postfix(Smelter __instance)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value)
            return;

        if (CraftyBoxesPlugin.fillAllModKey.Value.MainKey == KeyCode.None) return;
        if (__instance.m_addWoodSwitch)
            __instance.m_addWoodSwitch.m_hoverText +=
                $"\n[<color=yellow><b>{CraftyBoxesPlugin.fillAllModKey.Value} + $KEY_Use</b></color>] $piece_smelter_add (Inventory & Nearby Containers)";
        if (!__instance.m_addOreSwitch)
            return;
        Switch addOreSwitch = __instance.m_addOreSwitch;
        addOreSwitch.m_hoverText +=
            $"\n[<color=yellow><b>{CraftyBoxesPlugin.fillAllModKey.Value} + $KEY_Use</b></color>] {__instance.m_addOreTooltip} (Inventory & Nearby Containers)";
    }
}

[HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddOre))]
static class SmelterOnAddOrePatch
{
    static bool Prefix(Smelter __instance, Humanoid user, ItemDrop.ItemData item, ZNetView ___m_nview)
    {
        bool pullAll = Input.GetKey(CraftyBoxesPlugin.fillAllModKey.Value.MainKey); // Used to be fillAllModKey.Value.isPressed(); something is wrong with KeyboardShortcuts always returning false
        if (!CraftyBoxesPlugin.modEnabled.Value || (!CraftyBoxesPlugin.AllowByKey() && !pullAll) || item != null ||
            __instance.GetQueueSize() >= __instance.m_maxOre)
            return true;

        Inventory inventory = user.GetInventory();


        if (__instance.m_conversion.Any(itemConversion =>
                inventory.HaveItem(itemConversion.m_from.m_itemData.m_shared.m_name) && !pullAll))
        {
            return true;
        }

        Dictionary<string, int> added = new();

        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);

        foreach (Smelter.ItemConversion itemConversion in __instance.m_conversion)
        {
            if (__instance.GetQueueSize() >= __instance.m_maxOre ||
                (added.Any() && !pullAll))
                break;

            string name = itemConversion.m_from.m_itemData.m_shared.m_name;
            if (pullAll && inventory.HaveItem(name))
            {
                ItemDrop.ItemData newItem = inventory.GetItem(name);

                if (CraftyBoxesPlugin.oreDisallowTypes.Value.Split(',').Contains(newItem.m_dropPrefab.name))
                {
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"(SmelterOnAddOrePatch) Player has {newItem.m_stack} {newItem.m_dropPrefab.name} but it's forbidden by config");
                    continue;
                }

                int amount = pullAll
                    ? Mathf.Min(
                        __instance.m_maxOre - __instance.GetQueueSize(),
                        inventory.CountItems(name))
                    : 1;

                if (!added.ContainsKey(name))
                    added[name] = 0;
                added[name] += amount;

                inventory.RemoveItem(itemConversion.m_from.m_itemData.m_shared.m_name, amount);
                //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inventory, new object[] { });

                for (int i = 0; i < amount; i++)
                    ___m_nview.InvokeRPC("AddOre", newItem.m_dropPrefab.name);

                user.Message(MessageHud.MessageType.TopLeft, $"$msg_added {amount} {name}");
                if (__instance.GetQueueSize() >= __instance.m_maxOre)
                    break;
            }

            foreach (Container c in nearbyContainers)
            {
                ItemDrop.ItemData newItem = c.GetInventory().GetItem(name);
                if (newItem == null) continue;
                if (CraftyBoxesPlugin.oreDisallowTypes.Value.Split(',').Contains(newItem.m_dropPrefab.name))
                {
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"(SmelterOnAddOrePatch) Container at {c.transform.position} has {newItem.m_stack} {newItem.m_dropPrefab.name} but it's forbidden by config");
                    continue;
                }

                int amount = pullAll
                    ? Mathf.Min(
                        __instance.m_maxOre - __instance.GetQueueSize(),
                        c.GetInventory().CountItems(name))
                    : 1;

                if (!added.ContainsKey(name))
                    added[name] = 0;
                added[name] += amount;
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Pull ALL is {pullAll}");
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"(SmelterOnAddOrePatch) Container at {c.transform.position} has {newItem.m_stack} {newItem.m_dropPrefab.name}, taking {amount}");

                c.GetInventory().RemoveItem(name, amount);
                c.Save();
                //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });

                for (int i = 0; i < amount; i++)
                    ___m_nview.InvokeRPC("AddOre", newItem.m_dropPrefab.name);

                user.Message(MessageHud.MessageType.TopLeft, $"$msg_added {amount} {name}");

                if (__instance.GetQueueSize() >= __instance.m_maxOre ||
                    !pullAll)
                    break;
            }
        }

        if (!added.Any())
            user.Message(MessageHud.MessageType.Center, "$msg_noprocessableitems");
        else
        {
            List<string> outAdded = added.Select(kvp => $"$msg_added {kvp.Value} {kvp.Key}").ToList();

            user.Message(MessageHud.MessageType.Center, string.Join("\n", outAdded));
        }

        return false;
    }
}

[HarmonyPatch(typeof(Smelter), nameof(Smelter.OnAddFuel))]
static class SmelterOnAddFuelPatch
{
    static bool Prefix(Smelter __instance, ref bool __result, ZNetView ___m_nview, Humanoid user,
        ItemDrop.ItemData item)
    {
        bool pullAll = Input.GetKey(CraftyBoxesPlugin.fillAllModKey.Value.MainKey); // Used to be fillAllModKey.Value.IsPressed(); something is wrong with KeyboardShortcuts always returning false
        Inventory inventory = user.GetInventory();
        if (!CraftyBoxesPlugin.modEnabled.Value || (!CraftyBoxesPlugin.AllowByKey() && !pullAll) || item != null ||
            inventory == null ||
            (inventory.HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name) && !pullAll))
            return true;

        __result = true;

        int added = 0;

        if (__instance.GetFuel() > __instance.m_maxFuel - 1)
        {
            user.Message(MessageHud.MessageType.Center, "$msg_itsfull");
            __result = false;
            return false;
        }

        if (pullAll && inventory.HaveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name))
        {
            int amount = (int)Mathf.Min(
                __instance.m_maxFuel - __instance.GetFuel(),
                inventory.CountItems(__instance.m_fuelItem.m_itemData.m_shared.m_name));
            inventory.RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, amount);
            //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(inventory, new object[] { });
            for (int i = 0; i < amount; i++)
                ___m_nview.InvokeRPC("AddFuel");

            added += amount;

            user.Message(MessageHud.MessageType.TopLeft,
                Localization.instance.Localize("$msg_fireadding", __instance.m_fuelItem.m_itemData.m_shared.m_name));

            __result = false;
        }

        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);

        foreach (Container c in nearbyContainers)
        {
            ItemDrop.ItemData newItem = c.GetInventory().GetItem(__instance.m_fuelItem.m_itemData.m_shared.m_name);
            if (newItem == null) continue;
            if (CraftyBoxesPlugin.fuelDisallowTypes.Value.Split(',').Contains(newItem.m_dropPrefab.name))
            {
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"(SmelterOnAddFuelPatch) Container at {c.transform.position} has {newItem.m_stack} {newItem.m_dropPrefab.name} but it's forbidden by config");
                continue;
            }
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Pull ALL is {pullAll}");
            int amount = pullAll
                ? (int)Mathf.Min(
                    __instance.m_maxFuel - __instance.GetFuel(), newItem.m_stack)
                : 1;

            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                $"(SmelterOnAddFuelPatch) Container at {c.transform.position} has {newItem.m_stack} {newItem.m_dropPrefab.name}, taking {amount}");

            c.GetInventory().RemoveItem(__instance.m_fuelItem.m_itemData.m_shared.m_name, amount);
            c.Save();
            //typeof(Inventory).GetMethod("Changed", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(c.GetInventory(), new object[] { });

            for (int i = 0; i < amount; i++)
                ___m_nview.InvokeRPC("AddFuel");

            added += amount;

            user.Message(MessageHud.MessageType.TopLeft,
                "$msg_added " + __instance.m_fuelItem.m_itemData.m_shared.m_name);

            __result = false;

            if (!pullAll || Mathf.CeilToInt(___m_nview.GetZDO().GetFloat("fuel")) >= __instance.m_maxFuel)
                return false;
        }

        user.Message(MessageHud.MessageType.Center,
            added == 0
                ? "$msg_noprocessableitems"
                : $"$msg_added {added} {__instance.m_fuelItem.m_itemData.m_shared.m_name}");

        return __result;
    }
}