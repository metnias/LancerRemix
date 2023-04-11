using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static LancerRemix.LancerMenu.SelectMenuPatch;

namespace LancerRemix.LancerMenu
{
    internal static class MultiplayerPatch
    {
        internal static void SubPatch()
        {
            On.Menu.MultiplayerMenu.Singal += ArenaMenuSingal;
        }

        #region Jolly

        internal static void OnJollyEnablePatch()
        {
            SymbolButtonToggleLancerButton.SubPatch();
        }

        internal static void OnJollyDisablePatch()
        {
            SymbolButtonToggleLancerButton.SubUnpatch();
        }

        #endregion Jolly

        #region Arena

        private static void ArenaMenuSingal(On.Menu.MultiplayerMenu.orig_Singal orig, MultiplayerMenu self, MenuObject sender, string message)
        {
            if (string.IsNullOrEmpty(message)) goto Skip;
            if (message == "PLAY!" || message == "RESUME")
            {
                UpdateIsPlayerLancer(false);
                SaveLancerPlayers(self.manager.rainWorld.progression.miscProgressionData);
            }
        Skip: orig(self, sender, message);
        }

        #endregion Arena
    }
}