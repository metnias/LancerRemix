using CatSub.Cat;
using MoreSlugcats;
using Noise;
using RWCustom;
using System;
using UnityEngine;
using AnimIndex = Player.AnimationIndex;
using BodyIndex = Player.BodyModeIndex;

namespace LancerRemix.Cat
{
    public class LancerSupplement : CatSupplement, IAmLancer
    {
        /// <summary>
        /// Slide throw backward: backflip upwards
        /// forward: hard hit & stun, but loses spear
        /// cannot pull spear from wall/alive creatures
        ///
        /// parry: grab / throw. throw parry will fling your spear. (24 ticks for now)
        /// grab parry will flip lizards
        ///
        /// normal stab: will never stun
        /// </summary>
        public LancerSupplement(Player player) : base(player)
        {
            player.playerState.isPup = true;
            isLonk = LancerEnums.GetBasis(player.SlugCatClass) == SlugcatStats.Name.Yellow;
            UpdateHasExhaustion();
        }

        public LancerSupplement() : base()
        {
        }

        protected readonly bool isLonk = false;
        protected bool hasExhaustion = false;

        private void UpdateHasExhaustion()
        {
            hasExhaustion = self.Malnourished || isLonk;
        }

        protected Spear lanceSpear = null;
        protected int lanceGrasp = -1;
        protected int lanceTimer = 0; // throw button: makes you lose spear
        protected int blockTimer = 0; // grab button
        protected readonly int blockTime = 12;
        protected bool spendSpear = false;
        public bool SpendSpear => spendSpear;
        protected bool grabParried = false;
        protected bool violenceParried = false;
        public bool IsGrabParried => grabParried;
        public bool HasParried => grabParried || violenceParried;

        public float BlockVisualAmount(float timeStacker)
            => lanceTimer != 0 ? 0f : Mathf.Clamp(Mathf.Lerp((float)blockTimer, blockTimer - (blockTimer != 0 ? Math.Sign(blockTimer) : 0), timeStacker) / blockTime, -1f, 1f);

        public int HasLanceReady()
        {
            for (int i = 0; i < self.grasps.Length; ++i)
                if (self.grasps[i]?.grabbed is Spear) return i;
            return -1;
        }

        public override string TargetSubVersion => "1.0";

        private float aerobicCache;

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            aerobicCache = self.aerobicLevel;
            base.Update(null, eu);
            if (hasExhaustion)
            {
                if (self.aerobicLevel >= 0.95f) self.gourmandExhausted = true;
                else if (self.aerobicLevel < 0.4f) self.gourmandExhausted = false;
                if (self.gourmandExhausted)
                {
                    self.slowMovementStun = Math.Max(self.slowMovementStun, (int)Custom.LerpMap(self.aerobicLevel, 0.7f, 0.4f, 6f, 0f));
                    self.lungsExhausted = true;
                }
                else
                    self.slowMovementStun = Math.Max(self.slowMovementStun, (int)Custom.LerpMap(self.aerobicLevel, 1f, 0.4f, 2f, 0f, 2f));
            }
        }

        public virtual void UpdateMSC(On.Player.orig_UpdateMSC orig)
        {
            if (hasExhaustion)
            {
                if (self.lungsExhausted && !self.gourmandExhausted)
                {
                    aerobicCache = 1f;
                }
                else
                {
                    float moveExhaust = 400f;
                    float stillExhaust = 1100f;
                    if (self.gourmandExhausted)
                    {
                        moveExhaust = self.bodyMode == BodyIndex.Crawl ? 400f : 800f;
                        stillExhaust = self.bodyMode == BodyIndex.Crawl ? 125f : 200f;
                    }
                    aerobicCache = Mathf.Max(1f - self.airInLungs, aerobicCache - ((!self.slugcatStats.malnourished) ? 1f : 1.2f) / (((self.input[0].x != 0 || self.input[0].y != 0) ? moveExhaust : stillExhaust) * (1f + 3f * Mathf.InverseLerp(0.9f, 1f, self.aerobicLevel))));
                }
                if (ModManager.MSC && self.Wounded && aerobicCache > 0.98f) aerobicCache = 0.35f;
                self.aerobicLevel = aerobicCache;
            }
            orig(self);
        }

