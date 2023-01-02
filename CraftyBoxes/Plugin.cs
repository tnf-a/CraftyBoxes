﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using CraftyBoxes.Utils;
using HarmonyLib;
using JetBrains.Annotations;
using ServerSync;
using UnityEngine;

namespace CraftyBoxes
{
    [BepInPlugin(ModGuid, ModName, ModVersion)]
    [BepInIncompatibility("com.odinplusqol.mod")]
    [BepInIncompatibility("aedenthorn.CraftFromContainers")]
    public class CraftyBoxesPlugin : BaseUnityPlugin
    {
        internal const string ModName = "OdinsCraftyBoxes";
        internal const string ModVersion = "1.0.8";
        internal const string Author = "odinplus";
        private const string ModGuid = Author + "qol." + ModName;
        private const string ConfigFileName = ModGuid + ".cfg";

        private static readonly string ConfigFileFullPath =
            Paths.ConfigPath + Path.DirectorySeparatorChar + ConfigFileName;

        internal static string ConnectionError = "";
        private readonly Harmony _harmony = new(ModGuid);
        internal static CraftyBoxesPlugin context = null!;

        public static readonly ManualLogSource CraftyBoxesLogger =
            BepInEx.Logging.Logger.CreateLogSource(ModName);

        private static readonly ConfigSync ConfigSync = new(ModGuid)
            { DisplayName = ModName, CurrentVersion = ModVersion, MinimumRequiredVersion = ModVersion };

        public void Awake()
        {
            _serverConfigLocked = config("General", "Force Server Config", true, "Force Server Config");
            _ = ConfigSync.AddLockingConfigEntry(_serverConfigLocked);

            context = this;

            ConfigGen.Generate();

            if (!modEnabled.Value)
                return;
            wasAllowed = !switchPrevent.Value;

            Assembly assembly = Assembly.GetExecutingAssembly();
            _harmony.PatchAll(assembly);
            SetupWatcher();
        }
        
