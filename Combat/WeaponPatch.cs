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

            /// TODO: 
            /// monk difficulty adjust
            /// story patch
            /// jolly & custom color ui support
            /// expedition support
        }

        #region Spear

        private static bool SpearHit(On.Spear.orig_HitSomething orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            if (self.thrownBy is Player atkPlayer && IsPlayerLancer(atkPlayer))
            { // Retrieve spear
            }
            else if (result.obj is Player defPlayer && IsPlayerLancer(defPlayer))
            { // Parry check
            }
            return orig(self, result, eu);
        }

        private static void SpearLodgeCreature(On.Spear.orig_LodgeInCreature orig, Spear self, SharedPhysics.CollisionResult result, bool eu)
        {
            orig(self, result, eu);
            if (!(self.thrownBy is Player player) || !IsPlayerLancer(player)) return;

            if (self is ExplosiveSpear)
            {
                GetSub<LancerSupplement>(player)?.ReleaseLanceSpear();
                return;
            }
            GetSub<LancerSupplement>(player)?.RetrieveLanceSpear(self);
        }

        private static Vector2 GetAimDir(Spear spear, Player lancer, float timeStacker)
        {
            float block = GetSub<LancerSupplement>(lancer)?.BlockAmount(timeStacker) ?? 0f;
            if (block >= 0f)
                return Vector3.Slerp(lancer.ThrowDirection >= 0f ? Vector3.right : Vector3.left, Vector3.up, block);
            var rot = Vector3.Slerp(spear.lastRotation, spear.rotation, timeStacker);
            return Vector3.Slerp(lancer.ThrowDirection >= 0f ? Vector3.right : Vector3.left, rot.normalized, -block);
        }

        private static void SpearUpdate(On.Spear.orig_Update orig, Spear self, bool eu)
        {
            orig(self, eu);
            if (self.grabbedBy.Count <= 0 || !(self.grabbedBy[0].grabber is Player player) || !IsPlayerLancer(player))
                return;

            Vector2 aimDir = GetAimDir(self, player, 0f);;

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
            if (self.grabbedBy.Count <= 0 || !(self.grabbedBy[0].grabber is Player player) || !IsPlayerLancer(player)) return;

            Vector2 aimDir = GetAimDir(self, player, timeStacker);
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
            for (int i = (!(self is ExplosiveSpear)) ? 0 : 1; i >= 0; i--)
                sLeaser.sprites[i].rotation = Custom.AimFromOneVectorToAnother(Vector2.zero, aimDir);
        }

        #endregion Spear
    }
}