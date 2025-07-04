﻿using LancerRemix.Story;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Runtime.CompilerServices;
using static CatSub.Story.SaveManager;
using static LancerRemix.LancerEnums;
using Debug = UnityEngine.Debug;
using SlugName = SlugcatStats.Name;
using SlugTime = SlugcatStats.Timeline;

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
            On.PlayerProgression.SaveDeathPersistentDataOfCurrentState += SaveLancerPersDataOfCurrentState;
            //IL.PlayerProgression.SaveDeathPersistentDataOfCurrentState += SaveLancerPersDataOfCurrentState;
            //IL.PlayerProgression.LoadMapTexture += LoadLancerMapTexture;
            //On.PlayerProgression.LoadGameState += LoadLancerStateInstead;
            IL.PlayerProgression.LoadGameState += LoadLancerState;

            On.MoreSlugcats.SpeedRunTimer.GetCampaignTimeTracker += GetLancerCampaignTimeTracker;

            On.RoomSettings.ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame += LancerRoomSettings;
            On.Region.GetRegionFullName += LancerRegionFullName;
            On.WorldLoader.ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues += LancerWorldLoader;
            On.DeathPersistentSaveData.CanUseUnlockedGates += LonkNoUnlockGate;
            On.Region.ctor_string_int_int_Timeline += LancerGetBasisRegion;
            On.Region.LoadAllRegions_Timeline += LoadAllLancerRegion;
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

        /// <summary>
        /// Dead Lunter Switch to Stats
        /// </summary>
        private static void ContinueStartedLancer(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugName storyGameCharacter)
        {
            if (IsStoryLancer)
            {
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
            if (IsStoryLancer)
            {
                var basis = GetBasis(saveStateNumber);
                saveStateNumber = GetLancer(saveStateNumber);
#if LATCHER
                if (ModManager.Watcher && basis == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                {
                    SetMiscValue(self.miscProgressionData, Latcher.ModifyLatcher.LATCHER_BEATEN, false);
                    SetMiscValue(self.miscProgressionData, Latcher.ModifyLatcher.LATCHER_CAMPAIGNSEED, 0);
                    SetMiscValue(self.miscProgressionData, Latcher.ModifyLatcher.LATCHER_BEATEN_ST, false);
                    SetMiscValue(self.miscProgressionData, Latcher.ModifyLatcher.LATCHER_BEATEN_RP, false);
                    SetMiscValue(self.miscProgressionData, Latcher.ModifyLatcher.LATCHER_ENDINGID, 0);
                }
#endif
            }

            /*
            Debug.Log($"WipeSaveStateTest: {saveStateNumber.value}");
            var list = new List<string>();
            foreach (var slug in ExtEnumBase.GetNames(typeof(SlugName)))
                if (self.IsThereASavedGame(new SlugName(slug, false))) list.Add(slug);
            Debug.Log("Orig: " + string.Join(", ", list));
            */

            orig(self, saveStateNumber);

            /*
            list.Clear();
            foreach (var slug in ExtEnumBase.GetNames(typeof(SlugName)))
                if (self.IsThereASavedGame(new SlugName(slug, false))) list.Add(slug);
            Debug.Log("Left: " + string.Join(", ", list));
            */
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
                if (LancerGenerator.IsCustomLancer(GetLancer(basis)))
                {
                    return orig(self, saveCurrentState, saveMaps, saveMiscProg);
                }
                self.currentSaveState.saveStateNumber = GetLancer(basis);
                // UnityEngine.Debug.Log($"{self.currentSaveState.saveStateNumber}({basis}) redsDeath: {self.currentSaveState.deathPersistentSaveData.redsDeath}");
                var res = orig(self, saveCurrentState, saveMaps, saveMiscProg);
                self.currentSaveState.saveStateNumber = basis;
                return res;
            }
            SetMiscValue(self.miscProgressionData, CURRSLUGCATLANCER, false);
            return orig(self, saveCurrentState, saveMaps, saveMiscProg);
        }

        private static void SaveLancerPersDataOfCurrentState(On.PlayerProgression.orig_SaveDeathPersistentDataOfCurrentState orig,
            PlayerProgression self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (IsStoryLancer && self.currentSaveState != null)
            {
                var basis = self.currentSaveState.saveStateNumber;
                if (!LancerGenerator.IsCustomLancer(GetLancer(basis)))
                {
                    self.currentSaveState.saveStateNumber = GetLancer(basis);
                    orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
                    self.currentSaveState.saveStateNumber = basis;
                    return;
                }
            }
            orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }

        /*
        private static void SaveLancerPersDataOfCurrentState(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.SaveLancerPersDataOfCurrentState);

            if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdloc(2),
                x => x.MatchLdloc(7),
                x => x.MatchCall(typeof(string).GetMethod(nameof(string.Concat), new Type[] { typeof(string), typeof(string) })),
                x => x.MatchStloc(2),
                x => x.MatchBr(out var _))) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblNope = cursor.DefineLabel();
            lblNope.Target = cursor.Prev;

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdstr(""),
                x => x.MatchStloc(7),
                x => x.MatchLdloc(1),
                x => x.MatchLdloc(4))) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblOkay = cursor.DefineLabel();
            lblOkay.Target = cursor.Prev;

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(6),
                x => x.MatchLdcI4(1),
                x => x.MatchLdelemRef(),
                x => x.MatchCall(typeof(BackwardsCompatibilityRemix).GetMethod(nameof(BackwardsCompatibilityRemix.ParseSaveNumber))))) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblNoLancer = cursor.DefineLabel();
            lblNoLancer.Target = cursor.Prev;
            cursor.GotoLabel(lblNoLancer, MoveType.Before);

            cursor.EmitDelegate<Func<bool>>(() => IsStoryLancer);
            cursor.Emit(OpCodes.Brfalse, lblNoLancer);

            cursor.Emit(OpCodes.Ldloc, 6);
            cursor.Emit(OpCodes.Ldc_I4, 1);
            cursor.Emit(OpCodes.Ldelem_Ref);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string, PlayerProgression, bool>>((save, self) =>
                BackwardsCompatibilityRemix.ParseSaveNumber(save) == self.currentSaveState.saveStateNumber
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Br, lblNope);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.SaveLancerPersDataOfCurrentState);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static void LoadLancerMapTexture(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LoadLancerMapTexture);

            // Add Jump after if
            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(0),
                x => x.MatchBrtrue(out var _),
                x => x.MatchLdloc(4),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdstr("MAPUPDATE_"))) return;
            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblMapUpdate = cursor.DefineLabel();
            lblMapUpdate.Target = cursor.Prev;
            // Add jump inside if
            if (!cursor.TryGotoPrev(MoveType.After,
                x => x.MatchCall(typeof(PlayerProgression).GetMethod(nameof(PlayerProgression.LoadByteStringIntoMapTexture), BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.Instance)),
                x => x.MatchLdcI4(1),
                x => x.MatchStloc(1))) return;
            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblOkay = cursor.DefineLabel();
            lblOkay.Target = cursor.Prev;
            // Add jump for no lancer
            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(4),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdstr("MAP_"))) return;
            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblNoLancer = cursor.DefineLabel();
            lblNoLancer.Target = cursor.Prev;
            cursor.GotoLabel(lblNoLancer, MoveType.Before);
            // jump forward for no lancer
            cursor.EmitDelegate<Func<bool>>(() => IsStoryLancer);
            cursor.Emit(OpCodes.Brfalse, lblNoLancer);
            // lancer check instead
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<string[], PlayerProgression, string, bool>>((array, self, regionName) =>
                array[0] == "MAP_" + GetLancer(self.PlayingAsSlugcat).value
                && array[1] == regionName
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Br, lblMapUpdate);

            // add jump after if
            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(1),
                x => x.MatchLdloc(0),
                x => x.MatchAnd(),
                x => x.MatchBrfalse(out var _),
                x => x.MatchRet())) return;
            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblNope = cursor.DefineLabel();
            lblNope.Target = cursor.Prev;
            cursor.GotoLabel(lblNope, MoveType.Before);
            // add jump for no lancer
            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(4),
                x => x.MatchLdcI4(0),
                x => x.MatchLdelemRef(),
                x => x.MatchLdstr("MAPUPDATE_"))) return;
            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            lblNoLancer = cursor.DefineLabel();
            lblNoLancer.Target = cursor.Prev;
            cursor.GotoLabel(lblNoLancer, MoveType.Before);
            // jump forward for no lancer
            cursor.EmitDelegate<Func<bool>>(() => IsStoryLancer);
            cursor.Emit(OpCodes.Brfalse, lblNoLancer);
            // lancer check instead
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<string[], PlayerProgression, string, bool>>((array, self, regionName) =>
                array[0] == "MAPUPDATE_" + GetLancer(self.PlayingAsSlugcat).value
                && array[1] == regionName
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Br, lblMapUpdate);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LoadLancerMapTexture);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        /*
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
        */

        private static void LoadLancerState(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LoadLancerState);

            // Add label after if
            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdloc(2),
                x => x.MatchLdcI4(1),
                x => x.MatchAdd())) return;
            DebugLogCursor();
            var lblNope = cursor.DefineLabel();
            cursor.Emit(OpCodes.Nop);
            cursor.MarkLabel(lblNope);

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(PlayerProgression).GetField(nameof(PlayerProgression.currentSaveState))),
                x => x.MatchLdloc(3),
                x => x.MatchLdcI4(1))) return;
            DebugLogCursor();
            var lblOkay = cursor.DefineLabel();
            cursor.Emit(OpCodes.Nop);
            cursor.MarkLabel(lblOkay);

            if (!cursor.TryGotoPrev(MoveType.Before,
                x => x.MatchLdloc(3),
                x => x.MatchLdcI4(1),
                x => x.MatchLdelemRef(),
                x => x.MatchCall(typeof(BackwardsCompatibilityRemix).GetMethod(nameof(BackwardsCompatibilityRemix.ParseSaveNumber))))) return;
            DebugLogCursor();
            var lblNoLancer = cursor.DefineLabel();
            cursor.Emit(OpCodes.Nop);
            cursor.MarkLabel(lblNoLancer);
            cursor.GotoLabel(lblNoLancer, MoveType.Before);

            cursor.EmitDelegate<Func<bool>>(() => IsStoryLancer);
            cursor.Emit(OpCodes.Brfalse, lblNoLancer);

            cursor.Emit(OpCodes.Ldloc, 3);
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<string[], PlayerProgression, bool>>((array, self) =>
                BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == GetLancer(self.currentSaveState.saveStateNumber)
                    && !LancerGenerator.IsCustomLancer(GetLancer(self.currentSaveState.saveStateNumber))
            );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Br, lblNope);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LoadLancerState);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        #endregion SaveState

        private static SpeedRunTimer.CampaignTimeTracker GetLancerCampaignTimeTracker(On.MoreSlugcats.SpeedRunTimer.orig_GetCampaignTimeTracker orig, SlugName slugcat)
        {
            if (slugcat == null) return orig(slugcat);
            if (IsStoryLancer) slugcat = GetLancer(slugcat);
            return orig(slugcat);
        }

        #region Region

        private static SlugName GetStoryBasisForLancer(SlugName storyIndex)
        {
            if (storyIndex == null) return SlugName.White; // failsafe
            var slugName = new SlugName(storyIndex.value, false);
            if ((IsLancer(slugName) || IsStoryLancer) && !LancerGenerator.IsCustomLancer(GetLancer(slugName)))
                return LancerGenerator.GetStoryBasisForLancer(slugName);
            return storyIndex; // Not lancer or is custom
        }

        private static void LancerRoomSettings(On.RoomSettings.orig_ctor_Room_string_Region_bool_bool_Timeline_RainWorldGame orig,
            RoomSettings self, Room room, string name, Region region, bool template, bool firstTemplate, SlugTime timelinePoint, RainWorldGame game)
        {
            if (IsStoryLancer && game != null)
            {
                var story = GetStoryBasisForLancer(game.StoryCharacter);
                timelinePoint = SlugcatStats.SlugcatToTimeline(story);

                // LonkInvSLRoomSettings
                if (ModManager.MSC && SLOracleModify.IsMoonComatose(GetLancer(game.StoryCharacter)) && region.name == "SL")
                    timelinePoint = SlugTime.Sofanthiel;

                //Debug.Log($"LancerRoomSettings: {game.StoryCharacter} {game.TimelinePoint} > {timelinePoint} ({SLOracleModify.IsMoonComatose(story)})");
            }

            orig(self, room, name, region, template, firstTemplate, timelinePoint, game);
        }

        private static string LancerRegionFullName(On.Region.orig_GetRegionFullName orig, string regionAcro, SlugName slugcatIndex)
        {
            if (slugcatIndex != null)
                slugcatIndex = GetStoryBasisForLancer(slugcatIndex);
            return orig(regionAcro, slugcatIndex);
        }

        private static void LancerWorldLoader(On.WorldLoader.orig_ctor_RainWorldGame_Name_Timeline_bool_string_Region_SetupValues orig, WorldLoader self,
            RainWorldGame game, SlugName playerCharacter, SlugTime timelinePosition, bool singleRoomWorld, string worldName, Region region, RainWorldGame.SetupValues setupValues)
        {
            if (singleRoomWorld) goto Init;
            Debug.Log($"LancerWorldLoader: {playerCharacter} {timelinePosition} > {GetStoryBasisForLancer(playerCharacter)}");
            playerCharacter = GetStoryBasisForLancer(playerCharacter);
            timelinePosition = SlugcatStats.SlugcatToTimeline(playerCharacter);

        Init: orig(self, game, playerCharacter, timelinePosition, singleRoomWorld, worldName, region, setupValues);
        }

        private static bool LonkNoUnlockGate(On.DeathPersistentSaveData.orig_CanUseUnlockedGates orig, DeathPersistentSaveData self, SlugName slugcat)
        {
            if (IsStoryLancer && GetBasis(slugcat) == SlugName.Yellow)
                return ModManager.MMF && MMF.cfgGlobalMonkGates != null && MMF.cfgGlobalMonkGates.Value;
            return orig(self, slugcat);
        }

        private static void LancerGetBasisRegion(On.Region.orig_ctor_string_int_int_Timeline orig, Region self, string name, int firstRoomIndex, int regionNumber, SlugTime timelineIndex)
        {
            if (timelineIndex != null)
            {
                var story = GetStoryBasisForLancer(new SlugName(timelineIndex.value, false));
                timelineIndex = SlugcatStats.SlugcatToTimeline(story);
            }
            orig(self, name, firstRoomIndex, regionNumber, timelineIndex);
        }

        private static Region[] LoadAllLancerRegion(On.Region.orig_LoadAllRegions_Timeline orig, SlugTime timelineIndex)
        {
            if (timelineIndex != null)
            {
                var story = GetStoryBasisForLancer(new SlugName(timelineIndex.value, false));
                timelineIndex = SlugcatStats.SlugcatToTimeline(story);
            }
            return orig(timelineIndex);
        }

        private static void UpdateConditionalLancerShelters(On.PlayerProgression.MiscProgressionData.orig_updateConditionalShelters orig,
            PlayerProgression.MiscProgressionData self, string room, SlugName slugcatIndex)
        {
            if (IsStoryLancer && !LancerGenerator.IsCustomLancer(GetLancer(slugcatIndex))) slugcatIndex = GetLancer(slugcatIndex);
            orig(self, room, slugcatIndex);
        }

        #endregion Region
    }
}