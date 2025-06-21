using CatSub.Story;
using LancerRemix.Cat;
using LancerRemix.Story;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
using System.IO;
using UnityEngine;
using static LancerRemix.LancerEnums;
using MenuSceneID = Menu.MenuScene.SceneID;
using SlugName = SlugcatStats.Name;

//using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;

namespace LancerRemix.LancerMenu
{
    internal static class MenuModifier
    {
        internal static void Patch()
        {
            On.Menu.MenuScene.ctor += LancerSceneSwap;
            IL.Menu.FastTravelScreen.ctor += LancerTravelScreen;
            On.Menu.StoryGameStatisticsScreen.AddBkgIllustration += LancerStatsBkgScene;
            IL.Menu.IntroRoll.ctor += LancerIntroRoll;

            SelectMenuPatch.SubPatch();
            MultiplayerPatch.SubPatch();

            if (ModManager.JollyCoop) OnJollyEnablePatch();
            if (ModManager.MSC) OnMSCEnablePatch();
            if (ModManager.MMF) HornColorPick.OnMMFEnablePatch();
        }

        internal static void OnJollyEnablePatch()
        {
            MultiplayerPatch.OnJollyEnableSubPatch();
            HornColorPick.OnJollyEnableSubPatch();
        }

        internal static void OnJollyDisablePatch()
        {
            MultiplayerPatch.OnJollyDisableSubPatch();
            HornColorPick.OnJollyDisableSubPatch();
        }

        internal static void OnMSCEnablePatch()
        {
            MultiplayerPatch.OnMSCEnableSubPatch();
        }

        internal static void OnMSCDisablePatch()
        {
            MultiplayerPatch.OnMSCDisableSubPatch();
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos, MenuDepthIllustration.MenuShader shader = null)
            => SelectMenuPatch.ReplaceIllust(scene, sceneFolder, flatImage, layerImageOrig, layerImage, layerPos, shader ?? MenuDepthIllustration.MenuShader.Normal);

