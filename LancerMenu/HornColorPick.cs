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

        private static MenuTabWrapper tabWrapper;
        private static UIelementWrapper cpkWrapper;
        private static OpColorPicker cpk;

        private static void InitializeWrapper(MenuObject owner, Vector2 pos, int player)
        {
            if (tabWrapper != null) DestroyWrappers();
            tabWrapper = new MenuTabWrapper(owner.menu, owner);
            Vector2 size = new Vector2(270f, 240f);
            var rect = new OpRect(pos, size);
            new UIelementWrapper(tabWrapper, rect);
            var label = new OpLabel(pos + new Vector2(100f, 180f), new Vector2(140f, 40f), Translate("Horns"), bigText: true);
            new UIelementWrapper(tabWrapper, label);
            var cView = new OpImage(pos + new Vector2(30f, 180f), "square");
            new UIelementWrapper(tabWrapper, cView);
            var cViewRect = new OpRect(pos + new Vector2(30f, 180f), new Vector2(40f, 40f), 0f);
            new UIelementWrapper(tabWrapper, cViewRect);
            cpk = new OpColorPicker(hornColors[player], pos + new Vector2(60f, 20f));
            new UIelementWrapper(tabWrapper, cpk);
            cpk.OnValueUpdate += (config, value, oldValue) => { cView.color = cpk.valueColor; };
            cpk.wrapper.ReloadConfig();

            string Translate(string text)
                => Custom.rainWorld.inGameTranslator.Translate(text);
        }

        private static void SaveColor() => cpk.wrapper.SaveConfig();

        private static void ResetColor() => cpk.wrapper.ResetConfig();

        private static void DestroyWrappers()
        {
            if (cpkWrapper != null)
            {
                cpk.Unload();
                cpk = null;
                cpkWrapper.RemoveSprites();
                cpkWrapper = null;
            }
            tabWrapper.RemoveSprites();
            tabWrapper = null;
        }

        #region Jolly

        internal static void OnJollyEnableSubPatch()
        {
        }

        internal static void OnJollyDisableSubPatch()
        {
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