using RWCustom;
using Watcher;
using static LancerRemix.Cat.ModifyCat;

namespace LancerRemix.Latcher
{
    internal static class LatcherTutorial
    {
        internal static void SubPatch()
        {
            if (!ModManager.Watcher) return;
            OnWatcherEnableSubPatch();
        }

        internal static void OnWatcherEnableSubPatch()
        {
            On.Watcher.CamoTutorial.Update += LatcherCamoTutorial;
        }

        internal static void OnWatcherDisableSubPatch()
        {
            On.Watcher.CamoTutorial.Update -= LatcherCamoTutorial;
        }

        private static void LatcherCamoTutorial(On.Watcher.CamoTutorial.orig_Update orig, CamoTutorial self, bool eu)
        {
            if (!IsStoryLancer) { orig(self, eu); return; }

            self.evenUpdate = eu;

            if (!self.room.game.IsStorySession || !Watcher.Watcher.cfgWatcherTutorials.Value)
            {
                self.Destroy();
                return;
            }
            if (self.room.game.GetStorySession.saveState.miscWorldSaveData.camoTutorialCounter > ((self.room.game.GetStorySession.saveState.miscWorldSaveData.usedCamoAbility == 1) ? 1 : 3))
            {
                self.Destroy();
                return;
            }
            if (self.room.game.session.Players[0].realizedCreature != null && self.room.game.cameras[0].hud != null && self.room.game.cameras[0].hud.textPrompt != null && self.room.game.cameras[0].hud.textPrompt.messages.Count < 1)
            {
                int num = self.message;
                if (num == 0)
                {
                    self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.manager.rainWorld.inGameTranslator.Translate(
                        "Hold <SPECIAL> to slow down time").Replace("<SPECIAL>", GetButtonName_Special()), 120, 160, false, true);
                    self.message++;
                    return;
                }
                if (num != 1) return;
                self.room.game.GetStorySession.saveState.miscWorldSaveData.camoTutorialCounter = self.room.game.GetStorySession.saveState.miscWorldSaveData.camoTutorialCounter + 1;
                self.Destroy();
            }
        }

        public static string GetButtonName_Special()
        {
            Options.ControlSetup.Preset activePreset = Custom.rainWorld.options.controls[0].GetActivePreset();
            string text;
            if (activePreset == Options.ControlSetup.Preset.PS4DualShock || activePreset == Options.ControlSetup.Preset.PS5DualSense)
                text = "Triangle";
            else
            {
                if (activePreset == Options.ControlSetup.Preset.XBox)
                    return "Y";

                if (activePreset == Options.ControlSetup.Preset.SwitchHandheld || activePreset == Options.ControlSetup.Preset.SwitchDualJoycon || activePreset == Options.ControlSetup.Preset.SwitchProController)
                    text = "switch_controls_special";
                else if (activePreset == Options.ControlSetup.Preset.SwitchSingleJoyconL)
                    text = "switch_controls_singlejoyconl_special";
                else if (activePreset == Options.ControlSetup.Preset.SwitchSingleJoyconR)
                    text = "switch_controls_singlejoyconr_special";
                else
                {
                    if (activePreset == Options.ControlSetup.Preset.KeyboardSinglePlayer)
                        return "C";

                    text = "Special Button";
                }
            }

            return OptionInterface.Translate(text);
        }
    }
}