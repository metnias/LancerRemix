﻿using BepInEx;
using BepInEx.Logging;
using LancerRemix;
using LancerRemix.Cat;
using LancerRemix.Combat;
using LancerRemix.LancerMenu;
using LancerRemix.Story;
using Menu.Remix;
using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

#if LATCHER

using LancerRemix.Latcher;

#endif

#region Assembly attributes

#pragma warning disable CS0618
[assembly: AssemblyVersion(LancerPlugin.PLUGIN_VERSION)]
[assembly: AssemblyFileVersion(LancerPlugin.PLUGIN_VERSION)]
[assembly: AssemblyTitle(LancerPlugin.PLUGIN_NAME + " (" + LancerPlugin.PLUGIN_ID + ")")]
[assembly: AssemblyProduct(LancerPlugin.PLUGIN_NAME)]
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]
#pragma warning restore CS0618

#endregion Assembly attributes

namespace LancerRemix
{
    [BepInDependency("com.rainworldgame.topicular.catsupplement.plugin")]
    [BepInDependency("slime-cubed.slugbase")]
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("RainWorld.exe")]
    public class LancerPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.rainworldgame.topicular.lancer.plugin";
        public const string PLUGIN_NAME = "Lancer";
        public const string PLUGIN_VERSION = "1.3.1.6";

        private static bool init = false;
        internal static ManualLogSource LogSource { get; private set; }

        public static LancerPlugin Instance { get; private set; }

        public static OptionInterface OI { get; private set; }

        public void OnEnable()
        {
            Instance = this;
            LogSource = Logger;

            On.RainWorld.OnModsInit += WrapInit(Init);
            On.ProcessManager.PreSwitchMainProcess += RegisterLancersAfterMainMenu;
            On.RainWorld.OnModsEnabled += OnModsEnabled;
            On.RainWorld.OnModsDisabled += OnModsDisabled;
        }

        private static void Init(RainWorld rw)
        {
            OI = MachineConnector.GetRegisteredOI("topicular.lancer");
            if (OI is InternalOI_Auto) (OI as InternalOI_Auto).automated = false;

#if LATCHER
            try
            {
                LogSource.LogInfo(AssetManager.ResolveFilePath($"assetbundles/latcher"));
                var bundle = AssetBundle.LoadFromFile(AssetManager.ResolveFilePath($"assetbundles/latcher"));
                var shader = bundle.LoadAsset<Shader>("RippleGoldenBasic");
                rw.Shaders.Add("LatcherRippleGolden", FShader.CreateShader("LatcherRippleGolden", shader));
            }
            catch (Exception e) { LogSource.LogError(e); }
#endif

            LancerEnums.RegisterExtEnum();

            ModifyCat.Patch();
            MenuModifier.Patch();
            LancerGenerator.Patch();
            CreaturePatch.Patch();
            WeaponPatch.Patch();
            DreamHandler.Patch();
            TutorialModify.Patch();

            lastMSCEnabled = ModManager.MSC;
            lastJollyEnabled = ModManager.JollyCoop;
            lastMMFEnabled = ModManager.MMF;
            lastWatcherEnabled = ModManager.Watcher;

            Instance.Logger.LogMessage("The Lancer is Intialized.");
            Instance.Logger.LogMessage($"ILhooks: {Convert.ToString(ILhookFlags, 2)} ({(ILhookSuccess() ? "Success" : "Failed")})");
            if (!ILhookSuccess()) Debug.LogError($"Lancer failed some of ILhooks: {Convert.ToString(ILhookFlags, 2)}");

            MSCLANCERS = MachineConnector.IsThisModActive("topicular.morelancer");
            if (MSCLANCERS) LogSource.LogInfo($"More Lancers detected!");
        }

        public static On.RainWorld.hook_OnModsInit WrapInit(Action<RainWorld> loadResources)
        {
            return (orig, self) =>
            {
                orig(self);
                if (init) return;

                try
                {
                    loadResources(self);
                }
                catch (Exception e)
                {
                    Debug.LogException(e);
                }
                init = true;
            };
        }

        internal static bool MSCLANCERS = false;

        private static void RegisterLancersAfterMainMenu(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (self.currentMainLoop?.ID == ProcessManager.ProcessID.MainMenu)
            {
                try
                {
                    LancerEnums.RegisterLancers();
                    HornColorPick.Initalize();
                }
                catch (Exception e) { Debug.LogException(e); }
            }
            orig(self, ID);
        }

        #region ModToggleReactor

        private static bool lastMSCEnabled;
        private static bool lastJollyEnabled;
        private static bool lastMMFEnabled;
        private static bool lastWatcherEnabled;

        internal static bool AnyModChanged { get; private set; } = true;

        internal static bool CheckedAnyModChanged() => AnyModChanged = false;

        private static void OnModsEnabled(On.RainWorld.orig_OnModsEnabled orig, RainWorld rw, ModManager.Mod[] newlyEnabledMods)
        {
            orig(rw, newlyEnabledMods);
            if (newlyEnabledMods.Length > 0) AnyModChanged = true;
            if (!lastMSCEnabled && ModManager.MSC)
            {
                LogSource.LogInfo("Lancer detected MSC newly enabled.");
                ModifyCat.OnMSCEnablePatch();
                DreamHandler.OnMSCEnablePatch();
                WeaponPatch.OnMSCEnablePatch();
                MenuModifier.OnMSCEnablePatch();
                lastMSCEnabled = ModManager.MSC;
            }
            if (!lastJollyEnabled && ModManager.JollyCoop)
            {
                LogSource.LogInfo("Lancer detected Jolly newly enabled.");
                MenuModifier.OnJollyEnablePatch();
                lastJollyEnabled = ModManager.JollyCoop;
            }
            if (!lastMMFEnabled && ModManager.MMF)
            {
                LogSource.LogInfo("Lancer detected MMF newly enabled.");
                TutorialModify.OnMMFEnablePatch();
                SelectMenuPatch.OnMMFEnablePatch();
                HornColorPick.OnMMFEnablePatch();
                lastMMFEnabled = ModManager.MMF;
            }
            if (!lastWatcherEnabled && ModManager.Watcher)
            {
                LogSource.LogInfo("Lancer detected Watcher newly enabled.");
#if LATCHER
                ModifyLatcher.OnWatcherEnablePatch();
#endif
                lastWatcherEnabled = ModManager.Watcher;
            }
        }

        private static void OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld rw, ModManager.Mod[] newlyDisabledMods)
        {
            orig(rw, newlyDisabledMods);
            if (newlyDisabledMods.Length > 0) AnyModChanged = true;
            if (lastMSCEnabled && !ModManager.MSC)
            {
                LogSource.LogInfo("Lancer detected MSC newly disabled.");
                ModifyCat.OnMSCDisablePatch();
                DreamHandler.OnMSCDisablePatch();
                WeaponPatch.OnMSCDisablePatch();
                MenuModifier.OnMSCDisablePatch();
                lastMSCEnabled = ModManager.MSC;
            }
            if (lastJollyEnabled && !ModManager.JollyCoop)
            {
                LogSource.LogInfo("Lancer detected Jolly newly disabled.");
                MenuModifier.OnJollyDisablePatch();
                lastJollyEnabled = ModManager.JollyCoop;
            }
            if (lastMMFEnabled && !ModManager.MMF)
            {
                LogSource.LogInfo("Lancer detected MMF newly disabled.");
                TutorialModify.OnMMFDisablePatch();
                SelectMenuPatch.OnMMFDisablePatch();
                HornColorPick.OnMMFDisablePatch();
                lastMMFEnabled = ModManager.MMF;
            }
            if (lastWatcherEnabled && !ModManager.Watcher)
            {
                LogSource.LogInfo("Lancer detected Watcher newly disabled.");
#if LATCHER
                ModifyLatcher.OnWatcherDisablePatch();
#endif
                lastWatcherEnabled = ModManager.Watcher;
            }
        }

        #endregion ModToggleReactor

        #region ILhookTrackers

        private static int ILhookFlags = 0;

        [Flags]
        internal enum ILhooks : int
        {
            SaveLancerPersDataOfCurrentState = 1 << 0,
            LoadLancerMapTexture = 1 << 1,
            LancerTravelScreen = 1 << 2,
            MineForLunterData = 1 << 3,
            LoadLancerState = 1 << 4,
            LonkEatMeatUpdate = 1 << 5,
            LancerStartGamePatch = 1 << 6,
            LancerCustomColorSlider = 1 << 7,
            BigNeedleWormParryCheck = 1 << 8,
            KingTuskParryCheck = 1 << 9,
            LanceFarStickPrevent = 1 << 10,
            LancerIntroRoll = 1 << 11,

#if LATCHER
            LatcherControlMapPatch = 1 << 12,
            LatcherAddRoomSpecificScriptPatch = 1 << 13,
            DrillCrabNoAttackOnRipple = 1 << 14,
#endif
        }

        internal static void ILhookTry(ILhooks flag)
        {
            LogSource.LogInfo($"{flag} Hook try");
            ILhookFlags |= (int)flag;
        }

        internal static void ILhookOkay(ILhooks flag)
        {
            LogSource.LogInfo($"{flag} Hook success");
            ILhookFlags &= ~(int)flag;
        }

        internal static bool ILhookSuccess() => ILhookFlags == 0;

        #endregion ILhookTrackers
    }
}