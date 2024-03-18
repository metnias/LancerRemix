using LancerRemix.Cat;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using UnityEngine;
using static LancerRemix.Cat.ModifyCat;
using Random = UnityEngine.Random;

namespace LancerRemix.Combat
{
    public class WeaponPatch
    {
        internal static void Patch()
        {
            On.Weapon.Thrown += LancerThrownWeapon;
            On.Spear.HitSomething += SpearHit;
            On.Spear.LodgeInCreature_CollisionResult_bool_bool += SpearLodgeCreature;
            On.Spear.Update += SpearUpdate;
            IL.Spear.Update += LanceFarStickPrevent;
            On.PlayerCarryableItem.Update += LanceReduceWaterFriction;
            On.Spear.DrawSprites += SpearDrawSprites;

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
            On.MoreSlugcats.ElectricSpear.DrawSprites += ElecSpearDrawSprites;
        }

        internal static void OnMSCDisablePatch()
        {
            On.MoreSlugcats.ElectricSpear.DrawSprites -= ElecSpearDrawSprites;
        }

        private static void LancerThrownWeapon(On.Weapon.orig_Thrown orig, Weapon self, Creature thrownBy, Vector2 thrownPos, Vector2? firstFrameTraceFromPos, IntVector2 throwDir, float frc, bool eu)
        {
            if (thrownBy != null && thrownBy is Player player && IsPlayerLancer(player) && !IsPlayerCustomLancer(player) && !(self is Spear))
            {
                if (self is Rock) frc = Mathf.Lerp(0.8f, 1.2f, player.Adrenaline);
                else frc = Mathf.Lerp(0.6f, 0.9f, player.Adrenaline);
                if (player.gourmandExhausted) frc *= 0.5f;
            }
            orig(self, thrownBy, thrownPos, firstFrameTraceFromPos, throwDir, frc, eu);
        }

        #region Spear

        public static Func<Player, Spear, float, Vector2?> OnGetGrabLancerAimDir = null;
        public static Func<Player, Spear, float, Vector2?> OnGetThrwLancerAimDir = null;

        private static bool GetLancerAimDir(Spear self, float timeStacker, out Vector2 aimDir)
        {
            if (self.grabbedBy.Count > 0 && self.grabbedBy[0].grabber is Player grabPlayer && IsPlayerLancer(grabPlayer))
            {
                if (IsPlayerCustomLancer(grabPlayer))
                {
                    var customDir = OnGetGrabLancerAimDir?.Invoke(grabPlayer, self, timeStacker);
                    if (customDir.HasValue && customDir.Value != Vector2.zero) { aimDir = customDir.Value; return true; }
                }

                float block = GetSub<LancerSupplement>(grabPlayer)?.BlockVisualAmount(timeStacker) ?? 0f;
                if (block >= 0f)
                    aimDir = Vector3.Slerp(grabPlayer.ThrowDirection >= 0f ? Vector3.right : Vector3.left, Vector3.up, Custom.LerpCircEaseOut(0.0f, 1.0f, block));
                else
                    aimDir = Vector3.Slerp(grabPlayer.ThrowDirection >= 0f ? Vector3.right : Vector3.left, Vector2.down, Custom.LerpCircEaseIn(0.0f, 0.3f, -block));
                return true;
            }
            else if (self.mode == Weapon.Mode.Thrown && self.thrownBy != null && self.thrownBy is Player thrwPlayer && IsPlayerLancer(thrwPlayer))
            {
                if (IsPlayerCustomLancer(thrwPlayer))
                {
                    var customDir = OnGetThrwLancerAimDir?.Invoke(thrwPlayer, self, timeStacker);
                    if (customDir.HasValue && customDir != Vector2.zero) { aimDir = customDir.Value; return true; }
                }
                //var rot = Vector3.Slerp(spear.lastRotation, spear.rotation, timeStacker);
                aimDir = self.throwDir.ToVector2();
                return true;
            }
            aimDir = Vector2.zero;
            return false;
        }

        public static Func<Player, bool, Spear, SharedPhysics.CollisionResult, bool, bool?> OnLancerSpearHit = null;

