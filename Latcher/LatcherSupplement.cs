using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;
using AnimIndex = Player.AnimationIndex;
using BodyIndex = Player.BodyModeIndex;

namespace LancerRemix.Latcher
{
    public class LatcherSupplement : LancerSupplement
    {
        public LatcherSupplement(Player player) : base(player)
        {
            if (self.room.game.session is StoryGameSession sgs && self.rippleLevel > 0f)
                sgs.saveState.theGlow = true;
        }

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(orig, eu);
            if (self.room == null) return;

            if (self.sporeParticleTicks > 0)
            {
                if (self.sporeParticleTicks % 4 == 0)
                {
                    var bodyChunk = self.bodyChunks[Random.Range(0, self.bodyChunks.Length)];
                    Vector2 vel = bodyChunk.vel * 0.5f + Custom.RNV() * Random.Range(0f, 3f);
                    var sporeCloud = new SporeCloud(bodyChunk.pos, vel, new Color(0.02f, 0.1f, 0.08f), Random.Range(0.65f, 0.8f), null, 0, null, self.abstractPhysicalObject.rippleLayer)
                    {
                        pos = bodyChunk.pos + Random.insideUnitCircle * bodyChunk.rad,
                        nonToxic = true
                    };
                    self.room.AddObject(sporeCloud);
                }
            }

            if (self.camoProgress > 0f && self.mushroomCounter == 0)
            {
                //float mushroomEffect = .1f;
                //if (self.rippleLevel >= 5f) mushroomEffect = 1f;
                //else if (self.rippleLevel >= 4f) mushroomEffect = .8f;
                //else if (self.rippleLevel >= 3f) mushroomEffect = .5f;
                self.mushroomEffect = Mathf.Max(self.mushroomEffect, .1f * self.camoProgress); //mushroomEffect * self.camoProgress);

                if (self.adrenalineEffect != null)
                    self.adrenalineEffect.intensity = Mathf.Max(self.adrenalineEffect.intensity, self.camoProgress);
                //self.adrenalineEffect.intensity = Mathf.Max(self.adrenalineEffect.intensity, (1f - mushroomEffect) * self.camoProgress);
            }
        }

        public override void ThrowObject(On.Player.orig_ThrowObject orig, int grasp, bool eu)
        {
            if (self.grasps[grasp]?.grabbed != null && !(self.grasps[grasp].grabbed is Spear)
                && LatcherMusicbox.IsLatcherRipple)
            {
                self.room.AddObject(new ShockWave(self.grasps[grasp].grabbed.bodyChunks[0].pos,
                    self.grasps[grasp].grabbed.bodyChunks[0].rad, self.grasps[grasp].grabbed.bodyChunks[0].mass,
                    16));
            }

            base.ThrowObject(orig, grasp, eu);
        }

        protected override void LancerTerrainImpact(IntVector2 direction, float speed, bool firstContact)
        {
            if (speed > 10f)
                self.Blink(Custom.IntClamp((int)speed, 10, 60) / 2);
            if (self.input[0].downDiagonal != 0 && self.animation != AnimIndex.Roll
                && ((speed > 9f && speed < 12f) || self.animation == AnimIndex.Flip ||
                (self.animation == AnimIndex.RocketJump && self.rocketJumpFromBellySlide))
                && direction.y < 0 && self.allowRoll > 0 && self.consistentDownDiagonal > ((speed <= 30f) ? 6 : 1))
            { //roll easier
                if (self.animation == AnimIndex.RocketJump && self.rocketJumpFromBellySlide)
                {
                    self.bodyChunks[1].vel.y += 3f;
                    self.bodyChunks[1].pos.y += 3f;
                    self.bodyChunks[0].vel.y -= 3f;
                    self.bodyChunks[0].pos.y -= 3f;
                }
                self.room.PlaySound(SoundID.Slugcat_Roll_Init, self.mainBodyChunk.pos, 1f, 1f);
                self.animation = AnimIndex.Roll;
                self.rollDirection = self.input[0].downDiagonal;
                self.rollCounter = 0;
                self.bodyChunks[0].vel.x = Mathf.Lerp(self.bodyChunks[0].vel.x, 9f * (float)self.input[0].x, 0.7f);
                self.bodyChunks[1].vel.x = Mathf.Lerp(self.bodyChunks[1].vel.x, 9f * (float)self.input[0].x, 0.7f);
                self.standing = false;
            }
            else if (firstContact)
            {
                if (speed > 50f && speed <= 60f && direction.y < 0)
                {
                    if (self.room != null && self.room.abstractRoom.name == "HI_W05") return; // Latcher death protection in intro
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                    Debug.Log("Lancer Fall damage death");
                    self.Die();
                }
                else if (speed > 30f && speed <= 50f)
                {
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                    self.Stun((int)Custom.LerpMap(speed, 30f, 50f, 40f, 140f, 2.5f));
                }
            }
        }
    }
}