using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using static CatSub.Story.SaveManager;
using static Conversation;
using static LancerRemix.LancerEnums;
using static LancerRemix.LancerGenerator;
using ConvID = Conversation.ID;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;
using SSAction = SSOracleBehavior.Action;

namespace LancerRemix.Story
{
    internal static class SSOracleModify
    {
        internal static void SubPatch()
        {
            On.SSOracleBehavior.Update += LonkKarmaCapOneStep;
            On.SSOracleBehavior.ThrowOutBehavior.Update += ThrowUpdatePatch;
            On.SSOracleBehavior.PebblesConversation.AddEvents += AddLancerEvents;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        internal const string LUNTERTOOKNSHKEYBACK = "LunterTookNSHKeyBack";

        private static void LonkKarmaCapOneStep(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            if (!IsStoryLancer) goto NoLonk;
            var basis = GetBasis(self.oracle.room.game.StoryCharacter);
            if (basis != SlugName.Yellow) goto NoLonk;
            if (self.inActionCounter == 299 && self.action == SSAction.General_GiveMark)
            {
                self.inActionCounter += 2;

                self.player.mainBodyChunk.vel += Custom.RNV() * 10f;
                self.player.bodyChunks[1].vel += Custom.RNV() * 10f;
                self.player.Stun(40);
                (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.theMark = true;
                self.oracle.room.game.GetStorySession.saveState.IncreaseKarmaCapOneStep();
                (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karma = (self.oracle.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.karmaCap;
                for (int l = 0; l < self.oracle.room.game.cameras.Length; l++)
                    self.oracle.room.game.cameras[l].hud.karmaMeter?.UpdateGraphic();
                for (int m = 0; m < 20; m++)
                    self.oracle.room.AddObject(new Spark(self.player.mainBodyChunk.pos, Custom.RNV() * UnityEngine.Random.value * 40f, new Color(1f, 1f, 1f), null, 30, 120));
                self.oracle.room.PlaySound(SoundID.SS_AI_Give_The_Mark_Boom, 0f, 1f, 1f);

                Debug.Log("Lonk receive one karma up");
                return;
            }
        NoLonk: orig.Invoke(self, eu);
        }

        private static void AddLancerEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if (!IsStoryLancer) goto NoLancer;

            if (self.id == ConvID.Pebbles_White || self.id == ConvID.Pebbles_Yellow)
            {
                var lancer = GetLancer(self.owner.oracle.room.game.StoryCharacter);
                if (IsTimelineInbetween(lancer, SlugName.Yellow, ModManager.MSC ? MSCName.Rivulet : null))
                {
                    // Hidden Lonk Dialogue
                    self.LoadEventsFromFile(48, GetLancer(SlugName.White), false, 0);
                    return;
                }
                bool preMove = IsTimelineInbetween(lancer, null, ModManager.MSC ? MSCName.Gourmand : SlugName.White);
                if (!self.owner.playerEnteredWithMark)
                {
                    self.events.Add(new TextEvent(self, 0, ".  .  .", 0));
                    self.events.Add(new TextEvent(self, 0, self.Translate("...is this reaching you?"), 0));
                    self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 4));
                }
                else
                {
                    self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 210));
                }
                self.events.Add(new TextEvent(self, 0, self.Translate("A tiny animal. A young one, too. Your journey has taken you deep into my chamber,<LINE>but I doubt you came here on purpose."), 0));
                self.events.Add(new TextEvent(self, 0, self.Translate("I'm afraid I can't assist you in the way you would have hoped for.<LINE> I do not know what would drive you to come all the way here in search for your family... Or how you managed it."), 0));
                if (preMove) // Yellow
                {
                    self.events.Add(new TextEvent(self, 0, self.Translate("I can assure you they are nowhere in my premises, your kind don't seem to take well to this place.<LINE>My overseers would take great delight in reporting an entire group of you, seeing how they react to just one."), 0));
                }
                else // White
                {
                    self.events.Add(new TextEvent(self, 0, self.Translate("Your family may be long gone by now... I can assure you they are nowhere in my premises,<LINE>your kind don't seem to take well to this place. My overseers would take great delight<LINE>in reporting an entire group of you, seeing how they react to just one."), 0));
                }
                self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 20));
                self.events.Add(new TextEvent(self, 0, self.Translate("However, if it's any consolation, I will give you an opportunity.<LINE>To go the way of my creators. Eternal bliss, how does that sound?"), 0));
                self.events.Add(new TextEvent(self, 0, self.Translate("From here, go west past the Farm Arrays and find the place where the land fissures.<LINE>Clamber down into the earth, as deep as you can reach, and then go further."), 0));
                self.events.Add(new TextEvent(self, 0, self.Translate("Saying you'll need luck is an understatement... but I admit I feel<LINE>a pang of sympathy for you, and it feels wrong to leave you empty handed."), 0));
                self.events.Add(new TextEvent(self, 0, self.Translate("At the end of time none of this will matter, I suppose. Even if you fail<LINE>you'll live an eternity of lives more, and maybe eventually, you'll make it in the end."), 0));
                self.events.Add(new SSOracleBehavior.PebblesConversation.PauseAndWaitForStillEvent(self, self.convBehav, 4));
                self.events.Add(new TextEvent(self, 0, self.Translate("Either way, you must leave now. There's a perfectly good access shaft right here."), 0));
                if (!TryJoke())
                    self.events.Add(new TextEvent(self, 0, self.Translate("I hope you find what you're looking for. Don't bother coming back."), 30));

