using JollyCoop.JollyMenu;
using LancerRemix.Cat;
using Menu;
using Menu.Remix;
using Menu.Remix.MixedUI;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
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

        private static OptionInterface OI => LancerPlugin.oi;

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
            Vector2 size = new Vector2(270f, 240f);
            if (hasBox)
            {
                var rect = new OpRect(pos, size) { colorEdge = MenuColorEffect.rgbWhite };
                new UIelementWrapper(tabWrapper, rect);
            }
            var label = new OpLabel(pos + new Vector2(100f, 180f), new Vector2(140f, 40f), Translate("Horn"), bigText: true) { color = MenuColorEffect.rgbWhite };
            new UIelementWrapper(tabWrapper, label);
            var cView = new OpImage(pos + new Vector2(32f, 182f), "square");
            new UIelementWrapper(tabWrapper, cView);
            var cViewRect = new OpRect(pos + new Vector2(30f, 180f), new Vector2(40f, 40f), 0f) { colorEdge = MenuColorEffect.rgbWhite };
            new UIelementWrapper(tabWrapper, cViewRect);
            cpk = new OpColorPicker(hornColors[player], pos + new Vector2(60f, 20f));
            new UIelementWrapper(tabWrapper, cpk);
            cpk.OnValueUpdate += (config, value, oldValue) => { cView.color = cpk.valueColor; };
            cpk.wrapper.ReloadConfig();
            cView.color = cpk.valueColor;

            string Translate(string text)
                => Custom.rainWorld.inGameTranslator.Translate(text);
        }

        private static void SaveColor() => cpk?.wrapper.SaveConfig();

        private static void ResetColor() => cpk?.wrapper.ResetConfig();

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
            InitializeWrapper(self.pages[0], self.body.litSlider.pos + new Vector2(0f, -120f), playerNumber, true);
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
            orig(self, sender, message);
            if (message.StartsWith("RESET_COLOR_DIALOG_")) ResetColor();
        }

        #endregion Jolly

        #region MMF

        internal static void OnMMFEnablePatch()
        {
        }

        internal static void OnMMFDisablePatch()
        {
        }

        #endregion MMF
    }
}