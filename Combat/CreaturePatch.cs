using LancerRemix.Cat;
using MoreSlugcats;
using RWCustom;
using UnityEngine;
using static LancerRemix.Cat.ModifyCat;

namespace LancerRemix.Combat
{
    internal static class CreaturePatch
    {
        internal static void Patch()
        {
            On.Creature.Grab += CreatureGrabLancer;
            On.Creature.Violence += LancerViolencePatch;
            On.Vulture.Violence += VultureLancerDropMask;
            On.MoreSlugcats.VultureMaskGraphics.DrawSprites += MaskDrawPatch;
            On.ScavengerAI.CollectScore_PhysicalObject_bool += ScavHornMaskNoPickUp;
            On.LizardAI.DetermineBehavior += LizardHornMaskBehavior;
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
            if (source?.owner is Player atkPlayer && IsPlayerLancer(atkPlayer))
            {
                if (GetSub<LancerSupplement>(atkPlayer)?.IsSlideLance == true) stunBonus *= 2f;
                else stunBonus = -10000f;
            }
            if (self is Player player && IsPlayerLancer(player))
            { GetSub<LancerSupplement>(player)?.Violence(orig, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus); return; }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
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
                source.owner.room.PlaySound(SoundID.Spear_Fragment_Bounce, source.owner.firstChunk.pos, 1.2f, 0.8f);
                GetSub<LancerSupplement>((source.owner as Spear).thrownBy as Player)?.ReleaseLanceSpear();
            }
            float disencouraged = vulture.AI.disencouraged;
            orig.Invoke(vulture, source, directionAndMomentum, hitChunk, onAppendagePos, type, damage, stunBonus);
            if (lancer) vulture.AI.disencouraged = (disencouraged * 1.5f + vulture.AI.disencouraged) / 2.5f;
        }

        #region HornOnMask

