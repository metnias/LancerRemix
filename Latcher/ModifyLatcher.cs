using MonoMod.RuntimeDetour;
using RWCustom;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using Watcher;
using static LancerRemix.Cat.ModifyCat;
using static LancerRemix.LancerEnums;
using WatcherName = Watcher.WatcherEnums.SlugcatStatsName;
using Random = UnityEngine.Random;
using Menu;

namespace LancerRemix.Latcher
{
    internal static class ModifyLatcher
    {
        internal static void SubPatch()
        {
            hooks.Clear();
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
                typeof(ModifyLatcher).GetMethod(nameof(LocustCamoFactor), BindingFlags.Static | BindingFlags.NonPublic)
            ));

            On.Player.WatcherUpdate += LatcherUpdate;
            On.Player.CamoUpdate += LatcherCamoUpdate;

            LatcherPatch.OnWatcherEnableSubPatch();
            LatcherTutorial.OnWatcherEnableSubPatch();
        }

        internal static void OnWatcherDisablePatch()
        {
            foreach (var hook in hooks) hook.Undo();
            hooks.Clear();

            On.Player.WatcherUpdate -= LatcherUpdate;
            On.Player.CamoUpdate -= LatcherCamoUpdate;

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

        private delegate float orig_LocustCamoFactor(LocustSystem.Swarm self);

        private static float LocustCamoFactor(orig_LocustCamoFactor orig, LocustSystem.Swarm self)
        {
            var res = orig(self);
            if (res > 0f && self.target is Player player && IsPlayerLatcher(player))
                return 0f; // No camo for Latcher
            return res;
        }

        #endregion Properties

        private static void LatcherUpdate(On.Player.orig_WatcherUpdate orig, Player self)
        {
            if (IsPlayerLatcher(self))
            {
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

            orig(self);
        }

        private static void LatcherCamoUpdate(On.Player.orig_CamoUpdate orig, Player self)
        {
            float lastTrailPaletteAmount = self.rippleData.trailPaletteAmount;
            orig(self);
            if (!IsPlayerLatcher(self)) return;

            if (self.isCamo)
                self.camoCharge = Mathf.Min(self.camoCharge + (LatcherMusicbox.playerSlowRatio - 1f), self.usableCamoLimit);
            // Additional penalty with slowed down game
            // TODO: I should adjust the camoLimit itself later

            if (self.rippleData != null)
            {
                // Reverse camo effect
                if (self.isCamo)
                    self.rippleData.trailPaletteAmount = Mathf.Lerp(lastTrailPaletteAmount, 1f, LatcherMusicbox.playerSlowRatio * 0.09f);
                else
                    self.rippleData.trailPaletteAmount = Mathf.Lerp(lastTrailPaletteAmount, 0f, 0.003f);
            }
            // No Ripple Layer
            self.ChangeRippleLayer(0);
            // TEMP; this *works* but has lots of visual glitches
            if (self.isCamo && self.rippleLevel >= 5.0f)
                Shader.DisableKeyword("RIPPLE");
            else
                Shader.EnableKeyword("RIPPLE");

            if (LatcherMusicbox.IsLatcherRipple)
                foreach (var grabber in self.grabbedBy) grabber?.Release();
        }
    }
}