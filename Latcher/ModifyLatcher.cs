#if LATCHER

using CatSub.Story;
using LancerRemix.LancerMenu;
using Menu;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using UnityEngine;
using Watcher;
using static LancerRemix.Cat.ModifyCat;
using static LancerRemix.LancerEnums;
using Random = UnityEngine.Random;
using WatcherName = Watcher.WatcherEnums.SlugcatStatsName;

namespace LancerRemix.Latcher
{
    internal static class ModifyLatcher
    {
        internal static void SubPatch()
        {
            LatcherPatch.SubPatch();
            LatcherTutorial.SubPatch();
            LatcherMusicbox.SubPatch();

            if (!ModManager.Watcher) return;
            OnWatcherEnablePatch();
        }

        internal static void OnWatcherEnablePatch()
        {
            hooks.Clear();
            hooks.Add(new Hook(
                typeof(Player).GetProperty(nameof(Player.watcherDynamicWarpInput), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherDynamicWarpInput), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(Player).GetProperty(nameof(Player.RippleAbilityActivationButtonCondition), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherRippleAbilityActivationButtonCondition), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(KarmaLadderScreen).GetProperty(nameof(KarmaLadderScreen.WatcherMode), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(KarmaLadderScreenLatcherMode), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(LocustSystem.Swarm).GetProperty(nameof(LocustSystem.Swarm.CamoFactor), BindingFlags.Instance | BindingFlags.NonPublic).GetGetMethod(true),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherLocustCamoFactor), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(Player).GetProperty(nameof(Player.camoLimit), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherCamoLimit), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            #region MISCPROG

            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenGet), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenSet), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.watcherCampaignSeed), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherCampaignSeedGet), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.watcherCampaignSeed), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherCampaignSeedSet), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher_SpinningTop), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenSTGet), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher_SpinningTop), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenSTSet), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher_SentientRot), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenRPGet), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.beaten_Watcher_SentientRot), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherBeatenRPSet), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.watcherEndingID), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherEndingIDGet), BindingFlags.Static | BindingFlags.NonPublic)
            ));
            hooks.Add(new Hook(
                typeof(PlayerProgression.MiscProgressionData).GetProperty(nameof(PlayerProgression.MiscProgressionData.watcherEndingID), BindingFlags.Instance | BindingFlags.Public).GetSetMethod(),
                typeof(ModifyLatcher).GetMethod(nameof(LatcherEndingIDSet), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            #endregion MISCPROG

            On.Player.WatcherUpdate += LatcherUpdate;
            On.Player.CamoUpdate += LatcherCamoUpdate;

            On.Menu.MenuScene.BuildRippleSleepScene += BuildLatcherRippleSleepScene;
            On.Menu.MenuScene.BuildWatcherSleepScreen += BuildLatcherSleepScreen;
            On.Menu.MenuScene.BuildVoidBathScene += BuildLatcherVoidBathScene;
            On.Menu.MenuScene.BuildSpinningTopEndingScene += BuildLatcherSpinningTopEndingScene;
            On.Menu.MenuScene.BuildPrinceEndingScene += BuildLatcherPrinceEndingScene;

            LatcherPatch.OnWatcherEnableSubPatch();
            LatcherTutorial.OnWatcherEnableSubPatch();
        }

        internal static void OnWatcherDisablePatch()
        {
            foreach (var hook in hooks) hook.Undo();
            hooks.Clear();

            On.Player.WatcherUpdate -= LatcherUpdate;
            On.Player.CamoUpdate -= LatcherCamoUpdate;

            On.Menu.MenuScene.BuildRippleSleepScene -= BuildLatcherRippleSleepScene;
            On.Menu.MenuScene.BuildWatcherSleepScreen -= BuildLatcherSleepScreen;

            LatcherPatch.OnWatcherDisableSubPatch();
            LatcherTutorial.OnWatcherDisableSubPatch();
        }

        private static bool IsPlayerLatcher(Player player)
            => ModManager.Watcher && IsPlayerLancer(player) && GetBasis(player.SlugCatClass) == WatcherName.Watcher;

        #region Properties

        private static readonly List<Hook> hooks = new List<Hook>();

        private delegate bool orig_watcherDynamicWarpInput(Player self);

        private static bool LatcherDynamicWarpInput(orig_watcherDynamicWarpInput orig, Player self)
        {
            var res = orig(self);
            if (IsPlayerLatcher(self))
            {
                return self.CanSpawnDynamicWarpPoints && self.warpExhausionTime <= 0
                    && ((self.bodyMode == Player.BodyModeIndex.Stand && self.canJump > 0)
                    || self.bodyMode == Player.BodyModeIndex.Swimming || self.bodyMode == Player.BodyModeIndex.ZeroG
                    || (self.bodyMode == Player.BodyModeIndex.Default && self.canJump > 0))
                    && self.input[0].spec && self.input[0].y > 0;
            }
            return res;
        }

        private delegate bool orig_RippleAbilityActivationButtonCondition(Player self);

        private static bool LatcherRippleAbilityActivationButtonCondition(orig_RippleAbilityActivationButtonCondition orig, Player self)
        {
            var res = orig(self);
            if (IsPlayerLatcher(self))
            {
                bool animOkay = self.animation != Player.AnimationIndex.BellySlide && self.animation != Player.AnimationIndex.CrawlTurn && self.animation != Player.AnimationIndex.CorridorTurn && self.animation != Player.AnimationIndex.Flip && self.animation != Player.AnimationIndex.Roll && self.animation != Player.AnimationIndex.GrapplingSwing && self.animation != Player.AnimationIndex.RocketJump;
                return !self.watcherDynamicWarpInput && self.warpExhausionTime <= 0 && self.input[0].spec && self.Consious && !self.Stunned && self.camoCharge < self.usableCamoLimit && self.cancelCamoCooldown <= 0 && !self.camoInputsNeedReset && animOkay && (self.rippleLevel < 5f || self.room.game.cameras[0].warpPointTimer == null);
            }
            return res;
        }

        private delegate bool orig_KarmaLadderScreenLatcherMode(KarmaLadderScreen self);

        private static bool KarmaLadderScreenLatcherMode(orig_KarmaLadderScreenLatcherMode orig, KarmaLadderScreen self)
        {
            if (self.saveState != null && GetBasis(self.saveState.saveStateNumber) == WatcherName.Watcher
                && self.saveState.deathPersistentSaveData.maximumRippleLevel >= 1f && !self.RippleLadderMode)
                return true;
            return orig(self);
        }

        private delegate float orig_LatcherLocustCamoFactor(LocustSystem.Swarm self);

        private static float LatcherLocustCamoFactor(orig_LatcherLocustCamoFactor orig, LocustSystem.Swarm self)
        {
            var res = orig(self);
            if (res > 0f && self.target is Player player && IsPlayerLatcher(player))
                return 0f; // No camo for Latcher
            return res;
        }

        private delegate float orig_LatcherCamoLimit(Player self);

        private static float LatcherCamoLimit(orig_LatcherCamoLimit orig, Player self)
        {
            if (!IsPlayerLatcher(self)) return orig(self);
            if (self.rippleLevel >= 5f) return 2400f;
            if (self.rippleLevel >= 4f) return 2100f;
            if (self.rippleLevel >= 2f) return 1800f;
            return 1200f;
        }

        #region MISCPROG

        internal const string LATCHER_BEATEN = "LATCHERBEATEN";
        internal const string LATCHER_CAMPAIGNSEED = "LATCHERCAMPAIGNSEED";
        internal const string LATCHER_BEATEN_ST = "LATCHERBEATENST";
        internal const string LATCHER_BEATEN_RP = "LATCHERBEATENRP";
        internal const string LATCHER_ENDINGID = "LATCHERENDINGID";

        private static bool NotInGame => Custom.rainWorld.processManager.currentMainLoop == null
            || (Custom.rainWorld.processManager.currentMainLoop.ID == ProcessManager.ProcessID.MainMenu ||
            Custom.rainWorld.processManager.currentMainLoop.ID == ProcessManager.ProcessID.SlugcatSelect ||
            Custom.rainWorld.processManager.currentMainLoop.ID == ProcessManager.ProcessID.OptionsMenu ||
            Custom.rainWorld.processManager.currentMainLoop.ID == ProcessManager.ProcessID.MultiplayerMenu);

        private delegate int orig_MiscIntGet(PlayerProgression.MiscProgressionData self);

        private delegate void orig_MiscIntSet(PlayerProgression.MiscProgressionData self, int value);

        private delegate bool orig_MiscBoolGet(PlayerProgression.MiscProgressionData self);

        private delegate void orig_MiscBoolSet(PlayerProgression.MiscProgressionData self, bool value);

        private static bool LatcherBeatenGet(orig_MiscBoolGet orig, PlayerProgression.MiscProgressionData self)
        {
            if (NotInGame || !IsStoryLancer) return orig(self);
            try
            {
                return SaveManager.GetMiscValue<bool>(self, LATCHER_BEATEN);
            }
            catch
            {
                SaveManager.SetMiscValue(self, LATCHER_BEATEN, false);
                return false;
            }
        }

        private static void LatcherBeatenSet(orig_MiscBoolSet orig, PlayerProgression.MiscProgressionData self, bool value)
        {
            if (NotInGame || !IsStoryLancer) { orig(self, value); return; }
            SaveManager.SetMiscValue(self, LATCHER_BEATEN, value);
        }

        private static int LatcherCampaignSeedGet(orig_MiscIntGet orig, PlayerProgression.MiscProgressionData self)
        {
            if (NotInGame || !IsStoryLancer) return orig(self);
            if (Watcher.Watcher.cfgForcedCampaignSeed.Value <= 0)
            {
                try
                {
                    return SaveManager.GetMiscValue<int>(self, LATCHER_CAMPAIGNSEED);
                }
                catch
                {
                    SaveManager.SetMiscValue(self, LATCHER_CAMPAIGNSEED, 0);
                    return 0;
                }
            }
            return Watcher.Watcher.cfgForcedCampaignSeed.Value;
        }

        private static void LatcherCampaignSeedSet(orig_MiscIntSet orig, PlayerProgression.MiscProgressionData self, int value)
        {
            if (NotInGame || !IsStoryLancer) { orig(self, value); return; }
            if (Watcher.Watcher.cfgForcedCampaignSeed.Value <= 0)
                SaveManager.SetMiscValue(self, LATCHER_CAMPAIGNSEED, value);
        }

        private static bool LatcherBeatenSTGet(orig_MiscBoolGet orig, PlayerProgression.MiscProgressionData self)
        {
            if (NotInGame || !IsStoryLancer) return orig(self);
            try
            {
                return SaveManager.GetMiscValue<bool>(self, LATCHER_BEATEN_ST);
            }
            catch
            {
                SaveManager.SetMiscValue(self, LATCHER_BEATEN_ST, false);
                return false;
            }
        }

        private static void LatcherBeatenSTSet(orig_MiscBoolSet orig, PlayerProgression.MiscProgressionData self, bool value)
        {
            if (NotInGame || !IsStoryLancer) { orig(self, value); return; }
            SaveManager.SetMiscValue(self, LATCHER_BEATEN_ST, value);
        }

        private static bool LatcherBeatenRPGet(orig_MiscBoolGet orig, PlayerProgression.MiscProgressionData self)
        {
            if (NotInGame || !IsStoryLancer) return orig(self);
            try
            {
                return SaveManager.GetMiscValue<bool>(self, LATCHER_BEATEN_RP);
            }
            catch
            {
                SaveManager.SetMiscValue(self, LATCHER_BEATEN_RP, false);
                return false;
            }
        }

        private static void LatcherBeatenRPSet(orig_MiscBoolSet orig, PlayerProgression.MiscProgressionData self, bool value)
        {
            if (NotInGame || !IsStoryLancer) { orig(self, value); return; }
            SaveManager.SetMiscValue(self, LATCHER_BEATEN_RP, value);
        }

        private static int LatcherEndingIDGet(orig_MiscIntGet orig, PlayerProgression.MiscProgressionData self)
        {
            if (NotInGame || !IsStoryLancer) return orig(self);
            try
            {
                return SaveManager.GetMiscValue<int>(self, LATCHER_ENDINGID);
            }
            catch
            {
                SaveManager.SetMiscValue(self, LATCHER_ENDINGID, 0);
                return 0;
            }
        }

        private static void LatcherEndingIDSet(orig_MiscIntSet orig, PlayerProgression.MiscProgressionData self, int value)
        {
            if (NotInGame || !IsStoryLancer) { orig(self, value); return; }
            SaveManager.SetMiscValue(self, LATCHER_ENDINGID, value);
        }

        #endregion MISCPROG

        #endregion Properties

        private static void LatcherUpdate(On.Player.orig_WatcherUpdate orig, Player self)
        {
            orig(self);
            if (!IsPlayerLatcher(self)) return;

            if (self.room.game.devToolsActive && Input.GetKey(KeyCode.LeftControl) && Input.GetKeyDown("4"))
            {
                self.devMaxLevelRipple = !self.devMaxLevelRipple;
            }
            if (self.abstractPhysicalObject.world.game.IsStorySession && self.room.game.rainWorld.progression.miscProgressionData.watcherCampaignSeed == 0)
            {
                self.room.game.rainWorld.progression.miscProgressionData.watcherCampaignSeed = Random.Range(1, 100000);
            }
            if (self.room.game.IsStorySession && self.room.game.GetStorySession.saveState.miscWorldSaveData.lockedOuterRimProgressionFlag && self.room.world.name.ToLowerInvariant() != "wora")
            {
                float numberOfPrinceEncountersPlusTwo = 2f + (float)self.room.game.GetStorySession.saveState.miscWorldSaveData.numberOfPrinceEncounters;
                float maximumRippleLevel = (self.room.world.game.session as StoryGameSession).saveState.deathPersistentSaveData.maximumRippleLevel;
                if (self.devMaxLevelRipple)
                {
                    maximumRippleLevel = Mathf.Max(Player.devMaxLevelRippleValue, maximumRippleLevel);
                }
                if (maximumRippleLevel >= numberOfPrinceEncountersPlusTwo)
                {
                    self.room.game.GetStorySession.saveState.miscWorldSaveData.numberOfPrinceEncounters++;
                }
                self.room.game.GetStorySession.saveState.miscWorldSaveData.lockedOuterRimProgressionFlag = false;
            }
            if (!self.input[0].spec)
            {
                self.camoInputsNeedReset = false;
            }
            if (self.rippleLevel > 0f)
            {
                self.glowing = true;
            }
            if (self.standingInWarpPointProtectionTime > 0)
            {
                self.standingInWarpPointProtectionTime--;
            }
            if (self.CanLevitate)
            {
                self.TickLevitation(true);
            }
            if (self.rippleLevel >= 0.5f && (self.performingActivationTimer > 0 || self.RippleAbilityActivationButtonCondition))
            {
                if (self.rippleRingDelay.isFinished && self.camoProgress <= 0.2f)
                {
                    self.SpawnRippleRing();
                    self.rippleRingDelay.Reset();
                }
                self.manyRingsProgress.Tick();
                if (self.rippleLevel >= 4f)
                {
                    float num3 = Mathf.InverseLerp(4f, 5f, self.rippleLevel);
                    if (Random.value > 0.98f - self.manyRingsProgress.normalized * (0.1f + num3 * 0.1f) && self.camoProgress <= 0.2f)
                    {
                        self.room.AddObject(new RippleRing(self.mainBodyChunk.pos + Custom.RNV() * Random.Range(1f, (200f + num3 * 200f) * self.manyRingsProgress.normalized), 80 + UnityEngine.Random.Range(0, 40), 0.75f + self.transitionScale01 * 0.5f, 0.5f + UnityEngine.Random.value * 0.5f));
                    }
                }
                if (self.startingCamoStateOnActivate == -1)
                {
                    self.startingCamoStateOnActivate = (self.isCamo ? 1 : 0);
                    self.ringsToSpiralsTarget = (float)self.startingCamoStateOnActivate;
                }
                if (self.transitionRipple == null)
                {
                    self.rippleAnimationJitterTimer = Random.Range(0, 100);
                    self.rippleAnimationIntensityTarget = 0f;
                    self.transitionRipple = self.SpawnWatcherMechanicRipple();
                    self.transitionRipple.Data.scale = self.GetTransitionRippleTargets(self.rippleLevel).Item1;
                }
                self.activateCamoTimer++;
                if (!self.CanLevitate)
                {
                    self.rippleActivating = true;
                }
                if (self.activateCamoTimer == ((self.startingCamoStateOnActivate == 0) ? self.enterIntoCamoDuration : self.exitOutOfCamoDuration) && self.performingActivationTimer == 0)
                {
                    self.ToggleCamo();
                    self.reachedCamoToggle = true;
                }
                if (self.performingActivationTimer > 0)
                {
                    self.performingActivationTimer++;
                    if (self.performingActivationTimer >= self.performingActivationDuration)
                    {
                        self.performingActivationTimer = 0;
                        self.cancelCamoCooldown = 120;
                        self.camoInputsNeedReset = true;
                    }
                }
                else if (self.activateCamoTimer >= self.enterIntoCamoDuration)
                {
                    self.performingActivationTimer = 1;
                }
            }
            else
            {
                self.rippleRingDelay.Tick();
                if (self.activateCamoTimer > 0)
                {
                    if (self.rippleLevel >= 1f && self.activateCamoTimer > 20 && (double)self.camoProgress < 0.5 && self.activateDynamicWarpTimer == 0)
                    {
                        self.SpawnRippleRing();
                    }
                    self.manyRingsProgress.Reset();
                    self.activateCamoTimer = 0;
                    self.performingActivationTimer = 0;
                    self.StopLevitation();
                    self.canJump = 0;
                    self.wantToJump = 0;
                    if (self.reachedCamoToggle && self.rippleLevel >= 3f && UnityEngine.Random.value <= 1f / (self.isCamo ? 8f : 4f))
                    {
                        self.SpawnPersistentRipple(125f, 225f, UnityEngine.Random.Range(2, 5));
                    }
                    self.cancelCamoCooldown = 120;
                    self.camoInputsNeedReset = true;
                }
                if (self.cancelCamoCooldown > 0)
                {
                    self.cancelCamoCooldown--;
                }
                if (self.canJump > 0 || self.bodyMode == Player.BodyModeIndex.ClimbingOnBeam)
                {
                    self.cancelCamoCooldown = 0;
                }
                else if (self.cancelCamoCooldown > 40 && (self.bodyMode == Player.BodyModeIndex.CorridorClimb || self.bodyMode == Player.BodyModeIndex.Swimming || self.bodyMode == Player.BodyModeIndex.ZeroG))
                {
                    self.cancelCamoCooldown = 40;
                }
                self.rippleActivating = false;
                self.reachedCamoToggle = false;
                self.startingCamoStateOnActivate = -1;
            }
            if (self.warpExhausionTime > 0)
            {
                self.warpExhausionTime--;
                self.slowMovementStun = Mathf.Max(self.slowMovementStun, (int)Custom.LerpMap(self.aerobicLevel, 0.7f, 0.4f, 6f, 0f));
                self.lungsExhausted = true;
            }
            if (self.rippleLevel >= 5f)
            {
                self.room.game.cameras[0].virtualMicrophone.rippleDimension = Mathf.Lerp(self.room.game.cameras[0].virtualMicrophone.rippleDimension, self.isCamo ? 1f : 0f, 0.01f);
                if (self.room.game.manager.musicPlayer != null)
                {
                    self.room.game.manager.musicPlayer.rippleDimension = Mathf.Lerp(self.room.game.manager.musicPlayer.rippleDimension, self.isCamo ? 1f : 0f, 0.01f);
                }
            }
            self.CamoUpdate();
            if (self.room.game.IsStorySession && self.CanSpawnDynamicWarpPoints && !self.room.abstractRoom.shelter && self.room.warpPoints.Count == 0 && !self.room.spawnedSpinningTop && self.room.game.GetStorySession.saveState.miscWorldSaveData.cycleFirstStartedWarpJourney > 0 && self.room.game.GetStorySession.saveState.cycleNumber > self.room.game.GetStorySession.saveState.miscWorldSaveData.cycleFirstStartedWarpJourney)
            {
                self.warpTutorialConditionTimer++;
                if (!self.room.game.GetStorySession.saveState.deathPersistentSaveData.StableWarpTutorial && self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma && self.warpTutorialConditionTimer >= 200 && !self.showedStableWarpTutorialThisCycle)
                {
                    self.room.game.GetStorySession.saveState.deathPersistentSaveData.StableWarpTutorial = true;
                    self.showedStableWarpTutorialThisCycle = true;
                    self.room.AddObject(new StableWarpTutorial(self.room));
                }
                if (!self.room.game.GetStorySession.saveState.deathPersistentSaveData.badWarpTutorial && self.room.game.GetStorySession.saveState.miscWorldSaveData.cycleLastShownStableWarpTutorial > 0 && self.room.game.GetStorySession.saveState.cycleNumber > self.room.game.GetStorySession.saveState.miscWorldSaveData.cycleLastShownStableWarpTutorial && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.reinforcedKarma && self.warpTutorialConditionTimer >= 200 && !self.showedBadWarpTutorialThisCycle)
                {
                    self.room.game.GetStorySession.saveState.deathPersistentSaveData.badWarpTutorial = true;
                    self.showedBadWarpTutorialThisCycle = true;
                    self.room.AddObject(new BadWarpTutorial(self.room));
                }
            }
            else
            {
                self.warpTutorialConditionTimer = 0;
            }
            bool showWarpFatigueTutorial = self.room.game.IsStorySession && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.warpFatigueTutorial && self.room.game.GetStorySession.saveState.miscWorldSaveData.warpFatigueTutorialCounter == 0 && self.room.game.GetStorySession.warpTraversalsLeftUntilFullWarpFatigue < self.room.game.GetStorySession.warpFatigueDecayLength && !self.showedWarpFatigueTutorialThisCycle;
            bool showWarpExhaustionTutorial = self.room.game.IsStorySession && !self.room.game.GetStorySession.saveState.deathPersistentSaveData.warpExhaustionTutorial && self.room.game.GetStorySession.saveState.miscWorldSaveData.warpExhaustionTutorialCounter == 0 && self.room.game.GetStorySession.warpTraversalsLeftUntilFullWarpFatigue == 0 && !self.showedWarpExhausionTutorialThisCycle;
            if (showWarpFatigueTutorial || showWarpExhaustionTutorial)
            {
                bool isThereWarpPointInRange = false;
                for (int i = 0; i < self.room.warpPoints.Count; i++)
                {
                    if (self.room.warpPoints[i].anyPlayersInRange || self.room.warpPoints[i].activated || self.room.warpPoints[i].warpSequenceInProgress)
                    {
                        isThereWarpPointInRange = true;
                        break;
                    }
                }
                if (!isThereWarpPointInRange)
                {
                    self.warpFatigueTutorialConditionTimer++;
                }
                else
                {
                    self.warpFatigueTutorialConditionTimer = 0;
                }
                if (self.warpFatigueTutorialConditionTimer >= 200)
                {
                    if (showWarpFatigueTutorial)
                    {
                        self.room.game.GetStorySession.saveState.deathPersistentSaveData.warpFatigueTutorial = true;
                        self.showedWarpFatigueTutorialThisCycle = true;
                        self.room.AddObject(new WarpFatigueTutorial(self.room));
                    }
                    else if (showWarpExhaustionTutorial)
                    {
                        self.showedWarpExhausionTutorialThisCycle = true;
                        self.room.game.GetStorySession.saveState.deathPersistentSaveData.warpExhaustionTutorial = true;
                        self.room.AddObject(new WarpExhausionTutorial(self.room));
                    }
                }
            }
            SoundID soundID = null;
            bool dynamicWarpInput = false;
            if (self.activateCamoTimer == 0 && self.watcherDynamicWarpInput && self.dynamicWarpCooldown <= 0 && self.activateDynamicWarpTimer > 0)
            {
                soundID = (self.KarmaIsReinforced ? WatcherEnums.WatcherSoundID.Player_Generating_Warp_Point_LOOP : WatcherEnums.WatcherSoundID.Player_Generating_Bad_Warp_Point_LOOP);
                dynamicWarpInput = true;
            }
            else if (self.rippleLevel >= 0.5f && self.activateCamoTimer > 0 && (self.performingActivationTimer > 0 || self.RippleAbilityActivationButtonCondition))
            {
                if (self.camoDirectionSoundID == null)
                {
                    self.camoDirectionSoundID = (self.isCamo ? WatcherEnums.WatcherSoundID.Player_Deactivating_Camo_LOOP : WatcherEnums.WatcherSoundID.Player_Activating_Camo_LOOP);
                }
                soundID = self.camoDirectionSoundID;
                dynamicWarpInput = false;
            }
            if (soundID != null && (self.watcherAbilitySoundLoop == null || soundID != self.watcherAbilitySoundID))
            {
                self.watcherAbilitySoundLoop = new StaticSoundLoop(soundID, self.mainBodyChunk.pos, self.room, 1f, 1f)
                {
                    fadeOutOnDestroyFrames = 80
                };
                self.watcherAbilitySoundID = soundID;
            }
            if (soundID == null)
            {
                self.camoDirectionSoundID = null;
            }
            if (self.watcherAbilitySoundLoop != null)
            {
                self.watcherAbilitySoundLoop.Update();
                self.watcherAbilitySoundLoop.pos = self.mainBodyChunk.pos;
                if (soundID == null)
                {
                    self.watcherAbilitySoundLoop.volume = Mathf.Lerp(self.watcherAbilitySoundLoop.volume, 0f, 0.06f);
                    if (self.watcherAbilitySoundLoop.volume < 0.05f)
                    {
                        self.watcherAbilitySoundLoop.volume = 0f;
                    }
                }
                else
                {
                    self.watcherAbilitySoundLoop.pitch = 1f;
                    if (dynamicWarpInput && (float)self.activateDynamicWarpTimer > 0f)
                    {
                        self.watcherAbilitySoundLoop.volume = Mathf.InverseLerp(0f, 20f, (float)self.activateDynamicWarpTimer) * ((self.KarmaIsReinforced && self.warpSpawningRipple != null) ? (0.5f + self.warpSpawningRipple.ringScale * 0.5f) : 1f);
                        StaticSoundLoop staticSoundLoop = self.watcherAbilitySoundLoop;
                        WarpSpawningRipple warpSpawningRipple = self.warpSpawningRipple;
                        staticSoundLoop.pitch = Custom.LerpMap((warpSpawningRipple != null) ? warpSpawningRipple.ringScale : 1f, 1f, 0f, 1f, self.KarmaIsReinforced ? 0.1f : 1.8f);
                    }
                    else if (!dynamicWarpInput && (float)self.activateCamoTimer > 0f)
                    {
                        int num4 = (self.camoDirectionSoundID == WatcherEnums.WatcherSoundID.Player_Deactivating_Camo_LOOP) ? self.exitOutOfCamoDuration : self.enterIntoCamoDuration;
                        if (self.camoDirectionSoundID == WatcherEnums.WatcherSoundID.Player_Deactivating_Camo_LOOP)
                        {
                            self.watcherAbilitySoundLoop.pitch = 0.7f;
                        }
                        if (self.activateCamoTimer <= num4)
                        {
                            self.watcherAbilitySoundLoop.volume = Mathf.Lerp(0.2f, 1f, Mathf.InverseLerp(0f, (float)num4, (float)self.activateCamoTimer));
                        }
                        else
                        {
                            self.watcherAbilitySoundLoop.volume = Mathf.Lerp(self.watcherAbilitySoundLoop.volume, 0f, 0.04f);
                        }
                    }
                    else
                    {
                        self.watcherAbilitySoundLoop.volume = 1f;
                    }
                }
            }
        }

        private static void LatcherCamoUpdate(On.Player.orig_CamoUpdate orig, Player self)
        {
            float lastTrailPaletteAmount = self.rippleData.trailPaletteAmount;
            orig(self);
            if (!IsPlayerLatcher(self)) return;

            if (self.isCamo)
                self.camoCharge = Mathf.Min(self.camoCharge + (LatcherMusicbox.playerSlowRatio - 1f), self.usableCamoLimit);
            // Additional penalty with slowed down game

            if (self.rippleData != null)
            {
                // Reverse camo effect
                if (self.isCamo)
                    self.rippleData.trailPaletteAmount = Mathf.Lerp(lastTrailPaletteAmount, 1f, LatcherMusicbox.playerSlowRatio * 0.09f);
                else
                    self.rippleData.trailPaletteAmount = Mathf.Lerp(lastTrailPaletteAmount, 0f, 0.003f);
            }

            bool isLatcherRipple = LatcherMusicbox.IsLatcherRipple;
            self.abstractPhysicalObject.rippleBothSides = self.abstractPhysicalObject.rippleLayer == 1;

            if (isLatcherRipple)
                foreach (var grabber in self.grabbedBy) grabber?.Release();
        }

        #region MenuScenes

        private static void ReplaceIllust(MenuScene scene, string sceneFolder, string flatImage, string layerImageOrig, string layerImage, Vector2 layerPos, MenuDepthIllustration.MenuShader shader = null)
            => SelectMenuPatch.ReplaceIllust(scene, sceneFolder, flatImage, layerImageOrig, layerImage, layerPos, shader ?? MenuDepthIllustration.MenuShader.Normal);

        private static void ReplaceCrossfade(MenuScene scene, string sceneFolder, string layerImageOrig, string layerImage, Vector2 layerPos, MenuDepthIllustration.MenuShader shader = null)
        {
            if (scene.flatMode) return;

            foreach (var pair in scene.crossFades)
            {
                var list = pair.Value;
                for (int i = 0; i < list.Count; ++i)
                {
                    if (!string.Equals(list[i].fileName, layerImageOrig)) continue;

                    float depth = list[i].depth;
                    list[i].RemoveSprites();
                    list[i] = null;

                    list[i] =
                        new MenuDepthIllustration(scene.page.menu, scene, sceneFolder, layerImage, layerPos, depth, shader ?? MenuDepthIllustration.MenuShader.Basic);
                    int n = pair.Key;
                    if (n < scene.depthIllustrations.Count - 1)
                    {
                        for (int t = n + 1; t < scene.depthIllustrations.Count; ++t)
                        {
                            if (scene.depthIllustrations[t].sprite == null || !scene.depthIllustrations[t].spriteAdded) continue;
                            list[i].sprite.MoveBehindOtherNode(scene.depthIllustrations[t].sprite);
                            break;
                        }
                    }
                    list[i].setAlpha = 0f;
                    Debug.Log($"Replaced Crossfade {n}-{i}: [{layerImage}] <- [{layerImageOrig}]");
                    return;
                }
            }
        }

        private static void ReplaceFlatIllust(MenuScene scene, string sceneFolder, string flatImage, string flatImageOrig)
        {
            int i = 0;
            for (; i < scene.flatIllustrations.Count; ++i)
                if (string.Equals(scene.flatIllustrations[i].fileName, flatImageOrig, StringComparison.InvariantCultureIgnoreCase)) break;
            if (i >= scene.flatIllustrations.Count)
            {
                var B = new System.Text.StringBuilder();
                B.AppendLine($"layerImage [{flatImageOrig}] is not in these ({i}/{scene.flatIllustrations.Count}):");
                for (i = 0; i < scene.flatIllustrations.Count; ++i)
                    B.AppendLine($"{i}: [{scene.flatIllustrations[i].fileName}] == [{flatImageOrig}] ? {string.Equals(scene.flatIllustrations[i].fileName, flatImageOrig, StringComparison.InvariantCultureIgnoreCase)}");
                Debug.LogError(B.ToString());
                throw new ArgumentOutOfRangeException(flatImageOrig, B.ToString());
            }
            scene.flatIllustrations[i].RemoveSprites();
            scene.flatIllustrations[i] = null;
            // LancerPlugin.LogSource.LogMessage($"({i}/{scene.depthIllustrations.Count}) replaced to {layerImage}");
            scene.flatIllustrations[i] =
                new MenuIllustration(scene.page.menu, scene, sceneFolder, flatImage, new Vector2(683f, 384f), false, true);
            if (i < scene.flatIllustrations.Count - 1)
                scene.flatIllustrations[i].sprite.MoveBehindOtherNode(scene.flatIllustrations[i + 1].sprite);
            scene.subObjects.Add(scene.flatIllustrations[i]);
            if (scene.useFlatCrossfades)
                scene.flatIllustrations[i].alpha = i > 0 ? 0 : 1;
            Debug.Log($"Replaced Illust {i}: [{flatImage}] <- [{flatImageOrig}]");
        }

        private static void BuildLatcherRippleSleepScene(On.Menu.MenuScene.orig_BuildRippleSleepScene orig,
            MenuScene self, bool playerDied)
        {
            string sceneFolder = $"Scenes{Path.DirectorySeparatorChar}ripple screen - latcher";
            orig(self, playerDied);
            if (!IsStoryLancer) return;
            if (self.flatMode)
            {
                ReplaceFlatIllust(self, sceneFolder, "ripple - flat - latcher", "ripple - flat - watcher");
                ReplaceFlatIllust(self, sceneFolder, "ripple - flat - latcher - b", "ripple - flat - watcher - b");
                return;
            }
            ReplaceIllust(self, sceneFolder, null, "ripple - 1", "ripple latcher - 1", new Vector2(434f, 156f), MenuDepthIllustration.MenuShader.Basic);
            ReplaceIllust(self, sceneFolder, null, "ripple - 1b", "ripple latcher - 1b", new Vector2(435f, 155f), MenuDepthIllustration.MenuShader.Basic);
        }

        private static void BuildLatcherSleepScreen(On.Menu.MenuScene.orig_BuildWatcherSleepScreen orig,
            MenuScene self)
        {
            orig(self);
            if (!IsStoryLancer) return;
            string sceneFolder = $"Scenes{Path.DirectorySeparatorChar}sleep screen - latcher";

            #region GetIndex

            float rippleLevel = 0f;
            if (self.menu.manager.rainWorld.progression.IsThereASavedGame(GetLancer(WatcherName.Watcher)))
            {
                if (self.menu.manager.rainWorld.progression.currentSaveState != null && self.menu.manager.rainWorld.progression.currentSaveState.saveStateNumber == GetLancer(WatcherName.Watcher))
                {
                    rippleLevel = self.menu.manager.rainWorld.progression.currentSaveState.deathPersistentSaveData.rippleLevel;
                }
                else if (self.menu.manager.rainWorld.progression.HasSaveData)
                {
                    string[] progLinesFromMemory = self.menu.manager.rainWorld.progression.GetProgLinesFromMemory();
                    if (progLinesFromMemory.Length != 0)
                    {
                        for (int i = 0; i < progLinesFromMemory.Length; i++)
                        {
                            string[] array = Regex.Split(progLinesFromMemory[i], "<progDivB>");
                            if (array.Length == 2 && array[0] == "SAVE STATE" && BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) == GetLancer(WatcherName.Watcher))
                            {
                                var miner = new List<SaveStateMiner.Target>
                                {
                                    new SaveStateMiner.Target(">RIPPLELEVEL", "<dpB>", "<dpA>", 20)
                                };
                                var mineResult = SaveStateMiner.Mine(self.menu.manager.rainWorld, array[1], miner);
                                for (int j = 0; j < mineResult.Count; j++)
                                {
                                    string name = mineResult[j].name;
                                    if (name != null && name == ">RIPPLELEVEL")
                                    {
                                        try
                                        {
                                            rippleLevel = float.Parse(mineResult[j].data, NumberStyles.Any, CultureInfo.InvariantCulture);
                                        }
                                        catch
                                        {
                                            Custom.LogWarning(new string[]
                                            {
                                                "failed to assign ripple level. Data:",
                                                mineResult[j].data
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            string postfix = "C";
            if (rippleLevel < 0.25f) postfix = "A";
            else if (rippleLevel < 0.5f) postfix = "B";

            #endregion GetIndex

            postfix = postfix.ToLower();
            ReplaceIllust(self, sceneFolder,
                $"sleep screen - latcher - flat - {postfix}",
                $"sleep - 2{postfix} - watcher",
                $"sleep - 2{postfix} - latcher",
                new Vector2(800f, 81f), MenuDepthIllustration.MenuShader.Basic);
        }

        private static void BuildLatcherVoidBathScene(On.Menu.MenuScene.orig_BuildVoidBathScene orig,
            MenuScene self, int index)
        {
            orig(self, index);
            if (!IsStoryLancer) return;
            string sceneFolder = $"Scenes{Path.DirectorySeparatorChar}outro void bathl {index}";

            switch (index)
            {
                case 1:
                    ReplaceIllust(self, sceneFolder, "outro void bath 1 - flat_L",
                        "outro void bath 1 slugcat - 1", "outro void bath 1 slugcat - 1_L",
                        new Vector2(501f, -93f), MenuDepthIllustration.MenuShader.Normal);
                    break;

                case 2:
                    ReplaceIllust(self, sceneFolder, null,
                        "outro void bath 2 slugcat watching - 1", "outro void bath 2 slugcat watching - 1_L",
                        new Vector2(488f, -36f), MenuDepthIllustration.MenuShader.Normal);
                    if (!self.flatMode) break;
                    ReplaceFlatIllust(self, sceneFolder, "outro void bath 2 - flat_L", "outro void bath 2 - flat");
                    ReplaceFlatIllust(self, sceneFolder, "outro void bath 2 - flat - b_L", "outro void bath 2 - flat - b");
                    break;

                case 3:
                    ReplaceIllust(self, sceneFolder, null,
                        "outro void bath 3 watcher watching - 2", "outro void bath 3 watcher watching - 2_L",
                        new Vector2(360f, 118f), MenuDepthIllustration.MenuShader.Normal);
                    if (!self.flatMode) break;
                    ReplaceFlatIllust(self, sceneFolder, "outro void bath 3 - flat_L", "outro void bath 3 - flat");
                    ReplaceFlatIllust(self, sceneFolder, "outro void bath 3 - flat - b_L", "outro void bath 3 - flat - b");
                    break;
            }
        }

        private static void BuildLatcherSpinningTopEndingScene(On.Menu.MenuScene.orig_BuildSpinningTopEndingScene orig,
            MenuScene self, int index)
        {
            orig(self, index);
            if (!IsStoryLancer) return;
            string sceneFolder = $"Scenes{Path.DirectorySeparatorChar}outro spinning topl {index}";

            switch (index)
            {
                case 1:
                    ReplaceIllust(self, sceneFolder, "outro spinning top 1 - flat_L",
                        "outro spinning top 1 watcher toypicking - 1", "outro spinning top 1 watcher toypicking - 1_L",
                        new Vector2(13f, -81f), MenuDepthIllustration.MenuShader.Normal);
                    break;

                case 2:
                    ReplaceIllust(self, sceneFolder, "outro spinning top 2 - flat_L",
                        "outro spinning top 2 watcher playing - 3", "outro spinning top 2 watcher playing - 3_L",
                        new Vector2(25f, 4f), MenuDepthIllustration.MenuShader.Normal);
                    break;

                case 3:
                    ReplaceIllust(self, sceneFolder, "outro spinning top 3 - flat_L",
                        "outro spinning top 3 watcher alone - 3", "outro spinning top 3 watcher alone - 3_L",
                        new Vector2(126f, -1f), MenuDepthIllustration.MenuShader.LightEdges);
                    break;
            }
        }

        private static void BuildLatcherPrinceEndingScene(On.Menu.MenuScene.orig_BuildPrinceEndingScene orig,
            MenuScene self, int index)
        {
            orig(self, index);
            if (!IsStoryLancer) return;
            string sceneFolder = $"Scenes{Path.DirectorySeparatorChar}outro princel {index}";

            switch (index)
            {
                case 5:
                    if (self.flatMode)
                    {
                        ReplaceFlatIllust(self, sceneFolder, "outro prince 5 - flat_L", "outro prince 5 - flat");
                        ReplaceFlatIllust(self, sceneFolder, "outro prince 5-1 - flat_L", "outro prince 5-1 - flat");
                        ReplaceFlatIllust(self, sceneFolder, "outro prince 5-2 - flat_L", "outro prince 5-2 - flat");
                    }
                    else
                    {
                        ReplaceIllust(self, sceneFolder, null,
                            "outro prince 5 slugcat overseeing - 1", "outro prince 5 slugcat overseeing - 1_L",
                            new Vector2(321f, -73f), MenuDepthIllustration.MenuShader.Basic);
                        ReplaceCrossfade(self, sceneFolder,
                            "outro prince 5 slugcat overseeing - 1b", "outro prince 5 slugcat overseeing - 1b_L",
                            new Vector2(321f, -73f), MenuDepthIllustration.MenuShader.Basic);
                        ReplaceCrossfade(self, sceneFolder,
                            "outro prince 5 slugcat overseeing - 1c", "outro prince 5 slugcat overseeing - 1c_L",
                            new Vector2(321f, -73f), MenuDepthIllustration.MenuShader.Basic);
                    }
                    break;

                case 6:
                    ReplaceIllust(self, sceneFolder, "outro prince 6 - flat_L",
                        "outro prince 6 watcher watching - 2", "outro prince 6 watcher watching - 2_L",
                        new Vector2(81f, -56f), MenuDepthIllustration.MenuShader.Normal);
                    break;
            }
        }

        #endregion MenuScenes
    }
}

#endif