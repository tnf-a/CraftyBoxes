using System;
using System.Collections.Generic;
using System.Linq;
using CraftyBoxes.Utils;
using HarmonyLib;
using UnityEngine.UI;

namespace CraftyBoxes.Patches;

[HarmonyPatch(typeof(Hud), nameof(Hud.SetupPieceInfo))]
public class HUDPatches
{
    private static void Postfix(Piece piece, Text ___m_buildSelection)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || piece == null ||
            piece.m_name == "$piece_repair")
            return;
        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
            "(HUDPatches) Setting color and amount for number of pieces the player can make.");
        List<Container> containers =
            Functions.GetNearbyContainers(Player.m_localPlayer.transform.position);
        int num = piece.m_resources.Select<Piece.Requirement, int>(
            (Func<Piece.Requirement, int>)(resource =>
                (Player.m_localPlayer.GetInventory()
                     .CountItems(resource.m_resItem.m_itemData.m_shared.m_name) +
                 containers.Sum((Func<Container, int>)(container =>
                     container.GetInventory()
                         .CountItems(resource.m_resItem.m_itemData.m_shared.m_name)))) /
                resource.m_amount)).AddItem(int.MaxValue).Min();
        string color = num > 0 ? "green" : "red";
        ___m_buildSelection.text = Localization.instance.Localize(piece.m_name) + $" (<color={color}>" +
                                   (num == int.MaxValue ? "∞" : num.ToString()) + "</color>)";
    }
}