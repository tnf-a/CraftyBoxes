using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CraftyBoxes.Utils;
using HarmonyLib;
using UnityEngine;

namespace CraftyBoxes.Patches;

[HarmonyPatch]
public static class HaveRequirementsPatch
{
    public static MethodBase TargetMethod() => AccessTools.DeclaredMethod(typeof(Player),
        nameof(Player.HaveRequirementItems), new[] { typeof(Recipe), typeof(bool), typeof(int) });

    static void Postfix(Player __instance, ref bool __result, Recipe piece, bool discover,
        int qualityLevel, HashSet<string> ___m_knownMaterial)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || __result || discover || !CraftyBoxesPlugin.AllowByKey())
            return;
        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);
        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (!requirement.m_resItem) continue;
            int amount = requirement.GetAmount(qualityLevel);
            int invAmount = __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
            if (invAmount >= amount) continue;
            invAmount += nearbyContainers.Sum(c =>
                c.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name));
            if (invAmount < amount)
                return;
        }

        __result = true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.HaveRequirements), typeof(Piece), typeof(Player.RequirementMode))]
static class HaveRequirementsPatch2
{
    static void Postfix(Player __instance, ref bool __result, Piece piece, Player.RequirementMode mode,
        HashSet<string> ___m_knownMaterial, Dictionary<string, int> ___m_knownStations)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || __result || __instance?.transform?.position == null ||
            !CraftyBoxesPlugin.AllowByKey())
            return;

        if (piece.m_craftingStation)
        {
            if (mode is Player.RequirementMode.IsKnown or Player.RequirementMode.CanAlmostBuild)
            {
                if (!___m_knownStations.ContainsKey(piece.m_craftingStation.m_name))
                {
                    return;
                }
            }
            else if (!CraftingStation.HaveBuildStationInRange(piece.m_craftingStation.m_name,
                         __instance.transform.position))
            {
                return;
            }
        }

        if (piece.m_dlc.Length > 0 && !DLCMan.instance.IsDLCInstalled(piece.m_dlc))
        {
            return;
        }

        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);

        foreach (Piece.Requirement requirement in piece.m_resources)
        {
            if (requirement.m_resItem && requirement.m_amount > 0)
            {
                switch (mode)
                {
                    case Player.RequirementMode.IsKnown
                        when !___m_knownMaterial.Contains(requirement.m_resItem.m_itemData.m_shared.m_name):
                        return;
                    case Player.RequirementMode.CanAlmostBuild when __instance.GetInventory()
                        .HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name):
                        continue;
                    case Player.RequirementMode.CanAlmostBuild:
                    {
                        bool hasItem = nearbyContainers.Any(c =>
                            c.GetInventory().HaveItem(requirement.m_resItem.m_itemData.m_shared.m_name));

                        if (!hasItem)
                            return;
                        break;
                    }
                    case Player.RequirementMode.CanBuild
                        when __instance.GetInventory().CountItems(requirement.m_resItem.m_itemData.m_shared.m_name) <
                             requirement.m_amount:
                    {
                        int hasItems = __instance.GetInventory()
                            .CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                        foreach (Container c in nearbyContainers)
                        {
                            try
                            {
                                hasItems += c.GetInventory()
                                    .CountItems(requirement.m_resItem.m_itemData.m_shared.m_name);
                                if (hasItems >= requirement.m_amount)
                                {
                                    break;
                                }
                            }
                            catch
                            {
                                // ignored
                            }
                        }

                        if (hasItems < requirement.m_amount)
                            return;
                        break;
                    }
                }
            }
        }

        __result = true;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.ConsumeResources))]
static class ConsumeResourcesPatch
{
    static bool Prefix(Player __instance, Piece.Requirement[] requirements, int qualityLevel)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey())
            return true;

        Inventory pInventory = __instance.GetInventory();
        List<Container> nearbyContainers = Functions.GetNearbyContainers(__instance.transform.position);
        foreach (Piece.Requirement requirement in requirements)
        {
            if (!requirement.m_resItem) continue;
            int totalRequirement = requirement.GetAmount(qualityLevel);
            if (totalRequirement <= 0)
                continue;

            string reqName = requirement.m_resItem.m_itemData.m_shared.m_name;
            int totalAmount = pInventory.CountItems(reqName);
            CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                $"(ConsumeResourcesPatch) Have {totalAmount}/{totalRequirement} {reqName} in player inventory");
            pInventory.RemoveItem(reqName, Math.Min(totalAmount, totalRequirement));

            if (totalAmount >= totalRequirement) continue;
            foreach (Container c in nearbyContainers)
            {
                Inventory cInventory = c.GetInventory();
                int thisAmount = Mathf.Min(cInventory.CountItems(reqName), totalRequirement - totalAmount);

                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                    $"(ConsumeResourcesPatch) Container at {c.transform.position} has {cInventory.CountItems(reqName)}");

                if (thisAmount == 0)
                    continue;


                for (int i = 0; i < cInventory.GetAllItems().Count; ++i)
                {
                    ItemDrop.ItemData item = cInventory.GetItem(i);
                    if (item.m_shared.m_name != reqName) continue;
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"(ConsumeResourcesPatch) Got stack of {item.m_stack} {reqName}");
                    int stackAmount = Mathf.Min(item.m_stack, totalRequirement - totalAmount);
                    if (stackAmount == item.m_stack)
                    {
                        cInventory.RemoveItem(item);
                        --i;
                    }
                    else
                        item.m_stack -= stackAmount;

                    totalAmount += stackAmount;
                    CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
                        $"(ConsumeResourcesPatch) Total amount is now {totalAmount}/{totalRequirement} {reqName}");

                    if (totalAmount >= totalRequirement)
                        break;
                }

                c.Save();
                cInventory.Changed();

                if (totalAmount < totalRequirement) continue;
                CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug($"(ConsumeResourcesPatch) Consumed enough {reqName}");
                break;
            }
        }

        return false;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacementGhost))]
