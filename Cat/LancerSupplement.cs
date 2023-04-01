using CatSub.Cat;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using static MonoMod.InlineRT.MonoModRule;
using AnimIndex = Player.AnimationIndex;
using BodyIndex = Player.BodyModeIndex;

namespace LancerRemix.Cat
{
    internal class LancerSupplement : CatSupplement, IAmLancer
    {
        /// <summary>
        /// Slide throw backward: backflip upwards
        /// forward: hard hit & stun, but loses spear
        /// pulling spear from wall/creatures takes time
        ///
        /// parry: grab / throw. throw parry will fling your spear. (12 ticks for now)
        /// grab parry will flip lizards
        ///
        /// normal stab: will never stun
        /// </summary>
        public LancerSupplement(Player player) : base(player)
        {
            player.playerState.isPup = true;
        }

        public LancerSupplement() : base()
        {
        }

        private Spear lanceSpear = null;
        private int lanceDelay = 0;
        private int lanceGrasp = -1;
        private int lanceTimer = 0; // throw button: makes you lose spear
        private int blockTimer = 0; // grab button
        private int OnParry => Math.Max(lanceTimer, blockTimer);
        private bool slideLance = false;

        public override string TargetSubVersion => "1.0";

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(null, eu);
        }

        public virtual void MovementUpdate(On.Player.orig_MovementUpdate orig, bool eu)
        {
            orig(self, eu);
            if (lanceTimer > 0)
            {
                --lanceTimer;
                if (lanceTimer == 0 && !slideLance
                    && (lanceSpear?.mode == Weapon.Mode.Thrown || lanceSpear?.mode == Weapon.Mode.Free))
                {
                    self.SlugcatGrab(lanceSpear, lanceGrasp); // retrieve
                    lanceSpear = null;
                }
            }
            if (blockTimer > 0) --blockTimer;
            if (lanceDelay > 0) --lanceDelay;
        }

        public override void Destroy(On.Player.orig_Destroy orig)
        {
            base.Destroy(null);
        }

        public virtual void Grabbed(On.Player.orig_Grabbed orig, Creature.Grasp grasp)
        {
            if (OnParry < 1) goto NoParry;
            if (!(grasp.grabber is Lizard) && !(grasp.grabber is Vulture) && !(grasp.grabber is BigSpider) && !(grasp.grabber is DropBug)) goto NoParry;
            // Parry!

            grasp.grabber.Stun(Mathf.CeilToInt(Mathf.Lerp(80, 40, grasp.grabber.TotalMass / 10f)));

            // effect
            self.room.PlaySound(SoundID.Spear_Damage_Creature_But_Fall_Out, grasp.grabber.mainBodyChunk, false, 1.5f, 0.8f);
            lanceTimer = 0; blockTimer = 0;
            return;
        NoParry: orig(self, grasp);
        }