        private static void LancerSleepScene(MenuScene scene)
        {
            SlugName basis;
            if (scene.menu.manager.currentMainLoop is RainWorldGame)
                basis = (scene.menu.manager.currentMainLoop as RainWorldGame).StoryCharacter;
            else
                basis = scene.menu.manager.rainWorld.progression.PlayingAsSlugcat;
            basis = GetBasis(basis);
            if (basis == SlugName.White)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}sleep screen - lancer",
                    "sleep lancer - white - flat", "sleep - 2 - white", "lancer - 2 - white", new Vector2(677f, 63f));
            }
            else if (basis == SlugName.Yellow)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}sleep screen - lancer",
                    "sleep lancer - yellow - flat", "sleep - 2 - yellow", "lancer - 2 - yellow", new Vector2(677f, 63f));
            }
            else if (basis == SlugName.Red)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}sleep screen - lancer",
                    "sleep lancer - red - flat", "sleep - 2 - red", "lancer - 2 - red", new Vector2(817f, 112f));
            }
        }

        private static void LancerStatsBkgScene(On.Menu.StoryGameStatisticsScreen.orig_AddBkgIllustration orig, StoryGameStatisticsScreen self)
        {
            if (!IsStoryLancer) goto NoLancer;
            var basis = GetBasis(ModManager.MSC ? RainWorld.lastActiveSaveSlot : SlugName.Red);
            var lancer = GetLancer(basis);

            var saveGameData = SlugcatSelectMenu.MineForSaveData(self.manager, lancer);
            if (saveGameData != null && saveGameData.ascended) // && (!ModManager.MSC || RainWorld.lastActiveSaveSlot != MSCName.Saint)
            {
                self.scene = new InteractiveMenuScene(self, self.pages[0], MenuSceneID.Red_Ascend);
                self.pages[0].subObjects.Add(self.scene);
                return;
            }

        NoLancer:
            orig(self);
        }

        private static void LancerSceneSwap(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuSceneID sceneID)
        {
            if (IsStoryLancer)
            {
                if (sceneID == MenuSceneID.Outro_Hunter_1_Swim)
                    sceneID = SceneOutroLHunter1Swim;
            }
            orig(self, menu, owner, sceneID);
            if (!IsStoryLancer) return;
            if (sceneID == MenuSceneID.SleepScreen) LancerSleepScene(self);
            else if (sceneID == MenuSceneID.Outro_3_Face) LancerOutroFace(self);

            #region LunterOutro

            else if (sceneID == MenuSceneID.Outro_Hunter_2_Sink)
            {
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}outro lhunter 2 - sink", "outro Lhunter 2 - sink - flat",
                    "outro hunter 2 - sink - 4", "outro Lhunter 2 - sink - 4", new Vector2(179f, 127f));
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}outro lhunter 2 - sink", null,
                    "outro hunter 2 - sink - 3", "outro Lhunter 2 - sink - 3", new Vector2(544f, 159f));
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}outro lhunter 2 - sink", null,
                    "outro hunter 2 - sink - 1", "outro Lhunter 2 - sink - 1", new Vector2(315f, 49f));
            }
            else if (sceneID == MenuSceneID.Outro_Hunter_3_Embrace)
            {
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}outro Lhunter 3 - embrace", "outro Lhunter 3 - embrace - flat",
                    "outro hunter 3 - embrace - 2", "outro Lhunter 3 - embrace - 2", new Vector2(488f, 208f));
            }
            /*
            else if (sceneID == MenuSceneID.Red_Ascend)
            {
                ReplaceIllust(self, "", "", "", "", new Vector2());
            }
            else if (sceneID == MenuSceneID.RedsDeathStatisticsBkg)
            {
                ReplaceIllust(self, "", "", "", "", new Vector2());
            }
            */

            #endregion LunterOutro
        }

        private static void LancerOutroFace(MenuScene self)
        {
            var basis = DreamHandler.OutroLancerFaceBasis;
            if (basis == null) return;

            if (basis == SlugName.White)
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}Outro L_3", "outro 3 - face - Lsurv - flat",
                    "2 - facecloseup", "2 - facecloseup - Lsurv", new Vector2(43f, -59f));
            else if (basis == SlugName.Yellow)
                ReplaceIllust(self, $"scenes{Path.DirectorySeparatorChar}Outro L_3", "outro 3 - face - Lmonk - flat",
                    "2 - facecloseup", "2 - facecloseup - Lmonk", new Vector2(43f, -59f));
        }

        private static void LancerTravelScreen(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LancerTravelScreen);

            /*
            if (!cursor.TryGotoNext(MoveType.After,
                z => z.MatchLdcI4(-1),
                z => z.MatchStloc(1))) return;

            DebugLogCursor();
            // if !IsStoryLancer => skip ahead

            #region SkipNoLancer

            cursor.Emit(OpCodes.Nop);
            var lblNoLancer = cursor.DefineLabel();
            lblNoLancer.Target = cursor.Prev;
            cursor.GotoLabel(lblNoLancer, MoveType.Before);

            cursor.EmitDelegate<Func<bool>>(() =>
            {
                return IsStoryLancer;
            }
                );
            cursor.Emit(OpCodes.Brfalse, lblNoLancer);

            #endregion SkipNoLancer

            // num = GetLancer(menu.manager.rainWorld.progression.PlayingAsSlugcat).Index

            #region SetNumToLancer

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<FastTravelScreen, int>>(
                (self) =>
                {
                    var lancer = GetLancer(self.manager.rainWorld.progression.PlayingAsSlugcat);
                    Debug.Log($"Lancer: Switched FastTravelScreen for {lancer.value}({lancer.Index}) (basis: {self.manager.rainWorld.progression.PlayingAsSlugcat})");
                    return lancer.Index;
                }
                );
            cursor.Emit(OpCodes.Stloc, 1);

            #endregion SetNumToLancer

            */

            if (!cursor.TryGotoNext(MoveType.After,
                z => z.MatchStfld(typeof(FastTravelScreen).GetField(nameof(FastTravelScreen.activeMenuSlugcat))))) return;

            DebugLogCursor();

            #region SetNumToLancer

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Action<FastTravelScreen>>(
                (self) =>
                {
                    if (!IsStoryLancer) return;
                    var lancer = GetLancer(self.activeMenuSlugcat);
                    Debug.Log($"Lancer: Switched FastTravelScreen for {lancer.value}({lancer.Index}) (basis: {self.activeMenuSlugcat})");
                    self.activeMenuSlugcat = lancer;
                }
                );

            #endregion SetNumToLancer

            DebugLogCursor();

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LancerTravelScreen);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static void LancerIntroRoll(ILContext il)
        {
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LancerIntroRoll);

            var cursor = new ILCursor(il);
            ILLabel introRollCAdded = null;

            if (!cursor.TryGotoNext(
                MoveType.AfterLabel,
                x => x.MatchLdsfld<ModManager>(nameof(ModManager.Watcher)),
                x => x.MatchBrfalse(out _))
                || !cursor.Clone().TryGotoNext(x => x.MatchBr(out introRollCAdded)))
                return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<IntroRoll, bool>>(self =>
            {
                SlugName curSlugcat = self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat;
                bool lastPlayedLancer = IsLancer(curSlugcat);
                lastPlayedLancer |= TryGetCurrSlugcatLancer(self.manager);
                if (!lastPlayedLancer) return false;
                var basis = GetBasis(curSlugcat);
                if (basis == SlugName.White || basis == SlugName.Yellow || basis == SlugName.Red)
                {
                    self.illustrations[2] = new MenuIllustration(self, self.pages[0], "", "title_card_lancer", new Vector2(0f, 0f), true, false);
                    return true;
                }
                if (basis == Watcher.WatcherEnums.SlugcatStatsName.Watcher)
                {
                    self.illustrations[2] = new MenuIllustration(self, self.pages[0], "", "title_card_latcher", new Vector2(0f, 0f), true, false);
                    return true;
                }

                return false;
            });
            cursor.Emit(OpCodes.Brtrue, introRollCAdded);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LancerIntroRoll);

            bool TryGetCurrSlugcatLancer(ProcessManager manager)
            {
                try
                { return SaveManager.GetMiscValue<bool>(manager.rainWorld.progression.miscProgressionData, SwapSave.CURRSLUGCATLANCER); }
                catch // first install
                { SaveManager.SetMiscValue(manager.rainWorld.progression.miscProgressionData, SwapSave.CURRSLUGCATLANCER, true); }
                return true;
            }
            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }
    }
}