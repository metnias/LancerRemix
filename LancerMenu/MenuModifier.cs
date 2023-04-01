﻿using LancerRemix.Cat;
using Menu;
using System.IO;
using UnityEngine;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;
using MenueSceneID = Menu.MenuScene.SceneID;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;

namespace LancerRemix.LancerMenu
{
    internal static class MenuModifier
    {
        internal static void Patch()
        {
            On.Menu.MenuScene.ctor += LancerSceneSwap;
            IL.Menu.FastTravelScreen.ctor += LancerTravelScreen;

            SelectMenuPatch.SubPatch();
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos)
            => SelectMenuPatch.ReplaceIllust(scene, sceneFolder, flatImage, layerImageOrig, layerImage, layerPos, false);

        private static void LancerSleepScene(MenuScene scene)
        {
            SlugName basis;
            if (scene.menu.manager.currentMainLoop is RainWorldGame)
                basis = (scene.menu.manager.currentMainLoop as RainWorldGame).StoryCharacter;
            else
                basis = scene.menu.manager.rainWorld.progression.PlayingAsSlugcat;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis == SlugName.White)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}sleep screen - lancer",
                    "sleep lancer - white - flat", "sleep - 2 - white", "lancer - 2 - white", new Vector2(677f, 63f));
                //ReplaceIllust(menu.scene, $"scenes{Path.DirectorySeparatorChar}sleep screen - white",
                //    "sleep screen - white - flat", "sleep - 2 - white", "sleep - 2 - white", new Vector2(677f, 63f));
            }
            /*
            else if (basis == SlugName.Yellow)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - yellow - flat", "yellow slugcat - 1", "yellow lancer - 1", new Vector2(528f, 211f));
            }
            else if (basis == SlugName.Red)
            {
                ReplaceIllust(scene, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - red - flat", "red slugcat - 1", "red lancer - 1", new Vector2(462f, 225f));
            }
            */
        }

        private static void LancerSceneSwap(On.Menu.MenuScene.orig_ctor orig, MenuScene self, Menu.Menu menu, MenuObject owner, MenueSceneID sceneID)
        {
            orig(self, menu, owner, sceneID);
            if (sceneID == MenueSceneID.SleepScreen)
            {
                LancerSleepScene(self);
            }
        }

        private static void LancerTravelScreen(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.LogSource.LogInfo("LancerTravelScreen Patch");

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
                    var lancer = self.manager.rainWorld.progression.PlayingAsSlugcat;
                    if (HasLancer(lancer)) lancer = GetLancer(lancer);
                    Debug.Log($"Lancer: Switched FastTravelScreen for {lancer.value}({lancer.Index}) (basis: {self.manager.rainWorld.progression.PlayingAsSlugcat})");
                    return lancer.Index;
                }
                );
            cursor.Emit(OpCodes.Stloc, 1);

            #endregion SetNumToLancer

            LancerPlugin.LogSource.LogInfo("LancerTravelScreen Patch Done");

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }
    }
}