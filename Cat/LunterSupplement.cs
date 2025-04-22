using LancerRemix.Combat;
using RWCustom;
using UnityEngine;
using AnimIndex = Player.AnimationIndex;

namespace LancerRemix.Cat
{
    public class LunterSupplement : LancerSupplement
    {
        public LunterSupplement()
        {
        }

        public LunterSupplement(Player player) : base(player)
        {
            maskOnHorn = new MaskOnHorn(this);
        }

        public readonly MaskOnHorn maskOnHorn = null;

        public virtual void ObjectEaten(On.Player.orig_ObjectEaten orig, IPlayerEdible edible)
        {
            orig(self, edible);
            maskOnHorn.LockInteraction();
        }

        public override void Stun(On.Player.orig_Stun orig, int st)
        {
            base.Stun(orig, st);
            if (maskOnHorn.HasAMask && st > UnityEngine.Random.Range(40, 80))
                maskOnHorn.DropMask();
        }

        public override void Die(On.Player.orig_Die orig)
        {
            base.Die(orig);
            maskOnHorn.DropMask();
        }

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(orig, eu);
            maskOnHorn.Update(eu);
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