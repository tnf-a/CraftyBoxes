using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using CraftyBoxes.Compatibility.WardIsLove;
using HarmonyLib;
using UnityEngine;

namespace CraftyBoxes.Utils;

public static class Functions
{
    /*public static IEnumerator AddContainer(Container container, ZNetView nview)
    {
        yield return null;
        try
        {
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                $"Checking {container.name} {nview != null} {nview?.GetZDO() != null} {nview?.GetZDO()?.GetLong("creator".GetStableHashCode(), 0L)}");
            if (container.GetInventory() != null && nview?.GetZDO() != null && (container.name.StartsWith("piece_") ||
                    container.name.StartsWith("Container") ||
                    nview.GetZDO().GetLong("creator".GetStableHashCode()) != 0))
            {
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Adding {container.name}");
                CraftyBoxesPlugin.ContainerList.Add(container);
            }
        }
        catch
        {
            // ignored
        }

        yield break;
    }*/

    public static void CheckOdinsQOLConfig()
    {
        CraftyBoxesPlugin.itemStackSizeMultiplier = 0;
        CraftyBoxesPlugin.itemWeightReduction = 0;
        Dictionary<string, PluginInfo> pluginInfos = Chainloader.PluginInfos;
        foreach (PluginInfo plugin in
                 pluginInfos.Values.Where(plugin => plugin?.Metadata.GUID == "com.odinplusqol.mod"))
        {
            CraftyBoxesPlugin.odinQolInstalled = CraftyBoxesPlugin.modEnabled.Value;
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug("Found OdinPlusQoL");
            foreach (ConfigDefinition key in plugin.Instance.Config.Keys)
            {
                switch (key.Key)
                {
                    case "Item Stack Increase":
                        CraftyBoxesPlugin.itemStackSizeMultiplier = (float)plugin.Instance.Config[key].BoxedValue;
                        break;
                    case "Item Weight Increase":
                        CraftyBoxesPlugin.itemWeightReduction = (float)plugin.Instance.Config[key].BoxedValue;
                        break;
                }
            }
        }
    }

