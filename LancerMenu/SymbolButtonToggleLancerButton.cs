#define NO_MSC

using JollyCoop.JollyMenu;
using Menu;
using System.Globalization;
using System.Text;
using UnityEngine;

namespace LancerRemix.LancerMenu
{
    internal class SymbolButtonToggleLancerButton : SymbolButtonTogglePupButton
    {
        #region Patch

        internal static void SubPatch()
        {
            On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor += JollyLancerSelector;
            On.JollyCoop.JollyMenu.JollySlidingMenu.Singal += LancerButtonSignal;
        }

        internal static void SubUnpatch()
        {
            On.JollyCoop.JollyMenu.JollyPlayerSelector.ctor -= JollyLancerSelector;
            On.JollyCoop.JollyMenu.JollySlidingMenu.Singal -= LancerButtonSignal;
        }

        private static void JollyLancerSelector(On.JollyCoop.JollyMenu.JollyPlayerSelector.orig_ctor orig, JollyPlayerSelector self,
            JollySetupDialog menu, MenuObject owner, Vector2 pos, int index)
        {
            orig(self, menu, owner, pos, index);
            self.pupButton.RemoveSprites();
            self.subObjects.Remove(self.pupButton);
            self.pupButton = null;

            var status = self.JollyOptions(index).isPup ? PlayerSize.Pup : PlayerSize.Normal;
            if (SelectMenuPatch.GetLancerPlayers(index)) status = PlayerSize.Lancer;
            self.pupButton = new SymbolButtonToggleLancerButton(menu, self, "toggle_pup_" + index.ToString(), new Vector2(self.classButton.size.x + 10f, -35.5f), new Vector2(45f, 45f), "pup_on", self.GetPupButtonOffName(), status, null, null);
            self.subObjects.Add(self.pupButton);
            menu.elementDescription.Add($"toggle_lancer_{index}_on", menu.Translate("description_lancer_on").Replace("<p_n>", (index + 1).ToString()));
            self.dirty = true;
        }

        private static void LancerButtonSignal(On.JollyCoop.JollyMenu.JollySlidingMenu.orig_Singal orig, JollySlidingMenu self,
            MenuObject sender, string message)
        {
            if (message.Contains("toggle_pup"))
            {
                // off > on > lancer
                if (message.Contains("off")) goto NoLancerButton;
                bool isLancer;
                if (message.Contains("on"))
                {
                    isLancer = false;
                    message = message.Replace("_on", "");
                }
                else
                {
                    isLancer = true;
                    message = message.Replace("_lancer", "");
                }
                if (int.TryParse(char.ToString(message[message.Length - 1]), NumberStyles.Any, CultureInfo.InvariantCulture, out int num)
                    && num < self.playerSelector.Length)
                {
                    self.JollyOptions(num).isPup = !isLancer;
                    SelectMenuPatch.SetLancerPlayers(num, isLancer);
                }
                return;
            }
        NoLancerButton: orig(self, sender, message);
        }

        #endregion Patch

        public SymbolButtonToggleLancerButton(Menu.Menu menu, MenuObject owner, string signal, Vector2 pos, Vector2 size, string symbolNameOn, string symbolNameOff, PlayerSize status, string stringLabelOn = null, string stringLabelOff = null) : base(menu, owner, signal, pos, size, symbolNameOn, symbolNameOff, status != PlayerSize.Normal, stringLabelOn, stringLabelOff)
        {
            this.status = status;
            if (!int.TryParse(signal.Substring(signal.Length - 2), out playerNum)) playerNum = 0;
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
        public string GetSignalText()
        {
            var sb = new StringBuilder();
            sb.Append(BASE_SIGNAL);
            sb.Append(playerNum);
            switch (status)
            {
                case PlayerSize.Normal: sb.Append("_off"); break;
                case PlayerSize.Pup: sb.Append("_on"); break;
                case PlayerSize.Lancer: sb.Append("_lancer"); break;
            }
            return sb.ToString();
        }

        public string symbolLancerOn = "";

        public override void LoadIcon()
        {
            base.LoadIcon();
        }

        public void ToPup()
        {
            isToggled = false;
            if (belowLabel != null) belowLabel.label.text = labelNameOff;
            status = PlayerSize.Pup;
            signalText = GetSignalText();
        }

        public override void Toggle()
        {
            switch (status)
            {
                case PlayerSize.Normal: // off > on
                    {
                        ToPup();
                    }
                    break;

                case PlayerSize.Pup: // on > lancer
                    {
                        isToggled = false;
                        //if (belowLabel != null) belowLabel.label.text = labelNameOn;
                    }
                    status = PlayerSize.Lancer;
                    break;

                case PlayerSize.Lancer: // lancer > off
                    {
                        isToggled = true;
                        if (belowLabel != null) belowLabel.label.text = labelNameOff;
                    }
                    status = PlayerSize.Normal;
                    break;
            }
            signalText = GetSignalText();

            faceSymbol.fileName = "face_" + symbol.fileName;
            LoadIcon();
        }

        public override void Update()
        {
            base.Update();
        }
    }
}