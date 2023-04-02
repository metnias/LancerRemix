using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using static LancerRemix.Cat.ModifyCat;

namespace LancerRemix.Combat
{
    internal class WeaponPatch
    {
        internal static void Patch()
        {
            On.Spear.HitSomething += SpearHit;
            On.Spear.LodgeInCreature += SpearLodgeCreature;
            On.Spear.Update += SpearUpdate;
            On.Spear.DrawSprites += SpearDrawSprites;

            /// TODO: implement block and parry
            /// danger grasp lance
            /// monk difficulty adjust
            /// vulture mask
            /// story patch
            /// jolly & custom color ui support
            /// expedition support
        }

        #region Spear

        private static bool SpearHit(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (self.thrownBy is Player atkPlayer && IsLancer(atkPlayer))
            { // Retrieve spear
            }
            else if (result.obj is Player defPlayer && IsLancer(defPlayer))
            { // Parry check
            }
            return orig(self, result, eu);
        }

        private static void SpearLodgeCreature(On.Spear.orig_LodgeInCreature orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            orig(self, result, eu);
            if (!(self.thrownBy is Player player) || !IsLancer(player)) return;

            if (self is ExplosiveSpear)
            {
                GetSub<LancerSupplement>(player)?.ReleaseLanceSpear();
                return;
            }
            GetSub<LancerSupplement>(player)?.RetrieveLanceSpear(self);
        }

        private static Vector2 GetAimDir(Player lancer, float timeStacker)
        {
            var mainPos = Vector2.Lerp(lancer.mainBodyChunk.lastPos, lancer.mainBodyChunk.pos, timeStacker);
            var lanceDir = Custom.DirVec(mainPos, new Vector2(mainPos.x + (float)lancer.ThrowDirection, mainPos.y));
            // turn this using block timer
            return lanceDir;
        }

        private static void SpearUpdate(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig(self, eu);
            if (self.grabbedBy.Count <= 0 || !(self.grabbedBy[0].grabber is Player player) || !IsLancer(player))
                return;

            Vector2 aimDir = GetAimDir(player, 0f);

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

        private static void SpearDrawSprites(On.Spear.orig_DrawSprites orig, Spear spear,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig.Invoke(spear, sLeaser, rCam, timeStacker, camPos);
            if (!sLeaser.sprites[0].isVisible) return;
            if (spear.grabbedBy.Count <= 0 || !(spear.grabbedBy[0].grabber is Player player) || !IsLancer(player)) return;

            Vector2 aimDir = GetAimDir(player, timeStacker);
            if (spear is ExplosiveSpear)
            {
                (spear as ExplosiveSpear).rag[0, 0] = Vector2.Lerp(spear.firstChunk.lastPos, spear.firstChunk.pos, 1f) + aimDir * 15f;
                (spear as ExplosiveSpear).rag[0, 2] *= 0f;
                Vector2 tie = Vector2.Lerp((spear as ExplosiveSpear).rag[0, 1], (spear as ExplosiveSpear).rag[0, 0], timeStacker);
                float vel = 2f * Vector3.Slerp((spear as ExplosiveSpear).rag[0, 4], (spear as ExplosiveSpear).rag[0, 3], timeStacker).x;
                Vector2 normalized = ((spear as ExplosiveSpear).rag[0, 0] - tie).normalized;
                Vector2 perp = Custom.PerpendicularVector(normalized);
                float d = Vector2.Distance((spear as ExplosiveSpear).rag[0, 0], tie) / 5f;
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(0, (spear as ExplosiveSpear).rag[0, 0] - normalized * d - perp * vel * 0.5f - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(1, (spear as ExplosiveSpear).rag[0, 0] - normalized * d + perp * vel * 0.5f - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(2, tie + normalized * d - perp * vel - camPos);
                (sLeaser.sprites[2] as TriangleMesh).MoveVertice(3, tie + normalized * d + perp * vel - camPos);
            }
            for (int i = (!(spear is ExplosiveSpear)) ? 0 : 1; i >= 0; i--)
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, aimDir);
        }

        #endregion Spear
    }
}