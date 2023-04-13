using LancerRemix.Cat;
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

        private readonly static Configurable<Color>[] hornColors
            = new Configurable<Color>[4];

        public static Color GetHornColor(int playerNumber)
            => hornColors[playerNumber].Value;


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
