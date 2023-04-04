using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using static LancerRemix.Cat.ModifyCat;

namespace LancerRemix.Combat
{
    internal static class CreaturePatch
    {
        internal static void Patch()
        {
            On.Creature.Violence += LancerStabNoStun;
            On.Vulture.Violence += VultureLancerDropMask;
        }

        private static void LancerStabNoStun(On.Creature.orig_Violence orig, Creature self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source?.owner is Player player && IsLancer(player))
                if (damage < 0.9f) stunBonus = -10000f;
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }

        private static void VultureLancerDropMask(On.Vulture.orig_Violence orig, Vulture vulture, BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos onAppendagePos, Creature.DamageType type, float damage, float stunBonus)
        {
            bool lancer = source?.owner is Spear && (source.owner as Spear).thrownBy is Player player
                && IsLancer(player);
            if (hitChunk != null && hitChunk.index == 4 && (vulture.State as Vulture.VultureState).mask
                && damage <= 0.9f && lancer && Random.value > Custom.LerpMap(damage, 0.0f, 0.9f, 0.0f, 0.5f))
            {
                vulture.DropMask(((directionAndMomentum == null) ? new Vector2(0f, 0f) : (directionAndMomentum.Value / 5f)) + Custom.RNV() * 7f * UnityEngine.Random.value);
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
    }
}