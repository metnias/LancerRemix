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

            On.RainWorld.OnModsInit += Extras.WrapInit(Init);
            On.RainWorld.PostModsInit += PostInit;
            On.RainWorld.OnModsEnabled += OnModsEnabled;
            On.RainWorld.OnModsDisabled += OnModsDisabled;
        }

        private static void Init(RainWorld rw)
        {
            if (init) return;
            init = true;

            LancerEnums.RegisterExtEnum();
            TutorialPatch.SubPatch();
            ModifyCat.SubPatch();
            //SaveManager.SubPatch();
            MenuModifier.SubPatch();
            LancerGenerator.SubPatch();

            instance.Logger.LogMessage("The Lancer is Intilaized.");
        }

        private static void PostInit(On.RainWorld.orig_PostModsInit orig, RainWorld rw)
        {
            orig(rw);

            LancerEnums.RegisterLancers();
        }

        private static bool lastMSCEnabled;

        private static void OnModsEnabled(On.RainWorld.orig_OnModsEnabled orig, RainWorld rw, ModManager.Mod[] newlyEnabledMods)
        {
            orig(rw, newlyEnabledMods);
            LancerEnums.RegisterLancers();
            if (!lastMSCEnabled && ModManager.MSC)
            {
                LogSource.LogInfo("Lancer detected MSC newly enabled.");
                //ModifyCat.OnMSCEnablePatch();
                //AltEndingHandler.OnMSCEnablePatch();
                lastMSCEnabled = ModManager.MSC;
            }
        }

        private static void OnModsDisabled(On.RainWorld.orig_OnModsDisabled orig, RainWorld rw, ModManager.Mod[] newlyDisabledMods)
        {
            orig(rw, newlyDisabledMods);
            if (lastMSCEnabled && !ModManager.MSC)
            {
                LogSource.LogInfo("Lancer detected MSC newly disabled.");
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