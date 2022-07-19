using System.Collections.Generic;
using System.Linq;
using CraftyBoxes.Utils;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace CraftyBoxes.Patches;

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.Update))]
static class InventoryGuiUpdatePatch
{
    static void Prefix(InventoryGui __instance, Animator ___m_animator)
    {
        if (Player.m_localPlayer && CraftyBoxesPlugin.wasAllowed != CraftyBoxesPlugin.AllowByKey() &&
            ___m_animator.GetBool("visible"))
        {
            __instance.UpdateCraftingPanel();
        }
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.SetupRequirement))]
static class InventoryGuiSetupRequirementPatch
{
    static void Postfix(InventoryGui __instance, Transform elementRoot, Piece.Requirement req, Player player,
        bool craft, int quality)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey())
            return;
        if (req.m_resItem == null) return;
        int invAmount = player.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name);
        int amount = req.GetAmount(quality);
        if (amount <= 0)
        {
            return;
        }

        Text text = elementRoot.transform.Find("res_amount").GetComponent<Text>();
        if (invAmount < amount)
        {
            List<Container> nearbyContainers = Functions.GetNearbyContainers(Player.m_localPlayer.transform.position);
            invAmount +=
                nearbyContainers.Sum(c => c.GetInventory().CountItems(req.m_resItem.m_itemData.m_shared.m_name));

            if (invAmount >= amount)
                text.color = ((Mathf.Sin(Time.time * 10f) > 0f)
                    ? CraftyBoxesPlugin.flashColor.Value
                    : CraftyBoxesPlugin.unFlashColor.Value);
        }

        text.text = CraftyBoxesPlugin.resourceString.Value.Trim().Length > 0
            ? string.Format(CraftyBoxesPlugin.resourceString.Value, invAmount, amount)
            : amount.ToString();
        text.resizeTextForBestFit = true;
    }
}

[HarmonyPatch(typeof(InventoryGui), nameof(InventoryGui.OnCraftPressed))]
static class InventoryGuiOnCraftPressedPatch
{
    static bool Prefix(InventoryGui __instance, KeyValuePair<Recipe, ItemDrop.ItemData> ___m_selectedRecipe,
        ItemDrop.ItemData ___m_craftUpgradeItem)
    {
        if (!CraftyBoxesPlugin.modEnabled.Value || !CraftyBoxesPlugin.AllowByKey() ||
            !CraftyBoxesPlugin.pullItemsKey.Value.IsPressed() || ___m_selectedRecipe.Key == null)
            return true;

        int qualityLevel = (___m_craftUpgradeItem != null) ? (___m_craftUpgradeItem.m_quality + 1) : 1;
        if (qualityLevel > ___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_maxQuality)
        {
            return true;
        }

        CraftyBoxesPlugin.CraftyBoxesLogger.LogDebug(
            $"(InventoryGuiOnCraftPressedPatch) Pulling resources to player inventory for crafting item {___m_selectedRecipe.Key.m_item.m_itemData.m_shared.m_name}");
        Functions.PullResources(Player.m_localPlayer, ___m_selectedRecipe.Key.m_resources, qualityLevel);

        return false;
    }
}