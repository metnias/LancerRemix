using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LancerRemix.LancerMenu
{
    internal static class MenuModifier
    {
        internal static void SubPatch()
        {
            SelectMenuPatch.MiniPatch();
            /// TODO: add Lancer toggle button in slugcat select menu
            /// clicking that swooshes illust upwards.
            /// 
            /// or just save which players should be lancer
            /// and attach supplements to them separately
            /// lancer supplements will pass null for orig to prevent double updates
        }
    }
}