        public virtual void MovementUpdate(On.Player.orig_MovementUpdate orig, bool eu)
        {
            orig(self, eu);
            if (lanceTimer > 0)
            {
                --lanceTimer;
                if (lanceTimer == 0
                    && (lanceSpear?.mode == Weapon.Mode.Thrown || lanceSpear?.mode == Weapon.Mode.Free))
                    RetrieveLanceSpear(lanceSpear);
            }
            else if (lanceTimer < 0) ++lanceTimer;
            if (blockTimer > 0)
            {
                --blockTimer;
                if (blockTimer == 0 || (lanceTimer <= 0 && HasLanceReady() < 0))
                {
                    grabParried = false; violenceParried = false;
                    blockTimer = -blockTime; // block cooltime
                    ClearLeftoverStick(Owner);
                }
            }
            else if (blockTimer < 0) ++blockTimer;
            else if (HasLanceReady() >= 0 && lanceTimer == 0 && self.wantToPickUp > 0)
            {
                self.wantToPickUp = 0;
                blockTimer = blockTime; // block
                grabParried = false; violenceParried = false;
                self.room.PlaySound(SoundID.Slugcat_Pick_Up_Spear, self.mainBodyChunk, false, 1.2f, 1.2f);
            }
        }

        public static void ClearLeftoverStick(AbstractCreature crit, bool isB = true)
        {
            if (!isB && crit.creatureTemplate.smallCreature) return;
            for (int i = crit.stuckObjects.Count - 1; i >= 0; --i)
            {
                if (crit.stuckObjects[i] is AbstractPhysicalObject.CreatureGripStick stick && CheckStick(stick))
                {
                    if (!IsRealStick(stick))
                    {
                        Debug.Log($"Lancer cleared leftoverStick: {stick.A.ID} ~ {stick.B.ID}");
                        crit.stuckObjects[i].Deactivate();
                    }
                }
            }

            bool CheckStick(AbstractPhysicalObject.CreatureGripStick stick)
            {
                if (isB) return stick.B == crit
                    && (stick.A as AbstractCreature)?.creatureTemplate.smallCreature != true;
                return stick.A == crit;
            }

            bool IsRealStick(AbstractPhysicalObject.CreatureGripStick stick)
            {
                if (isB)
                {
                    if ((stick.A as AbstractCreature).realizedCreature != null)
                        foreach (var grasp in (stick.A as AbstractCreature).realizedCreature.grasps)
                            if (grasp?.grabbed.abstractPhysicalObject == crit) return true;
                }
                else
                {
                    if (crit.realizedCreature != null)
                        foreach (var grasp in crit.realizedCreature.grasps)
                            if (grasp?.grabbed.abstractPhysicalObject == stick.B) return true;
                }
                return false;
            }
        }

