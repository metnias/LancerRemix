#define NO_MSC

using JollyCoop.JollyMenu;
using LancerRemix.Cat;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static LancerRemix.LancerMenu.SelectMenuPatch;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.LancerMenu
{
    internal static class HornColorPick
    {
        internal static void Initalize()
        {
            for (int i = 0; i < hornColors.Length; ++i)
            {
                Color c;
                switch (i)
                {
                    default:
                    case 0:
                        c = LancerDecoration.DefaultHornColor(SlugName.White); break;
                    case 1:
                        c = LancerDecoration.DefaultHornColor(SlugName.Yellow); break;
                    case 2:
                        c = LancerDecoration.DefaultHornColor(SlugName.Red); break;
                    case 3:
                        c = LancerDecoration.DefaultHornColor(SlugName.Night); break;
                }
                hornColors[i] = OI.config.Bind($"LancerP{i}HornColour", c);
            }
        }

        private static OptionInterface OI => LancerPlugin.OI;

        private static readonly Configurable<Color>[] hornColors
            = new Configurable<Color>[4];

        public static Color GetHornColor(int playerNumber)
            => hornColors[playerNumber].Value;

        #region Wrappers

        private static MenuTabWrapper tabWrapper;
        private static OpColorPicker cpk;

        private static void InitializeWrapper(MenuObject owner, Vector2 pos, int player, bool hasBox)
        {
            if (tabWrapper != null) DestroyWrappers();
            tabWrapper = new MenuTabWrapper(owner.menu, owner);
            owner.subObjects.Add(tabWrapper);
            Vector2 size = new Vector2(280f, 180f);
            if (hasBox)
            {
                var rect = new OpRect(pos, size) { colorEdge = MenuColorEffect.rgbWhite };
                new UIelementWrapper(tabWrapper, rect);
            }
            var label = new OpLabel(pos + new Vector2(15f, 75f), new Vector2(90f, 40f), Translate("Horn"), bigText: true) { color = MenuColorEffect.rgbWhite };
            new UIelementWrapper(tabWrapper, label);
            var cView = new OpImage(pos + new Vector2(42f, 127f), "square");
            new UIelementWrapper(tabWrapper, cView);
            var cViewRect = new OpRect(pos + new Vector2(40f, 125f), new Vector2(40f, 40f), 0f) { colorEdge = MenuColorEffect.rgbWhite };
            new UIelementWrapper(tabWrapper, cViewRect);
            cpk = new OpColorPicker(hornColors[player], pos + new Vector2(115f, 15f));
            new UIelementWrapper(tabWrapper, cpk);
            cpk.OnValueUpdate += (config, value, oldValue) => { cView.color = cpk.valueColor; };
            cpk.wrapper.ReloadConfig();
            cView.color = cpk.valueColor;

            string Translate(string text)
                => Custom.rainWorld.inGameTranslator.Translate(text);
        }

        private static void SaveColor() => cpk?.wrapper.SaveConfig();

        internal static void ResetColor(SlugName playerClass)
        {
            if (cpk == null) return;
            var basis = LancerEnums.GetBasis(playerClass);
            cpk.valueColor = LancerDecoration.DefaultHornColor(basis);
        }

        private static void DestroyWrappers()
        {
            if (cpk != null)
            {
                cpk?.Unload();
                cpk = null;
            }
            tabWrapper?.RemoveSprites();
            tabWrapper = null;
        }

        #endregion Wrappers

        #region Jolly

        internal static void OnJollyEnableSubPatch()
        {
            On.JollyCoop.JollyMenu.ColorChangeDialog.ctor += JollyHornChangeDialogCtor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.SaveColorChange += JollySaveHornColorChange;
            On.JollyCoop.JollyMenu.ColorChangeDialog.Singal += JollyResetHornColor;
        }

        internal static void OnJollyDisableSubPatch()
        {
            On.JollyCoop.JollyMenu.ColorChangeDialog.ctor -= JollyHornChangeDialogCtor;
            On.JollyCoop.JollyMenu.ColorChangeDialog.SaveColorChange -= JollySaveHornColorChange;
            On.JollyCoop.JollyMenu.ColorChangeDialog.Singal -= JollyResetHornColor;
        }

        private static void JollyHornChangeDialogCtor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_ctor orig, ColorChangeDialog self,
            JollySetupDialog jollyDialog, SlugName playerClass, int playerNumber, ProcessManager manager, List<string> names)
        {
            orig(self, jollyDialog, playerClass, playerNumber, manager, names);
            if (!GetLancerPlayers(playerNumber)) return;
            InitializeWrapper(self.pages[0], self.body.litSlider.pos + new Vector2(0f, -80f), playerNumber, true);
            self.MutualVerticalButtonBind(cpk.wrapper, self.body.litSlider);
        }

        private static void JollySaveHornColorChange(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_SaveColorChange orig)
        {
            orig();
            SaveColor();
            DestroyWrappers();
        }

        private static void JollyResetHornColor(On.JollyCoop.JollyMenu.ColorChangeDialog.orig_Singal orig, ColorChangeDialog self, MenuObject sender, string message)
        {
            if (message.StartsWith("RESET_COLOR_DIALOG_")
                && GetLancerPlayers(self.playerNumber))
            {
                self.PlaySound(SoundID.MENU_Remove_Level);
                self.JollyOptions.SetColorsToDefault(LancerEnums.GetLancer(self.playerClass));
                self.body.color = self.JollyOptions.GetBodyColor();
                self.body.RGB2HSL();
                self.face.color = self.JollyOptions.GetFaceColor();
                self.face.RGB2HSL();
                if (self.unique != null)
                {
                    self.unique.color = self.JollyOptions.GetUniqueColor();
                    self.unique.RGB2HSL();
                }
                ResetColor(self.playerClass);
                return;
            }
            orig(self, sender, message);
        }

        #endregion Jolly

        #region MMF

        internal static void OnMMFEnablePatch()
        {
            On.Menu.SlugcatSelectMenu.CustomColorInterface.ctor += LancerCustomColorInterfaceCtor;
            On.Menu.SlugcatSelectMenu.CustomColorInterface.RemoveSprites += LancerCustomColorInterfaceRemove;
        }

        internal static void OnMMFDisablePatch()
        {
            On.Menu.SlugcatSelectMenu.CustomColorInterface.ctor -= LancerCustomColorInterfaceCtor;
            On.Menu.SlugcatSelectMenu.CustomColorInterface.RemoveSprites -= LancerCustomColorInterfaceRemove;
        }

        private static void LancerCustomColorInterfaceCtor(On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_ctor orig, SlugcatSelectMenu.CustomColorInterface self,
            Menu.Menu menu, MenuObject owner, Vector2 pos, SlugName slugcatID, List<string> names, List<string> defaultColors)
        {
            if (SlugcatPageLancer)
            {
                slugcatID = LancerEnums.GetLancer(slugcatID);
                defaultColors = PlayerGraphics.DefaultBodyPartColorHex(slugcatID);
                for (int i = 0; i < defaultColors.Count; i++)
                {
                    Vector3 hsl = Custom.RGB2HSL(Custom.hexToColor(defaultColors[i]));
                    defaultColors[i] = $"{hsl[0]},{hsl[1]},{hsl[2]}";
                }
            }
            orig(self, menu, owner, pos, slugcatID, names, defaultColors);
            if (!SlugcatPageLancer) return;
            var offset = pos;
            offset.y -= (self.bodyColors.Length + 3) * 40f;
            InitializeWrapper(self, offset + new Vector2(-80f, -180f), 0, false);
        }

        private static void LancerCustomColorInterfaceRemove(On.Menu.SlugcatSelectMenu.CustomColorInterface.orig_RemoveSprites orig, SlugcatSelectMenu.CustomColorInterface self)
        {
            SaveColor();
            DestroyWrappers();
            orig(self);
        }

        #endregion MMF
    }
}