    public static void AddContainer(Container container, ZNetView nview)
    {
        try
        {
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                $"Checking {container.name} {nview != null} {nview?.GetZDO() != null} {nview?.GetZDO()?.GetLong("creator".GetStableHashCode(), 0L)}");
            if (container.GetInventory() == null || nview?.GetZDO() == null ||
                (!container.name.StartsWith("piece_", StringComparison.Ordinal) &&
                 !container.name.StartsWith("Container", StringComparison.Ordinal) &&
                 nview.GetZDO().GetLong("creator".GetStableHashCode()) == 0)) return;
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Adding {container.name}");
            CraftyBoxesPlugin.ContainerList.Add(container);
        }
        catch
        {
            // ignored
        }
    }


    public static List<Container> GetNearbyContainers(Vector3 center)
    {
        List<Container> containers = new();
        foreach (Container container in CraftyBoxesPlugin.ContainerList)
        {
            if (container != null
                && container.GetComponentInParent<Piece>() != null
                && Player.m_localPlayer != null
                && container?.transform != null
                && container.GetInventory() != null
                && (CraftyBoxesPlugin.mRange.Value <= 0 || Vector3.Distance(center, container.transform.position) <
                    CraftyBoxesPlugin.mRange.Value)
                && container.CheckAccess(Player.m_localPlayer.GetPlayerID()) && !container.IsInUse())
            {
                if (WardIsLovePlugin.IsLoaded() && WardIsLovePlugin.WardEnabled()!.Value)
                {
                    if (!WardMonoscript.CheckInWardMonoscript(container.transform.position))
                    {
                        container.Load();
                        containers.Add(container);
                        continue;
                    }

                    if (!WardMonoscript.CheckAccess(container.transform.position, 0f, false)) continue;
                    //container.GetComponent<ZNetView>()?.ClaimOwnership();
                    container.Load();
                    containers.Add(container);
                }
                else
                {
                    container.Load();
                    containers.Add(container);
                }
            }
        }


        return containers;
    }

    public static int ConnectionExists(CraftingStation station)
    {
        foreach (CraftyBoxesPlugin.ConnectionParams c in CraftyBoxesPlugin.ContainerConnections.Where(c =>
                     Vector3.Distance(c.stationPos, station.GetConnectionEffectPoint()) < 0.1f))
        {
            return CraftyBoxesPlugin.ContainerConnections.IndexOf(c);
        }

        return -1;
    }

    public static void StopConnectionEffects()
    {
        if (CraftyBoxesPlugin.ContainerConnections.Count > 0)
        {
            foreach (CraftyBoxesPlugin.ConnectionParams c in CraftyBoxesPlugin.ContainerConnections)
            {
                UnityEngine.Object.Destroy(c.connection);
            }
        }

        CraftyBoxesPlugin.ContainerConnections.Clear();
    }

    internal static void PullResources(Player player, Piece.Requirement[] resources, int qualityLevel)
    {
        Inventory pInventory = Player.m_localPlayer.GetInventory();
        List<Container> nearbyContainers = GetNearbyContainers(Player.m_localPlayer.transform.position);
        bool skipThis = false;
        foreach (Piece.Requirement requirement in resources)
        {
            if (requirement.m_resItem)
            {
                int totalRequirement = requirement.GetAmount(qualityLevel);
                if (totalRequirement <= 0)
                    continue;

                string reqName = requirement.m_resItem.m_itemData.m_shared.m_name;
                int totalAmount = 0;
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"have {totalAmount}/{totalRequirement} {reqName} in player inventory");

                foreach (Container c in nearbyContainers)
                {
                    Inventory cInventory = c.GetInventory();
                    int thisAmount = Mathf.Min(cInventory.CountItems(reqName), totalRequirement - totalAmount);

                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"Container at {c.transform.position} has {cInventory.CountItems(reqName)}");

                    if (thisAmount == 0)
                        continue;


                    for (int i = 0; i < cInventory.GetAllItems().Count; ++i)
                    {
                        ItemDrop.ItemData item = cInventory.GetItem(i);
                        if (item.m_shared.m_name != reqName) continue;
                        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Got stack of {item.m_stack} {reqName}");
                        int stackAmount = Mathf.Min(item.m_stack, totalRequirement - totalAmount);

                        if (!pInventory.HaveEmptySlot())
                            stackAmount =
                                Math.Min(
                                    pInventory.FindFreeStackSpace(item.m_shared.m_name), stackAmount);
                        skipThis = false;
                        foreach (string s in CraftyBoxesPlugin.CFCItemDisallowTypes.Value.Split(','))
                        {
                            if (!requirement.m_resItem.m_itemData.m_dropPrefab.name.Contains(s)) continue;
                            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                                $"Can't send {s} to player it is contained in the ItemDisallowTypes list for CraftFromContainers");
                            skipThis = true;
                        }

                        if (skipThis) continue;

                        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Sending {stackAmount} {reqName} to player");

                        ItemDrop.ItemData sendItem = item.Clone();
                        sendItem.m_stack = stackAmount;

                        if (CraftyBoxesPlugin.odinQolInstalled)
                        {
                            if (CraftyBoxesPlugin.itemStackSizeMultiplier > 0)
                            {
                                sendItem.m_shared.m_weight = ApplyModifierValue(sendItem.m_shared.m_weight,
                                    CraftyBoxesPlugin.itemWeightReduction);

                                if (sendItem.m_shared.m_maxStackSize > 1)
                                    if (CraftyBoxesPlugin.itemStackSizeMultiplier >= 1)
                                        sendItem.m_shared.m_maxStackSize =
                                            requirement.m_resItem.m_itemData.m_shared.m_maxStackSize *
                                            (int)CraftyBoxesPlugin.itemStackSizeMultiplier;
                            }
                        }
                        else
                        {
                            sendItem.m_shared.m_maxStackSize =
                                requirement.m_resItem.m_itemData.m_shared.m_maxStackSize;
                        }

                        pInventory.AddItem(sendItem);

                        if (stackAmount == item.m_stack)
                        {
                            cInventory.RemoveItem(item);
                            --i;
                        }
                        else
                            item.m_stack -= stackAmount;

                        totalAmount += stackAmount;
                        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                            $"Total amount is now {totalAmount}/{totalRequirement} {reqName}");

                        if (totalAmount >= totalRequirement)
                            break;
                    }

                    c.Save();
                    cInventory.Changed();

                    if (totalAmount < totalRequirement) continue;
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"Pulled enough {reqName}");
                    break;
                }
            }

            if (CraftyBoxesPlugin.pulledMessage.Value?.Length > 0)
                player.Message(MessageHud.MessageType.Center, CraftyBoxesPlugin.pulledMessage.Value);
        }
    }

    public static bool HaveRequiredItemCount(Player player, Piece piece, Player.RequirementMode mode,
        Inventory inventory, HashSet<string> knownMaterial)
    {
        List<Container> nearbyContainers = GetNearbyContainers(player.transform.position);

        foreach (Piece.Requirement resource in piece.m_resources)
        {
            if (resource.m_resItem && resource.m_amount > 0)
            {
                switch (mode)
                {
                    case Player.RequirementMode.CanBuild:
                        int inInventory = inventory.CountItems(resource.m_resItem.m_itemData.m_shared.m_name);
                        int itemCount = inInventory;
                        if (itemCount < resource.m_amount)
                        {
                            bool enoughInContainers = false;
                            foreach (Container c in nearbyContainers)
                            {
                                try
                                {
                                    itemCount += c.GetInventory()
                                        .CountItems(resource.m_resItem.m_itemData.m_shared.m_name);
                                    if (itemCount < resource.m_amount) continue;
                                    enoughInContainers = true;
                                    break;
                                }
                                catch
                                {
                                    // ignored
                                }
                            }

                            if (!enoughInContainers)
                            {
                                return false;
                            }
                        }

                        continue;
                    case Player.RequirementMode.IsKnown:
                        if (!knownMaterial.Contains(resource.m_resItem.m_itemData.m_shared.m_name))
                        {
                            return false;
                        }

                        continue;
                    case Player.RequirementMode.CanAlmostBuild:
                        if (!inventory.HaveItem(resource.m_resItem.m_itemData.m_shared.m_name))
                        {
                            bool enoughInContainers = nearbyContainers.Any(c =>
                                c.GetInventory().HaveItem(resource.m_resItem.m_itemData.m_shared.m_name));

                            if (!enoughInContainers)
                            {
                                return false;
                            }
                        }

                        continue;
                    default:
                        continue;
                }
            }
        }

        return true;
    }

    private static float ApplyModifierValue(float targetValue, float value)
    {
        if (value <= -100)
            value = -100;

        float newValue;

        if (value >= 0)
            newValue = targetValue + targetValue / 100 * value;
        else
            newValue = targetValue - targetValue / 100 * (value * -1);

        return newValue;
    }
}