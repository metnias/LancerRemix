#define NO_MSC

using Menu;
using Menu.Remix;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using static LancerRemix.LancerMenu.SelectMenuPatch;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.LancerMenu
{
    internal static class MultiplayerPatch
    {
        internal static void SubPatch()
        {
            On.Menu.MultiplayerMenu.Singal += ArenaMenuSingal;
        }

        #region Jolly

        internal static void OnJollyEnableSubPatch()
        {
            SymbolButtonToggleLancerButton.SubPatch();
        }

        internal static void OnJollyDisableSubPatch()
        {
            SymbolButtonToggleLancerButton.SubUnpatch();
        }

        #endregion Jolly

        #region Arena

        internal static void OnMSCEnableSubPatch()
        {
            On.Menu.MultiplayerMenu.Update += ArenaClassLancerToggle;
            On.Menu.MultiplayerMenu.CustomUpdateInfoText += ArenaClassLancerDesc;
            On.Menu.MultiplayerMenu.ArenaImage += LancerArenaImage;
        }

        internal static void OnMSCDisableSubPatch()
        {
            On.Menu.MultiplayerMenu.Update -= ArenaClassLancerToggle;
            On.Menu.MultiplayerMenu.CustomUpdateInfoText -= ArenaClassLancerDesc;
            On.Menu.MultiplayerMenu.ArenaImage -= LancerArenaImage;
        }

        private static void ArenaClassLancerToggle(On.Menu.MultiplayerMenu.orig_Update orig, MultiplayerMenu self)
        {
            orig(self);
            if (self.FreezeMenuFunctions || !(self.selectedObject is SimpleButton btn) || !btn.signalText.Contains("CLASSCHANGE")) return;
            if (self.manager.menuesMouseMode) // right click to lancer toggle
            {
                if (Input.GetMouseButton(1))
                {
                    if (!classBtnHeld) self.PlaySound(SoundID.MENU_Button_Press_Init);
                    classBtnHeld = true;
                }
                else
                {
                    if (classBtnHeld) ToggleLancer();
                    classBtnHeld = false;
                }
            }
            else // thrw btn to lancer toggle
            {
                if (self.input.thrw)
                {
                    if (!classBtnHeld) self.PlaySound(SoundID.MENU_Button_Press_Init);
                    classBtnHeld = true;
                }
                else
                {
                    if (classBtnHeld) ToggleLancer();
                    classBtnHeld = false;
                }
            }

            void ToggleLancer()
            {
                int player = btn.signalText[btn.signalText.Length - 1] - '0';
                SetLancerPlayers(player, !GetLancerPlayers(player));
                self.playerJoinButtons[player].portrait.fileName = self.ArenaImage(self.GetArenaSetup.playerClass[player], player);
                self.playerJoinButtons[player].portrait.LoadFile();
                self.playerJoinButtons[player].portrait.sprite.SetElementByName(self.playerJoinButtons[player].portrait.fileName);
                self.PlaySound(SoundID.MENU_Button_Standard_Button_Pressed);
            }
        }
        private static bool classBtnHeld = false;

        private static string LancerArenaImage(On.Menu.MultiplayerMenu.orig_ArenaImage orig, MultiplayerMenu self, SlugName classID, int color)
        {
            if (classID == null || classID.Index < 0) goto Orig;
            if (GetLancerPlayers(color))
            {
                var res = orig(self, classID, color);
                var lancer = res + "-lancer";
                string path = "Illustrations" + Path.DirectorySeparatorChar.ToString() + lancer + ".png";
                if (File.Exists(AssetManager.ResolveFilePath(path))) return lancer;
                return res;
            }
        Orig: return orig(self, classID, color);
        }

        private static string ArenaClassLancerDesc(On.Menu.MultiplayerMenu.orig_CustomUpdateInfoText orig, MultiplayerMenu self)
        {
            if (self.selectedObject is SimpleButton btn && btn.signalText.Contains("CLASSCHANGE"))
            {
                string res;
                if (self.manager.menuesMouseMode) res = self.Translate("Left click to change class, right click to toggle lancer for Player <X>");
                else
                {
                    res = self.Translate("<A> to change class, <B> to toggle lancer for Player <X>");
                    res = res.Replace("<A>", OptionalText.GetButtonName_Jump());
                    res = res.Replace("<B>", OptionalText.GetButtonName_Throw());
                }
                string replacement = ((char)(btn.signalText[btn.signalText.Length - 1] + 1)).ToString();
                return Regex.Replace(res, @"<X>", replacement);
            }
            return orig(self);
        }

        private static void ArenaMenuSingal(On.Menu.MultiplayerMenu.orig_Singal orig, MultiplayerMenu self, MenuObject sender, string message)
        {
            if (string.IsNullOrEmpty(message)) goto Skip;
            if (message == "PLAY!" || message == "RESUME")
            {
                UpdateIsPlayerLancer(false);
                if (!ModManager.MSC)
                    for (int i = 0; i < 4; ++i) SetLancerPlayers(i, false);
#if NO_MSC
                else
                {
                    for (int i = 0; i < 4; ++i)
                        if (SlugcatStats.IsSlugcatFromMSC(self.GetArenaSetup.playerClass[i]))
                            SetLancerPlayers(i, false);
                }
#endif
                SaveLancerPlayers(self.manager.rainWorld.progression.miscProgressionData);
            }
        Skip: orig(self, sender, message);
        }

        #endregion Arena
    }
}