        internal static void AutoDoc()
        {
            // Store Regex to get all characters after a [
            Regex regex = new(@"\[(.*?)\]");

            // Strip using the regex above from Config[x].Description.Description
            string Strip(string x) => regex.Match(x).Groups[1].Value;
            StringBuilder sb = new();
            string lastSection = "";
            foreach (ConfigDefinition x in context.Config.Keys)
            {
                // skip first line
                if (x.Section != lastSection)
                {
                    lastSection = x.Section;
                    sb.Append($"{Environment.NewLine}`{x.Section}`{Environment.NewLine}");
                }
                sb.Append($"\n{x.Key} [{Strip(context.Config[x].Description.Description)}]" +
                          $"{Environment.NewLine}   * {context.Config[x].Description.Description.Replace("[Synced with Server]", "").Replace("[Not Synced with Server]", "")}" +
                          $"{Environment.NewLine}     * Default Value: {context.Config[x].GetSerializedValue()}{Environment.NewLine}");
            }
            File.WriteAllText(Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)!, $"{ModName}_AutoDoc.md"), sb.ToString());
        }

        private void OnDestroy()
        {
            Config.Save();
            Functions.StopConnectionEffects();
        }

        private void LateUpdate()
        {
            wasAllowed = AllowByKey();
        }

        internal static bool AllowByKey()
        {
            if (preventModKey.Value.IsPressed())
                return switchPrevent.Value;
            return !switchPrevent.Value;
        }

        internal static bool CheckKeyHeld(string value, bool req = true)
        {
            try
            {
                return Input.GetKey(value.ToLower());
            }
            catch
            {
                return !req;
            }
        }


        private void SetupWatcher()
        {
            FileSystemWatcher watcher = new(Paths.ConfigPath, ConfigFileName);
            watcher.Changed += ReadConfigValues;
            watcher.Created += ReadConfigValues;
            watcher.Renamed += ReadConfigValues;
            watcher.IncludeSubdirectories = true;
            watcher.SynchronizingObject = ThreadingHelper.SynchronizingObject;
            watcher.EnableRaisingEvents = true;
        }

        private void ReadConfigValues(object sender, FileSystemEventArgs e)
        {
            if (!File.Exists(ConfigFileFullPath)) return;
            try
            {
                CraftyBoxesLogger.LogDebug("ReadConfigValues called");
                Config.Reload();
            }
            catch
            {
                CraftyBoxesLogger.LogError($"There was an issue loading your {ConfigFileName}");
                CraftyBoxesLogger.LogError("Please check your config entries for spelling and format!");
            }
        }


        #region ConfigOptions

        private static ConfigEntry<bool> _serverConfigLocked = null!;
        internal static bool wasAllowed;

        internal static readonly List<ConnectionParams> ContainerConnections = new();
        internal static GameObject connectionVfxPrefab = null!;

        public static ConfigEntry<bool> showGhostConnections = null!;
        public static ConfigEntry<float> ghostConnectionStartOffset = null!;
        public static ConfigEntry<float> ghostConnectionRemovalDelay = null!;
        public static ConfigEntry<float> mRange = null!;
        public static ConfigEntry<Color> flashColor = null!;
        public static ConfigEntry<Color> unFlashColor = null!;
        public static ConfigEntry<string> resourceString = null!;
        public static ConfigEntry<string> pulledMessage = null!;
        public static ConfigEntry<string> fuelDisallowTypes = null!;
        public static ConfigEntry<string> oreDisallowTypes = null!;
        public static ConfigEntry<string> CFCItemDisallowTypes = null!;
        public static ConfigEntry<KeyboardShortcut> pullItemsKey = null!;
        public static ConfigEntry<KeyboardShortcut> preventModKey = null!;
        public static ConfigEntry<KeyboardShortcut> fillAllModKey = null!;
        public static ConfigEntry<bool> switchPrevent = null!;
        public static ConfigEntry<bool> modEnabled = null!;

        public class ConnectionParams
        {
            public GameObject connection = null!;
            public Vector3 stationPos;
        }

        public static readonly List<Container> ContainerList = new();

        [UsedImplicitly] public static bool odinQolInstalled;
        [UsedImplicitly] public static float itemStackSizeMultiplier;
        [UsedImplicitly] public static float itemWeightReduction;

        internal ConfigEntry<T> config<T>(string group, string name, T value, ConfigDescription description,
            bool synchronizedSetting = true)
        {
            ConfigDescription extendedDescription =
                new(
                    description.Description +
                    (synchronizedSetting ? " [Synced with Server]" : " [Not Synced with Server]"),
                    description.AcceptableValues, description.Tags);
            ConfigEntry<T> configEntry = Config.Bind(group, name, value, extendedDescription);
            //var configEntry = Config.Bind(group, name, value, description);

            SyncedConfigEntry<T> syncedConfigEntry = ConfigSync.AddConfigEntry(configEntry);
            syncedConfigEntry.SynchronizedConfig = synchronizedSetting;

            return configEntry;
        }

        internal ConfigEntry<T> config<T>(string group, string name, T value, string description,
            bool synchronizedSetting = true)
        {
            return config(group, name, value, new ConfigDescription(description), synchronizedSetting);
        }

        private class ConfigurationManagerAttributes
        {
            public bool? Browsable = false;
        }

        public class AcceptableShortcuts : AcceptableValueBase
        {
            public AcceptableShortcuts() : base(typeof(KeyboardShortcut))
            {
            }

            public override object Clamp(object value) => value;
            public override bool IsValid(object value) => true;

            public override string ToDescriptionString() =>
                "# Acceptable values: " + string.Join(", ", KeyboardShortcut.AllKeyCodes);
        }

        #endregion
    }
    
    [HarmonyPatch(typeof(FejdStartup), nameof(FejdStartup.Awake))]
    static class CFCFejdStartupAwakePatch
    {

        static void Postfix(FejdStartup __instance)
        {
            if (!CraftyBoxesPlugin.modEnabled.Value)
                return;
            Functions.CheckOdinsQOLConfig();
        }
    }

#if DEBUG
    [HarmonyPatch(typeof(Player),nameof(Player.Awake))]
    static class Player_Awake_Patch
    {
        static void Postfix(Player __instance)
        {
            CraftyBoxesPlugin.AutoDoc();
        }
    }
#endif
}