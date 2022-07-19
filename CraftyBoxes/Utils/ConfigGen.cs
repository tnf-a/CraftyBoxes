using BepInEx.Configuration;
using UnityEngine;

namespace CraftyBoxes.Utils;

public static class ConfigGen
{
    internal static void Generate()
    {
        CraftyBoxesPlugin.modEnabled = CraftyBoxesPlugin.context.config("General", "Enabled", true, "Enable this mod");

        CraftyBoxesPlugin.mRange = CraftyBoxesPlugin.context.config("General", "ContainerRange", 10f,
            "The maximum range from which to pull items from");
        CraftyBoxesPlugin.resourceString = CraftyBoxesPlugin.context.config("General", "ResourceCostString", "{0}/{1}",
            "String used to show required and available resources. {0} is replaced by how much is available, and {1} is replaced by how much is required. Set to nothing to leave it as default.",
            false);
        CraftyBoxesPlugin.flashColor = CraftyBoxesPlugin.context.config("General", "FlashColor", Color.yellow,
            "Resource amounts will flash to this colour when coming from containers", false);
        CraftyBoxesPlugin.unFlashColor = CraftyBoxesPlugin.context.config("General", "UnFlashColor", Color.white,
            "Resource amounts will flash from this colour when coming from containers (set both colors to the same color for no flashing)",
            false);
        CraftyBoxesPlugin.pulledMessage = CraftyBoxesPlugin.context.config("General", "PulledMessage",
            "Pulled items to inventory",
            "Message to show after pulling items to player inventory", false);
        CraftyBoxesPlugin.fuelDisallowTypes = CraftyBoxesPlugin.context.config("General", "FuelDisallowTypes",
            "RoundLog,FineWood",
            "Types of item to disallow as fuel (i.e. anything that is consumed), comma-separated. Uses Prefab names.");
        CraftyBoxesPlugin.oreDisallowTypes = CraftyBoxesPlugin.context.config("General", "OreDisallowTypes",
            "RoundLog,FineWood",
            "Types of item to disallow as ore (i.e. anything that is transformed), comma-separated). Uses Prefab names.");

        CraftyBoxesPlugin.showGhostConnections = CraftyBoxesPlugin.context.config("Station Connections",
            "ShowConnections", false,
            "If true, will display connections to nearby workstations within range when building containers", false);
        CraftyBoxesPlugin.ghostConnectionStartOffset = CraftyBoxesPlugin.context.config("Station Connections",
            "ConnectionStartOffset", 1.25f,
            "Height offset for the connection VFX start position", false);
        CraftyBoxesPlugin.ghostConnectionRemovalDelay =
            CraftyBoxesPlugin.context.config("Station Connections", "ConnectionRemoveDelay", 0.05f, "", false);

        CraftyBoxesPlugin.switchPrevent = CraftyBoxesPlugin.context.config("Hot Keys", "SwitchPrevent", false,
            "If true, holding down the PreventModKey modifier key will allow this mod's behavior; If false, holding down the key will prevent it.",
            false);

        CraftyBoxesPlugin.preventModKey = CraftyBoxesPlugin.context.config("Hot Keys", "PreventModKey",
            new KeyboardShortcut(KeyCode.LeftAlt),
            new ConfigDescription(
                "Modifier key to toggle fuel and ore filling behaviour when down. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html",
                new CraftyBoxesPlugin.AcceptableShortcuts()), false);
        CraftyBoxesPlugin.pullItemsKey = CraftyBoxesPlugin.context.config("Hot Keys", "PullItemsKey",
            new KeyboardShortcut(KeyCode.LeftControl),
            new ConfigDescription(
                "Holding down this key while crafting or building will pull resources into your inventory instead of building. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html",
                new CraftyBoxesPlugin.AcceptableShortcuts()), false);
        CraftyBoxesPlugin.fillAllModKey = CraftyBoxesPlugin.context.config("Hot Keys", "FillAllModKey",
            new KeyboardShortcut(KeyCode.LeftShift),
            new ConfigDescription(
                "Modifier key to pull all available fuel or ore when down. Use https://docs.unity3d.com/Manual/ConventionalGameInput.html",
                new CraftyBoxesPlugin.AcceptableShortcuts()), false);
    }
}