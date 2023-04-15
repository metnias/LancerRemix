using JollyCoop;
using JollyCoop.JollyMenu;
using LancerRemix.Cat;
using Menu;
using RWCustom;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using static LancerRemix.LancerMenu.SelectMenuPatch;

namespace LancerRemix.LancerMenu
{
    internal class SymbolButtonToggleLancerButton : SymbolButtonTogglePupButton
    {
        #region Patch

        internal static void SubPatch()
        {
            On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyLancerSelector;
            On.JollyCoop.JollyMenu.JollySlidingMenu.Singal += LancerButtonSignal;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.JollyPortraitName += JollyLancerPortraitName;
            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite += LancerHasNoUniqueSprite;
        }

        internal static void SubUnpatch()
        {
            On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor -= JollyLancerSelector;
            On.JollyCoop.JollyMenu.JollySlidingMenu.Singal -= LancerButtonSignal;
            On.JollyCoop.JollyMenu.JollyPlayerSelector.JollyPortraitName -= JollyLancerPortraitName;
            On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.HasUniqueSprite -= LancerHasNoUniqueSprite;
        }

        private static void JollyLancerSelector(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_ctor orig, JollyPlayerSelector self,
            JollySetupDialog menu, MenuObject owner, Vector2 pos, int index)
        {
            orig(self, menu, owner, pos, index);
            self.subObjects.Remove(self.pupButton);
            self.page.selectables.Remove(self.pupButton);
            self.pupButton.RemoveSprites();
            self.pupButton = null;

            var status = self.JollyOptions(index).isPup ? PlayerSize.Pup : PlayerSize.Normal;
            if (SelectMenuPatch.GetLancerPlayers(index)) status = PlayerSize.Lancer;
            self.pupButton = new SymbolButtonToggleLancerButton(menu, self, "toggle_pup_" + index.ToString(), new Vector2(self.classButton.size.x + 10f, -35.5f), new Vector2(45f, 45f), "pup_on", self.GetPupButtonOffName(), status, null, null);
            self.subObjects.Add(self.pupButton);
            menu.elementDescription.Remove($"toggle_pup_{index}_off");
            menu.elementDescription.Add($"toggle_pup_{index}_off", menu.Translate("Player <p_n> will be lancer").Replace("<p_n>", (index + 1).ToString()));
            menu.elementDescription.Add($"toggle_pup_{index}_lancer", menu.Translate("description_pup_on").Replace("<p_n>", (index + 1).ToString()));
            self.dirty = true;
        }

        private static void LancerButtonSignal(On.JollyCoop.JollyMenu.JollySlidingMenu.orig_Singal orig, JollySlidingMenu self,
            MenuObject sender, string message)
        {
            if (sender is SymbolButtonToggleLancerButton lancerBtn)
            {
                int num = lancerBtn.playerNum;
                switch (lancerBtn.status)
                {
                    default:
                    case PlayerSize.Normal: // off > on
                        self.JollyOptions(num).isPup = true;
                        SetLancerPlayers(num, false);
                        break;

                    case PlayerSize.Pup: // on > lancer
                        self.JollyOptions(num).isPup = true;
                        SetLancerPlayers(num, true);
                        break;

                    case PlayerSize.Lancer: // lancer > off
                        self.JollyOptions(num).isPup = false;
                        SetLancerPlayers(num, false);
                        break;
                }
                UpdateIsPlayerLancer(false);
                self.playerSelector[num].dirty = true;
                return;
            }
            orig(self, sender, message);
        }

        private static string JollyLancerPortraitName(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_JollyPortraitName orig, JollyPlayerSelector self, SlugcatStats.Name classID, int colorIndexFile)
        {
            if (GetLancerPlayers(self.index))
            {
                int lancerIndexFile = self.portraitTint ? colorIndexFile : 4;
                var res = orig(self, classID, lancerIndexFile);
                var lancer = res + "-lancer";
                string path = "Illustrations" + Path.DirectorySeparatorChar.ToString() + lancer + ".png";
                if (File.Exists(AssetManager.ResolveFilePath(path))) return lancer;
            }
            return orig(self, classID, colorIndexFile);
        }

