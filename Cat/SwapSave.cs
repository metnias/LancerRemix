using Menu;
using System.IO;
using System.Text.RegularExpressions;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    internal static class SwapSave
    {
        // To fix: passage to use lancer map progress
        internal static void SubPatch()
        {
            On.PlayerProgression.IsThereASavedGame += IsThereASavedLancer;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += ContinueStartedLancer;
            On.PlayerProgression.WipeSaveState += WipeSaveLancer;

            On.PlayerProgression.SaveToDisk += SaveToLancer;
            //On.SaveState.SaveToString += SaveStateToLancer;
            On.PlayerProgression.LoadGameState += LoadLancerStateInstead;
            //IL.PlayerProgression.LoadGameState += LoadLancerState;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        #region SlugcatSelectMenu

        private static bool IsThereASavedLancer(On.PlayerProgression.orig_IsThereASavedGame orig, PlayerProgression self, SlugName saveStateNumber)
        {
            if (IsStoryLancer && HasLancer(saveStateNumber)) saveStateNumber = GetLancer(saveStateNumber);
            return orig(self, saveStateNumber);
        }

        private static void ContinueStartedLancer(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugName storyGameCharacter)
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
            if (IsStoryLancer && HasLancer(saveStateNumber)) saveStateNumber = GetLancer(saveStateNumber);
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

        private static bool SaveToLancer(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self,
            bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (IsStoryLancer && self.currentSaveState != null)
            {
                var basis = self.currentSaveState.saveStateNumber;
                self.currentSaveState.saveStateNumber = GetLancer(basis);
                var res = orig(self, saveCurrentState, saveMaps, saveMiscProg);
                self.currentSaveState.saveStateNumber = basis;
                return res;
            }
            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private static SaveState LoadLancerStateInstead(On.PlayerProgression.orig_LoadGameState orig, PlayerProgression self,
            string saveFilePath, RainWorldGame game, bool saveAsDeathOrQuit)
        {
            if (!IsStoryLancer) return orig(self, saveFilePath, game, saveAsDeathOrQuit);
            string[] rawData = GetRawData();
            var lancer = GetLancer(self.currentSaveState.saveStateNumber);
            for (int i = 0; i < rawData.Length; i++)
            {
                string[] rawStates = Regex.Split(rawData[i], "<progDivB>");
                if (rawStates.Length == 2 && rawStates[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(rawStates[1]) == lancer)
                {
                    self.currentSaveState.LoadGame(rawStates[1], game);
                    if (saveAsDeathOrQuit)
                        self.SaveDeathPersistentDataOfCurrentState(true, true);
                    return self.currentSaveState;
                }
            }
            return null;

            string[] GetRawData()
            {
                string[] array;
                if (saveFilePath == null)
                    array = self.GetProgLinesFromMemory();
                else
                {
                    string text = File.ReadAllText(saveFilePath);
                    if (text.Length > 32)
                        array = Regex.Split(text.Substring(32), "<progDivA>");
                    else array = new string[0];
                }
                return array;
            }
        }

        /*
        private static void LoadLancerState(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.LogSource.LogInfo("LoadLancerState Patch");

            #region AddLblContinue

            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(1),
                x => x.MatchLdcI4(1),
                x => x.MatchAdd())) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblContinue = cursor.DefineLabel();
            lblContinue.Target = cursor.Prev;

            #endregion AddLblContinue

            #region AddLblOkay

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(PlayerProgression).GetField(nameof(PlayerProgression.currentSaveState))),
                x => x.MatchLdloc(3))) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblOkay = cursor.DefineLabel();
            lblOkay.Target = cursor.Prev;

            #endregion AddLblOkay

            #region AddLblNoLancer

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(3),
                x => x.MatchLdcI4(1),
                x => x.MatchLdelemRef(),
                x => x.MatchCall(typeof(BackwardsCompatibilityRemix).GetMethod(nameof(BackwardsCompatibilityRemix.ParseSaveNumber))))) return;

            DebugLogCursor();

            cursor.Emit(OpCodes.Nop);
            var lblNoLancer = cursor.DefineLabel();
            lblNoLancer.Target = cursor.Prev;
            cursor.GotoLabel(lblNoLancer, MoveType.Before);
            DebugLogCursor();

            #endregion AddLblNoLancer

            cursor.EmitDelegate<Func<bool>>(
                () => IsStoryLancer
                );
            cursor.Emit(OpCodes.Brfalse, lblNoLancer); // if no lancer, skip

            cursor.Emit(OpCodes.Ldloc, 3);
            cursor.Emit(OpCodes.Ldc_I4, 1);
            cursor.Emit(OpCodes.Ldelem_Ref);
            cursor.Emit(OpCodes.Call, typeof(BackwardsCompatibilityRemix).GetMethod(nameof(BackwardsCompatibilityRemix.ParseSaveNumber)));
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldfld, typeof(PlayerProgression).GetField(nameof(PlayerProgression.currentSaveState)));
            cursor.Emit(OpCodes.Ldfld, typeof(SaveState).GetField(nameof(SaveState.saveStateNumber)));
            cursor.EmitDelegate<Func<SlugName, SlugName, bool>>(
                (basis, data) =>
                {
                    return GetLancer(basis) == data;
                }
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Br, lblContinue);

            LancerPlugin.LogSource.LogInfo("LoadLancerState Done");

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }
        */

        #endregion SaveState
    }
}