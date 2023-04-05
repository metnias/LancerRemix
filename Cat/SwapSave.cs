using Menu;
using System.IO;
using System.Text.RegularExpressions;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;
using static CatSub.Story.SaveManager;
using MoreSlugcats;

namespace LancerRemix.Cat
{
    internal static class SwapSave
    {
        internal static void SubPatch()
        {
            On.PlayerProgression.IsThereASavedGame += IsThereASavedLancer;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += ContinueStartedLancer;
            On.PlayerProgression.WipeSaveState += WipeSaveLancer;

            On.PlayerProgression.SaveToDisk += SaveToLancer;
            //On.SaveState.SaveToString += SaveStateToLancer;
            On.PlayerProgression.LoadGameState += LoadLancerStateInstead;
            //IL.PlayerProgression.LoadGameState += LoadLancerState;

            On.RoomSettings.ctor += LancerRoomSettings;
            On.Region.GetRegionFullName += LancerRegionFullName;
            On.WorldLoader.ctor_RainWorldGame_Name_bool_string_Region_SetupValues += LancerWorldLoader;
            On.DeathPersistentSaveData.CanUseUnlockedGates += LonkNoUnlockGate;
            On.Region.ctor += LancerGetBasisRegion;
            On.Region.LoadAllRegions += LoadAllLancerRegion;
            On.PlayerProgression.MiscProgressionData.updateConditionalShelters += UpdateConditionalLancerShelters;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        internal const string CURRSLUGCATLANCER = "CurrentlySelectedSinglePlayerIsLancer";

        #region SlugcatSelectMenu

        private static bool IsThereASavedLancer(On.PlayerProgression.orig_IsThereASavedGame orig, PlayerProgression self, SlugName saveStateNumber)
        {
            if (IsStoryLancer) saveStateNumber = GetLancer(saveStateNumber);
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

        private static bool SaveToLancer(On.PlayerProgression.orig_SaveToDisk orig, PlayerProgression self,
            bool saveCurrentState, bool saveMaps, bool saveMiscProg)
        {
            if (IsStoryLancer && self.currentSaveState != null)
            {
                SetMiscValue(self.miscProgressionData, CURRSLUGCATLANCER, true);
                var basis = self.currentSaveState.saveStateNumber;
                self.currentSaveState.saveStateNumber = GetLancer(basis);
                UnityEngine.Debug.Log($"{self.currentSaveState.saveStateNumber}({basis}) redsDeath: {self.currentSaveState.deathPersistentSaveData.redsDeath}");
                var res = orig(self, saveCurrentState, saveMaps, saveMiscProg);
                self.currentSaveState.saveStateNumber = basis;
                return res;
            }
            SetMiscValue(self.miscProgressionData, CURRSLUGCATLANCER, false);
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

        #region Region

        private static SlugName GetStoryBasisForLancer(SlugName storyIndex)
        {
            if (storyIndex == null) return null;
            if (IsLancer(storyIndex) || IsStoryLancer)
                return LancerGenerator.GetStoryBasisForLancer(storyIndex);
            return storyIndex; // Not lancer
        }

        private static void LancerRoomSettings(On.RoomSettings.orig_ctor orig, RoomSettings self, string name, Region region, bool template, bool firstTemplate, SlugName playerChar)
        {
            playerChar = GetStoryBasisForLancer(playerChar);
            orig(self, name, region, template, firstTemplate, playerChar);
        }

        private static string LancerRegionFullName(On.Region.orig_GetRegionFullName orig, string regionAcro, SlugName slugcatIndex)
        {
            slugcatIndex = GetStoryBasisForLancer(slugcatIndex);
            return orig(regionAcro, slugcatIndex);
        }

        private static void LancerWorldLoader(On.WorldLoader.orig_ctor_RainWorldGame_Name_bool_string_Region_SetupValues orig, WorldLoader self,
            RainWorldGame game, SlugName playerCharacter, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (singleRoomWorld) goto Init;
            playerCharacter = GetStoryBasisForLancer(playerCharacter);
        Init: orig(self, game, playerCharacter, singleRoomWorld, worldName, region, setupValues);
        }

        private static bool LonkNoUnlockGate(On.DeathPersistentSaveData.orig_CanUseUnlockedGates orig, DeathPersistentSaveData self, SlugName slugcat)
        {
            if (IsStoryLancer && GetBasis(slugcat) == SlugName.Yellow)
                return ModManager.MMF && MMF.cfgGlobalMonkGates != null && MMF.cfgGlobalMonkGates.Value;
            return orig(self, slugcat);
        }

        private static void LancerGetBasisRegion(On.Region.orig_ctor orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugName storyIndex)
        {
            storyIndex = GetStoryBasisForLancer(storyIndex);
            orig(self, name, firstRoomIndex, regionNumber, storyIndex);
        }

        private static Region[] LoadAllLancerRegion(On.Region.orig_LoadAllRegions orig, SlugName storyIndex)
        {
            storyIndex = GetStoryBasisForLancer(storyIndex);
            return orig(storyIndex);
        }

        private static void UpdateConditionalLancerShelters(On.PlayerProgression.MiscProgressionData.orig_updateConditionalShelters orig,
            PlayerProgression.MiscProgressionData self, string room, SlugName slugcatIndex)
        {
            if (IsStoryLancer) slugcatIndex = GetLancer(slugcatIndex);
            orig(self, room, slugcatIndex);
        }

        #endregion Region
    }
}