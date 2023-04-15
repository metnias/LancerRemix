#define NO_MSC

using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using MoreSlugcats;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using static LancerRemix.LancerMenu.SelectMenuPatch;
using SlugName = SlugcatStats.Name;
using JollyName = JollyCoop.JollyEnums.Name;
using RWCustom;
using LancerRemix.Cat;

namespace LancerRemix.LancerMenu
{
    internal static class MultiplayerPatch
    {
        internal static void SubPatch()
        {
            On.Menu.MultiplayerMenu.Singal += ArenaMenuSingal;
#if NO_MSC
            On.Menu.ChallengeSelectPage.StartButton_OnPressDone += ExpeditionDisableMSCLancers;
#endif
        }

        #region Jolly

        internal static void OnJollyEnableSubPatch()
        {
            SymbolButtonToggleLancerButton.SubPatch();
            On.JollyCoop.JollyCustom.SlugClassMenu += JollyClassMenuAvoidLancer;
            On.PlayerGraphics.SlugcatColor += CoopLancerColor;
        }

        internal static void OnJollyDisableSubPatch()
        {
            SymbolButtonToggleLancerButton.SubUnpatch();
            On.JollyCoop.JollyCustom.SlugClassMenu -= JollyClassMenuAvoidLancer;
            On.PlayerGraphics.SlugcatColor -= CoopLancerColor;
        }

        private static SlugName JollyClassMenuAvoidLancer(On.JollyCoop.JollyCustom.orig_SlugClassMenu orig, int playerNumber, SlugName fallBack)
        {
            var res = orig(playerNumber, fallBack);
            return LancerEnums.GetBasis(res);
        }

        private static Color CoopLancerColor(On.PlayerGraphics.orig_SlugcatColor orig, SlugName i)
        {
            if (ModManager.CoopAvailable && Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.DEFAULT)
            {
                int num = 0;
                if (i == JollyName.JollyPlayer2) num = 1;
                if (i == JollyName.JollyPlayer3) num = 2;
                if (i == JollyName.JollyPlayer4) num = 3;

                i = Custom.rainWorld.options.jollyPlayerOptionsArray[num].playerClass ?? i;
                if (ModifyCat.IsPlayerLancer(num)) i = LancerEnums.GetLancer(i);
                return PlayerGraphics.DefaultSlugcatColor(i);
            }
            return orig(i);
        }

        #endregion Jolly

        #region Arena

        internal static void OnMSCEnableSubPatch()
        {
            On.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons += InitArenaLancerIcons;
            On.Menu.MultiplayerMenu.GrafUpdate += GrafUpdateArenaLancerIcons;
            On.Menu.MultiplayerMenu.ShutDownProcess += ClearArenaLancerIcons;
            On.Menu.MultiplayerMenu.Update += ArenaClassLancerToggle;
            On.Menu.MultiplayerMenu.CustomUpdateInfoText += ArenaClassLancerDesc;
            On.Menu.MultiplayerMenu.ArenaImage += LancerArenaImage;
        }

        internal static void OnMSCDisableSubPatch()
        {
            On.Menu.MultiplayerMenu.InitiateGameTypeSpecificButtons -= InitArenaLancerIcons;
            On.Menu.MultiplayerMenu.GrafUpdate -= GrafUpdateArenaLancerIcons;
            On.Menu.MultiplayerMenu.ShutDownProcess -= ClearArenaLancerIcons;
            On.Menu.MultiplayerMenu.Update -= ArenaClassLancerToggle;
            On.Menu.MultiplayerMenu.CustomUpdateInfoText -= ArenaClassLancerDesc;
            On.Menu.MultiplayerMenu.ArenaImage -= LancerArenaImage;
        }

        private static FSprite[] spearIcons;

        private static void InitArenaLancerIcons(On.Menu.MultiplayerMenu.orig_InitiateGameTypeSpecificButtons orig, MultiplayerMenu self)
        {
            orig(self);
            if (self.playerClassButtons == null) return;
            spearIcons = new FSprite[self.playerClassButtons.Length];
            for (int i = 0; i < self.playerClassButtons.Length; ++i)
            {
                spearIcons[i] = new FSprite("Symbol_Spear", true) { scale = 0.5f };
                self.playerClassButtons[i].Container.AddChild(spearIcons[i]);
                spearIcons[i].x = self.playerClassButtons[i].DrawX(0f) + self.playerClassButtons[i].size.x - 12f;
                spearIcons[i].y = self.playerClassButtons[i].DrawY(0f) + self.playerClassButtons[i].size.y * 0.5f;
            }
        }

        private static void GrafUpdateArenaLancerIcons(On.Menu.MultiplayerMenu.orig_GrafUpdate orig, MultiplayerMenu self, float timeStacker)
        {
            orig(self, timeStacker);
            if (self.playerClassButtons == null) return;
            for (int i = 0; i < self.playerClassButtons.Length; ++i)
            {
                spearIcons[i].isVisible = GetLancerPlayers(i);
                spearIcons[i].color = self.playerClassButtons[i].MyColor(timeStacker);
            }
        }

        private static void ClearArenaLancerIcons(On.Menu.MultiplayerMenu.orig_ShutDownProcess orig, MultiplayerMenu self)
        {
            if (self.playerClassButtons != null)
            {
                for (int i = 0; i < self.playerClassButtons.Length; ++i) spearIcons[i].RemoveFromContainer();
                spearIcons = null;
            }
            orig(self);
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
            else // pckp btn to lancer toggle
            {
                if (self.input.pckp)
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
                    res = res.Replace("<B>", OptionalText.GetButtonName_PickUp());
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
                if (!ModManager.MSC || self.currentGameType == MoreSlugcatsEnums.GameTypeID.Challenge)
                    for (int i = 0; i < 4; ++i) SetLancerPlayers(i, false);
#if NO_MSC
                else
                {
                    for (int i = 0; i < 4; ++i)
                        if (self.GetArenaSetup.playerClass[i] != null && SlugcatStats.IsSlugcatFromMSC(self.GetArenaSetup.playerClass[i]))
                            SetLancerPlayers(i, false);
                }
#endif
                SaveLancerPlayers(self.manager.rainWorld.progression.miscProgressionData);
            }
        Skip: orig(self, sender, message);
        }

        #endregion Arena

#if NO_MSC

        private static void ExpeditionDisableMSCLancers(On.Menu.ChallengeSelectPage.orig_StartButton_OnPressDone orig,
            ChallengeSelectPage self, UIfocusable trigger)
        {
            UpdateIsPlayerLancer(false);
            if (!ModManager.JollyCoop || !ModManager.CoopAvailable)
                for (int i = 0; i < 4; ++i) SetLancerPlayers(i, false);
#if NO_MSC
            else
            {
                for (int i = 0; i < self.menu.manager.rainWorld.options.JollyPlayerCount; ++i)
                {
                    var character = self.menu.manager.rainWorld.options.jollyPlayerOptionsArray[i].playerClass;
                    if (character != null && SlugcatStats.IsSlugcatFromMSC(LancerEnums.GetBasis(character)))
                        SetLancerPlayers(i, false);
                }
            }
#endif
            SaveLancerPlayers(self.menu.manager.rainWorld.progression.miscProgressionData);
            orig(self, trigger);
        }

#endif
    }
}