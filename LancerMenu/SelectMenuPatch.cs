using JollyCoop.JollyMenu;
using Menu;
using RWCustom;
using System.IO;
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
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += UpdateLancerStartButtonText;
            On.Menu.SlugcatSelectMenu.Singal += SignalPatch;
            On.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += PageGrafUpdatePatch;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += LancerPageContinue.PageContinueUpdatePatch;
        }

        private static void CtorPatch(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
        {
            orig(self, manager);
            slugcatPageLancer = true; lancerTransition = 1f; lastLancerTransition = 1f; //= false;
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
                "lancer_on", "lancer_off", slugcatPageLancer, false);
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

        private static void UpdateLancerStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            self.startButton.GetButtonBehavior.greyedOut = false;
            if (!slugcatPageLancer) { orig(self); return; }
            if (!HasLancer(self.slugcatColorOrder[self.slugcatPageIndex])) { self.startButton.GetButtonBehavior.greyedOut = true; return; }
            self.startButton.fillTime = (self.restartChecked ? 120f : 40f);
            var lancer = GetLancer(self.slugcatColorOrder[self.slugcatPageIndex]);
            if (self.saveGameData[lancer] == null)
            {
                self.startButton.menuLabel.text = self.Translate("NEW GAME");
                return;
            }
            if (self.restartChecked)
            {
                self.startButton.menuLabel.text = self.Translate("DELETE SAVE").Replace(" ", "\r\n");
                return;
            }
            if (GetBasis(lancer) == SlugName.Red && self.saveGameData[lancer].redsDeath)
            {
                self.startButton.menuLabel.text = self.Translate("STATISTICS");
                return;
            }
            /*
            if (ModManager.MSC && this.slugcatPages[this.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Artificer && this.artificerIsDead)
            {
                this.startButton.menuLabel.text = base.Translate("STATISTICS");
                return;
            }
            if (ModManager.MSC && this.slugcatPages[this.slugcatPageIndex].slugcatNumber == MoreSlugcatsEnums.SlugcatStatsName.Saint && this.saintIsDead)
            {
                this.startButton.menuLabel.text = base.Translate("STATISTICS");
                return;
            } */
            self.startButton.menuLabel.text = self.Translate("CONTINUE");
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
            float offset = Mathf.Lerp(lastLancerTransition, lancerTransition, timeStacker) * VOFFSET + 0.01f;
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
                ReplaceIllust($"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - white - flat", "white slugcat - 2", "white lancer - 2", new Vector2(503f, 178f));
            }
            else if (basis == SlugName.Yellow)
            {
                ReplaceIllust($"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - yellow - flat", "yellow slugcat - 1", "yellow lancer - 1", new Vector2(528f, 211f));
            }
            else if (basis == SlugName.Red)
            {
                if (page.menu.manager.rainWorld.progression.miscProgressionData.redUnlocked)
                    ReplaceIllust($"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                        "lancer - red - flat", "red slugcat - 1", "red lancer - 1", new Vector2(462f, 225f));
                else
                    ReplaceIllust($"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - red dark - flat", "red slugcat - 1 - dark", "red lancer - 1 - dark", new Vector2(462f, 225f));
            }

            void ReplaceIllust(string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos)
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
                        new MenuDepthIllustration(page.menu, page.slugcatImage, sceneFolder, layerImage, layerPos, 2.7f, MenuDepthIllustration.MenuShader.Basic);
                    if (i < page.slugcatImage.depthIllustrations.Count - 1)
                        page.slugcatImage.depthIllustrations[i].sprite.MoveBehindOtherNode(page.slugcatImage.depthIllustrations[i + 1].sprite);
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
                VanillaLancerText();
                //sceneOffset.y -= VOFFSET;
            }

            internal SlugName basisNumber;

            private void VanillaLancerText()
            {
            }
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

            internal static void PageContinueUpdatePatch(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_Update orig, SlugcatPageContinue self)
            {
                orig(self);
                float offset = lancerTransition * VOFFSET;
                if (IsLancerPage(self)) offset -= VOFFSET;
                self.hud.karmaMeter.pos.y += offset;
                self.hud.foodMeter.pos.y += offset;
            }
        }
    }
}