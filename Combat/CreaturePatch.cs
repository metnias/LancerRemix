﻿using LancerRemix.Cat;
using MoreSlugcats;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static LancerRemix.Cat.ModifyCat;
using Shipping = CreatureTemplate.Relationship;

namespace LancerRemix.Combat
{
    internal static class CreaturePatch
    {
        internal static void Patch()
        {
            On.Creature.Grab += CreatureGrabLancer;
            On.Creature.Violence += LancerViolencePatch;
            On.Lizard.Violence += LancerLizardViolencePatch;
            On.Creature.Stun += StunPatch;
            On.Vulture.Violence += VultureLancerDropMask;
            On.MoreSlugcats.VultureMaskGraphics.DrawSprites += MaskOnHornDrawPatch;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavHornMaskNoPickUp;
            On.LizardAI.IUseARelationshipTracker_UpdateDynamicRelationship += LizardHornOnMaskRelationship;
        }

        private static bool CreatureGrabLancer(On.Creature.orig_Grab orig, Creature self, PhysicalObject obj, int graspUsed, int chunkGrabbed, Creature.Grasp.Shareability shareability, float dominance, bool overrideEquallyDominant, bool pacifying)
        {
            var res = orig(self, obj, graspUsed, chunkGrabbed, shareability, dominance, overrideEquallyDominant, pacifying);
            if (!(obj is Player player) || !IsPlayerLancer(player)) return res;
            if (GetSub<LancerSupplement>(player)?.IsGrabParried == true)
            {
                self.ReleaseGrasp(graspUsed);
                return false;
            }
            return res;
        }

        private static void LancerViolencePatch(On.Creature.orig_Violence orig, Creature self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source?.owner is Spear spear && spear.thrownBy is Player atkPlayer && IsPlayerLancer(atkPlayer))
            {
                if (GetSub<LancerSupplement>(atkPlayer)?.SpendSpear == true) stunBonus *= 2f;
                else { stunBonus = -10000f; StunIgnores.Add(self.abstractCreature); }
            }
            if (self is Player player && IsPlayerLancer(player))
            { GetSub<LancerSupplement>(player)?.Violence(orig, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus); return; }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            StunIgnores.Remove(self.abstractCreature);
        }

        private static void LancerLizardViolencePatch(On.Lizard.orig_Violence orig, Lizard self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source?.owner is Spear spear && spear.thrownBy is Player atkPlayer && IsPlayerLancer(atkPlayer))
            {
                if (GetSub<LancerSupplement>(atkPlayer)?.SpendSpear == true) stunBonus *= 2f;
                else { stunBonus = -10000f; StunIgnores.Add(self.abstractCreature); }
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
            StunIgnores.Remove(self.abstractCreature);
        }

        private static readonly HashSet<AbstractCreature> StunIgnores = new HashSet<AbstractCreature>();

        private static void StunPatch(On.Creature.orig_Stun orig, Creature self, int st)
        {
            if (StunIgnores.Contains(self.abstractCreature)) st = 0;
            int s = self.stun;
            orig(self, st);
        }

