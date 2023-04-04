using LancerRemix.Cat;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static LancerRemix.LancerEnums;
using static LancerRemix.LancerGenerator;
using ConvID = Conversation.ID;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;

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

        private static void LonkKarmaCapOneStep(On.SSOracleBehavior.orig_Update orig, SSOracleBehavior self, bool eu)
        {
            if (!IsStoryLancer) goto NoLonk;
            var basis = self.player?.slugcatStats.name;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Yellow) goto NoLonk;
            if (self.inActionCounter == 299 && self.action == SSOracleBehavior.Action.General_GiveMark)
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
                return;
            }
        NoLonk: orig.Invoke(self, eu);
        }

        private static void AddLancerEvents(On.SSOracleBehavior.PebblesConversation.orig_AddEvents orig, SSOracleBehavior.PebblesConversation self)
        {
            if (!IsStoryLancer) goto NoLancer;

            if (self.id == ConvID.Pebbles_White)
            {
                return;
            }
            if (self.id == ConvID.Pebbles_Red_Green_Neuron)
            {
                return;
            }
            if (self.id == ConvID.Pebbles_Red_No_Neuron)
            {
                return;
            }
            if (self.id == ConvID.Pebbles_Yellow)
            {
                return;
            }
        NoLancer: orig.Invoke(self);
        }

        public static NSHSwarmer greenNeuron;
        public static bool lunterStoleNeuron = true; public static bool secondEntry = true;
        public static bool banned = false;

        private static void ThrowUpdatePatch(On.SSOracleBehavior.ThrowOutBehavior.orig_Update orig, SSOracleBehavior.ThrowOutBehavior self)
        {
            if (!IsStoryLancer) goto NoLancer;
            var basis = self.owner.oracle.room.game.StoryCharacter;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red) goto NoLancer;

            /*
            Vector2 GrabPos = (self.oracle.graphicsModule == null) ? self.oracle.firstChunk.pos : (self.oracle.graphicsModule as OracleGraphics).hands[1].pos;

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
                        lunterStoleNeuron = true;
                        self.telekinThrowOut = false;
                        self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut);
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

            switch (self.action)
            {
                case SSOracleBehavior.Action.ThrowOut_ThrowOut:
                    if (banned) { banned = false; self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut); return; }
                    //stolen neuron
                    if (self.player.room == self.oracle.room)
                    {
                        self.owner.throwOutCounter++;
                    }
                    self.movementBehavior = SSOracleBehavior.MovementBehavior.KeepDistance;
                    self.telekinThrowOut = (self.owner.throwOutCounter > 1150);
                    if (self.owner.throwOutCounter == 50)
                    {
                        self.dialogBox.Interrupt(LancerMod.Translate("What? Why would you take it back? Do you even know where you're going with that?"), 0);
                    }
                    else if (self.owner.throwOutCounter == 250)
                    {
                        self.dialogBox.Interrupt(LancerMod.Translate("Fine, little creature. Have it your way. Her name is Looks to the Moon, and she lies decrepit in the waters to the east."), 0);
                    }
                    if (self.owner.throwOutCounter == 700)
                    {
                        self.dialogBox.Interrupt(LancerMod.Translate("It's a shame you'd choose to waste such a precious resource on her.<LINE>But I respect your courage, and if you feel that's what's right, I won't stand in your way."), 0);
                    }
                    else if (self.owner.throwOutCounter == 1200)
                    {
                        self.telekinThrowOut = true;
                        self.dialogBox.Interrupt(LancerMod.Translate("Leave now, and don't come back."), 0);
                        banned = true;
                        self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_SecondThrowOut);
                    }
                    if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
                    {
                        self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
                        self.owner.getToWorking = 1f;
                    }
                    break;

                case SSOracleBehavior.Action.ThrowOut_Polite_ThrowOut:
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
                        self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
                    }
                    else if (self.inActionCounter == 1100)
                    {
                        self.dialogBox.NewMessage(LancerMod.Translate("Do you need some help, tiny creature?"), 0);
                    }
                    //if (lunterStoleNeuron) { instance.owner.NewAction(SSOracleBehavior.Action.ThrowOut_ThrowOut); }
                    break;

                case SSOracleBehavior.Action.ThrowOut_SecondThrowOut:
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
                            self.dialogBox.Interrupt(LancerMod.Translate("Why have you returned? There is nothing left for you here."), 0);
                        }
                        else if (lunterStoleNeuron && secondEntry)
                        {
                            self.dialogBox.Interrupt(LancerMod.Translate("I implore you, little creature, for the sake of both of us, to just leave already."), 0);
                        }
                        else
                        {
                            self.telekinThrowOut = false;
                            self.dialogBox.Interrupt(LancerMod.Translate("Well, child, if you'd rather spend time with me than search for eternal happiness, I will gladly accommodate you."), 0);
                        }
                    }
                    else if (!banned && self.owner.throwOutCounter == 250)
                    {
                        if (!lunterStoleNeuron && secondEntry)
                        {
                            self.dialogBox.Interrupt(LancerMod.Translate("Please don't waste any more of your time, little creature."), 0);
                        }
                        else if (lunterStoleNeuron && secondEntry)
                        {
                            self.dialogBox.Interrupt(LancerMod.Translate("I cannot assist you further."), 0);
                        }
                        else
                        {
                            self.dialogBox.Interrupt(LancerMod.Translate("Just be aware of the mistake you're making."), 0);
                            self.owner.getToWorking = 1f;
                        }
                    }
                    else if (!banned && self.owner.throwOutCounter == 700)
                    {
                        if (!lunterStoleNeuron && secondEntry)
                        {
                            self.dialogBox.Interrupt(LancerMod.Translate("It is too long a journey to stall for even a second."), 0);
                        }
                    }
                    if (self.owner.playerOutOfRoomCounter > 100 && self.owner.throwOutCounter > 400)
                    {
                        self.owner.NewAction(SSOracleBehavior.Action.General_Idle);
                        self.owner.getToWorking = 1f;
                    }
                    break;

                case SSOracleBehavior.Action.ThrowOut_KillOnSight:
                    if (self.player.room == self.oracle.room)
                    {
                        secondEntry = false;
                        self.owner.NewAction(SSOracleBehavior.Action.ThrowOut_SecondThrowOut);
                    }
                    break;

                default:
                    orig.Invoke(self);
                    break;
            }*/
            return;
        NoLancer: orig.Invoke(self);
        }
    }
}