using LancerRemix.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LancerRemix.Combat
{
    internal static class CreaturePatch
    {
        internal static void Patch()
        {
            On.Creature.Violence += LancerStabNoStun;
        }

        private static void LancerStabNoStun(On.Creature.orig_Violence orig, Creature self,
            BodyChunk source, Vector2? directionAndMomentum, BodyChunk hitChunk, PhysicalObject.Appendage.Pos hitAppendage, Creature.DamageType type, float damage, float stunBonus)
        {
            if (source?.owner is Player player && ModifyCat.IsLancer(player))
            {
                if (damage < 1f) stunBonus = -10000f;
            }
            orig(self, source, directionAndMomentum, hitChunk, hitAppendage, type, damage, stunBonus);
        }
    }
}