        private static bool LancerHasNoUniqueSprite(On.JollyCoop.JollyMenu.SymbolButtonTogglePupButton.orig_HasUniqueSprite orig, SymbolButtonTogglePupButton self)
        {
            if (self is SymbolButtonToggleLancerButton lancerBtn && lancerBtn.status == PlayerSize.Lancer) return false;
            return orig(self);
        }

        #endregion Patch

        public SymbolButtonToggleLancerButton(Menu.Menu menu, MenuObject owner, string signal, Vector2 pos, Vector2 size, string symbolNameOn, string symbolNameOff, PlayerSize status, string stringLabelOn = null, string stringLabelOff = null)
            : base(menu, owner, signal, pos, size, symbolNameOn, symbolNameOff, status != PlayerSize.Normal, stringLabelOn, stringLabelOff)
        {
            this.status = status;
            playerNum = signal[signal.Length - 1] - '0';
            playerNum = Custom.IntClamp(playerNum, 0, 3);
            signalText = GetSignalText();
            origSymbolPos = size * 0.5f;
        }

        public enum PlayerSize
        {
            Normal,
            Pup,
            Lancer
        };

        /// <summary>
        /// 0off 1on 2lancer
        /// </summary>
        public PlayerSize status;

        private const string BASE_SIGNAL = "toggle_pup_";
        private readonly int playerNum;
        private readonly Vector2 origSymbolPos;

        public string GetSignalText()
        {
            var sb = new StringBuilder();
            sb.Append(BASE_SIGNAL);
            sb.Append(playerNum);
            switch (status)
            {
                default:
                case PlayerSize.Normal: sb.Append("_on"); break;
                case PlayerSize.Pup: sb.Append("_lancer"); break;
                case PlayerSize.Lancer: sb.Append("_off"); break;
            }
            return sb.ToString();
        }

        private const string SYMBOL_LANCER_ON = "lancer_on";

        public override void LoadIcon()
        {
            base.LoadIcon();

            if (status != PlayerSize.Lancer)
            {
                symbol.pos = origSymbolPos;
                symbol.lastPos = symbol.pos;
                return;
            }

            symbol.fileName = SYMBOL_LANCER_ON;
            symbol.LoadFile();
            symbol.sprite.SetElementByName(SYMBOL_LANCER_ON);
            symbol.pos = origSymbolPos + new Vector2(-0.5f, 4f);
            symbol.lastPos = symbol.pos;

            /*
            if (Custom.rainWorld.options.jollyColorMode == Options.JollyColorMode.DEFAULT)
            {
                var color = PlayerGraphics.DefaultSlugcatColor(LancerEnums.GetLancer((owner as JollyPlayerSelector).JollyOptions(playerNum).playerClass));
                color = JollyCustom.ColorClamp(color, 0.01f, 360f, -1f, 360f, 0.25f, 360f);
                (owner as JollyPlayerSelector).bodyTintColor = color;
            }
            */

            faceSymbol.fileName = "face_" + symbolNameOn;
            faceSymbol.LoadFile();
            faceSymbol.sprite.SetElementByName(faceSymbol.fileName);
        }

        public override void Toggle()
        {
            switch (status)
            {
                default:
                case PlayerSize.Normal: // off > on
                    {
                        isToggled = true;
                        status = PlayerSize.Pup;
                        symbol.fileName = symbolNameOff;
                    }
                    break;

                case PlayerSize.Pup: // on > lancer
                    {
                        isToggled = false;
                        status = PlayerSize.Lancer;
                        symbol.fileName = symbolNameOn;
                    }
                    break;

                case PlayerSize.Lancer: // lancer > off
                    {
                        isToggled = false;
                        status = PlayerSize.Normal;
                        symbol.fileName = symbolNameOn;
                    }
                    break;
            }
            signalText = GetSignalText();
            faceSymbol.fileName = "face_" + symbol.fileName;
            LoadIcon();
        }

        public override void Update()
        {
            base.Update();
            //if (status == PlayerSize.Lancer && symbol.fileName == symbolNameOn) Toggle();
        }
    }
}