using LancerRemix.Cat;
using Menu;
using System.IO;
using UnityEngine;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;
using MenueSceneID = Menu.MenuScene.SceneID;

namespace LancerRemix.LancerMenu
{
    internal static class MenuModifier
    {
        internal static void Patch()
        {
            On.Menu.MenuScene.ctor += LancerSceneSwap;

            SelectMenuPatch.SubPatch();
            /// TODO: add Lancer toggle button in slugcat select menu
            /// clicking that swooshes illust upwards.
            ///
            /// or just save which players should be lancer
            /// and attach supplements to them separately
            /// lancer supplements will pass null for orig to prevent double updates
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
    }
}