using LancerRemix.Cat;
using LancerRemix.LancerMenu;
using Menu;
using RWCustom;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using UnityEngine;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal static class LurvivorScripts
    {
        internal static void SubPatch()
        {
        }

        internal static void OnMSCEnableSubPatch()
        {
            On.Menu.MenuScene.BuildVanillaAltEnd += BuildLancerOEEnd;
        }

        internal static void OnMSCDisableSubPatch()
        {
            On.Menu.MenuScene.BuildVanillaAltEnd -= BuildLancerOEEnd;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos, MenuDepthIllustration.MenuShader shader = null)
            => SelectMenuPatch.ReplaceIllust(scene, sceneFolder, flatImage, layerImageOrig, layerImage, layerPos, shader ?? MenuDepthIllustration.MenuShader.LightEdges);

        private static bool yellowHasLancer = false;

        private static void BuildLancerOEEnd(On.Menu.MenuScene.orig_BuildVanillaAltEnd orig, MenuScene self,
            int sceneID, SlugName character, int slugpups)
        {
            bool yellow = !IsStoryLancer && character == SlugName.Yellow;
            if (IsStoryLancer)
            {
                character = SlugName.Yellow; slugpups = 0;
                if (sceneID == 2) sceneID = 3;
            }
            orig(self, sceneID, character, slugpups);
            if (!IsStoryLancer && !yellow) return;
            if (yellow && sceneID == 1) yellowHasLancer = GetLurvivorOEEnd();
            if (yellow && !yellowHasLancer) return;

            string LANCERFOLDER = $"Scenes{Path.DirectorySeparatorChar}Outro L_B";

            switch (sceneID)
            {
                case 1:
                    if (!yellow)
                        ReplaceIllust(self, LANCERFOLDER,
                            "Outro 1_B - Clearing - LWhite - Flat", "Outro 1_B - Clearing - 0_Yellow_0", "Outro 1_B - Clearing - 0_LWhite",
                            new Vector2(406f, -115f));
                    break;

                default:
                case 2:
                    break;

                case 3:
                    if (!yellow)
                    {
                        ReplaceIllust(self, LANCERFOLDER,
                            "Outro 3_B - Return - LWhite - Flat", "Outro 3_B - Return - 1", "Outro 3_B - Return - LWhite 1", new Vector2(-62f, -29f));
                        ReplaceIllust(self, LANCERFOLDER,
                            string.Empty, "outro 3_B - Return - 0", "Outro 3_B - Return - LWhite 0", new Vector2(812f, -46f));
                    }
                    else
                        ReplaceIllust(self, LANCERFOLDER,
                            "Outro 3_B - Return - YellowL - Flat", "Outro 3_B - Return - 1", "Outro 3_B - Return - YellowL 1",
                            new Vector2(406f, -115f));
                    break;

                case 4:
                    if (!yellow)
                    {
                        ReplaceIllust(self, LANCERFOLDER,
                            "Outro 4_B - Home - LWhite - Flat", "Outro 4_B - Home - 4_Yellow_0", "Outro 4_B - Home - LWhite 4", new Vector2(574f, -21f));
                        ReplaceIllust(self, self.sceneFolder,
                            string.Empty, "outro 4_b - home - 2 - yellow", "outro 4_b - home - 2 - white", new Vector2(351f, 386f));
                        ReplaceIllust(self, LANCERFOLDER,
                            string.Empty, "Outro 4_B - Home - 0 - Yellow", "Outro 4_B - Home - LWhite 0", new Vector2(200f, -55f));
                    }
                    else
                        ReplaceIllust(self, LANCERFOLDER,
                            "Outro 4_B - Home - YellowL - Flat", "outro 4_b - home - 0 - yellow", "Outro 4_B - Home - YellowL 0",
                            new Vector2(200f, -55f));
                    break;
            }

            bool GetLurvivorOEEnd()
            {
                var progLines = Custom.rainWorld.progression?.GetProgLinesFromMemory();
                if (progLines == null || progLines.Length == 0) return false;
                var whiteLancer = LancerEnums.GetLancer(SlugName.White);
                for (int i = 0; i < progLines.Length; ++i)
                {
                    var array = Regex.Split(progLines[i], "<progDivB>");
                    if (array.Length != 2 || array[0] != "SAVE STATE"
                        || BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) != whiteLancer) continue;

                    const string ALTEND = ">ALTENDING";
                    var mineTarget = new List<SaveStateMiner.Target>()
                    { new SaveStateMiner.Target(ALTEND, null, "<dpA>", 20) };
                    var mineResult = SaveStateMiner.Mine(Custom.rainWorld, array[1], mineTarget);
                    if (mineResult.Count > 0 && mineResult[0].name == ALTEND) return true;
                    return false;
                }
                return false;
            }
        }
    }
}