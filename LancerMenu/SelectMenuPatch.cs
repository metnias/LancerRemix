using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using UnityEngine;
using static LancerRemix.LancerEnums;
using static Menu.SlugcatSelectMenu;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.LancerMenu
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
            lancerButton = new SymbolButtonToggle(self, self.pages[0], LANCER_SIGNAL, new Vector2(1016f, 50f), new Vector2(50f, 50f),
                "ps4_circle_button", "ps4_cross_button", slugcatPageLancer, false); // TODO: add sprites in illustrations
            self.pages[0].subObjects.Add(lancerButton);
            self.MutualHorizontalButtonBind(lancerButton, self.nextButton);

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
            lancerTransition = Custom.LerpAndTick(lancerTransition, slugcatPageLancer ? 1f : 0f, 0.07f, 0.03f);
        }

        private static void SignalPatch(On.Menu.SlugcatSelectMenu.orig_Singal orig, SlugcatSelectMenu self, MenuObject sender, string message)
        {
            if (message.StartsWith(LANCER_SIGNAL))
            {
                slugcatPageLancer = message.EndsWith("_on");
                //LancerPlugin.LogSource.LogMessage($"slugcatPageLancer: {slugcatPageLancer}");
                self.PlaySound(SoundID.MENU_Next_Slugcat);
                self.UpdateStartButtonText();
                return;
            }
            orig(self, sender, message);
        }

        private static void PageGrafUpdatePatch(On.Menu.SlugcatSelectMenu.SlugcatPage.orig_GrafUpdate orig, SlugcatPage self, float timeStacker)
        {
            orig(self, timeStacker);
            float offset = Mathf.Lerp(lastLancerTransition, lancerTransition, timeStacker) * VOFFSET;
            if (IsLancerPage(self)) offset -= VOFFSET;
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
                illust.sprite.y += offset;
            foreach (var illust in self.slugcatImage.flatIllustrations)
                illust.sprite.y += offset;
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

        private static void LancerPortrait(SlugcatPage page)
        {
            foreach (var illust in page.slugcatImage.depthIllustrations)
                illust.sprite.MoveBehindOtherNode((page.menu as SlugcatSelectMenu).pages[0].Container);
            foreach (var illust in page.slugcatImage.flatIllustrations)
                illust.sprite.MoveBehindOtherNode((page.menu as SlugcatSelectMenu).pages[0].Container);

            var basis = GetBasis(page.slugcatNumber);
            if (basis == SlugName.White)
            {
                ReplaceIllust("slugcat - lancer", "lancer - white - flat", "White Slugcat - 2", "white lancer - 2");
            }

            void ReplaceIllust(string sceneFolder, string flatImage, string layerImageOrig, string layerImage)
            {
                if (page.slugcatImage.flatMode)
                {
                    page.slugcatImage.flatIllustrations[0].RemoveSprites();
                    page.slugcatImage.flatIllustrations.Clear();
                    page.slugcatImage.AddIllustration(new MenuIllustration(page.menu, page.slugcatImage, sceneFolder, flatImage, new Vector2(683f, 384f), false, true));
                }
                else
                {
                    int i = 0;
                    for (; i < page.slugcatImage.depthIllustrations.Count; ++i)
                        if (string.Compare(page.slugcatImage.depthIllustrations[i].fileName, layerImageOrig, true) == 0) break;
                    //Vector2 pos = page.slugcatImage.depthIllustrations[i].pos;
                    page.slugcatImage.depthIllustrations[i].RemoveSprites();
                    page.slugcatImage.depthIllustrations[i] = null;
                    // LancerPlugin.LogSource.LogMessage($"({i}/{page.slugcatImage.depthIllustrations.Count}) replaced to {layerImage}");
                    page.slugcatImage.depthIllustrations[i] =
                        new MenuDepthIllustration(page.menu, page.slugcatImage, sceneFolder, layerImage, Vector2.zero, 2.7f, MenuDepthIllustration.MenuShader.Basic);
                    if (i < page.slugcatImage.depthIllustrations.Count - 1)
                        page.slugcatImage.depthIllustrations[i].sprite.MoveBehindOtherNode(page.slugcatImage.depthIllustrations[i + 1].sprite);
                    page.slugcatImage.RefreshPositions();
                }
            }
        }

        internal class LancerPageNewGame : SlugcatPageNewGame
        {
            public LancerPageNewGame(Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
            {
                basisNumber = slugcatNumber;
                slugcatNumber = lancerNumber;

                LancerPortrait(this);
                //sceneOffset.y -= VOFFSET;
            }

            internal SlugName basisNumber;
        }

        internal class LancerPageContinue : SlugcatPageContinue
        {
            public LancerPageContinue(Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
            {
                basisNumber = slugcatNumber;
                slugcatNumber = lancerNumber;

                LancerPortrait(this);
                //sceneOffset.y -= VOFFSET;
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