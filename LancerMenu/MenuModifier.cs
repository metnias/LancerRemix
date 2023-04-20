﻿using LancerRemix.Cat;
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

namespace LancerRemix.LancerMenu
{
    internal static class MenuModifier
    {
        internal static void Patch()
        {
            On.Menu.MenuScene.ctor += LancerSceneSwap;
            IL.Menu.FastTravelScreen.ctor += LancerTravelScreen;

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

        private static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos)
            => SelectMenuPatch.ReplaceIllust(scene, sceneFolder, flatImage, layerImageOrig, layerImage, layerPos, MenuDepthIllustration.MenuShader.Normal);

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

        private static void LancerSceneSwap(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenuSceneID sceneID)
        {
            orig(self, menu, owner, sceneID);
            if (!IsStoryLancer) return;
            if (sceneID == MenuSceneID.SleepScreen) LancerSleepScene(self);
            else if (sceneID == MenuSceneID.Outro_3_Face) LancerOutroFace(self);
            #region LunterOutro
            else if (sceneID == MenuSceneID.Outro_Hunter_1_Swim)
            {
                ReplaceIllust(self, "", "", "", "", new Vector2());
            }
            else if (sceneID == MenuSceneID.Outro_Hunter_2_Sink)
            {
                ReplaceIllust(self, "", "", "", "", new Vector2());
            }
            else if (sceneID == MenuSceneID.Outro_Hunter_3_Embrace)
            {
                ReplaceIllust(self, "", "", "", "", new Vector2());
            }
            #endregion LunterOutro
        }

        private static void LancerOutroFace(MenuScene self)
        {
            var basis = DreamHandler.OutroLancerFaceBasis;
            if (basis == null) return;

            if (basis == SlugName.White)
                ReplaceIllust(self, "", "", "", "", new Vector2());
            else if (basis == SlugName.Yellow)
                ReplaceIllust(self, "", "", "", "", new Vector2());
        }

        private static void LancerTravelScreen(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LancerTravelScreen);

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

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LancerTravelScreen);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }
    }
}