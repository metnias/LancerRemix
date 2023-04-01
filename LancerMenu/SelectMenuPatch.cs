using JollyCoop.JollyMenu;
using LancerRemix.Cat;
using Menu;
using MonoMod.RuntimeDetour;
using RWCustom;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using static LancerRemix.LancerEnums;
using static Menu.SlugcatSelectMenu;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.LancerMenu
{
    internal static class SelectMenuPatch
    {
        internal static void SubPatch()
        {
            On.Menu.SlugcatSelectMenu.ctor += CtorPatch;
            On.Menu.SlugcatSelectMenu.colorFromIndex += LancerFromIndex;
            On.Menu.SlugcatSelectMenu.indexFromColor += IndexFromLancer;
            On.Menu.SlugcatSelectMenu.SetChecked += SetLancerChecked;
            On.Menu.SlugcatSelectMenu.Update += UpdatePatch;
            On.Menu.SlugcatSelectMenu.UpdateStartButtonText += UpdateLancerStartButtonText;
            On.Menu.SlugcatSelectMenu.UpdateSelectedSlugcatInMiscProg += UpdateSelectedLancerInMiscProg;
            On.Menu.SlugcatSelectMenu.Singal += SignalPatch;
            On.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += PageGrafUpdatePatch;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += LancerPageContinue.PageContinueUpdatePatch;

            LancerPageContinue.SubPatch();
        }

        private static void CtorPatch(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
        {
            _lancerInit = false;
            ModifyCat.SetIsLancer(false, new bool[4]);
            orig(self, manager);
            slugcatPageLancer = false; lancerTransition = 0f; lastLancerTransition = 0f;
            lancerPages = new SlugcatPage[self.slugcatPages.Count];
            foreach (var lancer in AllLancer)
            {
                int order = GetBasisOrder(lancer);
                if (order < 0) continue;
                self.saveGameData[lancer] = MineForSaveData(manager, lancer);
                if (self.saveGameData[lancer] != null)
                    lancerPages[order] = new LancerPageContinue(self, null, 1 + order, lancer);
                else
                    lancerPages[order] = new LancerPageNewGame(self, null, 1 + order, lancer);
                self.pages.Add(lancerPages[order]);

                UpdateCurrentlySelectedLancer(lancer, order);
            }
            // Add Toggle Button
            lancerButton = new SymbolButtonToggle(self, self.pages[0], LANCER_SIGNAL, new Vector2(1016f, 50f), new Vector2(50f, 50f),
                "lancer_on", "lancer_off", slugcatPageLancer, false);
            self.pages[0].subObjects.Add(lancerButton);
            self.MutualHorizontalButtonBind(lancerButton, self.nextButton);
            _lancerInit = true;
            self.UpdateStartButtonText();
            self.UpdateSelectedSlugcatInMiscProg();

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
        private static bool _lancerInit = false;
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
            self.startButton.GetButtonBehavior.greyedOut |= lancerTransition > 0.1f && lancerTransition < 0.9f;
        }

        private static SlugName LancerFromIndex(On.Menu.SlugcatSelectMenu.orig_colorFromIndex orig, SlugcatSelectMenu self, int index)
        {
            var res = orig(self, index);
            if (slugcatPageLancer && HasLancer(res)) res = GetLancer(res);
            return res;
        }

        private static int IndexFromLancer(On.Menu.SlugcatSelectMenu.orig_indexFromColor orig, SlugcatSelectMenu self, SlugName color)
        {
            if (IsLancer(color)) color = GetBasis(color);
            return orig(self, color);
        }

        private static void SetLancerChecked(On.Menu.SlugcatSelectMenu.orig_SetChecked orig, SlugcatSelectMenu self, CheckBox box, bool c)
        {
            if (slugcatPageLancer)
            {
                var basis = self.slugcatColorOrder[self.slugcatPageIndex];
                if (!HasLancer(basis)) goto NoLancer;
                var lancer = GetLancer(basis);
                if (!(box.IDString == "COLORS"))
                {
                    self.restartChecked = c;
                    self.UpdateStartButtonText();
                    return;
                }
                self.colorChecked = c;
                if (self.colorChecked && !self.CheckJollyCoopAvailable(self.colorFromIndex(self.slugcatPageIndex)))
                {
                    self.AddColorButtons();
                    self.manager.rainWorld.progression.miscProgressionData.colorsEnabled[lancer.value] = true;
                    return;
                }
                self.RemoveColorButtons();
                self.manager.rainWorld.progression.miscProgressionData.colorsEnabled[lancer.value] = false;
                return;
            }
        NoLancer: orig(self, box, c);
        }

        private static void UpdateLancerStartButtonText(On.Menu.SlugcatSelectMenu.orig_UpdateStartButtonText orig, SlugcatSelectMenu self)
        {
            if (!slugcatPageLancer) { orig(self); return; }
            if (!_lancerInit) return; // to prevent breaking
            var lancer = self.slugcatColorOrder[self.slugcatPageIndex];
            if (!HasLancer(lancer)) return;
            lancer = GetLancer(lancer);
            self.startButton.fillTime = (self.restartChecked ? 120f : 40f);
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

        private static void UpdateSelectedLancerInMiscProg(On.Menu.SlugcatSelectMenu.orig_UpdateSelectedSlugcatInMiscProg orig, SlugcatSelectMenu self)
        {
            if (!_lancerInit) return;
            orig(self);
            if (!slugcatPageLancer) return;
            self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                lancerPages[self.slugcatPageIndex]?.slugcatNumber;
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
            if (message == "START")
            {
                bool[] players = new bool[4];
                players[0] = slugcatPageLancer; // TODO: get jolly status as well
                ModifyCat.SetIsLancer(slugcatPageLancer, players);
                // StartGame(this.slugcatPages[this.slugcatPageIndex].slugcatNumber);
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
            UpdateEffectColor();

            MoveBehindGUIs();

            var basis = GetBasis(page.slugcatNumber);
            if (basis == SlugName.White)
            {
                ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - white - flat", "white slugcat - 2", "white lancer - 2", new Vector2(503f, 178f));
            }
            else if (basis == SlugName.Yellow)
            {
                ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - yellow - flat", "yellow slugcat - 1", "yellow lancer - 1", new Vector2(528f, 211f));
            }
            else if (basis == SlugName.Red)
            {
                if (page.menu.manager.rainWorld.progression.miscProgressionData.redUnlocked)
                    ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                        "lancer - red - flat", "red slugcat - 1", "red lancer - 1", new Vector2(462f, 225f));
                else
                    ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                    "lancer - red dark - flat", "red slugcat - 1 - dark", "red lancer - 1 - dark", new Vector2(462f, 225f));
                if (page.markSquare != null) { page.markSquare.RemoveFromContainer(); page.markSquare = null; }
                if (page.markGlow != null) { page.markGlow.RemoveFromContainer(); page.markGlow = null; }
            }

            void UpdateEffectColor()
            {
                page.effectColor = PlayerGraphics.DefaultSlugcatColor(page.slugcatNumber);
                if (page.HasMark)
                {
                    if (page.markSquare != null) page.markSquare.color = Color.Lerp(page.effectColor, Color.white, 0.7f);
                    if (page.markGlow != null) page.markGlow.color = page.effectColor;
                }
            }
            void MoveBehindGUIs()
            {
                var relImage = (page.menu as SlugcatSelectMenu).slugcatPages[0].slugcatImage;
                var relSprite = relImage.flatIllustrations.Count > 0 ? relImage.flatIllustrations[0].sprite
                    : relImage.depthIllustrations[0].sprite;
                foreach (var illust in page.slugcatImage.depthIllustrations)
                    illust.sprite.MoveBehindOtherNode(relSprite);
                foreach (var illust in page.slugcatImage.flatIllustrations)
                    illust.sprite.MoveBehindOtherNode(relSprite);
            }
        }

        internal static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos, bool basic = true)
        {
            if (scene.flatMode)
            {
                scene.flatIllustrations[0].RemoveSprites();
                scene.flatIllustrations.Clear();
                scene.AddIllustration(new MenuIllustration(scene.page.menu, scene, sceneFolder, flatImage, new Vector2(683f, 384f), false, true));
            }
            else
            {
                int i = 0;
                for (; i < scene.depthIllustrations.Count; ++i)
                    if (string.Compare(scene.depthIllustrations[i].fileName, layerImageOrig, true) == 0) break;
                float depth = scene.depthIllustrations[i].depth;
                scene.depthIllustrations[i].RemoveSprites();
                scene.depthIllustrations[i] = null;
                // LancerPlugin.LogSource.LogMessage($"({i}/{scene.depthIllustrations.Count}) replaced to {layerImage}");
                scene.depthIllustrations[i] =
                    new MenuDepthIllustration(scene.page.menu, scene, sceneFolder, layerImage, layerPos, depth, basic ? MenuDepthIllustration.MenuShader.Basic : MenuDepthIllustration.MenuShader.Normal);
                if (i < scene.depthIllustrations.Count - 1)
                    scene.depthIllustrations[i].sprite.MoveBehindOtherNode(scene.depthIllustrations[i + 1].sprite);
                scene.subObjects.Add(scene.depthIllustrations[i]);
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
            }

            internal SlugName basisNumber;

            private void VanillaLancerText()
            {
                string diff = difficultyLabel.text;
                string info = infoLabel.text;
                int lineCount = Enumerable.Count(info, (char f) => f == '\n');
                if (lineCount > 1) // revert offset to recalculate
                { imagePos.y -= 30f; sceneOffset.y -= 30f; }

                if (basisNumber == SlugName.Yellow)
                {
                    info = menu.Translate("Feeble but cautious cub. Stranded in a harsh world and surrounded by<LINE>its creatures, your journey will be a significantly more challenging one.");
                }
                if (!(menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber))
                {
                    if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basisNumber))
                    {
                        diff = "???";
                        info = menu.Translate("To be released...");
                    }
                }
                info = Custom.ReplaceLineDelimeters(info);
                lineCount = Enumerable.Count(info, (char f) => f == '\n');
                float yOffset = lineCount > 1 ? 30f : 0f;

                difficultyLabel.text = diff;
                infoLabel.text = info;

                difficultyLabel.pos.y = imagePos.y - 249f + yOffset;
                infoLabel.pos.y = imagePos.y - 249f - 60f + yOffset / 2f;
                if (lineCount > 1)
                { imagePos.y += 30f; sceneOffset.y += 30f; }
            }
        }

        internal class LancerPageContinue : SlugcatPageContinue
        {
            public LancerPageContinue(Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber) : base(menu, owner, pageIndex, GetBasis(lancerNumber))
            {
                basisNumber = slugcatNumber;
                slugcatNumber = lancerNumber;

                LancerPortrait(this);
            }

            internal SlugName basisNumber;

            private delegate SaveGameData orig_saveGameData(SlugcatPageContinue self);

            private static SaveGameData LancerGameData(orig_saveGameData orig, SlugcatPageContinue self)
            {
                if (self is LancerPageContinue)
                {
                    var lancerNumber = self.slugcatNumber;
                    if (HasLancer(lancerNumber)) lancerNumber = GetLancer(lancerNumber);
                    return (self.menu as SlugcatSelectMenu).saveGameData[lancerNumber];
                }
                return orig(self);
            }

            internal static void SubPatch()
            {
                var saveGameData = new Hook(
                    typeof(SlugcatPageContinue).GetProperty(nameof(SlugcatPageContinue.saveGameData), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                    typeof(LancerPageContinue).GetMethod(nameof(LancerPageContinue.LancerGameData), BindingFlags.Static | BindingFlags.NonPublic)
                );
            }

            internal static void PageContinueUpdatePatch(On.Menu.SlugcatSelectMenu.SlugcatPageContinue.orig_Update orig, SlugcatPageContinue self)
            {
                orig(self);
                float offset = lancerTransition * VOFFSET;
                if (IsLancerPage(self)) offset -= VOFFSET;
                self.hud.karmaMeter.pos.y += offset;
                self.hud.foodMeter.pos.y += offset;
            }

            public override bool HasMark
            {
                get
                {
                    if (basisNumber == SlugName.Red) return false;
                    return base.HasMark;
                }
            }
        }
    }
}