        public void TerrainImpact(On.Player.orig_TerrainImpact orig, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (speed > 9f)
                self.Blink(Custom.IntClamp((int)speed, 9, 60) / 2);
            if (self.input[0].downDiagonal != 0 && self.animation != AnimIndex.Roll
                && ((speed > 9f && speed < 12f) || self.animation == AnimIndex.Flip ||
                (self.animation == AnimIndex.RocketJump && self.rocketJumpFromBellySlide))
                && direction.y < 0 && self.allowRoll > 0 && self.consistentDownDiagonal > ((speed <= 24f) ? 6 : 1))
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
                if (speed > 40f && speed <= 60f && direction.y < 0)
                {
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Death, self.mainBodyChunk);
                    Debug.Log("Lancer Fall damage death");
                    self.Die();
                }
                else if (speed > 28f && speed <= 40f)
                {
                    self.room.PlaySound(SoundID.Slugcat_Terrain_Impact_Hard, self.mainBodyChunk);
                    self.Stun((int)Custom.LerpMap(speed, 28f, 40f, 40f, 140f, 2.5f));
                }
            }
            orig.Invoke(self, chunk, direction, speed, firstContact);
        }

        public void SetMalnourished(On.Player.orig_SetMalnourished orig, bool m)
        {
            orig(self, m);
            UpdateHasExhaustion();
        }

        public virtual void Stun(On.Player.orig_Stun orig, int st)
        {
            orig(self, st);
            ReleaseLanceSpear();
        }

        public virtual void Die(On.Player.orig_Die orig)
        {
            orig(self);
            ReleaseLanceSpear();
        }

        public override void Destroy(On.Player.orig_Destroy orig)
        {
            base.Destroy(null);
        }

        public static bool BiteParriable(Creature crit)
        {
            return crit is Lizard || crit is BigSpider || crit is DropBug || (crit is Vulture v && !v.IsMiros);
        }

        public virtual void Grabbed(On.Player.orig_Grabbed orig, Creature.Grasp grasp)
        {
            grabParried = false;
            bool guarded = true;
            if (blockTimer < 1)
            {
                if (this is LunterSupplement lunterSub && lunterSub.maskOnHorn.HasAMask) lunterSub.maskOnHorn.DropMask(true);
                else goto NoParry;
                guarded = false;
            }
            if (grasp.grabber == null || !BiteParriable(grasp.grabber)) goto NoParry;
            // Parry!
            grasp.grabber.Stun(Mathf.CeilToInt(Mathf.Lerp(80, 40, grasp.grabber.TotalMass / 10f)));
            Vector2 away = (grasp.grabber.mainBodyChunk.pos - self.mainBodyChunk.pos).normalized;
            away.y = 1f; away.Normalize();
            grasp.grabber.WeightedPush(0, grasp.grabber.bodyChunks.Length - 1, away, 20f);
            if (ModManager.MSC && GetParrySpear() is ElectricSpear elecSpear) { elecSpear.Zap(); elecSpear.Electrocute(grasp.grabber); }

            guarded &= lanceTimer == 0;
            AddParryEffect(guarded);
            if (hasExhaustion || (!guarded && !spendSpear)) FlingLance();
            // lanceTimer = 0; blockTimer = 0;
            grabParried = true;

            orig(self, grasp);
            grasp.Release();
            ClearLeftoverStick(Owner);
            return;
        NoParry: orig(self, grasp);
        }

        protected Spear GetParrySpear()
        {
            Spear spear = lanceSpear;
            if (lanceSpear == null)
            {
                for (int i = 0; i < self.grasps.Length; ++i)
                    if (self.grasps[i]?.grabbed is Spear)
                        return self.grasps[i].grabbed as Spear;
            }
            return spear;
        }

        protected void AddParryEffect(bool guarded)
        {
            var spear = GetParrySpear();
            if (spear != null) spear.vibrate = 20;

            self.room.AddObject(new ShockWave(self.mainBodyChunk.pos, 50f, 0.2f, 6, false));
            for (int l = 0; l < 5; l++)
                self.room.AddObject(new Spark(self.mainBodyChunk.pos, Custom.RNV() * 5f, Color.yellow, null, 25, 90));
            self.room.PlaySound(SoundID.Spear_Bounce_Off_Wall, self.mainBodyChunk, false, 1.5f, 0.8f);
            self.room.InGameNoise(new InGameNoise(self.mainBodyChunk.pos, guarded ? 200f : 700f, self, 1f));
            self.mushroomEffect += guarded ? 0.4f : 0.2f;
        }

        public virtual void Violence(On.Creature.orig_Violence orig,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            violenceParried = false;
            if (type == Creature.DamageType.Bite || type == Creature.DamageType.Blunt || type == Creature.DamageType.Stab)
            {
                if (type == Creature.DamageType.Bite)
                    if (!(source.owner is Creature crit) || !BiteParriable(crit)) goto NoParry;

                bool guarded = true;
                if (blockTimer < 1)
                {
                    if (this is LunterSupplement lunterSub && lunterSub.maskOnHorn.HasAMask) lunterSub.maskOnHorn.DropMask(true);
                    else goto NoParry;
                    guarded = false;
                }
                Vector2 away;
                var spear = GetParrySpear();
                if (source?.owner != null)
                {
                    if (source.owner is Creature crit)
                    {
                        away = (crit.mainBodyChunk.pos - self.mainBodyChunk.pos).normalized;
                        away.y = 1f; away.Normalize();
                        ClearLeftoverStick(crit.abstractCreature, false);
                        crit.Stun(Mathf.CeilToInt(Mathf.Lerp(80, 40, crit.TotalMass / 10f)));
                        if (ModManager.MSC && spear is ElectricSpear elecSpear) { elecSpear.Zap(); elecSpear.Electrocute(crit); }
                    }
                    else
                    {
                        away = (source.owner.bodyChunks[0].pos - self.mainBodyChunk.pos).normalized;
                        away.y = 1f; away.Normalize();
                        if (ModManager.MSC && spear is ElectricSpear elecSpear) elecSpear.Zap();
                    }
                    source.owner.WeightedPush(0, source.owner.bodyChunks.Length - 1, away, 20f);
                }
                violenceParried = true;

                guarded &= lanceTimer == 0;
                AddParryEffect(guarded);
                if (hasExhaustion || (!guarded && !spendSpear)) FlingLance();
                // lanceTimer = 0; blockTimer = 0;
                orig(self, source, null, hitChunk, hitAppendage, type, 0f, 0f);
                return;
            }
        NoParry: orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        public virtual void ThrowObject(On.Player.orig_ThrowObject orig, int grasp, bool eu)
        {
            if (!(self.grasps[grasp]?.grabbed is Spear spear)) { orig(self, grasp, eu); return; }

            LanceAttack(spear, orig, grasp, eu);
        }

        public virtual void LanceAttack(Spear spear, On.Player.orig_ThrowObject orig, int grasp, bool eu)
        {
            if (lanceTimer != 0) return;
            if (ModManager.MSC && spear.bugSpear) { orig(self, grasp, eu); return; } // throw bugSpear normally
            lanceGrasp = grasp;
            spendSpear = false;
            var lanceDir = GetLanceDir();
            var startPos = self.firstChunk.pos + lanceDir.ToVector2() * 8f;
            if (self.standing && self.animation != AnimIndex.Flip) startPos.y -= 10f;
            if (self.room.GetTile(startPos).Solid) startPos = self.mainBodyChunk.pos;
            if (self.graphicsModule != null) LookAtTarget();

            self.AerobicIncrease(0.9f);
            lanceSpear = spear;
            spear.spearDamageBonus = GetLanceDamage(self.slugcatStats.throwingSkill);
            if (self.exhausted || self.gourmandExhausted) spear.spearDamageBonus *= 0.4f;
            float pow = Mathf.Lerp(1f, 1.5f, self.Adrenaline);
            if (!spendSpear && self.animation == AnimIndex.BellySlide
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
                    //spear.spearDamageBonus *= 2f;
                    spendSpear = true;
                }
                else if (lanceDir.x == -self.rollDirection && !self.longBellySlide)
                { //reverse
                    lanceDir = new IntVector2(0, -1);
                    spear.alwaysStickInWalls = true;
                    spear.firstChunk.goThroughFloors = false;
                    spear.firstChunk.pos.y = spear.firstChunk.pos.y + 5f;
                    spear.changeDirCounter = 0;
                    self.room.AddObject(new ExplosionSpikes(self.room, self.bodyChunks[1].pos + new Vector2(self.rollDirection * -40f, 0f), 6, 5.5f, 4f, 4.5f, 21f, new Color(1f, 1f, 1f, 0.25f)));
                    self.bodyChunks[1].pos += new Vector2(5f * self.rollDirection, 5f);
                    self.bodyChunks[0].pos = self.bodyChunks[1].pos + new Vector2(5f * self.rollDirection, 5f);
                    self.bodyChunks[1].vel = new Vector2(self.rollDirection * 6f, 15f) * pow * ((!self.longBellySlide) ? 1f : 1.5f);
                    self.bodyChunks[0].vel = new Vector2(self.rollDirection * 6f, 15f) * pow * ((!self.longBellySlide) ? 1f : 1.5f);
                    self.animation = AnimIndex.RocketJump; //RocketJump
                    self.rocketJumpFromBellySlide = true;
                    self.room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, self.mainBodyChunk, false, 1f, 1f);
                    self.rollDirection = 0;
                    self.exitBellySlideCounter = 0;
                    self.AerobicIncrease(0.6f);
                    spendSpear = true;
                    Debug.Log("Slide Flip");
                }
            }
            spear.Thrown(self, startPos, new Vector2?(self.mainBodyChunk.pos - lanceDir.ToVector2() * 5f),
                    lanceDir, pow, eu);
            // spear.firstChunk.vel.x *= 0.85f;
            self.Blink(5);
            (self.graphicsModule as PlayerGraphics)?.ThrowObject(grasp, spear);
            spear.Forbid();
            self.ReleaseGrasp(grasp);
            self.dontGrabStuff = 10;
            self.bodyChunks[0].vel += lanceDir.ToVector2() * 7f;
            self.bodyChunks[1].vel -= lanceDir.ToVector2() * 4f;
            lanceTimer = lanceDir.y == 0 ? 3 : 4;
            blockTimer = spendSpear ? Mathf.CeilToInt(blockTime * 1.5f) : blockTime;
            grabParried = false; violenceParried = false;
            if (!spendSpear && this is LunterSupplement lunterSub) lunterSub.maskOnHorn.DropMask();
            if (spear.bugSpear) ReleaseLanceSpear();


            IntVector2 GetLanceDir()
            {
                var res = new IntVector2(self.ThrowDirection, 0);
                if (self.input[0].y != 0 && self.input[0].x == 0)
                {
                    if (self.animation == AnimIndex.Flip || self.bodyMode == BodyIndex.ZeroG)
                        res = new IntVector2(0, self.input[0].y);
                    else if (self.bodyMode == BodyIndex.Stand && self.input[0].y > 0)
                        res = new IntVector2(0, 1);
                    else if (self.mainBodyChunk.vel.y < -10f && self.input[0].y < 0)
                    { res = new IntVector2(0, -1); spendSpear = true; }
                }
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

        public virtual bool CanIPickThisUp(On.Player.orig_CanIPickThisUp orig, PhysicalObject obj)
        {
            var res = orig(self, obj);
            if (obj is Spear spear)
            {
                if (ModManager.MMF && MMF.cfgDislodgeSpears.Value) return res;
                if (spear.mode == Weapon.Mode.StuckInCreature && !(spear.stuckInObject as Creature).dead) return false;
            }
            return res;
        }

        public virtual void ThrowToGetFree(On.Player.orig_ThrowToGetFree orig, bool eu)
        {
            spendSpear = true;
            int lance = HasLanceReady();
            if (lanceTimer == 0 && lance >= 0)
            { lanceSpear = self.grasps[lance].grabbed as Spear; lanceTimer = 4; lanceGrasp = lance; }
            orig.Invoke(self, eu);
        }

        public void FlingLance()
        {
            Spear spear = lanceSpear;
            if (lanceSpear != null) ReleaseLanceSpear();
            else
            {
                for (int i = 0; i < self.grasps.Length; ++i)
                    if (self.grasps[i]?.grabbed is Spear)
                    { spear = self.grasps[i].grabbed as Spear; self.ReleaseGrasp(i); break; }
            }
            if (spear == null) return;
            blockTimer = -blockTime;

            float angleDeg = 50f;
            float vel = 10f;
            int dir = -self.ThrowDirection;
            float num3 = (dir < 0) ? Mathf.Min(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x) : Mathf.Max(self.bodyChunks[0].pos.x, self.bodyChunks[1].pos.x);
            foreach (var spearChunk in spear.bodyChunks)
            {
                if (dir < 0)
                {
                    if (spearChunk.pos.x > num3 - 8f) spearChunk.pos.x = num3 - 8f;
                    if (spearChunk.vel.x > 0f) spearChunk.vel.x = 0f;
                }
                else if (dir > 0)
                {
                    if (spearChunk.pos.x < num3 + 8f) spearChunk.pos.x = num3 + 8f;
                    if (spearChunk.vel.x < 0f) spearChunk.vel.x = 0f;
                }

                if (spearChunk.vel.y < 0f) spearChunk.vel.y = 0f;
                spearChunk.vel = Vector2.Lerp(spearChunk.vel * 0.35f, self.mainBodyChunk.vel, Custom.LerpMap(spear.TotalMass, 0.2f, 0.5f, 0.6f, 0.3f));
                spearChunk.vel += Custom.DegToVec(angleDeg * dir) * Mathf.Clamp(vel / (Mathf.Lerp(spear.TotalMass, 0.4f, 0.2f) * spear.bodyChunks.Length), 4f, 14f);
            }
            spear.vibrate = 20;
            spear.SetRandomSpin();
            SetLanceCooltime();
        }

        public void ReleaseLanceSpear()
        {
            if (lanceSpear != null)
            {
                lanceSpear.firstChunk.vel *= 0f;
                lanceSpear.Forbid();
                lanceSpear = null;
            }
            SetLanceCooltime();
            blockTimer = -blockTime;
        }

        public void RetrieveLanceSpear(Spear spear = null)
        {
            if (spear == null) spear = lanceSpear;
            if (lanceTimer < 0 || spear == null || spear.grabbedBy.Count > 0 || spear.room != self.room || self.grasps[lanceGrasp] != null)
            { ReleaseLanceSpear(); return; }
            self.SlugcatGrab(spear, lanceGrasp); // retrieve
            lanceSpear = null;
            SetLanceCooltime();
        }

        public void SetLanceCooltime()
        {
            lanceTimer = isLonk ? -16 : -24;
            if (self.exhausted || self.gourmandExhausted) lanceTimer -= 12;
        }

        protected float GetLanceDamage(int throwingSkill)
        {
            float dmg;
            switch (throwingSkill)
            {
                default:
                case 1:
                    dmg = 0.5f; break; //0.3f + 0.2f * Mathf.Pow(UnityEngine.Random.value, 3f);

                case 0:
                    dmg = 0.1f + 0.2f * Mathf.Pow(UnityEngine.Random.value, 4f); break;

                case 2:
                    dmg = 0.7f; break; //0.4f + 0.3f * Mathf.Pow(UnityEngine.Random.value, 3f);
            }
            if (self.Adrenaline > 0f) dmg *= 1.5f;
            return dmg;
        }
    }

    public interface IAmLancer
    {
    }
}