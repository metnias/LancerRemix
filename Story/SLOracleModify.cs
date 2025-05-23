﻿using LancerRemix.Cat;
using RWCustom;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static CatSub.Story.SaveManager;
using static Conversation;
using static LancerRemix.LancerEnums;
using static LancerRemix.LancerGenerator;
using ConvID = Conversation.ID;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal static class SLOracleModify
    {
        internal static void SubPatch()
        {
            On.SLOrcacleState.ForceResetState += LancerMoonState;
            On.SLOracleBehavior.Update += LancerMoonUpdatePatch;
            On.SLOracleBehaviorHasMark.NameForPlayer += NameForLancer;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += AddLancerEvents;
            On.SLOracleBehaviorHasMark.SpecialEvent += LancerSpecEvents;

            On.OverseerAbstractAI.PlayerGuideUpdate += LunterRemoveDupeOverseer;
            On.OverseerAbstractAI.Roam += LunterOverseerStayNearMoon;
            On.OverseerAI.Update += LunterOverseerLookAtMoon;
            On.SLOracleBehaviorHasMark.Update += LunterMoonLookHandler;

            On.Oracle.OracleArm.BaseDir += LonkSLOracleArmDir;
            On.Oracle.OracleArm.OnFramePos += LonkSLOracleArmPos;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;
        private const string REDALREADYSUCCEED = "RedAlreadyDeliveredPayload";

        private static void LancerMoonState(On.SLOrcacleState.orig_ForceResetState orig, SLOrcacleState self, SlugName saveStateNumber)
        {
            orig(self, saveStateNumber);
            var basis = GetBasis(saveStateNumber);
            var lancer = GetLancer(basis);
            var story = IsStoryLancer ? lancer : basis;

            if (IsTimelineInbetween(story, ModManager.MSC ? MSCName.Spear : null, SlugName.Red))
                self.neuronsLeft = 0; // dead after spear and before red
            else if (IsTimelineInbetween(story, SlugName.Red, SlugName.White) && Custom.rainWorld.progression?.currentSaveState != null)
            {
                self.neuronsLeft = TryMineRedData(); // dead if red has not succeed and before white
                if (basis == SlugName.Red && story == GetLancer(SlugName.Red))
                {
                    SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, REDALREADYSUCCEED, self.neuronsLeft > 0);
                    SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, SSOracleModify.LUNTERTOOKNSHKEYBACK, false);
                }
            }
            else if (ModManager.MSC && IsTimelineInbetween(story, MSCName.Rivulet, null)) // after riv
            {
                self.neuronsLeft = 7;
            }

            int TryMineRedData()
            {
                var progLines = Custom.rainWorld.progression.GetProgLinesFromMemory();
                if (progLines == null || progLines.Length == 0) return 0;
                for (int i = 0; i < progLines.Length; ++i)
                {
                    var array = Regex.Split(progLines[i], "<progDivB>");
                    if (array.Length != 2 || array[0] != "SAVE STATE" || BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) != SlugName.Red) continue;

                    const string MOONREVIVED = ">MOONREVIVED";
                    var mineTarget = new List<SaveStateMiner.Target>()
                    { new SaveStateMiner.Target(MOONREVIVED, null, "<mwA>", 20) };
                    var mineResult = SaveStateMiner.Mine(Custom.rainWorld, array[1], mineTarget);
                    if (mineResult.Count > 0 && mineResult[0].name == MOONREVIVED) return 5;
                    return 0;
                }
                return 0;
            }
        }

        private static void LancerMoonUpdatePatch(On.SLOracleBehavior.orig_Update orig, SLOracleBehavior self, bool eu)
        {
            orig(self, eu);
            if (!self.oracle.room.game.IsStorySession || !IsStoryLancer) return;
            if (self.player?.room != self.oracle.room || !self.hasNoticedPlayer) return;

            var basis = GetBasis(self.oracle.room.game.StoryCharacter);
            if (basis == SlugName.Red)
                LunterMoonBehaviourUpdate(self);
        }

        private static string NameForLancer(On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized)
        {
            if (!IsStoryLancer && self.player?.playerState.isPup != true) return orig(self, capitalized);
            const string PREFIX = "lancersl-";
            string name = PREFIX + "animal";
            bool damaged = self.DamagedMode && Random.value < 0.5f;
            if (Random.value > 0.3f)
            {
                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                {
                    if (self.State.totalPearlsBrought > 5 && !self.DamagedMode)
                        name = PREFIX + "student";
                    else
                        name = PREFIX + "child";
                }
                else if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    name = PREFIX + "imp";
                else
                    name = "creature";
            }
            var IGT = self.oracle.room.game.rainWorld.inGameTranslator;
            if (IGT.currentLanguage == InGameTranslator.LanguageID.Portuguese && (name == "friend" || name == "creature"))
            {
                string porName = IGT.Translate(name);
                if (porName.StartsWith(PREFIX)) porName = porName.Substring(PREFIX.Length);
                if (capitalized && InGameTranslator.LanguageID.UsesCapitals(IGT.currentLanguage))
                    porName = char.ToUpper(porName[0]).ToString() + porName.Substring(1);
                return porName;
            }
            string transName = IGT.Translate(name);
            if (transName.StartsWith(PREFIX)) transName = transName.Substring(PREFIX.Length);
            string little = IGT.Translate(PREFIX + "tiny");
            if (little.StartsWith(PREFIX)) little = little.Substring(PREFIX.Length);
            if (capitalized && InGameTranslator.LanguageID.UsesCapitals(IGT.currentLanguage))
                little = char.ToUpper(little[0]).ToString() + little.Substring(1);

            return little + (damaged ? "... " : " ") + transName;
        }

        private static void AddLancerEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if (!IsStoryLancer) goto NoLancer;
            var basis = GetBasis(self.currentSaveFile);
            // if (basis != SlugName.Red && basis != SlugName.White && basis != SlugName.Yellow) goto NoLancer;

            Debug.Log($"Lancer {self.id} {self.State.neuronsLeft}");
            var slBehavior = self.myBehavior as SLOracleBehaviorHasMark;

            #region Lurvivor

            // Lonk cannot talk with moon
            if (self.id == ConvID.MoonFirstPostMarkConversation)
            {
                switch (self.State.neuronsLeft)
                {
                    case 1:
                        self.events.Add(new TextEvent(self, 40, "...", 10));
                        break;

                    case 2:
                        self.events.Add(new TextEvent(self, 30, self.Translate("Get... get away... small.... thing."), 10));
                        self.events.Add(new TextEvent(self, 0, self.Translate("Please... thiss all I have left."), 10));
                        break;

                    case 3:
                        self.events.Add(new TextEvent(self, 30, self.Translate("You!"), 10));
                        self.events.Add(new TextEvent(self, 60, self.Translate("...you ate... me. Please go away. I won't speak... to you.<LINE>I... CAN'T speak to you... because... you ate...me..."), 0));
                        break;

                    case 4:
                        self.LoadEventsFromFile(35);
                        self.events.Add(new TextEvent(self, 0, self.Translate("I see that someone has given you the gift of communication.<LINE>Must have been Five Pebbles, as you don't look like you can travel very far at all..."), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("He's sick, if you haven't noticed. Being corrupted from the inside by his own experiments. Maybe they all are by now, who knows.<LINE>We weren't designed to transcend and it drives us mad."), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("It is good to have someone to talk to even if that's a child like you.<LINE>My last visitor stopped coming here some time ago, and<LINE>here I was about to get used to their visits."), 0));
                        break;

                    default:
                    case 5:
                        self.events.Add(new TextEvent(self, 0, self.Translate("Hello <PlayerName>. Are you lost?"), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("I am sorry to say that there is nothing here for you."), 0));
                        if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0)
                            self.events.Add(new TextEvent(self, 0, self.Translate("Perhaps... I saw you before?"), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("You must be very brave to have made it all the way here. But I'm sorry to say your journey here is in vain."), 5));
                        self.events.Add(new TextEvent(self, 0, self.Translate("As you can see, I have nothing for you. Not even my memories."), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("Or did I say that already?"), 5));
                        self.events.Add(new TextEvent(self, 0, self.Translate("I see that someone has given you the gift of communication.<LINE>Must have been Five Pebbles, as you don't look like you can travel very far at all..."), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("He's sick, if you haven't noticed. Being corrupted from the inside by his own experiments. Maybe they all are by now, who knows.<LINE>We weren't designed to transcend and it drives us mad."), 0));
                        self.events.Add(new TextEvent(self, 0, self.Translate("It is good to have someone to talk to even if that's a child like you.<LINE>My last visitor stopped coming here many cycles ago, and<LINE>here I was about to get used to its visits."), 0));
                        break;
                }
                return;
            }
            if (self.id == ConvID.MoonSecondPostMarkConversation)
            {
                switch (self.State.neuronsLeft)
                {
                    case 1:
                        self.events.Add(new TextEvent(self, 40, "...", 10));
                        break;

                    case 2:
                        self.events.Add(new TextEvent(self, 80, self.Translate("...leave..."), 10));
                        break;

                    case 3:
                        self.events.Add(new TextEvent(self, 20, self.Translate("You..."), 10));
                        self.events.Add(new TextEvent(self, 0, self.Translate("Please don't... take... more from me... Go."), 0));
                        break;

                    case 4:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.events.Add(new TextEvent(self, 30, self.Translate("Oh. You."), 0));
                        }
                        else
                        {
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                                self.events.Add(new TextEvent(self, 30, self.Translate("Hello there! You again!"), 0));
                            else
                                self.events.Add(new TextEvent(self, 30, self.Translate("Hello there. You again!"), 0));
                            self.events.Add(new TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
                            if (self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes && (!ModManager.MSC || IsTimelineInbetween(GetLancer(basis), MSCName.Rivulet, null))) // after riv
                            {
                                self.events.Add(new TextEvent(self, 0, self.Translate("I have had another visitor from your specie before. And they left me alive!<LINE>But... I have told you that already, haven't I?"), 0));
                                self.events.Add(new TextEvent(self, 0, self.Translate("You must excuse me if I repeat myself. My memory is bad.<LINE>I used to have a pathetic five neurons... And then you ate one.<LINE>Maybe I've told you that before as well."), 0));
                            }
                        }
                        break;

                    default:
                    case 5:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.events.Add(new TextEvent(self, 0, self.Translate("You again."), 10));
                        }
                        else
                        {
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                                self.events.Add(new TextEvent(self, 0, self.Translate("Oh, hello!"), 10));
                            else
                                self.events.Add(new TextEvent(self, 0, self.Translate("Oh, hello."), 10));
                            self.events.Add(new TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
                            if (self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes)
                            {
                                self.events.Add(new TextEvent(self, 0, self.Translate("There is nothing here. Not even my memories remain."), 0));
                                self.events.Add(new TextEvent(self, 30, self.Translate("Even the last visitor that came here from time to time left with nothing. But... I have told you that already, haven't I?"), 0));
                                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                                    self.events.Add(new TextEvent(self, 0, self.Translate("I do enjoy the company though. You're welcome to stay a while, quiet petite thing."), 5));
                            }
                        }
                        break;
                }
                return;
            }
            if (self.id == ConvID.MoonRecieveSwarmer)
            {
                if (self.State.neuronsLeft - 1 > 2 && slBehavior.respondToNeuronFromNoSpeakMode)
                {
                    self.events.Add(new TextEvent(self, 10, self.Translate("You... Strange thing. Now this?"), 10));
                    self.events.Add(new TextEvent(self, 0, self.Translate("I will accept your gift..."), 10));
                }
                int num = self.State.neuronsLeft - 1;
                switch (num + 1)
                {
                    case 0:
                    case 1:
                    case 2:
                    case 3:
                        goto NoLancer;

                    case 4:
                        self.events.Add(new TextEvent(self, 20, self.Translate("Thank you... That is a little better. Thank you, creature."), 10));
                        if (!slBehavior.respondToNeuronFromNoSpeakMode)
                            self.events.Add(new TextEvent(self, 0, self.Translate("Maybe this is asking too much for a child like you but...<LINE>could you bring me another one?"), 0));
                        break;

                    default:
                        if (slBehavior.respondToNeuronFromNoSpeakMode)
                        { goto NoLancer; }
                        else
                        {
                            if (self.State.neuronGiveConversationCounter == 0)
                            { goto NoLancer; }
                            else if (self.State.neuronGiveConversationCounter == 1)
                            {
                                self.events.Add(new TextEvent(self, 30, self.Translate("You get these at Five Pebbles'?<LINE>Thank you so much. I'm sure he won't mind."), 10));
                                self.events.Add(new TextEvent(self, 10, "...", 0));
                                self.events.Add(new TextEvent(self, 10, self.Translate("Or actually I'm sure he would, but he has so many of these~<LINE>it doesn't do him any difference.<LINE>For me though, it does! Thank you, tiny creature!"), 0));
                            }
                            else
                            { goto NoLancer; }
                            self.State.neuronGiveConversationCounter++;
                        }
                        break;
                }
                slBehavior.respondToNeuronFromNoSpeakMode = false;
                return;
            }

            #endregion Lurvivor

            #region Lunter

            if (basis != SlugName.Red) goto NoLancer; // not Lunter
            bool already = GetProgValue<bool>(Custom.rainWorld.progression?.currentSaveState.miscWorldSaveData, REDALREADYSUCCEED);
            //bool nsh = GetProgValue<bool>(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, LUNTERNSHAWARE);
            if (self.id == ConvID.Moon_Red_First_Conversation)
            {
                if (already)
                    self.LoadEventsFromFile(250, GetLancer(basis), false, 0);
                else
                    self.LoadEventsFromFile(50, GetLancer(basis), false, 0);

                Custom.rainWorld.progression.currentSaveState.miscWorldSaveData.playerGuideState.InfluenceLike(-6000f, false);
                Custom.rainWorld.progression.currentSaveState.miscWorldSaveData.playerGuideState.increaseLikeOnSave = false;
                return;
            }
            if (self.id == ConvID.Moon_Red_Second_Conversation)
            {
                if (already)
                    self.LoadEventsFromFile(255, GetLancer(basis), false, 0);
                else
                    self.LoadEventsFromFile(55, GetLancer(basis), false, 0);
                return;
            }
            if (self.id == ConvID.Moon_Pearl_Red_stomach)
            {
                self.PearlIntro();
                self.LoadEventsFromFile(51, GetLancer(basis), false, 0);
                if (already)
                    self.events.Add(new TextEvent(self, 0, self.Translate("This much effort, just to help me... It means more than you could ever know."), 20));
                else
                    self.events.Add(new TextEvent(self, 0, self.Translate("All of this trouble just to save me... You are more generous than anyone I've known."), 20));
                self.events.Add(new TextEvent(self, 0, self.Translate("I am happy that I'm not alone."), 20));
                return;
            }
            if (self.id == MoonRecieveNSHSwarmer)
            {
                self.LoadEventsFromFile(251, GetLancer(basis), false, 0);
                return;
            }

            #endregion Lunter

            orig.Invoke(self);
            return;
        NoLancer: orig.Invoke(self);
        }

        private static void LancerSpecEvents(On.SLOracleBehaviorHasMark.orig_SpecialEvent orig, SLOracleBehaviorHasMark self, string eventName)
        {
            if (eventName.Equals("lunterlook", System.StringComparison.InvariantCultureIgnoreCase))
            {
                lookOverseer = !lookOverseer;
                Debug.Log($"Lunter Moon look at {(lookOverseer ? "overseer" : "player")} (LockOverseer: {lockedOverseer?.realizedCreature != null})");
                return;
            }
            orig(self, eventName);
        }

        #region Lunter

        private static AbstractCreature lockedOverseer = null;

        private static bool lookOverseer = false;

        private static NSHSwarmer reelInSwarmer = null;
        private static float swarmerReelIn = 0f;

        private static void LunterRemoveDupeOverseer(On.OverseerAbstractAI.orig_PlayerGuideUpdate orig, OverseerAbstractAI self, int time)
        {
            if (IsStoryLancer && self.world.game.IsStorySession && GetBasis(self.world.game.StoryCharacter) == SlugName.Red)
            {
                if (!self.spearmasterLockedOverseer)
                {
                    if (self.RelevantPlayer?.Room.name == "SL_AI" || self.world.game.GetStorySession.saveState.miscWorldSaveData.EverMetMoon)
                    {
                        self.PlayerGuideGoAway(40);
                        self.parent.Die();
                        return;
                    }
                }
                else if (self.RelevantPlayer?.Room.name != "SL_AI")
                {
                    self.PlayerGuideGoAway(40);
                    self.parent.Die();
                    return;
                }
            }
            orig(self, time);
        }

        private static void LunterOverseerStayNearMoon(On.OverseerAbstractAI.orig_Roam orig, OverseerAbstractAI self, int time, float chance)
        {
            if (IsStoryLancer && self.world.game.IsStorySession && GetBasis(self.world.game.StoryCharacter) == SlugName.Red
                && self.spearmasterLockedOverseer && self.RelevantPlayer?.Room.name == "SL_AI")
            {
                if (self.freezeStandardRoamingOnTheseFrames > 0) return;

                var worldCoordinate = new WorldCoordinate(self.world.GetAbstractRoom("SL_AI").index, Random.Range(57, 62), Random.Range(9, 32), -1);
                self.SetDestinationNoPathing(worldCoordinate, true);
                if (self.parent.realizedCreature != null)
                    (self.parent.realizedCreature as Overseer).ZipToPosition(new Vector2(worldCoordinate.x * 20f, worldCoordinate.y * 20f));

                Debug.Log($"Lunter NSHOverseer stay near moon: {worldCoordinate}");

                self.freezeStandardRoamingOnTheseFrames = 10000;
                return;
            }
            orig(self, time, chance);
        }

        private static void LunterOverseerLookAtMoon(On.OverseerAI.orig_Update orig, OverseerAI self)
        {
            orig(self);
            if (IsStoryLancer && self.worldAI.world.game.IsStorySession && GetBasis(self.worldAI.world.game.StoryCharacter) == SlugName.Red
                && (self.creature.abstractAI as OverseerAbstractAI).spearmasterLockedOverseer && self.creature.Room.name == "SL_AI")
            {
                for (int i = 0; i < self.overseer.room.physicalObjects.Length; i++)
                    for (int j = 0; j < self.overseer.room.physicalObjects[i].Count; j++)
                        if (self.overseer.room.physicalObjects[i][j] is Oracle)
                        {
                            self.lookAt = self.overseer.room.physicalObjects[i][j].firstChunk.pos;
                            return;
                        }
            }
        }

        private static void LunterMoonBehaviourUpdate(SLOracleBehavior self)
        {
            ReelInNSHSwarmer();

            if (self.holdingObject != null && self.holdingObject is NSHSwarmer)
                ConvertingNSHSwarmer();

            if (!self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.EverMetMoon)
                SummonNSHOverseer();

            void ReelInNSHSwarmer()
            {
                for (int j = 0; j < self.oracle.room.abstractRoom.entities.Count; j++)
                {
                    if (self.oracle.room.abstractRoom.entities[j] is AbstractPhysicalObject && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject != null && Custom.DistLess((self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject.firstChunk.pos, self.oracle.firstChunk.pos, 500f) && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject.grabbedBy.Count == 0 && (self.oracle.room.abstractRoom.entities[j] as AbstractPhysicalObject).realizedObject is NSHSwarmer nshSwarmer)
                    {
                        if (reelInSwarmer == null && self.holdingObject == null)
                            reelInSwarmer = nshSwarmer;
                    }
                }
                if (reelInSwarmer != null && self.holdingObject == null)
                {
                    swarmerReelIn = Mathf.Min(1f, swarmerReelIn + 0.016666668f);
                    reelInSwarmer.firstChunk.vel *= Custom.LerpMap(swarmerReelIn, 0.4f, 1f, 1f, 0.3f, 6f);
                    reelInSwarmer.firstChunk.vel += Custom.DirVec(reelInSwarmer.firstChunk.pos, self.oracle.firstChunk.pos) * 3.2f * swarmerReelIn;
                    if (Custom.DistLess(reelInSwarmer.firstChunk.pos, self.oracle.firstChunk.pos, 30f))
                    {
                        self.GrabObject(reelInSwarmer);
                        reelInSwarmer = null;
                    }
                }
                else
                    swarmerReelIn = 0f;
            }

            void ConvertingNSHSwarmer()
            {
                if (self.oracle.room.game.cameras[0].hud.dialogBox == null || self.oracle.room.game.cameras[0].hud.dialogBox.messages.Count < 1)
                {
                    ++self.convertSwarmerCounter;
                    if (self.convertSwarmerCounter > 40)
                    {
                        Vector2 pos = self.holdingObject.firstChunk.pos;
                        self.holdingObject.Destroy();
                        self.holdingObject = null;
                        self.oracle.room.game.GetStorySession.saveState.miscWorldSaveData.moonRevived = true;
                        SLOracleSwarmer sloracleSwarmer = new SLOracleSwarmer(new AbstractPhysicalObject(self.oracle.room.world, AbstractPhysicalObject.AbstractObjectType.SLOracleSwarmer, null, self.oracle.room.GetWorldCoordinate(pos), self.oracle.room.game.GetNewID()), self.oracle.room.world);
                        self.oracle.room.abstractRoom.entities.Add(sloracleSwarmer.abstractPhysicalObject);
                        sloracleSwarmer.firstChunk.HardSetPosition(pos);
                        self.oracle.room.AddObject(sloracleSwarmer);
                        self.ConvertingSSSwarmer();
                        if (self is SLOracleBehaviorHasMark behavior && behavior.currentConversation.id == ConvID.MoonRecieveSwarmer)
                            behavior.currentConversation = new SLOracleBehaviorHasMark.MoonConversation(MoonRecieveNSHSwarmer, behavior, SLOracleBehaviorHasMark.MiscItemType.NA);
                    }
                }
            }

            void SummonNSHOverseer()
            {
                while (lockedOverseer == null)
                {
                    Debug.Log("Lunter Moon try summoning NSH overseer");
                    var worldCoordinate = new WorldCoordinate(self.oracle.room.world.offScreenDen.index, -1, -1, 0);
                    lockedOverseer = new AbstractCreature(self.oracle.room.world, StaticWorld.GetCreatureTemplate(CreatureTemplate.Type.Overseer), null, worldCoordinate, new EntityID(-1, 5));
                    if (self.oracle.room.world.GetAbstractRoom(worldCoordinate).offScreenDen)
                        self.oracle.room.world.GetAbstractRoom(worldCoordinate).entitiesInDens.Add(lockedOverseer);
                    else
                        self.oracle.room.world.GetAbstractRoom(worldCoordinate).AddEntity(lockedOverseer);
                    lockedOverseer.ignoreCycle = true;
                    (lockedOverseer.abstractAI as OverseerAbstractAI).spearmasterLockedOverseer = true;
                    (lockedOverseer.abstractAI as OverseerAbstractAI).SetAsPlayerGuide(2);
                    (lockedOverseer.abstractAI as OverseerAbstractAI).BringToRoomAndGuidePlayer(self.oracle.room.abstractRoom.index);
                }
                lookOverseer = true;
            }
        }

        private static void LunterMoonLookHandler(On.SLOracleBehaviorHasMark.orig_Update orig, SLOracleBehaviorHasMark self, bool eu)
        {
            orig(self, eu);

            if (!self.oracle.room.game.IsStorySession || !IsStoryLancer) return;
            if (self.player?.room != self.oracle.room || !self.hasNoticedPlayer) return;

            if (GetBasis(self.oracle.room.game.StoryCharacter) != SlugName.Red) return;

            if (lookOverseer && lockedOverseer?.realizedCreature != null)
                self.lookPoint = lockedOverseer.realizedCreature.DangerPos;
        }

        #endregion Lunter

        #region Lonk

        internal static bool IsMoonComatose(SlugName storyCharacter)
            => IsStoryLancer && IsTimelineInbetween(GetLancer(storyCharacter), ModManager.MSC ? MSCName.Spear : null, SlugName.Red);

        private static Vector2 LonkSLOracleArmDir(On.Oracle.OracleArm.orig_BaseDir orig, Oracle.OracleArm self, float timeStacker)
        {
            if (self.oracle.room.game.IsStorySession && IsMoonComatose(self.oracle.room.game.StoryCharacter) && self.oracle.ID == Oracle.OracleID.SL)
                return Vector2.down;
            return orig(self, timeStacker);
        }

        private static Vector2 LonkSLOracleArmPos(On.Oracle.OracleArm.orig_OnFramePos orig, Oracle.OracleArm self, float timeStacker)
        {
            if (self.oracle.room.game.IsStorySession && IsMoonComatose(self.oracle.room.game.StoryCharacter) && self.oracle.ID == Oracle.OracleID.SL)
                return new Vector2(1670f, 605f);
            return orig(self, timeStacker);
        }

        #endregion Lonk
    }
}