static class UpdatePlacementGhostPatch
{
    static void Postfix(Player __instance, bool flashGuardStone)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.showGhostConnections.Value)
        {
            return;
        }


        GameObject? placementGhost =
            __instance.m_placementGhost != null ? __instance.m_placementGhost : null;
        if (placementGhost == null)
        {
            return;
        }

        Container ghostContainer = placementGhost.GetComponent<Container>();
        if (ghostContainer == null)
        {
            return;
        }

        if (CraftyBoxesPlugin.connectionVfxPrefab == null)
        {
            return;
        }

        if (CraftingStation.m_allStations == null) return;
        bool bAddedConnections = false;
        foreach (CraftingStation station in CraftingStation.m_allStations)
        {
            int connectionIndex = Functions.ConnectionExists(station);
            bool connectionAlreadyExists = connectionIndex != -1;

            if (Vector3.Distance(station.transform.position, placementGhost.transform.position) <
                CraftyBoxesPlugin.mRange.Value)
            {
                bAddedConnections = true;

                Vector3 connectionStartPos = station.GetConnectionEffectPoint();
                Vector3 connectionEndPos = placementGhost.transform.position +
                                           Vector3.up * CraftyBoxesPlugin.ghostConnectionStartOffset.Value;

                CraftyBoxesPlugin.ConnectionParams tempConnection;
                if (!connectionAlreadyExists)
                {
                    tempConnection = new CraftyBoxesPlugin.ConnectionParams
                    {
                        stationPos = station.GetConnectionEffectPoint(),
                        connection = UnityEngine.Object.Instantiate(
                            CraftyBoxesPlugin.connectionVfxPrefab,
                            connectionStartPos, Quaternion.identity)
                    };
                }
                else
                {
                    tempConnection = CraftyBoxesPlugin.ContainerConnections[connectionIndex];
                }

                if (tempConnection.connection != null)
                {
                    Vector3 vector3 = connectionEndPos - connectionStartPos;
                    Quaternion quaternion = Quaternion.LookRotation(vector3.normalized);
                    tempConnection.connection.transform.position = connectionStartPos;
                    tempConnection.connection.transform.rotation = quaternion;
                    tempConnection.connection.transform.localScale = new Vector3(1f, 1f, vector3.magnitude);
                }

                if (!connectionAlreadyExists)
                {
                    CraftyBoxesPlugin.ContainerConnections.Add(tempConnection);
                }
            }
            else if (connectionAlreadyExists)
            {
                UnityEngine.Object.Destroy(
                    CraftyBoxesPlugin.ContainerConnections[connectionIndex].connection);
                CraftyBoxesPlugin.ContainerConnections.RemoveAt(connectionIndex);
            }
        }

        if (!bAddedConnections || CraftyBoxesPlugin.context == null) return;
        CraftyBoxesPlugin.context.CancelInvoke("StopConnectionEffects");
        CraftyBoxesPlugin.context.Invoke("StopConnectionEffects",
            CraftyBoxesPlugin.ghostConnectionRemovalDelay.Value);
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.UpdatePlacement))]
static class UpdatePlacementPatch
{
    static bool Prefix(Player __instance, bool takeInput, float dt, PieceTable ___m_buildPieces,
        GameObject ___m_placementGhost)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey() ||
            !CraftyBoxesPlugin.pullItemsKey.Value.IsPressed() || !__instance.InPlaceMode() ||
            !takeInput || Hud.IsPieceSelectionVisible())
        {
            return true;
        }

        if (!ZInput.GetButtonDown("Attack") && !ZInput.GetButtonDown("JoyPlace")) return true;
        Piece selectedPiece = ___m_buildPieces.GetSelectedPiece();
        if (selectedPiece == null) return false;
        if (selectedPiece.m_repairPiece)
            return true;
        if (___m_placementGhost == null) return false;
        Player.PlacementStatus placementStatus = __instance.m_placementStatus;
        if (placementStatus != 0) return false;
        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
            $"(UpdatePlacementPatch) Pulling resources to player inventory for piece {selectedPiece.name}");
        Functions.PullResources(__instance, selectedPiece.m_resources, 0);

        return false;
    }
}

[HarmonyPatch(typeof(Player), nameof(Player.InPlaceMode))]
static class Player_InPlaceMode_Patch
{
    static void Postfix(Player __instance, ref bool __result)
    {
        if (__result == false)
        {
            Functions.StopConnectionEffects();
        }
    }
}