        public virtual void ThrowObject(On.Player.orig_ThrowObject orig, int grasp, bool eu)
        {
            if (!(self.grasps[grasp]?.grabbed is Spear spear)) { orig(self, grasp, eu); return; }

            if ((lanceDelay | lanceTimer) > 0) return;
            if (ModManager.MSC && spear.bugSpear) { orig(self, grasp, eu); return; } // throw bugSpear normally
            lanceGrasp = grasp;
            var lanceDir = GetLanceDir();
            var startPos = self.firstChunk.pos + lanceDir.ToVector2() * 10f + new Vector2(0f, 4f);
            if (self.room.GetTile(startPos).Solid) startPos = self.mainBodyChunk.pos;
            if (self.graphicsModule != null) LookAtTarget();

            self.AerobicIncrease(0.5f);
            lanceSpear = spear;
            slideLance = false;
            spear.spearDamageBonus = GetLanceDamage(self.slugcatStats.throwingSkill);
            float pow = Mathf.Lerp(1f, 1.5f, self.Adrenaline);
            if (self.animation == AnimIndex.BellySlide
                && self.rollCounter > 8 && self.rollCounter < 15 && lanceDir.x == self.rollDirection)
            { // slide
                if (lanceDir.x == self.rollDirection && self.slugcatStats.throwingSkill > 0)
                { // slide forward
                    spear.firstChunk.vel.x = spear.firstChunk.vel.x + (float)lanceDir.x * 15f;
                    spear.floorBounceFrames = 30;
                    spear.alwaysStickInWalls = true;
                    spear.firstChunk.goThroughFloors = false;
                    spear.firstChunk.vel.y = spear.firstChunk.vel.y + 5f;
                    spear.changeDirCounter = 0;
                    self.rollCounter = 8;
                    self.mainBodyChunk.pos.x = self.mainBodyChunk.pos.x + (float)self.rollDirection * 6f;
                    //instance.room.AddObject(new ExplosionSpikes(instance.room, instance.bodyChunks[1].pos + new Vector2((float)instance.rollDirection * -40f, 0f), 6, 5.5f, 4f, 4.5f, 21f, new Color(1f, 1f, 1f, 0.25f)));
                    self.bodyChunks[1].pos.x = self.bodyChunks[1].pos.x + (float)self.rollDirection * 6f;
                    self.bodyChunks[1].pos.y = self.bodyChunks[1].pos.y + 17f;
                    self.mainBodyChunk.vel.x = self.mainBodyChunk.vel.x + (float)self.rollDirection * 16f;
                    self.bodyChunks[1].vel.x = self.bodyChunks[1].vel.x + (float)self.rollDirection * 16f;
                    //instance.room.PlaySound(SoundID.Slugcat_Belly_Slide_Init, instance.mainBodyChunk, false, 1f, 1f);
                    self.exitBellySlideCounter = 0;
                    self.longBellySlide = true;
                    spear.spearDamageBonus *= 2f;
                    slideLance = true;
                }
                else if (lanceDir.x == -self.rollDirection && !self.longBellySlide)
                { //reverse
                    lanceDir = new IntVector2(0, -1);
                    spear.alwaysStickInWalls = true;
                    spear.firstChunk.goThroughFloors = false;
                    spear.firstChunk.pos.y = spear.firstChunk.pos.y + 5f;
                    spear.changeDirCounter = 0;
                    self.room.AddObject(new ExplosionSpikes(self.room, self.bodyChunks[1].pos + new Vector2((float)self.rollDirection * -40f, 0f), 6, 5.5f, 4f, 4.5f, 21f, new Color(1f, 1f, 1f, 0.25f)));
                    self.bodyChunks[1].pos += new Vector2(5f * (float)self.rollDirection, 5f);
                    self.bodyChunks[0].pos = self.bodyChunks[1].pos + new Vector2(5f * (float)self.rollDirection, 5f);
                    self.bodyChunks[1].vel = new Vector2((float)self.rollDirection * 6f, 15f) * pow * ((!self.longBellySlide) ? 1f : 1.5f);
                    self.bodyChunks[0].vel = new Vector2((float)self.rollDirection * 6f, 15f) * pow * ((!self.longBellySlide) ? 1f : 1.5f);
                    self.animation = AnimIndex.Flip; //RocketJump
                    self.rocketJumpFromBellySlide = true;
                    self.room.PlaySound(SoundID.Slugcat_Rocket_Jump, self.mainBodyChunk, false, 1f, 1f);
                    self.rollDirection = 0;
                    //typeof(Player).GetField("exitBellySlideCounter", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance).SetValue(instance, 0);
                    self.AerobicIncrease(0.6f);
                }
            }
            spear.Thrown(self, startPos, new Vector2?(self.mainBodyChunk.pos - lanceDir.ToVector2() * 5f),
                    lanceDir, pow, eu);
            spear.firstChunk.vel.x = spear.firstChunk.vel.x * 0.85f;
            self.Blink(5);
            (self.graphicsModule as PlayerGraphics)?.ThrowObject(grasp, spear);
            spear.Forbid();
            self.ReleaseGrasp(grasp);
            lanceDelay = 30;
            self.dontGrabStuff = 10;
            self.bodyChunks[0].vel += lanceDir.ToVector2() * 4f;
            self.bodyChunks[1].vel -= lanceDir.ToVector2() * 3f;
            lanceTimer = lanceDir.y == 0 ? 4 : 6;
            blockTimer = 0;

            IntVector2 GetLanceDir()
            {
                var res = new IntVector2(self.ThrowDirection, 0);
                if (self.animation == AnimIndex.Flip && self.input[0].y != 0 && self.input[0].x == 0)
                    res = new IntVector2(0, (ModManager.MMF && MMF.cfgUpwardsSpearThrow.Value) ? self.input[0].y : -1);
                if (ModManager.MMF && self.bodyMode == BodyIndex.ZeroG && MMF.cfgUpwardsSpearThrow.Value)
                    if (self.input[0].y != 0)
                        res = new IntVector2(0, self.input[0].y);
                return res;
            }

            void LookAtTarget()
            {
                for (int i = 0; i < self.room.abstractRoom.creatures.Count; i++)
                {
                    if (self.room.abstractRoom.creatures[i].realizedCreature != null
                        && Custom.DistLess(self.firstChunk.pos, self.room.abstractRoom.creatures[i].realizedCreature.mainBodyChunk.pos, 200f)
                        && self != self.room.abstractRoom.creatures[i].realizedCreature)
                    {
                        Creature candidate = self.room.abstractRoom.creatures[i].realizedCreature;
                        for (int j = 0; j < candidate.bodyChunks.Length; j++)
                        {
                            if (Custom.DistLess(self.firstChunk.pos, candidate.bodyChunks[j].pos, 30f))
                            {
                                (self.graphicsModule as PlayerGraphics).LookAtObject(candidate);
                                return;
                            }
                        }
                    }
                }
            }
        }

        public void StunOrDie()
        {
            ReleaseLanceSpear();
        }

        private void ReleaseLanceSpear()
        {
            if (lanceSpear != null)
            {
                lanceSpear.firstChunk.vel *= 0f;
                lanceSpear = null;
            }
            lanceTimer = 0;
        }

        private static float GetLanceDamage(int throwingSkill)
        {
            switch (throwingSkill)
            {
                default:
                case 1:
                    return 0.3f + 0.2f * Mathf.Pow(UnityEngine.Random.value, 3f);

                case 0:
                    return 0.2f + 0.3f * Mathf.Pow(UnityEngine.Random.value, 4f);

                case 2:
                    return 0.4f + 0.3f * Mathf.Pow(UnityEngine.Random.value, 3f);
            }
        }
    }

    internal interface IAmLancer
    {
    }
}