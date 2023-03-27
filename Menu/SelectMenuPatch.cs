using Menu;
using static LancerRemix.LancerEnums;
using static Menu.SlugcatSelectMenu;
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
            slugcatPageLancer = false;
            lancerPages = new SlugcatPage[self.slugcatPages.Count];
            foreach (var lancer in AllLancer)
            {
                self.saveGameData[lancer] = MineForSaveData(manager, lancer);
                int order = GetBasisOrder(lancer);
                if (order < 0) continue;
                if (self.saveGameData[lancer] != null)
                {
                    lancerPages[order] = new LancerPageContinue(self, null, 1 + order, lancer);
                }
                else
                {
                    lancerPages[order] = new LancerPageNewGame(self, null, 1 + order, lancer);
                }
                self.pages.Add(lancerPages[order]);

                UpdateCurrentlySelectedLancer(lancer, order);
            }
            // Add Toggle Button


            int GetBasisOrder(SlugName lancer)
            {
                var basis = GetBasis(lancer);
                for (int i = 0; i < self.slugcatColorOrder.Count; ++i)
                    if (self.slugcatColorOrder[i] == basis) return i;
                return -1;
            }

            void UpdateCurrentlySelectedLancer(SlugName lancer, int order)
            {
                if (self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat == lancer)
                {
                    slugcatPageLancer = true;
                    self.slugcatPageIndex = order;
                }
            }
        }

        private static SlugcatPage[] lancerPages;
        private static bool slugcatPageLancer;

        private static bool IsLancerPage(SlugcatPage page)
            => page is LancerPageNewGame || page is LancerPageContinue;

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
