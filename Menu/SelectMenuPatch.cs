using Menu;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Menu.SlugcatSelectMenu;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Menu
{
    internal static class SelectMenuPatch
    {
        internal static void MiniPatch()
        {
            On.Menu.SlugcatSelectMenu.ctor += SlugcatSelectMenuCtorPatch;
        }

        private static void SlugcatSelectMenuCtorPatch(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
        {
            orig(self, manager);
            foreach (var lancer in AllLancer)
            {
                self.saveGameData[lancer] = MineForSaveData(manager, lancer);

            }
        }


    }

    internal class LancerPageNewGame : SlugcatPageNewGame
    {
        public LancerPageNewGame(global::Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
        {
            basisNumber = slugcatNumber;
            slugcatNumber = lancerNumber;
        }

        internal SlugName basisNumber;

    }

    internal class LancerPageContinue : SlugcatPageContinue
    {
        public LancerPageContinue(global::Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
        {
            basisNumber = slugcatNumber;
            slugcatNumber = lancerNumber;
        }

        internal SlugName basisNumber;

        public new SaveGameData saveGameData
        {
            get
            {
                return (menu as SlugcatSelectMenu).saveGameData[slugcatNumber];
            }
        }

    }
}
