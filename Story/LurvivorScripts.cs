using LancerRemix.Cat;
using Menu;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

        private static void BuildLancerOEEnd(On.Menu.MenuScene.orig_BuildVanillaAltEnd orig, MenuScene self,
            int sceneID, SlugName character, int slugpups)
        {
            // TODO: edit this for Lancer Surv & normal Monk
            if (character == null) character = SlugName.White;
            if (!IsStoryLancer && character != SlugName.Yellow) { orig(self, sceneID, character, slugpups); return; }
            //bool isLancer = IsStoryLancer;

            string sceneName;
            switch (sceneID)
            {
                default: sceneName = "Outro 1_B - Clearing"; break;
                case 2: sceneName = "Outro 2_B - Peek"; break;
                case 3: sceneName = "Outro 3_B - Return"; break;
                case 4: sceneName = "Outro 4_B - Home"; break;
            }
            int pupNum = Mathf.Min(slugpups, 2);
            if (character != SlugName.White) pupNum = 0;
            self.sceneFolder = "Scenes" + Path.DirectorySeparatorChar + sceneName;
            if (self.flatMode)
            {
                self.AddIllustration(new MenuIllustration(self.menu, self, self.sceneFolder, string.Concat(new string[]
                {
                    sceneName,
                    " - Flat_",
                    character.ToString(),
                    "_",
                    pupNum.ToString()
                }), new Vector2(683f, 384f), false, true));
                return;
            }
            if (sceneID == 1)
            {
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 1_B - Clearing - 3", new Vector2(71f, 49f), 10f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 1_B - Clearing - 2", new Vector2(71f, 49f), 4.5f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 1_B - Clearing - 1", new Vector2(71f, 49f), 1.9f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 1_B - Clearing - 0_" + character.ToString() + "_" + pupNum.ToString(), new Vector2(71f, 49f), 1.5f, MenuDepthIllustration.MenuShader.LightEdges));
                return;
            }
            if (sceneID == 2)
            {
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 2_B - Peek - 3", new Vector2(71f, 49f), 12f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 2_B - Peek - 2_" + character.ToString() + "_" + pupNum.ToString(), new Vector2(71f, 49f), 10f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 2_B - Peek - 1", new Vector2(71f, 49f), 6.5f, MenuDepthIllustration.MenuShader.Basic));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 2_B - Peek - 0", new Vector2(71f, 49f), 3.2f, MenuDepthIllustration.MenuShader.LightEdges));
                return;
            }
            if (sceneID == 3)
            {
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 3_B - Return - 3", new Vector2(71f, 49f), 8f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 3_B - Return - 2", new Vector2(71f, 49f), 4.5f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 3_B - Return - 1", new Vector2(71f, 49f), 3.8f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 3_B - Return - 0", new Vector2(71f, 49f), 1.5f, MenuDepthIllustration.MenuShader.LightEdges));
                return;
            }
            if (sceneID == 4)
            {
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 10", new Vector2(71f, 49f), 10f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 9", new Vector2(71f, 49f), 6.2f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 8", new Vector2(71f, 49f), 4.8f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 7", new Vector2(71f, 49f), 3.8f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 6", new Vector2(71f, 49f), 3.2f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 5_" + character.ToString() + "_" + ((pupNum > 0) ? "1" : "0"), new Vector2(71f, 49f), 3.2f, MenuDepthIllustration.MenuShader.Multiply));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 4_" + character.ToString() + "_" + ((pupNum > 0) ? "1" : "0"), new Vector2(71f, 49f), 3.2f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 3_" + ((pupNum > 1) ? "1" : "0"), new Vector2(71f, 49f), 2.4f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 2 - " + character.ToString(), new Vector2(71f, 49f), 2.4f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 1", new Vector2(71f, 49f), 1.8f, MenuDepthIllustration.MenuShader.LightEdges));
                self.AddIllustration(new MenuDepthIllustration(self.menu, self, self.sceneFolder, "Outro 4_B - Home - 0 - " + character.ToString(), new Vector2(71f, 49f), 1.5f, MenuDepthIllustration.MenuShader.LightEdges));
            }
        }
    }
}