        private static bool SpearHit(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (result.obj is Player hitPlayer && IsPlayerLancer(hitPlayer))
            {
                var hitSub = GetSub<LancerSupplement>(hitPlayer);
                if (hitSub != null)
                {
                    hitPlayer.Violence(self.firstChunk, new Vector2?(self.firstChunk.vel * self.firstChunk.mass * 2f), result.chunk, result.onAppendagePos, Creature.DamageType.Stab, 0f, 0f); // parry test
                    if (hitSub.HasParried)
                    {
                        self.vibrate = 20;
                        self.ChangeMode(Weapon.Mode.Free);
                        self.firstChunk.vel = self.firstChunk.vel * -0.5f + Custom.DegToVec(Random.value * 360f) * Mathf.Lerp(0.1f, 0.4f, Random.value) * self.firstChunk.vel.magnitude;
                        self.SetRandomSpin();
                        return false;
                    }
                }
            }
            var res = orig(self, result, eu);
            if (res && self.thrownBy is Player atkPlayer && IsPlayerLancer(atkPlayer))
            {
                if (IsPlayerCustomLancer(atkPlayer))
                {
                    var customResult = OnLancerSpearHit?.Invoke(atkPlayer, res, self, result, eu);
                    if (customResult.HasValue) return customResult.Value;
                }
                var sub = GetSub<LancerSupplement>(atkPlayer);
                if (sub != null)
                {
                    if (self is ExplosiveSpear) sub.ReleaseLanceSpear();
                    else if (!sub.SpendSpear || !(result.obj is Creature crit)) sub.RetrieveLanceSpear(self); // Retrieve spear
                    //else if (self.throwDir.y != 0)
                    //{
                    //    Debug.Log($"LancerDrop!");
                    //    SpendSpearAttack(crit);
                    //}
                    else
                    {
                        Debug.Log($"LancerSuperAttack!");
                        SpendSpearAttack(crit);

                        // WhiplastJump
                        atkPlayer.animation = Player.AnimationIndex.Flip;
                        atkPlayer.standing = true;
                        atkPlayer.room.AddObject(new ExplosionSpikes(atkPlayer.room, atkPlayer.bodyChunks[1].pos + new Vector2(0f, -atkPlayer.bodyChunks[1].rad), 8, 7f, 5f, 5.5f, 40f, new Color(1f, 1f, 1f, 0.5f)));
                        int back = 1, backCheck = 1;
                        while (backCheck < 4 && !atkPlayer.room.GetTile(atkPlayer.bodyChunks[0].pos + new Vector2((float)(backCheck * -(float)atkPlayer.rollDirection) * 15f, 0f)).Solid && !atkPlayer.room.GetTile(atkPlayer.bodyChunks[0].pos + new Vector2((float)(backCheck * -(float)atkPlayer.rollDirection) * 15f, 20f)).Solid)
                        { back = backCheck; ++backCheck; }
                        atkPlayer.bodyChunks[0].pos += new Vector2(atkPlayer.rollDirection * -(back * 15f + 8f), 14f);
                        atkPlayer.bodyChunks[1].pos += new Vector2(atkPlayer.rollDirection * -(back * 15f + 2f), 0f);
                        atkPlayer.bodyChunks[0].vel = new Vector2(atkPlayer.rollDirection * -8f, 10f);
                        atkPlayer.bodyChunks[1].vel = new Vector2(atkPlayer.rollDirection * -8f, 11f);
                        atkPlayer.rollDirection = -atkPlayer.rollDirection;
                        atkPlayer.flipFromSlide = true;
                        atkPlayer.whiplashJump = false;
                        atkPlayer.jumpBoost = 0f;
                        atkPlayer.room.PlaySound(SoundID.Slugcat_Sectret_Super_Wall_Jump, atkPlayer.mainBodyChunk, false, 1f, 1f);
                    }

                    void SpendSpearAttack(Creature crit)
                    {
                        float damage = self.spearDamageBonus;
                        if (!crit.dead)
                        {
                            if (!crit.abstractCreature.creatureTemplate.smallCreature)
                                atkPlayer.room.ScreenMovement(new Vector2?(atkPlayer.bodyChunks[0].pos), atkPlayer.mainBodyChunk.vel * damage * atkPlayer.bodyChunks[0].mass * 0.3f, Mathf.Max((damage * atkPlayer.bodyChunks[0].mass - 30f) / 50f, 0f));
                            crit.SetKillTag(atkPlayer.abstractCreature);
                            crit.Violence(atkPlayer.mainBodyChunk, new Vector2?(atkPlayer.mainBodyChunk.vel), crit.firstChunk, null, Creature.DamageType.Stab, damage, 50f);
                        }
                        atkPlayer.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Terrain, atkPlayer.mainBodyChunk, false, 1.2f, 1.0f);

                        sub.ReleaseLanceSpear();
                    }
                }
            }
            return res;
        }

