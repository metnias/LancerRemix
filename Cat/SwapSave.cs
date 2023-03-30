using Menu;
using MonoMod.Cil;
using System.Globalization;
using static LancerRemix.LancerEnums;
using static UnityEngine.RectTransform;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    internal static class SwapSave
    {
        internal static void SubPatch()
        {
            On.PlayerProgression.IsThereASavedGame += IsThereASavedLancer;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += ContinueStartedLancer;
            On.PlayerProgression.WipeSaveState += WipeSaveLancer;

            On.SaveState.SaveToString += SaveLancerToString;
            IL.PlayerProgression.LoadGameState += LoadLancerState;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        #region SlugcatSelectMenu

        private static bool IsThereASavedLancer(On.PlayerProgression.orig_IsThereASavedGame orig, PlayerProgression self, SlugName saveStateNumber)
        {
            if (IsStoryLancer) saveStateNumber = GetLancer(saveStateNumber);
            return orig(self, saveStateNumber);
        }

        private static void ContinueStartedLancer(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugcatStats.Name storyGameCharacter)
        {
            if (IsStoryLancer)
            {
                // Switch to Stats
                var lancer = GetLancer(storyGameCharacter);
                if (storyGameCharacter == SlugName.Red && self.saveGameData[lancer].redsDeath)
                {
                    self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(lancer, null, self.manager.menuSetup, false);
                    self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                    self.PlaySound(SoundID.MENU_Switch_Page_Out);
                    return;
                }
            }
            orig(self, storyGameCharacter);
        }

        private static void WipeSaveLancer(On.PlayerProgression.orig_WipeSaveState orig, PlayerProgression self, SlugName saveStateNumber)
        {
            if (IsStoryLancer) saveStateNumber = GetLancer(saveStateNumber);
            orig(self, saveStateNumber);
        }

        #endregion SlugcatSelectMenu

        #region SaveState

        private static string ReplaceFirst(this string text, string orig, string patch)
        {
            int index = text.IndexOf(orig);
            if (index >= 0) return $"{text.Substring(0, index)}{patch}{text.Substring(index + orig.Length)}";
            return text;
        }

        private static string SaveLancerToString(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            var data = orig(self);
            if (IsStoryLancer)
            {
                var lancer = GetLancer(self.saveStateNumber);
                data.ReplaceFirst(self.saveStateNumber.value, lancer.value);
            }
            return data;
        }

        private static void LoadLancerState(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.LogSource.LogInfo("LoadLancerState Patch");



            LancerPlugin.LogSource.LogInfo("LoadLancerState Done");

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        #endregion SaveState

    }
}