                return;
            }
            if (self.id == ConvID.Pebbles_Red_Green_Neuron)
            {
                self.LoadEventsFromFile(46, GetLancer(SlugName.Red), false, 0);
                SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, LUNTERTOOKNSHKEYBACK, false);
                secondEntry = true; banned = false;
                greenNeuron = self.owner.greenNeuron;
                return;
            }
            if (self.id == ConvID.Pebbles_Red_No_Neuron)
            {
                self.LoadEventsFromFile(47, GetLancer(SlugName.Red), false, 0);
                SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, LUNTERTOOKNSHKEYBACK, false);
                secondEntry = true; banned = true;
                return;
            }
        NoLancer: orig.Invoke(self);
            bool TryJoke()
            {
                if (ModManager.MSC && self.owner.CheckSlugpupsInRoom())
                {
                    self.events.Add(new TextEvent(self, 0, self.Translate("Best of luck to you, and your family. There is nothing else I can do."), 0));
                    self.events.Add(new TextEvent(self, 0, self.Translate("I must resume my work."), 0));
                    self.owner.CreatureJokeDialog();
                    return true;
                }
                if (ModManager.MMF && self.owner.CheckStrayCreatureInRoom() != CreatureTemplate.Type.StandardGroundCreature)
                {
                    self.events.Add(new TextEvent(self, 0, self.Translate("Best of luck to you, and your companion. There is nothing else I can do."), 0));
                    self.events.Add(new TextEvent(self, 0, self.Translate("I must resume my work."), 0));
                    self.owner.CreatureJokeDialog();
                    return true;
                }
                return false;
            }
        }

        private static NSHSwarmer greenNeuron = null;
        private static bool secondEntry = true;
        private static bool banned = false;

        private static void ThrowUpdatePatch(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
        {
            if (!IsStoryLancer) goto NoLancer;
            var basis = GetBasis(self.owner.oracle.room.game.StoryCharacter);
            if (basis != SlugName.Red) goto NoLancer;

            Vector2 GrabPos = (self.oracle.graphicsModule == null) ? self.oracle.firstChunk.pos : (self.oracle.graphicsModule as OracleGraphics).hands[1].pos;
            bool lunterStoleNeuron = GetProgValue<bool>(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, LUNTERTOOKNSHKEYBACK);

            if (self.player.room == self.oracle.room)
            {
                if (!lunterStoleNeuron && greenNeuron?.room == self.oracle.room)
                {
                    self.owner.greenNeuron = greenNeuron;
                    if (self.owner.greenNeuron != null && self.owner.greenNeuron.grabbedBy.Count < 1)
                    {
                        //instance.player.mainBodyChunk.vel *= Mathf.Lerp(0.9f, 1f, instance.oracle.room.gravity);
                        //instance.player.bodyChunks[1].vel *= Mathf.Lerp(0.9f, 1f, instance.oracle.room.gravity);
                        //instance.player.mainBodyChunk.vel += Custom.DirVec(instance.player.mainBodyChunk.pos, instance.owner.greenNeuron.firstChunk.pos)
                        //    * 0.5f * (1f - instance.oracle.room.gravity);
                        self.owner.greenNeuron.firstChunk.MoveFromOutsideMyUpdate(true, GrabPos);
                        self.owner.greenNeuron.firstChunk.vel *= 0f;
                        self.owner.greenNeuron.direction = Custom.PerpendicularVector(self.oracle.firstChunk.pos, self.owner.greenNeuron.firstChunk.pos);
                        self.owner.greenNeuron.storyFly = true;
                        if (self.owner.greenNeuron.storyFly)
                        {
                            self.owner.greenNeuron.storyFlyTarget = GrabPos;
                            if (Custom.DistLess(self.owner.greenNeuron.firstChunk.pos, self.player.firstChunk.pos, 40f))
                            {
                                self.player.ReleaseGrasp(1);
                                self.player.SlugcatGrab(self.owner.greenNeuron, 1);
                            }
                        }
                    }
                    if (self.owner.greenNeuron != null && self.owner.greenNeuron.grabbedBy.Count > 0 && self.owner.greenNeuron.grabbedBy[0].grabber is Player)
                    {
                        self.owner.greenNeuron.storyFly = false;
                        SetProgValue(Custom.rainWorld.progression.currentSaveState.miscWorldSaveData, LUNTERTOOKNSHKEYBACK, true);
                        self.telekinThrowOut = false;
                        self.owner.NewAction(SSAction.ThrowOut_ThrowOut);
                        self.owner.throwOutCounter = 0;
                        banned = false;
                        greenNeuron = null;
                    }
                }

                if (self.telekinThrowOut && !self.oracle.room.aimap.getAItile(self.player.mainBodyChunk.pos).narrowSpace)
                {
                    self.player.mainBodyChunk.vel += Custom.DirVec(self.player.mainBodyChunk.pos, self.oracle.room.MiddleOfTile(28, 32))
                        * 0.25f * (1f - self.oracle.room.gravity) * Mathf.InverseLerp(220f, 280f, (float)self.inActionCounter);
                }
            }

            if (self.action == SSAction.ThrowOut_ThrowOut)
            {
                if (banned) { banned = false; self.owner.NewAction(SSAction.ThrowOut_Polite_ThrowOut); return; }
                //stolen neuron
                if (self.player.room == self.oracle.room)
                {
                    self.owner.throwOutCounter++;
                }
                self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                self.telekinThrowOut = (self.owner.throwOutCounter > 1150);
                if (self.owner.throwOutCounter == 50)
                {
                    self.dialogBox.Interrupt(self.Translate("What? Why would you take it back? Do you even know where you're going with that?"), 0);
                    self.dialogBox.NewMessage(self.Translate("Fine, little creature. Have it your way. Her name is Looks to the Moon, and she lies decrepit in the waters to the east."), 0);
                    self.dialogBox.NewMessage(self.Translate("It's a shame you'd choose to waste such a precious resource on her.<LINE>But I respect your courage, and if you feel that's what's right, I won't stand in your way."), 0);
                }
                else if (self.owner.throwOutCounter == 1200)
                {
                    self.telekinThrowOut = true;
                    self.dialogBox.Interrupt(self.Translate("Leave now, and don't come back."), 0);
                    banned = true;
                    self.owner.NewAction(SSAction.ThrowOut_SecondThrowOut);
                }
                if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
                {
                    self.owner.NewAction(SSAction.General_Idle);
                    self.owner.getToWorking = 1f;
                }
                return;
            }
            if (self.action == SSAction.ThrowOut_Polite_ThrowOut)
            {
                self.owner.getToWorking = 1f;
                self.telekinThrowOut = self.inActionCounter > 1200;
                if (self.inActionCounter < 530)
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Idle;
                }
                else if (self.inActionCounter < 1050)
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                }
                else
                {
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.Talk;
                }
                if (self.owner.playerOutOfRoomCounter > 100 && self.inActionCounter > 400)
                {
                    self.owner.NewAction(SSAction.General_Idle);
                }
                else if (self.inActionCounter == 1100)
                {
                    self.dialogBox.NewMessage(self.Translate("Do you need some help, tiny creature?"), 0);
                }
                //if (lunterStoleNeuron) { instance.owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut); }
                return;
            }
            if (self.action == SSAction.ThrowOut_SecondThrowOut)
            {
                if (self.player.room == self.oracle.room)
                {
                    self.owner.throwOutCounter++;
                }
                self.movementBehavior = SSOracleBehavior.MovementBehavior.Investigate;
                self.telekinThrowOut = (self.inActionCounter > 220) && secondEntry || (banned && self.inActionCounter > 50);
                if (!banned && self.owner.throwOutCounter == 50)
                {
                    if (!lunterStoleNeuron && secondEntry)
                    {
                        self.dialogBox.Interrupt(self.Translate("Why have you returned? There is nothing left for you here."), 0);
                    }
                    else if (lunterStoleNeuron && secondEntry)
                    {
                        self.dialogBox.Interrupt(self.Translate("I implore you, little creature, for the sake of both of us, to just leave already."), 0);
                    }
                    else
                    {
                        self.telekinThrowOut = false;
                        self.dialogBox.Interrupt(self.Translate("Well, child, if you'd rather spend time with me than<LINE>search for eternal happiness, I will have to accommodate you for the time being."), 0);
                    }
                }
                else if (!banned && self.owner.throwOutCounter == 250)
                {
                    if (!lunterStoleNeuron && secondEntry)
                    {
                        self.dialogBox.Interrupt(self.Translate("Please don't waste any more of your time, little creature."), 0);
                    }
                    else if (lunterStoleNeuron && secondEntry)
                    {
                        self.dialogBox.Interrupt(self.Translate("I cannot assist you further."), 0);
                    }
                    else
                    {
                        self.dialogBox.Interrupt(self.Translate("Just be aware of the mistake you're making."), 0);
                        self.owner.getToWorking = 1f;
                    }
                }
                else if (!banned && self.owner.throwOutCounter == 700)
                {
                    if (!lunterStoleNeuron && secondEntry)
                    {
                        self.dialogBox.Interrupt(self.Translate("It is too long a journey to stall for even a moment."), 0);
                    }
                }
                if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
                {
                    self.owner.NewAction(SSAction.General_Idle);
                    self.owner.getToWorking = 1f;
                }
                return;
            }
            if (self.action == SSAction.ThrowOut_KillOnSight)
            {
                if (self.player.room == self.oracle.room)
                {
                    secondEntry = false;
                    self.owner.NewAction(SSAction.ThrowOut_SecondThrowOut);
                }
                return;
            }
        NoLancer: orig.Invoke(self);
        }
    }
}