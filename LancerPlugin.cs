using LancerRemix;
using LancerRemix.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Permissions;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using BepInEx.Logging;
using BepInEx;
using LancerRemix.LancerMenu;
using UnityEngine;

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
    [BepInPlugin(PLUGIN_ID, PLUGIN_NAME, PLUGIN_VERSION)]
    [BepInProcess("RainWorld.exe")]
    public class LancerPlugin : BaseUnityPlugin
    {
        public const string PLUGIN_ID = "com.rainworldgame.topicular.lancer.plugin";
        public const string PLUGIN_NAME = "Lancer";
        public const string PLUGIN_VERSION = "1.9.0.0";

        private static bool init = false;
        internal static ManualLogSource LogSource;

        public static LancerPlugin instance;

        public void OnEnable()
        {
            instance = this;
            LogSource = this.Logger;

            On.RainWorld.OnModsInit += WrapInit(Init);
            On.ProcessManager.PreSwitchMainProcess += RegisterLancersAfterMainMenu;
            On.RainWorld.OnModsEnabled += OnModsEnabled;
            On.RainWorld.OnModsDisabled += OnModsDisabled;
        }

        private static void Init(RainWorld rw)
        {
            LancerEnums.RegisterExtEnum();
            ModifyCat.Patch();
            MenuModifier.Patch();
            LancerGenerator.Patch();

            instance.Logger.LogMessage("The Lancer is Intialized.");
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

        private static void RegisterLancersAfterMainMenu(On.ProcessManager.orig_PreSwitchMainProcess orig, ProcessManager self, ProcessManager.ProcessID ID)
        {
            if (self.currentMainLoop?.ID == ProcessManager.ProcessID.MainMenu)
            {
                try
                {
                    LancerEnums.RegisterLancers();
                }
                catch (Exception e) { Debug.LogException(e); }
            }
            else if (ID == ProcessManager.ProcessID.SlugcatSelect)
            {
                ModifyCat.SetIsLancer(false, new bool[4]);
            }
            orig(self, ID);
        }

        private static bool lastMSCEnabled;

        internal static bool AnyModChanged { get; private set; } = true;

        internal static bool CheckedAnyModChanged() => AnyModChanged = false;

        private static void OnModsEnabled(On.RainWorld.orig_OnModsEnabled orig, RainWorld rw, ModManager.Mod[] newlyEnabledMods)
        {
            orig(rw, newlyEnabledMods);
            if (newlyEnabledMods.Length > 0) AnyModChanged = true;
            if (!lastMSCEnabled && ModManager.MSC)
            {
                //LogSource.LogInfo("Lancer detected MSC newly enabled.");
                //ModifyCat.OnMSCEnablePatch();
                //AltEndingHandler.OnMSCEnablePatch();
                lastMSCEnabled = ModManager.MSC;
            }
        }

        private static void OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld rw, ModManager.Mod[] newlyDisabledMods)
        {
            orig(rw, newlyDisabledMods);
            if (newlyDisabledMods.Length > 0) AnyModChanged = true;
            if (lastMSCEnabled && !ModManager.MSC)
            {
                //LogSource.LogInfo("Lancer detected MSC newly disabled.");
                //ModifyCat.OnMSCDisablePatch();
                //AltEndingHandler.OnMSCDisablePatch();
                lastMSCEnabled = ModManager.MSC;
            }
            /*
            if (!init) return;
            foreach (var mod in newlyDisabledMods)
            {
                if (mod.id == "maplecollection")
                {
                    ExtEnum_Maple.UnregisterExtEnum();

                    init = false;
                    return;
                }
            }*/
        }
    }
}