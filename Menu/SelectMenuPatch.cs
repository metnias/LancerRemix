using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using System;
using System.Linq;
using UnityEngine;
using static LancerRemix.LancerEnums;
using static Menu.SlugcatSelectMenu;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Menu
{
    internal static class SelectMenuPatch
    {
        internal static void MiniPatch()
        {
            On.Menu.SlugcatSelectMenu.ctor += CtorPatch;
            On.Menu.SlugcatSelectMenu.Update += UpdatePatch;
            On.Menu.SlugcatSelectMenu.Singal += SignalPatch;
            On.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += PageGrafUpdatePatch;
        }

        private static void CtorPatch(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
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
            lancerButton = new SymbolButtonToggle(self, self.pages[0], LANCER_SIGNAL, new Vector2(1116f, 50f), new Vector2(50f, 50f),
                "Big_Menu_Arrow", "Kill_Slugcat", slugcatPageLancer, false); // TODO: replace sprites
            self.pages[0].subObjects.Add(lancerButton);

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
                    slugcatPageLancer = true; lancerTransition = 1f; lastLancerTransition = 1f;
                    self.slugcatPageIndex = order;
                }
            }
        }

        private static SlugcatPage[] lancerPages;
        private static bool slugcatPageLancer;
        private static SymbolButtonToggle lancerButton;
        private static float lancerTransition = 0f;
        private static float lastLancerTransition = 0f;

        private static bool IsLancerPage(SlugcatPage page)
            => page is LancerPageNewGame || page is LancerPageContinue;

        internal const float VOFFSET = 800f;
        private static readonly string LANCER_SIGNAL = "LANCER";

        private static void UpdatePatch(On.Menu.SlugcatSelectMenu.orig_Update orig, SlugcatSelectMenu self)
        {
            orig(self);
            lastLancerTransition = lancerTransition;
            lancerTransition = Custom.LerpAndTick(lancerTransition, slugcatPageLancer ? 1f : 0f, 0.2f, 0.1f);
        }

        private static void SignalPatch(On.Menu.SlugcatSelectMenu.orig_Singal orig, SlugcatSelectMenu self, MenuObject sender, string message)
        {
            if (message == LANCER_SIGNAL)
            {
                slugcatPageLancer = !slugcatPageLancer;
                self.PlaySound(SoundID.MENU_Next_Slugcat);
                return;
            }
            orig(self, sender, message);
        }

        private static void PageGrafUpdatePatch(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_GrafUpdate orig, SlugcatPage self, float timeStacker)
        {
            orig(self, timeStacker);
            float offset = Mathf.Lerp(lastLancerTransition, lancerTransition, timeStacker) * VOFFSET;
            //if (IsLancerPage(self)) offset -= VOFFSET;
            if (self.markSquare != null && self.markGlow != null)
            {
                self.markSquare.y += offset;
                self.markGlow.y += offset;
            }
            if (self.glowSpriteA != null && self.glowSpriteB != null)
            {
                self.glowSpriteB.y += offset;
                self.glowSpriteA.y += offset;
            }
            foreach (var illust in self.slugcatImage.depthIllustrations)
                illust.pos.y += offset;
            foreach (var illust in self.slugcatImage.flatIllustrations)
                illust.pos.y += offset;
            if (self is SlugcatPageNewGame pageNew)
            {
                float o = pageNew.infoLabel.text.Contains("\n") ? 30f : 0f;
                pageNew.difficultyLabel.label.y = (self.imagePos.y - 249f + o) + offset;
                pageNew.infoLabel.label.y = (self.imagePos.y - 249f - 60f + o / 2f) + offset;
            }
            else if (self is SlugcatPageContinue pageCont)
            {
                pageCont.regionLabel.label.y = (self.imagePos.y - 249f) + offset;
            }
        }


        internal class LancerPageNewGame : SlugcatPageNewGame
        {
            public LancerPageNewGame(global::Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
            {
                basisNumber = slugcatNumber;
                slugcatNumber = lancerNumber;

                sceneOffset.y -= VOFFSET;
            }

            internal SlugName basisNumber;

        }

        internal class LancerPageContinue : SlugcatPageContinue
        {
            public LancerPageContinue(global::Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
            {
                basisNumber = slugcatNumber;
                slugcatNumber = lancerNumber;

                sceneOffset.y -= VOFFSET;
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

}
