using CatSub.Story;
using HUD;
using JollyCoop.JollyMenu;
using LancerRemix.Cat;
using Menu;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using MoreSlugcats;
using RWCustom;
using SlugBase;
using SlugBase.Assets;
using SlugBase.Features;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Watcher;
using static LancerRemix.LancerEnums;
using static Menu.SlugcatSelectMenu;
using SceneID = Menu.MenuScene.SceneID;
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
            On.Menu.SlugcatSelectMenu.CommunicateWithUpcomingProcess += CommWithNextProcess;
            On.Menu.SlugcatSelectMenu.Singal += SignalPatch;
            IL.Menu.SlugcatSelectMenu.StartGame += LancerStartGamePatch;
            On.Menu.SlugcatSelectMenu.ContinueStartedGame += ContinueLancerStartedGame;
            On.Menu.SlugcatSelectMenu.SlugcatPage.GrafUpdate += PageGrafUpdatePatch;
            On.Menu.SlugcatSelectMenu.SlugcatPageContinue.Update += LancerPageContinue.PageContinueUpdatePatch;

            LancerPageContinue.SubPatch();

            if (ModManager.MMF) OnMMFEnablePatch();
        }

        private static void CtorPatch(On.Menu.SlugcatSelectMenu.orig_ctor orig, SlugcatSelectMenu self, ProcessManager manager)
        {
            _lancerInit = false;
            redIsDead = false;
            //Debug.Log($"CtorStart slugcatPageLancer{slugcatPageLancer} currentlySelectedSinglePlayerSlugcat{manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat}");
            LoadLancerPlayers(Custom.rainWorld.progression.miscProgressionData);
            bool isSelectedLancer = IsLancer(manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat);
            ModifyCat.SetIsPlayerLancer(isSelectedLancer, lancerPlayers);
            manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat = GetBasis(manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat);

            orig(self, manager);
            if (isSelectedLancer || TryGetCurrSlugcatLancer())
            { slugcatPageLancer = true; lancerTransition = 1f; lastLancerTransition = 1f; }
            else
            { slugcatPageLancer = false; lancerTransition = 0f; lastLancerTransition = 0f; }

            lancerPages = new SlugcatPage[self.slugcatPages.Count];
            foreach (var lancer in AllLancer)
            {
                int order = GetBasisOrder(lancer);
                if (order < 0) continue;
                self.saveGameData[lancer] = MineForSaveData(manager, lancer);

                if (self.saveGameData[lancer] != null)
                {
                    if (GetBasis(lancer) == SlugName.Red)
                        if ((self.saveGameData[lancer].redsDeath && self.saveGameData[lancer].cycle >= RedsIllness.RedsCycles(self.saveGameData[lancer].redsExtraCycles)) || self.saveGameData[lancer].ascended)
                            redIsDead = true;
                    lancerPages[order] = new LancerPageContinue(self, null, 1 + order, lancer);
                }
                else
                    lancerPages[order] = new LancerPageNewGame(self, null, 1 + order, lancer);
                self.pages.Add(lancerPages[order]);
            }

            // Add Toggle Button
            lancerButton = new SymbolButtonToggle(self, self.pages[0], LANCER_SIGNAL, new Vector2(1016f, 50f), new Vector2(50f, 50f),
                "lancer_on", "lancer_off", slugcatPageLancer, false);
            self.pages[0].subObjects.Add(lancerButton);
            self.MutualHorizontalButtonBind(lancerButton, self.nextButton);
            _lancerInit = true;
            self.UpdateStartButtonText();
            self.UpdateSelectedSlugcatInMiscProg();

            Debug.Log($"CtorEnd slugcatPageLancer{slugcatPageLancer} currentlySelectedSinglePlayerSlugcat{self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat}");

            int GetBasisOrder(SlugName lancer)
            {
                var basis = GetBasis(lancer);
                for (int i = 0; i < self.slugcatColorOrder.Count; ++i)
                    if (self.slugcatColorOrder[i] == basis) return i;
                return -1;
            }

            bool TryGetCurrSlugcatLancer()
            {
                try
                { return SaveManager.GetMiscValue<bool>(manager.rainWorld.progression.miscProgressionData, SwapSave.CURRSLUGCATLANCER); }
                catch // first install
                { SaveManager.SetMiscValue(manager.rainWorld.progression.miscProgressionData, SwapSave.CURRSLUGCATLANCER, true); }
                return true;
            }
        }

        private static SlugcatPage[] lancerPages;
        private static bool slugcatPageLancer;
        private static bool _lancerInit = false;
        private static bool redIsDead = false;
        private static SymbolButtonToggle lancerButton;
        private static float lancerTransition = 0f;
        private static float lastLancerTransition = 0f;
        internal static bool SlugcatPageLancer => slugcatPageLancer;

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
            => orig(self, GetBasis(color));

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
            if (GetBasis(lancer) == SlugName.Red && redIsDead)
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
            SaveManager.SetMiscValue(self.manager.rainWorld.progression.miscProgressionData, SwapSave.CURRSLUGCATLANCER, slugcatPageLancer);
            if (!slugcatPageLancer) return;
            self.manager.rainWorld.progression.miscProgressionData.currentlySelectedSinglePlayerSlugcat =
                lancerPages[self.slugcatPageIndex]?.slugcatNumber;
            //Debug.Log($"UpdateSelectedLancerInMiscProg {lancerPages[self.slugcatPageIndex]?.slugcatNumber}({GetBasis(lancerPages[self.slugcatPageIndex]?.slugcatNumber)})");
        }

        #region LancerPlayers

        private static bool[] lancerPlayers = new bool[RainWorld.PlayerObjectBodyColors.Length];

        internal static void SetLancerPlayers(int num, bool lancer)
        {
            if (num >= lancerPlayers.Length) Array.Resize(ref lancerPlayers, (num + 3) / 4 * 4);
            lancerPlayers[num] = lancer;
        }

        internal static bool GetLancerPlayers(int num) => lancerPlayers[num];

        internal static void UpdateIsPlayerLancer(bool story)
            => ModifyCat.SetIsPlayerLancer(story, lancerPlayers);

        private const string LANCERPLAYERS = "LancerPlayers";

        internal static void SaveLancerPlayers(PlayerProgression.MiscProgressionData miscData)
        {
            int res = 0;
            for (int i = 0; i < lancerPlayers.Length; ++i)
                res |= lancerPlayers[i] ? 1 << i : 0;
            SaveManager.SetMiscValue(miscData, LANCERPLAYERS, res);
        }

        private static void LoadLancerPlayers(PlayerProgression.MiscProgressionData miscData)
        {
            int data = 0;
            try { data = SaveManager.GetMiscValue<int>(miscData, LANCERPLAYERS); }
            catch (Exception) { SaveManager.SetMiscValue(miscData, LANCERPLAYERS, 0); }

            for (int i = 0; i < lancerPlayers.Length; ++i)
                lancerPlayers[i] = (data & (1 << i)) > 0;
        }

        private static void CommWithNextProcess(On.Menu.SlugcatSelectMenu.orig_CommunicateWithUpcomingProcess orig, SlugcatSelectMenu self, MainLoopProcess nextProcess)
        {
            SaveLancerPlayers(self.manager.rainWorld.progression.miscProgressionData);
            orig(self, nextProcess);
        }

        #endregion LancerPlayers

        private static void SignalPatch(On.Menu.SlugcatSelectMenu.orig_Singal orig, SlugcatSelectMenu self, MenuObject sender, string message)
        {
            if (message.StartsWith(LANCER_SIGNAL))
            {
                slugcatPageLancer = message.EndsWith("_on");
                //LancerPlugin.LogSource.LogMessage($"slugcatPageLancer: {slugcatPageLancer}");
                self.PlaySound(SoundID.MENU_Next_Slugcat);
                self.UpdateStartButtonText();
                self.UpdateSelectedSlugcatInMiscProg();
                self.restartAvailable = false;
                return;
            }
            if (message == "START")
            {
                if (!ModManager.JollyCoop) lancerPlayers[0] = slugcatPageLancer;
                else
                {
                    for (int i = 0; i < 4; ++i)
                    {
                        var basis = GetBasis(Custom.rainWorld.options.jollyPlayerOptionsArray[i].playerClass);
                        if (basis?.Index >= 0 && SlugcatStats.IsSlugcatFromMSC(basis)
                            && (!LancerPlugin.MSCLANCERS || !LancerGenerator.HasCustomLancer(basis.value, out var _))) SetLancerPlayers(i, false);
                    }
                }

                bool isCustomLancer = LancerGenerator.IsCustomLancer(GetLancer(self.slugcatColorOrder[self.slugcatPageIndex]).value);
                UpdateIsPlayerLancer(slugcatPageLancer && !isCustomLancer);
                SaveLancerPlayers(self.manager.rainWorld.progression.miscProgressionData);
                if (isCustomLancer)
                {
                    self.StartGame(GetLancer(self.slugcatColorOrder[self.slugcatPageIndex]));
                    return;
                }
            }
            if (message == "DEFAULTCOL" && slugcatPageLancer)
            {
                var name = self.slugcatColorOrder[self.slugcatPageIndex];
                HornColorPick.ResetColor(name);

                int num = self.activeColorChooser;
                self.manager.rainWorld.progression.miscProgressionData.colorChoices[GetLancer(name).value][num] = self.colorInterface.defaultColors[self.activeColorChooser];
                float f = self.ValueOfSlider(self.hueSlider);
                float f2 = self.ValueOfSlider(self.satSlider);
                float f3 = self.ValueOfSlider(self.litSlider);
                self.SliderSetValue(self.hueSlider, f);
                self.SliderSetValue(self.satSlider, f2);
                self.SliderSetValue(self.litSlider, f3);
                self.PlaySound(SoundID.MENU_Remove_Level);
                return;
            }
            orig(self, sender, message);
        }

        private static void LancerStartGamePatch(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LancerStartGamePatch);

            #region CustomColor

            if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdnull(),
                x => x.MatchStsfld(typeof(PlayerGraphics).GetField(nameof(PlayerGraphics.customColors)))
                )) return;

            DebugLogCursor();
            cursor.Goto(cursor.Next, MoveType.After);
            DebugLogCursor();
            cursor.EmitDelegate<Action<SlugcatSelectMenu>>(
                (self) => { if (slugcatPageLancer) SetLancerCustomColors(self); }
                );
            cursor.Emit(OpCodes.Ldarg_0);

            #endregion CustomColor

            #region SkipIntro

            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld(typeof(MainLoopProcess).GetField(nameof(MainLoopProcess.manager))),
                x => x.MatchLdsfld(typeof(ProcessManager.ProcessID).GetField(nameof(ProcessManager.ProcessID.Game))),
                x => x.MatchCallOrCallvirt(typeof(ProcessManager).GetMethod(nameof(ProcessManager.RequestMainProcessSwitch), new Type[] { typeof(ProcessManager.ProcessID) }))
                )) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Nop);
            var lblOkay = cursor.DefineLabel();
            lblOkay.Target = cursor.Prev;

            /*
            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdarg(1),
                x => x.MatchLdsfld(typeof(SlugName).GetField(nameof(SlugName.Yellow)))
                )) return;
            */
            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdfld(typeof(MainLoopProcess).GetField(nameof(MainLoopProcess.manager))),
                x => x.MatchLdsfld(typeof(ProcessManager.ProcessID).GetField(nameof(ProcessManager.ProcessID.SlideShow))),
                x => x.MatchCallOrCallvirt(typeof(ProcessManager).GetMethod(nameof(ProcessManager.RequestMainProcessSwitch), new Type[] { typeof(ProcessManager.ProcessID) }))
                )) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Pop); // removes Ldarg0 before this
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<SlugName, bool>>((storyGameCharacter) =>
            {
                Debug.Log($"isLancer{slugcatPageLancer} {storyGameCharacter}");
                if (!slugcatPageLancer) return false;
                var basis = GetBasis(storyGameCharacter);
                return basis == SlugName.White || basis == SlugName.Yellow
                    || (ModManager.Watcher && basis == WatcherEnums.SlugcatStatsName.Watcher);
            }
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);
            cursor.Emit(OpCodes.Ldarg_0); // readd Ldarg0

            #endregion SkipIntro

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LancerStartGamePatch);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static void SetLancerCustomColors(SlugcatSelectMenu self)
        {
            var lancer = GetLancer(self.slugcatColorOrder[self.slugcatPageIndex]);
            if (ModManager.MMF && self.manager.rainWorld.progression.miscProgressionData.colorsEnabled.ContainsKey(lancer.value) && self.manager.rainWorld.progression.miscProgressionData.colorsEnabled[lancer.value])
            {
                List<Color> list = new List<Color>();
                for (int i = 0; i < self.manager.rainWorld.progression.miscProgressionData.colorChoices[lancer.value].Count; ++i)
                {
                    Vector3 hsl = new Vector3(1f, 1f, 1f);
                    if (self.manager.rainWorld.progression.miscProgressionData.colorChoices[lancer.value][i].Contains(","))
                    {
                        string[] array = self.manager.rainWorld.progression.miscProgressionData.colorChoices[lancer.value][i].Split(new char[] { ',' });
                        hsl = new Vector3(float.Parse(array[0], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture), float.Parse(array[2], NumberStyles.Any, CultureInfo.InvariantCulture));
                    }
                    list.Add(Custom.HSL2RGB(hsl[0], hsl[1], hsl[2]));
                }
                PlayerGraphics.customColors = list;
            }
            else
            {
                PlayerGraphics.customColors = null;
            }
            LancerPlugin.LogSource.LogInfo($"Lancer ({lancer}) CustomColors: {(PlayerGraphics.customColors != null ? PlayerGraphics.customColors.Count : 0)}");
        }

        private static void ContinueLancerStartedGame(On.Menu.SlugcatSelectMenu.orig_ContinueStartedGame orig, SlugcatSelectMenu self, SlugName storyGameCharacter)
        {
            if (slugcatPageLancer)
            {
                var basis = GetBasis(storyGameCharacter);
                var lancer = GetLancer(basis);
                if (basis == SlugName.Red)
                {
                    if (redIsDead)
                    {
                        self.redSaveState = self.manager.rainWorld.progression.GetOrInitiateSaveState(lancer, null, self.manager.menuSetup, false);
                        self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Statistics);
                        self.PlaySound(SoundID.MENU_Switch_Page_Out);
                    }
                    else ContinueGame();
                    return;
                }
            }
            orig(self, storyGameCharacter);

            void ContinueGame()
            {
                self.manager.menuSetup.startGameCondition = ProcessManager.MenuSetup.StoryGameInitCondition.Load;
                self.manager.RequestMainProcessSwitch(ProcessManager.ProcessID.Game);
                self.PlaySound(SoundID.MENU_Continue_Game);
            }
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
            var basis = GetBasis(page.slugcatNumber);
            bool ascended = page is SlugcatPageContinue pageCont && pageCont.saveGameData.ascended;
            // ascended = true; // test
            UpdateEffectColor();
            ReloadScene();
            MoveBehindGUIs();

            if (ascended) return;

            if (basis == SlugName.White)
            {
                if (page is LancerPageContinue lpc && lpc.saveGameData.altEnding)
                    ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}Outro L_B",
                        "slugcat end_b - Lwhite - flat", "slugcat end_b - yellow - slugcat f", "slugcat end_b - Lwhite - slugcat f", new Vector2(546f, 211f));
                else
                {
                    ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                        "lancer - white - flat", "white slugcat - 2", "white lancer - 2", new Vector2(503f, 178f));
                    MoveGlow("white lancer - 2");
                }
            }
            else if (basis == SlugName.Yellow)
            {
                ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                "lancer - yellow - flat", "yellow slugcat - 1", "yellow lancer - 1", new Vector2(528f, 211f));
                MoveGlow("yellow lancer - 1");
            }
            else if (basis == SlugName.Red)
            {
                if (page.slugcatImage.sceneID != SceneID.Slugcat_Dead_Red)
                {
                    if (page.menu.manager.rainWorld.progression.miscProgressionData.redUnlocked)
                    {
                        ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                            "lancer - red - flat", "red slugcat - 1", "red lancer - 1", new Vector2(462f, 225f));
                        MoveGlow("red lancer - 1");
                    }
                    else
                        ReplaceIllust(page.slugcatImage, $"scenes{Path.DirectorySeparatorChar}slugcat - lancer",
                        "lancer - red dark - flat", "red slugcat - 1 - dark", "red lancer - 1 - dark", new Vector2(462f, 225f));
                }
                if (page.markSquare != null) { page.markSquare.RemoveFromContainer(); page.markSquare = null; }
                if (page.markGlow != null) { page.markGlow.RemoveFromContainer(); page.markGlow = null; }
            }
            else if (ModManager.Watcher && basis == WatcherEnums.SlugcatStatsName.Watcher)
            {
                // TODO: REPLACE
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
            void ReloadScene()
            {
                var sceneID = GetLancerBasisScene();
                if (page.slugcatImage.sceneID == sceneID) return;
                page.RemoveSubObject(page.slugcatImage);
                page.slugcatImage.RemoveSprites(); page.slugcatImage.RemoveSubObject(page.slugcatImage); page.slugcatImage = null;
                page.slugcatImage = new InteractiveMenuScene(page.menu, page, sceneID);

                SceneID GetLancerBasisScene()
                {
                    var res = page.slugcatImage.sceneID;
                    if (basis == SlugName.White)
                    {
                        if (ascended) res = SceneGhostLancerWhite;
                        else if (page is LancerPageContinue lpc && lpc.saveGameData.altEnding) res = MoreSlugcatsEnums.MenuSceneID.AltEnd_Monk;
                        else res = SceneID.Slugcat_White;
                    }
                    else if (basis == SlugName.Yellow)
                    {
                        if (ascended) res = SceneGhostLancerYellow;
                        else res = SceneID.Slugcat_Yellow;
                    }
                    else if (basis == SlugName.Red)
                    {
                        if (ascended) res = SceneGhostLancerRed;
                        else if (page is LancerPageContinue && redIsDead) res = SceneID.Slugcat_Dead_Red;
                        else res = SceneID.Slugcat_Red;
                    }
                    else if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basis))
                    {
                        // TODO: fill this
                    }
                    else if (ModManager.Watcher && basis == WatcherEnums.SlugcatStatsName.Watcher)
                    {
                        // TODO: fill this
                    }
                    else if (SlugBaseCharacter.Registry.TryGet(basis, out var slugbase))
                    {
                        if (ascended && GameFeatures.SelectMenuSceneAscended.TryGet(slugbase, out var sbSceneAscended))
                            res = sbSceneAscended;
                        else if (GameFeatures.SelectMenuScene.TryGet(slugbase, out var sbScene))
                            res = sbScene;

                        if (CustomScene.Registry.TryGet(res, out var ctmScene))
                        {
                            page.markOffset = ctmScene.MarkPos ?? page.markOffset;
                            page.glowOffset = ctmScene.GlowPos ?? page.glowOffset;
                            page.sceneOffset = ctmScene.SelectMenuOffset ?? page.sceneOffset;
                            page.slugcatDepth = ctmScene.SlugcatDepth ?? page.slugcatDepth;
                        }
                    }

                    return res;
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
            void MoveGlow(string slugIllust)
            {
                var scene = page.slugcatImage;
                if (!page.HasGlow || scene.flatMode) return;
                if (page.glowSpriteA == null || page.glowSpriteB == null) return;
                int i = 0;
                for (; i < scene.depthIllustrations.Count; ++i)
                    if (string.Equals(scene.depthIllustrations[i].fileName, slugIllust, StringComparison.InvariantCultureIgnoreCase)) break;
                if (i >= scene.depthIllustrations.Count) return;
                page.glowSpriteA.MoveBehindOtherNode(scene.depthIllustrations[i].sprite);
                page.glowSpriteB.MoveBehindOtherNode(page.glowSpriteA);
            }
        }

        internal static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos, MenuDepthIllustration.MenuShader shader = null)
        {
            if (scene.flatMode)
            {
                if (string.IsNullOrEmpty(flatImage)) return;
                var old = scene.flatIllustrations[0];
                scene.flatIllustrations.Clear();
                scene.AddIllustration(new MenuIllustration(scene.page.menu, scene, sceneFolder, flatImage, new Vector2(683f, 384f), false, true));
                scene.flatIllustrations[scene.flatIllustrations.Count - 1].sprite.MoveBehindOtherNode(old.sprite);
                old.RemoveSprites();
            }
            else
            {
                int i = 0;
                for (; i < scene.depthIllustrations.Count; ++i)
                    if (string.Equals(scene.depthIllustrations[i].fileName, layerImageOrig, StringComparison.InvariantCultureIgnoreCase)) break;
                if (i >= scene.depthIllustrations.Count)
                {
                    var B = new System.Text.StringBuilder();
                    B.AppendLine($"layerImage [{layerImageOrig}] is not in these ({i}/{scene.depthIllustrations.Count}):");
                    for (i = 0; i < scene.depthIllustrations.Count; ++i)
                        B.AppendLine($"{i}: [{scene.depthIllustrations[i].fileName}] == [{layerImageOrig}] ? {string.Equals(scene.depthIllustrations[i].fileName, layerImageOrig, StringComparison.InvariantCultureIgnoreCase)}");
                    throw new ArgumentOutOfRangeException(B.ToString());
                }
                float depth = scene.depthIllustrations[i].depth;
                scene.depthIllustrations[i].RemoveSprites();
                scene.depthIllustrations[i] = null;
                // LancerPlugin.LogSource.LogMessage($"({i}/{scene.depthIllustrations.Count}) replaced to {layerImage}");
                scene.depthIllustrations[i] =
                    new MenuDepthIllustration(scene.page.menu, scene, sceneFolder, layerImage, layerPos, depth, shader ?? MenuDepthIllustration.MenuShader.Basic);
                if (i < scene.depthIllustrations.Count - 1)
                    scene.depthIllustrations[i].sprite.MoveBehindOtherNode(scene.depthIllustrations[i + 1].sprite);
                scene.subObjects.Add(scene.depthIllustrations[i]);
                Debug.Log($"Replace Illust {i}: [{layerImage}] <- [{layerImageOrig}]");
            }
        }

        internal class LancerPageNewGame : SlugcatPageNewGame
        {
            public LancerPageNewGame(Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber)
                : base(menu, owner, pageIndex, LancerGenerator.IsCustomLancer(lancerNumber.value) ? lancerNumber : GetBasis(lancerNumber))
            {
                basisNumber = GetBasis(slugcatNumber);
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
                    info = menu.Translate("Feeble but cautious. Stranded in a harsh world and surrounded by<LINE>its creatures, your journey will be a significantly more challenging one.");
                }
                if (!(menu as SlugcatSelectMenu).SlugcatUnlocked(slugcatNumber))
                {
                    bool isMSCLocked = ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basisNumber) &&
                        (!LancerPlugin.MSCLANCERS || !LancerGenerator.HasCustomLancer(basisNumber.value, out var _));
                    bool isWatcherLocked = ModManager.Watcher && basisNumber == WatcherEnums.SlugcatStatsName.Watcher;
#if LATCHER
                    if (ModManager.Watcher) isWatcherLocked &= !SlugcatStats.SlugcatUnlocked(WatcherEnums.SlugcatStatsName.Watcher, menu.manager.rainWorld);
#endif
                    if (isMSCLocked || isWatcherLocked)
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
            public LancerPageContinue(Menu.Menu menu, MenuObject owner, int pageIndex, SlugName lancerNumber)
                : base(menu, owner, pageIndex, LancerGenerator.IsCustomLancer(lancerNumber.value) ? lancerNumber : GetBasis(lancerNumber))
            {
                basisNumber = GetBasis(slugcatNumber);
                slugcatNumber = lancerNumber;

                LancerPortrait(this);

                if (SlugcatStats.SlugcatFoodMeter(slugcatNumber) != SlugcatStats.SlugcatFoodMeter(basisNumber))
                {
                    hud.foodMeter.slatedForDeletion = true;
                    hud.parts.Remove(hud.foodMeter);
                    hud.foodMeter.ClearSprites();
                    hud.AddPart(new FoodMeter(hud, SlugcatStats.SlugcatFoodMeter(slugcatNumber).x, SlugcatStats.SlugcatFoodMeter(slugcatNumber).y, null, 0));
                }
            }

            internal SlugName basisNumber;

            private delegate SaveGameData orig_saveGameData(SlugcatPageContinue self);

            private static SaveGameData LancerGameData(orig_saveGameData orig, SlugcatPageContinue self)
            {
                if (self is LancerPageContinue)
                    return (self.menu as SlugcatSelectMenu).saveGameData[GetLancer(self.slugcatNumber)];
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

        #region MMFCustomColor

        internal static void OnMMFEnablePatch()
        {
            IL.Menu.SlugcatSelectMenu.SliderSetValue += LancerCustomColorSlider;
            IL.Menu.SlugcatSelectMenu.ValueOfSlider += LancerCustomColorSlider;
        }

        internal static void OnMMFDisablePatch()
        {
            IL.Menu.SlugcatSelectMenu.SliderSetValue += LancerCustomColorSlider;
            IL.Menu.SlugcatSelectMenu.ValueOfSlider += LancerCustomColorSlider;
        }

        private static void LancerCustomColorSlider(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LancerCustomColorSlider);

            if (!cursor.TryGotoNext(MoveType.After, x => x.MatchStloc(0))) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldloc, 0);
            cursor.EmitDelegate<Func<SlugcatSelectMenu, SlugName, SlugName>>(
                    (self, name) =>
                    {
                        if (slugcatPageLancer) return GetLancer(name);
                        return name;
                    }
                );
            cursor.Emit(OpCodes.Stloc, 0);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LancerCustomColorSlider);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        #endregion MMFCustomColor
    }
}