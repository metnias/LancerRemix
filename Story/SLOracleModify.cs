using LancerRemix.Cat;
using RWCustom;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static CatSub.Story.SaveManager;
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
            On.SLOracleBehaviorHasMark.NameForPlayer += NameForLancer;
            On.SLOracleBehaviorHasMark.MoonConversation.AddEvents += AddLancerEvents;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;
        private const string REDALREADYSUCCEED = "RedAlreadyDeliveredPayload";

        private static void LancerMoonState(On.SLOrcacleState.orig_ForceResetState orig, SLOrcacleState self, SlugName saveStateNumber)
        {
            orig(self, saveStateNumber);
            var basis = saveStateNumber;
            if (IsLancer(basis)) basis = GetBasis(basis);
            var lancer = GetLancer(basis);
            var story = IsStoryLancer ? lancer : basis;

            if (IsTimelineInbetween(story, ModManager.MSC ? MSCName.Spear : null, SlugName.Red))
                self.neuronsLeft = 0; // dead after spear and before red
            else if (IsTimelineInbetween(story, SlugName.Red, SlugName.White))
            {
                self.neuronsLeft = TryMineRedData(); // dead if red has not succeed and before white
                SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, REDALREADYSUCCEED, self.neuronsLeft > 0);
            }
            else if (ModManager.MSC && IsTimelineInbetween(story, MSCName.Rivulet, null)) // after riv
            {
                self.neuronsLeft = 7;
            }

            int TryMineRedData()
            {
                var progLines = Custom.rainWorld.progression?.GetProgLinesFromMemory();
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
            if (self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Portuguese && (name == "friend" || name == "creature"))
            {
                string porName = self.Translate(name);
                if (porName.StartsWith(PREFIX)) porName = porName.Substring(PREFIX.Length);
                if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
                    porName = char.ToUpper(porName[0]).ToString() + porName.Substring(1);
                return porName;
            }
            string transName = self.Translate(name);
            if (transName.StartsWith(PREFIX)) transName = transName.Substring(PREFIX.Length);
            string little = self.Translate(PREFIX + "tiny");
            if (little.StartsWith(PREFIX)) little = little.Substring(PREFIX.Length);
            if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
                little = char.ToUpper(little[0]).ToString() + little.Substring(1);
            return little + (damaged ? "... " : " ") + transName;
        }

        private static void AddLancerEvents(On.SLOracleBehaviorHasMark.MoonConversation.orig_AddEvents orig, SLOracleBehaviorHasMark.MoonConversation self)
        {
            if (!IsStoryLancer) goto NoLancer;
            var basis = self.currentSaveFile;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red && basis != SlugName.White && basis != SlugName.Yellow) goto NoLancer;

            Debug.Log($"Lancer {self.id} {self.State.neuronsLeft}");
            var slBehavior = self.myBehavior as SLOracleBehaviorHasMark;

            #region Lurvivor

            // Lonk cannot talk with moon
            if (self.id == ConvID.MoonFirstPostMarkConversation)
            {
                switch (self.State.neuronsLeft)
                {
                    case 1:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        break;

                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Get... get away... small.... thing."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please... thiss all I have left."), 10));
                        break;

                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You!"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 60, self.Translate("...you ate... me. Please go away. I won't speak... to you.<LINE>I... CAN'T speak to you... because... you ate...me..."), 0));
                        break;

                    case 4:
                        self.LoadEventsFromFile(35);
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I see that someone has given you the gift of communication.<LINE>Must have been Five Pebbles, as you don't look like you can travel very far at all..."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("He's sick, if you haven't noticed. Being corrupted from the inside by his own experiments. Maybe they all are by now, who knows.<LINE>We weren't designed to transcend and it drives us mad."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is good to have someone to talk to even if that's a child like you.<LINE>My last visitor stopped coming here many cycles ago, and<LINE>here I was about to get used to its visits."), 0));
                        break;

                    default:
                    case 5:
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Hello <PlayerName>. Are you lost?"), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I am sorry to say that there is nothing here for you."), 0));
                        if (self.State.playerEncounters > 0 && self.State.playerEncountersWithMark == 0)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Perhaps... I saw you before?"), 0));
                        }
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You must be very brave to have made it all the way here. But I'm sorry to say your journey here is in vain."), 5));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("As you can see, I have nothing for you. Not even my memories."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Or did I say that already?"), 5));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I see that someone has given you the gift of communication.<LINE>Must have been Five Pebbles, as you don't look like you can travel very far at all..."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("He's sick, if you haven't noticed. Being corrupted from the inside by his own experiments. Maybe they all are by now, who knows.<LINE>We weren't designed to transcend and it drives us mad."), 0));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("It is good to have someone to talk to even if that's a child like you.<LINE>My last visitor stopped coming here many cycles ago, and<LINE>here I was about to get used to its visits."), 0));
                        break;
                }
                return;
            }
            if (self.id == ConvID.MoonSecondPostMarkConversation)
            {
                switch (self.State.neuronsLeft)
                {
                    case 1:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        break;

                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 80, self.Translate("...leave..."), 10));
                        break;

                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("You..."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Please don't... take... more from me... Go."), 0));
                        break;

                    case 4:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Oh. You."), 0));
                        }
                        else
                        {
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Hello there! You again!"), 0));
                            }
                            else
                            {
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Hello there. You again!"), 0));
                            }
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
                            if (self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes && (!ModManager.MSC || IsTimelineInbetween(GetLancer(basis), MSCName.Rivulet))) // after riv
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I have had scavengers come by before. Scavengers!<LINE>And they left me alive!<LINE>But... I have told you that already, haven't I?"), 0));
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You must excuse me if I repeat myself. My memory is bad.<LINE>I used to have a pathetic five neurons... And then you ate one.<LINE>Maybe I've told you that before as well."), 0));
                            }
                        }
                        break;

                    default:
                    case 5:
                        if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You again."), 10));
                        }
                        else
                        {
                            if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, hello!"), 10));
                            }
                            else
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Oh, hello."), 10));
                            }
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I wonder what it is that you want?"), 0));
                            if (self.State.GetOpinion != SLOrcacleState.PlayerOpinion.Dislikes)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("There is nothing here. Not even my memories remain."), 0));
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Even the scavengers that come here from time to time leave with nothing. But... I have told you that already, haven't I?"), 0));
                                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                                {
                                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I do enjoy the company though. You're welcome to stay a while, quiet petite thing."), 5));
                                }
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
                    self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("You... Strange thing. Now this?"), 10));
                    self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("I will accept your gift..."), 10));
                }
                int num = self.State.neuronsLeft - 1;
                switch (num + 1)
                {
                    case 0:
                    case 1:
                        break;

                    case 2:
                        self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("You!"), 10));
                        self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("...you...killed..."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, "...", 10));
                        self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("...me"), 10));
                        break;

                    case 3:
                        self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("...thank you... better..."), 10));
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("still, very... bad."), 10));
                        break;

                    case 4:
                        self.events.Add(new Conversation.TextEvent(self, 20, self.Translate("Thank you... That is a little better. Thank you, creature."), 10));
                        if (!slBehavior.respondToNeuronFromNoSpeakMode)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Maybe this is asking too much for a child like you but... could you bring me another one?"), 0));
                        }
                        break;

                    default:
                        if (slBehavior.respondToNeuronFromNoSpeakMode)
                        {
                            self.events.Add(new Conversation.TextEvent(self, 0, self.Translate("Thank you. I do wonder what you want."), 10));
                        }
                        else
                        {
                            if (self.State.neuronGiveConversationCounter == 0)
                            {
                                Debug.Log("moon recieve first neuron. Has neurons: " + self.State.neuronsLeft);
                                if (self.State.neuronsLeft == 5)
                                {
                                    self.LoadEventsFromFile(45);
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, LancerMod.oi.Translate("After all this time, a lifeline. Thank you, tiny creature."), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("I'll never feel the power I once had, but this is something to sustain an old soul."), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("I could read a bit of Five Pebbles in this neuron before formatting it.<LINE>A ghost left from his processing routines."), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("Erratic... Pulse. \"Erratic Pulse.\" I wonder what that means."), 0));
                                }
                                else
                                {
                                    self.LoadEventsFromFile(19);
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("I am grateful - the relief is indescribable!"), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, "...", 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("I could read a bit of Five Pebbles in this neuron before formatting it. His condition has severely deteriorated since last I<LINE>heard of him. The frustration he feels is profound, and that angst has seeped into every part of him, every neuron. "), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("We were supposed to help everyone, you know. Everything. That was our purpose: a great gift to the lesser beings of the world. <LINE>When facing our inability to do so, we all reacted differently. Many with madness."), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("But even back when we were all more or less connected there were those who reacted to their task with anger.<LINE>I can only imagine they are angrier now, alone in their cans, left only with their insatiable drive. "), 0));
                                    //instance.events.Add(new Conversation.TextEvent(instance, 0, instance.Translate("But to be honest, I don't know how many of us are still alive at this point."), 0));
                                }
                            }
                            else if (self.State.neuronGiveConversationCounter == 1)
                            {
                                self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("You get these at Five Pebbles'?<LINE>Thank you so much. I'm sure he won't mind."), 10));
                                self.events.Add(new Conversation.TextEvent(self, 10, "...", 0));
                                self.events.Add(new Conversation.TextEvent(self, 10, self.Translate("Or actually I'm sure he would, but he has so many of these~<LINE>it doesn't do him any difference.<LINE>For me though, it does! Thank you, tiny creature!"), 0));
                            }
                            else
                            {
                                switch (UnityEngine.Random.Range(0, 4))
                                {
                                    case 0:
                                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you, again. I feel wonderful."), 10));
                                        break;

                                    case 1:
                                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you so very much!"), 10));
                                        break;

                                    case 2:
                                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("It is strange... I'm remembering myself, but also... him."), 10));
                                        break;

                                    default:
                                        self.events.Add(new Conversation.TextEvent(self, 30, self.Translate("Thank you tiny creature... Sincerely."), 10));
                                        break;
                                }
                            }
                            self.State.neuronGiveConversationCounter++;
                        }
                        break;
                }
                slBehavior.respondToNeuronFromNoSpeakMode = false;
                return;
            }

            #endregion Lurvivor

            #region Lunter

            bool already = GetProgValue<bool>(Custom.rainWorld.progression?.currentSaveState.miscWorldSaveData, REDALREADYSUCCEED);
            if (self.id == ConvID.Moon_Red_First_Conversation)
            {
                if (already)
                    self.LoadEventsFromFile(49, GetLancer(basis), false, 0);
                else
                    self.LoadEventsFromFile(50, GetLancer(basis), false, 0);
                return;
            }
            if (self.id == ConvID.Moon_Red_Second_Conversation)
            {
                if (already)
                    self.LoadEventsFromFile(54, GetLancer(basis), false, 0);
                else
                    self.LoadEventsFromFile(55, GetLancer(basis), false, 0);
                return;
            }
            if (self.id == ConvID.Moon_Pearl_Red_stomach)
            {
                self.PearlIntro();
                self.LoadEventsFromFile(51, GetLancer(basis), false, 0);
                // add remarks depending on red success
                if (already)
                    self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                else
                    self.events.Add(new Conversation.TextEvent(self, 40, "...", 10));
                return;
            }

            #endregion Lunter

            orig.Invoke(self);

            return;
        NoLancer: orig.Invoke(self);
        }
    }
}