        public static Func<Player, Spear, SharedPhysics.CollisionResult, bool, bool?> OnLancerSpearLodgeCreature = null;

        private static void SpearLodgeCreature(On.Spear.orig_LodgeInCreature_CollisionResult_bool_bool orig, Spear self, SharedPhysics.CollisionResult result, bool eu, bool isJellyFish = false)
        {
            orig(self, result, eu, isJellyFish);
            if (!(self.thrownBy is Player player) || !IsPlayerLancer(player)) return;
            if (IsPlayerCustomLancer(player))
            {
                var customAction = OnLancerSpearLodgeCreature?.Invoke(player, self, result, eu);
                if (customAction.HasValue) return;
            }

            var sub = GetSub<LancerSupplement>(player);
            if (sub == null) return;

            if (self is ExplosiveSpear || sub.SpendSpear)
            { sub.ReleaseLanceSpear(); return; }
            sub.RetrieveLanceSpear(self);
        }

        private static void LanceFarStickPrevent(ILContext il)
        {
            var cursor = new ILCursor(il);

            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LanceFarStickPrevent);

            if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(5),
                x => x.MatchLdloc(5))) return;

            DebugLogCursor();

            bool setFlag2 = false;
            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<bool, Spear, bool>>(
                (flag, self) =>
                {
                    if (!flag) return flag;
                    if (self.thrownBy != null && self.thrownBy is Player thrwPlayer && IsPlayerLancer(thrwPlayer))
                    {
                        var sub = GetSub<LancerSupplement>(thrwPlayer);
                        if (sub != null && sub.SpendSpear) return flag;
                        if (self.throwDir.y == 0 && !Custom.DistLess(self.thrownPos, self.firstChunk.pos, 20f))
                        { flag = false; setFlag2 = true; }
                    }
                    return flag;
                }
                );
            cursor.Emit(OpCodes.Stloc, 5);

            cursor.Emit(OpCodes.Ldloc, 6);
            cursor.EmitDelegate<Func<bool, bool>>((flag2) => flag2 || setFlag2);
            cursor.Emit(OpCodes.Stloc, 6);

            cursor.Emit(OpCodes.Ldloc, 5);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LanceFarStickPrevent);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static void LanceReduceWaterFriction(On.PlayerCarryableItem.orig_Update orig, PlayerCarryableItem self, bool eu)
        {
            if (self is Spear spear && spear.mode == Weapon.Mode.Thrown)
            {
                if (spear.thrownBy != null && spear.thrownBy is Player thrwPlayer && IsPlayerLancer(thrwPlayer))
                {
                    var sub = GetSub<LancerSupplement>(thrwPlayer);
                    if (sub != null && !sub.SpendSpear) spear.waterRetardationImmunity = 0.98f; // 0.9f
                }
            }
            orig(self, eu);
        }

        private static void SpearUpdate(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig(self, eu);
            if (!GetLancerAimDir(self, 0f, out var aimDir)) return;

            if (self is ExplosiveSpear)
            {
                (self as ExplosiveSpear).rag[0, 0] = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, 1f) + aimDir * 15f;
                (self as ExplosiveSpear).rag[0, 2] *= 0f;
            }

            UpdateStuckObjects();

            void UpdateStuckObjects()
            {
                for (int l = self.abstractPhysicalObject.stuckObjects.Count - 1; l >= 0; l--)
                {
                    if (self.abstractPhysicalObject.stuckObjects[l] is AbstractPhysicalObject.ImpaledOnSpearStick)
                        if (self.abstractPhysicalObject.stuckObjects[l].B.realizedObject != null && (self.abstractPhysicalObject.stuckObjects[l].B.realizedObject.slatedForDeletetion || self.abstractPhysicalObject.stuckObjects[l].B.realizedObject.grabbedBy.Count > 0))
                            self.abstractPhysicalObject.stuckObjects[l].Deactivate();
                        else if (self.abstractPhysicalObject.stuckObjects[l].B.realizedObject != null && self.abstractPhysicalObject.stuckObjects[l].B.realizedObject.room == self.room)
                        {
                            self.abstractPhysicalObject.stuckObjects[l].B.realizedObject.firstChunk.MoveFromOutsideMyUpdate(eu, self.firstChunk.pos + aimDir * Custom.LerpMap((float)(self.abstractPhysicalObject.stuckObjects[l] as AbstractPhysicalObject.ImpaledOnSpearStick).onSpearPosition, 0f, 4f, 15f, -15f));
                            self.abstractPhysicalObject.stuckObjects[l].B.realizedObject.firstChunk.vel *= 0f;
                        }
                }
            }
        }

        private static void SpearDrawSprites(On.Spear.orig_DrawSprites orig, Spear self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(self, sLeaser, rCam, timeStacker, camPos);
            if (!sLeaser.sprites[0].isVisible) return;
            if (!GetLancerAimDir(self, 0f, out var aimDir)) return;

            if (self is ExplosiveSpear)
            {
                (self as ExplosiveSpear).rag[0, 0] = Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, 1f) + aimDir * 15f;
                (self as ExplosiveSpear).rag[0, 2] *= 0f;
                Vector2 tie = Vector2.Lerp((self as ExplosiveSpear).rag[0, 1], (self as ExplosiveSpear).rag[0, 0], timeStacker);
                float vel = 2f * Vector3.Slerp((self as ExplosiveSpear).rag[0, 4], (self as ExplosiveSpear).rag[0, 3], timeStacker).x;
                Vector2 normalized = ((self as ExplosiveSpear).rag[0, 0] - tie).normalized;
                Vector2 perp = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance((self as ExplosiveSpear).rag[0, 0], tie) / 5f;
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(0, (self as ExplosiveSpear).rag[0, 0] - normalized * d - perp * vel * 0.5f - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(1, (self as ExplosiveSpear).rag[0, 0] - normalized * d + perp * vel * 0.5f - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(2, tie + normalized * d - perp * vel - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(3, tie + normalized * d + perp * vel - camPos);
            }
            float aimRot = Custom.AimFromOneVectorToAnother(Vector2.zero, aimDir);
            for (int i = (self is ExplosiveSpear || self.bugSpear) ? 1 : 0; i >= 0; i--)
                sLeaser.sprites[i].rotation = aimRot;
        }

        private static void ElecSpearDrawSprites(On.MoreSlugcats.ElectricSpear.orig_DrawSprites orig, ElectricSpear self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (!GetLancerAimDir(self, 0f, out var aimDir)) return;
            float aimRot = Custom.AimFromOneVectorToAnother(Vector2.zero, aimDir);

            for (int i = 0; i < self.segments; i++)
            {
                Vector2 pos = ZapperAttachPos(i);
                sLeaser.sprites[1 + i].x = pos.x - camPos.x;
                sLeaser.sprites[1 + i].y = pos.y - camPos.y;
                sLeaser.sprites[1 + i].rotation = aimRot;
            }
            self.sparkPoint = self.PointAlongSpear(sLeaser, 0.9f);

            Vector2 ZapperAttachPos(int node)
            {
                Vector3 lastAimDir = aimDir * (float)node * -4f;
                return Vector2.Lerp(self.firstChunk.lastPos, self.firstChunk.pos, timeStacker) + new Vector2(aimDir.x, aimDir.y) * 30f + new Vector2(lastAimDir.x, lastAimDir.y);
            }
        }

        #endregion Spear
    }
}