        private static void MaskDrawPatch(On.MoreSlugcats.VultureMaskGraphics.orig_DrawSprites orig, VultureMaskGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            MaskOnHorn.AbstractOnHornStick hornStick = null;
            foreach (var stick in self.attachedTo.abstractPhysicalObject.stuckObjects)
                if (stick is MaskOnHorn.AbstractOnHornStick) { hornStick = stick as MaskOnHorn.AbstractOnHornStick; break; }
            if (hornStick == null || !(self.attachedTo is VultureMask))
            {
                if (self.attachedTo is VultureMask)
                {
                    (self.attachedTo as VultureMask).lastDonned = 0f; (self.attachedTo as VultureMask).donned = 0f; //don't put on mask
                }
                orig(self, sLeaser, rCam, timeStacker, camPos);
                return;
            }

            PlayerGraphics pg = hornStick.Player.realizedObject?.graphicsModule as PlayerGraphics;
            Vector2 pos0, rotA, rotB;
            float lgt, don;

            pos0 = Vector2.Lerp((self.attachedTo as VultureMask).firstChunk.lastPos, (self.attachedTo as VultureMask).firstChunk.pos, timeStacker);
            don = Mathf.Lerp((self.attachedTo as VultureMask).lastDonned, (self.attachedTo as VultureMask).donned, timeStacker);
            lgt = rCam.room.Darkness(pos0) * (1f - rCam.room.LightSourceExposure(pos0)) * 0.8f * (1f - (self.attachedTo as VultureMask).fallOffVultureMode);
            rotA = Vector3.Slerp((self.attachedTo as VultureMask).lastRotationA, (self.attachedTo as VultureMask).rotationA, timeStacker);
            rotB = new Vector2(0f, 1f); //Vector3.Slerp(mask.lastRotationB, mask.rotationB, timeStacker);

            //if (don <= 0f) { goto ApplyChanges; }
            float view = Mathf.Lerp((self.attachedTo as VultureMask).lastViewFromSide, (self.attachedTo as VultureMask).viewFromSide, timeStacker);
            Vector2 posM = Custom.DirVec(Vector2.Lerp(pg.drawPositions[1, 1], pg.drawPositions[1, 0], timeStacker), Vector2.Lerp(pg.drawPositions[0, 1], pg.drawPositions[0, 0], timeStacker));
            Vector2 pos0m = Vector2.Lerp(pg.drawPositions[0, 1], pg.drawPositions[0, 0], timeStacker) + posM * 3f;
            //pos0m = Vector2.Lerp(pos0m, Vector2.Lerp(pg.head.lastPos, pg.head.pos, timeStacker) + posM * 3f, 0.5f);
            pos0m = Vector2.Lerp(pos0m, Vector2.Lerp(pg.head.lastPos, pg.head.pos, timeStacker) - posM * 6f, 0.5f);
            pos0m += Vector2.Lerp(pg.lastLookDir, pg.lookDirection, timeStacker) * 1.5f;
            rotA = Vector3.Slerp(rotA, posM, don);
            if ((pg.owner as Player).eatCounter < 35)
            { //eating
                rotB = Vector3.Slerp(rotB, new Vector2(0f, -1f), don); //don
                pos0m += posM * Mathf.InverseLerp(35f, 15f, (float)(pg.owner as Player).eatCounter) * 9f;
                //Custom.LerpMap((float)(pg.owner as Player).eatCounter, 35f, 15f, 0f, 0.11f)
            }
            else
            {
                rotB = Vector3.Slerp(rotB, new Vector2(0f, 1f), don);
            }
            if (view != 0f)
            {
                rotA = Custom.DegToVec(Custom.VecToDeg(rotA) - 20f * view);
                rotB = Vector3.Slerp(rotB, Custom.DegToVec(-50f * view), Mathf.Abs(view));
                pos0m += posM * 2f * Mathf.Abs(view);
                pos0m -= Custom.PerpendicularVector(posM) * 4f * view;
            }
            pos0 = Vector2.Lerp(pos0, pos0m, don);
            //ApplyChanges:
            float deg = Custom.VecToDeg(rotB);
            int idx = Custom.IntClamp(Mathf.RoundToInt(Mathf.Abs(deg / 180f) * 8f), 0, 8);
            float size = (!(self.attachedTo as VultureMask).King) ? 1f : 1.15f;
            for (int i = 0; i < ((!(self.attachedTo as VultureMask).King) ? 3 : 4); i++)
            {
                sLeaser.sprites[i].element = Futile.atlasManager.GetElementWithName(((i != 3) ? "KrakenMask" : "KrakenArrow") + idx);
                sLeaser.sprites[i].rotation = Custom.VecToDeg(rotA);
                sLeaser.sprites[i].x = pos0.x - camPos.x;
                sLeaser.sprites[i].y = pos0.y - camPos.y;
                sLeaser.sprites[i].anchorY = Custom.LerpMap(Mathf.Abs(deg), 0f, 100f, 0.5f, 0.675f, 2.1f);
                sLeaser.sprites[i].anchorX = 0.5f - rotB.x * 0.1f * Mathf.Sign(deg);
            }
            sLeaser.sprites[1].scaleX *= 0.85f * size;
            sLeaser.sprites[1].scaleY = 0.9f * size;
            sLeaser.sprites[2].scaleY = 1.1f * size;
            sLeaser.sprites[2].anchorY += 0.015f;

            if ((self.attachedTo as VultureMask).blink > 0 && UnityEngine.Random.value < 0.5f)
            {
                for (int j = 0; j < ((!(self.attachedTo as VultureMask).King) ? 3 : 4); j++)
                { sLeaser.sprites[j].color = new Color(1f, 1f, 1f); }
            }
            else
            {
                hornStick.maskOnHorn.color = Color.Lerp(Color.Lerp(hornStick.maskOnHorn.ColorA.rgb, new Color(1f, 1f, 1f), 0.35f * (self.attachedTo as VultureMask).fallOffVultureMode), hornStick.maskOnHorn.blackColor, Mathf.Lerp(0.2f, 1f, Mathf.Pow(lgt, 2f)));
                sLeaser.sprites[0].color = hornStick.maskOnHorn.color;
                sLeaser.sprites[1].color = Color.Lerp(hornStick.maskOnHorn.color, hornStick.maskOnHorn.blackColor, Mathf.Lerp(0.75f, 1f, lgt));
                sLeaser.sprites[2].color = Color.Lerp(hornStick.maskOnHorn.color, hornStick.maskOnHorn.blackColor, Mathf.Lerp(0.75f, 1f, lgt));
                if ((self.attachedTo as VultureMask).King)
                {
                    sLeaser.sprites[3].color = Color.Lerp(Color.Lerp(Color.Lerp(HSLColor.Lerp(hornStick.maskOnHorn.ColorA, hornStick.maskOnHorn.ColorB, 0.8f - 0.3f * (self.attachedTo as VultureMask).fallOffVultureMode).rgb, hornStick.maskOnHorn.blackColor, 0.53f),
                        Color.Lerp(hornStick.maskOnHorn.ColorA.rgb, new Color(1f, 1f, 1f), 0.35f), 0.1f), hornStick.maskOnHorn.blackColor, 0.6f * lgt);
                }
            }
            if ((self.attachedTo as VultureMask).slatedForDeletetion || (self.attachedTo as VultureMask).room != rCam.room)
            {
                sLeaser.CleanSpritesAndRemove();
            }
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

        private static LizardAI.Behavior LizardHornMaskBehavior(On.LizardAI.orig_DetermineBehavior orig, LizardAI self)
        {
            if (self.creature.creatureTemplate.type == CreatureTemplate.Type.BlackLizard || self.creature.creatureTemplate.type == CreatureTemplate.Type.RedLizard)
                goto NoLunter;
            Tracker.CreatureRepresentation rep = null;
            Player player = null;
            for (int i = 0; i < self.tracker.CreaturesCount; i++)
            {
                rep = self.tracker.GetRep(i);
                if (rep.representedCreature != null && rep.representedCreature.creatureTemplate.type == CreatureTemplate.Type.Slugcat)
                {
                    if (rep.representedCreature.realizedCreature != null) { player = rep.representedCreature.realizedCreature as Player; break; }
                }
            }
            if (player == null || !IsPlayerLancer(player)) goto NoLunter;
            var lunterSub = GetSub<LunterSupplement>(player);
            if (lunterSub == null) goto NoLunter;

            if (rep.VisualContact && lunterSub.maskOnHorn.HasAMask)
            {
                bool king = lunterSub.maskOnHorn.Mask.King;
                if (self.usedToVultureMask < (!king ? 700 : 1200))
                {
                    self.usedToVultureMask += 2;
                    if (self.creature.creatureTemplate.type == CreatureTemplate.Type.GreenLizard && !king)
                    {
                        rep.dynamicRelationship.currentRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Ignores, 0f);
                    }
                    else
                    {
                        rep.dynamicRelationship.currentRelationship = new CreatureTemplate.Relationship(CreatureTemplate.Relationship.Type.Afraid,
                            Mathf.InverseLerp((float)(!king ? 700 : 1200), 600f, (float)self.usedToVultureMask)
                            * (!king ? ((self.creature.creatureTemplate.type != CreatureTemplate.Type.BlueLizard) ? 0.4f : 0.6f) //0.6 0.8
                            : ((self.creature.creatureTemplate.type != CreatureTemplate.Type.GreenLizard) ? 0.7f : 0.3f))); //0.9 0.4
                    }

                    self.preyTracker.ForgetPrey(player.abstractCreature);
                    self.threatTracker.AddThreatCreature(rep);
                }
            }
        NoLunter:
            return orig.Invoke(self);
        }

        #endregion HornOnMask
    }
}