        private static void VultureLancerDropMask(On.Vulture.orig_Violence orig, Vulture vulture, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            bool lancer = source?.owner is Spear && (source.owner as Spear).thrownBy is Player player
                && IsPlayerLancer(player);
            if (lancer && hitChunk != null && hitChunk.index == 4 && (vulture.State as Vulture.VultureState).mask
                && Random.value > Custom.LerpMap(damage, 0.0f, 0.9f, 0.0f, 0.5f))
            {
                vulture.DropMask(((directionAndMomentum == null) ? new Vector2(0f, 0f) : (directionAndMomentum.Value / 5f)) + Custom.RNV() * 7f * Random.value);
                damage *= 1.5f;
                //force stuck
                source.owner.room.AddObject(new ExplosionSpikes(source.owner.room, source.owner.firstChunk.pos, 5, 4f, 6f, 4.5f, 30f, new Color(1f, 1f, 1f, 0.5f)));
                source.owner.room.PlaySound(SoundID.Big_Needle_Worm_Impale_Terrain, source.owner.firstChunk, false, 1.2f, 1.0f);
                GetSub<LancerSupplement>((source.owner as Spear).thrownBy as Player)?.ReleaseLanceSpear();
            }
            float disencouraged = vulture.AI.disencouraged;
            orig.Invoke(vulture, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            if (lancer) vulture.AI.disencouraged = (disencouraged * 1.5f + vulture.AI.disencouraged) / 2.5f;
        }

        #region MaskOnHorn

        private static void MaskOnHornDrawPatch(On.MoreSlugcats.VultureMaskGraphics.orig_DrawSprites orig, VultureMaskGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            MaskOnHorn.AbstractOnHornStick hornStick = null;
            foreach (var stick in self.attachedTo.abstractPhysicalObject.stuckObjects)
                if (stick is MaskOnHorn.AbstractOnHornStick) { hornStick = stick as MaskOnHorn.AbstractOnHornStick; break; }
            if (hornStick == null || !(self.attachedTo is VultureMask)) goto NotHorned;

            PlayerGraphics pg = hornStick.Player.realizedObject?.graphicsModule as PlayerGraphics;
            Vector2 pos0, rot, anchor;
            pos0 = Vector2.Lerp((self.attachedTo as VultureMask).firstChunk.lastPos, (self.attachedTo as VultureMask).firstChunk.pos, timeStacker);
            float don = Mathf.Lerp((self.attachedTo as VultureMask).lastDonned, (self.attachedTo as VultureMask).donned, timeStacker);
            // float lgt = rCam.room.Darkness(pos0) * (1f - rCam.room.LightSourceExposure(pos0)) * 0.8f * (1f - (self.attachedTo as VultureMask).fallOffVultureMode);
            rot = Vector3.Slerp((self.attachedTo as VultureMask).lastRotationA, (self.attachedTo as VultureMask).rotationA, timeStacker);
            anchor = new Vector2(0f, 1f);

            //if (don <= 0f) { goto ApplyChanges; }
            float view = Mathf.Lerp((self.attachedTo as VultureMask).lastViewFromSide, (self.attachedTo as VultureMask).viewFromSide, timeStacker);
            Vector2 posM = Custom.DirVec(Vector2.Lerp(pg.drawPositions[1, 1], pg.drawPositions[1, 0], timeStacker), Vector2.Lerp(pg.drawPositions[0, 1], pg.drawPositions[0, 0], timeStacker));
            Vector2 pos0m = Vector2.Lerp(pg.drawPositions[0, 1], pg.drawPositions[0, 0], timeStacker) + posM * 3f;
            //pos0m = Vector2.Lerp(pos0m, Vector2.Lerp(pg.head.lastPos, pg.head.pos, timeStacker) + posM * 3f, 0.5f);
            pos0m = Vector2.Lerp(pos0m, Vector2.Lerp(pg.head.lastPos, pg.head.pos, timeStacker) - posM * 6f, 0.5f);
            pos0m += Vector2.Lerp(pg.lastLookDir, pg.lookDirection, timeStacker) * 1.5f;
            rot = Vector3.Slerp(rot, posM, don);

            if ((pg.owner as Player).eatCounter < 35)
            { //eating
                anchor = Vector3.Slerp(anchor, new Vector2(0f, -1f), don); //don
                pos0m += posM * Mathf.InverseLerp(35f, 15f, (float)(pg.owner as Player).eatCounter) * 9f;
                //Custom.LerpMap((float)(pg.owner as Player).eatCounter, 35f, 15f, 0f, 0.11f)
            }
            else
            {
                anchor = Vector3.Slerp(anchor, new Vector2(0f, 1f), don);
            }
            if (view != 0f)
            {
                rot = Custom.DegToVec(Custom.VecToDeg(rot) - 20f * view);
                anchor = Vector3.Slerp(anchor, Custom.DegToVec(-50f * view), Mathf.Abs(view));
                pos0m += posM * 2f * Mathf.Abs(view);
                pos0m -= Custom.PerpendicularVector(posM) * 4f * view;
            }
            pos0 = Vector2.Lerp(pos0, pos0m, don);

            self.overrideDrawVector = pos0;
            self.overrideRotationVector = rot;
            self.overrideAnchorVector = anchor;

        NotHorned:
            orig(self, sLeaser, rCam, timeStacker, camPos);
        }

        private static int ScavHornMaskNoPickUp(On.ScavengerAI.orig_CollectScore_PhysicalObject_bool orig, ScavengerAI self, PhysicalObject obj, bool weaponFiltered)
        {
            if (ModManager.MMF && !MMF.cfgHunterBackspearProtect.Value) goto NoProtect;
            if (obj is VultureMask mask)
            {
                foreach (var stick in mask.abstractPhysicalObject.stuckObjects)
                    if (stick is MaskOnHorn.AbstractOnHornStick) return 0;
            }
        NoProtect:
            return orig.Invoke(self, obj, weaponFiltered);
        }

        /// <summary>
        /// New code suggested by Shiny Kelp
        /// </summary>
        private static Shipping LizardHornOnMaskRelationship(On.LizardAI.orig_IUseARelationshipTracker_UpdateDynamicRelationship orig,
            LizardAI self, RelationshipTracker.DynamicRelationship dRelation)
        {
            if (dRelation.trackerRep.representedCreature?.realizedCreature is Player player)
            {
                if (!IsPlayerLancer(player)) goto NoLunter;
                var lunterSub = GetSub<LunterSupplement>(player);
                if (lunterSub == null) goto NoLunter;

                if (lunterSub.maskOnHorn.HasAMask)
                {
                    var auxGrasp = player.grasps[0];
                    player.grasps[0] = new Creature.Grasp(player, lunterSub.maskOnHorn.Mask, 0, 0, Creature.Grasp.Shareability.CanNotShare, 0f, false);
                    var result = orig(self, dRelation);
                    player.grasps[0] = auxGrasp;

                    ++self.usedToVultureMask; // horn mask is not as convincing
                    result.intensity *= 0.8f;
                    return result;
                }
            }

        NoLunter:
            return orig(self, dRelation);
        }

        #endregion MaskOnHorn
    }
}