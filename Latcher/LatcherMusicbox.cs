using RWCustom;
using UnityEngine;
using static LancerRemix.Latcher.LatcherPatch;
using Random = UnityEngine.Random;

namespace LancerRemix.Latcher
{
    internal static class LatcherMusicbox
    {
        internal static void SubPatch()
        {
            On.Room.Update += new On.Room.hook_Update(RoomUpdatePatch);
            On.AbstractRoom.Update += new On.AbstractRoom.hook_Update(AbstractRoomPatch);
            On.RoomCamera.SpriteLeaser.Update += new On.RoomCamera.SpriteLeaser.hook_Update(SpriteLeaserPatch);
            On.ShortcutHandler.Update += new On.ShortcutHandler.hook_Update(ShortcutHandlerPatch);
            On.RainCycle.Update += new On.RainCycle.hook_Update(RainTimerPatch);
            On.BodyChunk.Update += new On.BodyChunk.hook_Update(BodyChunkPatch);
            On.RainWorldGame.RawUpdate += new On.RainWorldGame.hook_RawUpdate(GameRawUpdatePatch);
            On.Player.CanIPickThisUp += new On.Player.hook_CanIPickThisUp(CanIPickUpPatch);
            On.WaterNut.Swell += new On.WaterNut.hook_Swell(WaterNutSwellPatch);
            On.VirtualMicrophone.DrawUpdate += new On.VirtualMicrophone.hook_DrawUpdate(MicrophoneDrawPatch);
            worldSpeed = 1f; worldTPS = playerTPS = 40f;
        }

        private static float worldTPS;
        private static float playerTPS;
        private static float worldSpeed;
        private const float STOP_THRESHOLD = 0.05f;

        private static void RoomUpdatePatch(On.Room.orig_Update orig, Room room)
        {
            worldSpeed = 1f;
            playerTPS = worldTPS = 40;
            if (room.game == null || !IsStoryLatcher(room.game)) goto normalSpeed;
            if (room.fullyLoaded && (!room.abstractRoom.gate && !room.abstractRoom.shelter))
            {
                #region CheckRipple

                var players = room.game.Players;
                if (players.Count < 1) goto normalSpeed;

                float maxRipple = 0f;
                for (int i = players.Count - 1; i >= 0; i--)
                {
                    if (!(players[i].realizedCreature is Player player)) continue;
                    maxRipple = Mathf.Max(player.camoProgress * player.rippleLevel, maxRipple);
                }
                if (Mathf.Approximately(maxRipple, 0f)) goto normalSpeed;

                if (maxRipple <= 2.5f)
                {
                    playerTPS = Mathf.Lerp(40f, 15f, maxRipple * 2f);
                    worldTPS = Mathf.Lerp(40f, 15f, maxRipple * 2f);
                }
                else if (maxRipple <= 3.5f)
                {
                    playerTPS = Mathf.Lerp(15f, 24f, (maxRipple - 2.5f) * 2f);
                    worldTPS = Mathf.Lerp(15f, 12f, (maxRipple - 2.5f) * 2f);
                }
                else if (maxRipple <= 4.5f)
                {
                    playerTPS = Mathf.Lerp(24f, 32f, (maxRipple - 3.5f) * 2f);
                    worldTPS = Mathf.Lerp(12f, 8f, (maxRipple - 3.5f) * 2f);
                }
                else
                {
                    playerTPS = Mathf.Lerp(32f, 40f, (maxRipple - 4.5f) * 2f);
                    worldTPS = Mathf.Lerp(8f, 0f, (maxRipple - 4.5f) * 2f);
                }
                worldSpeed = worldTPS / 40f;

                #endregion CheckRipple

                room.waterObject?.Update();
                room.fliesRoomAi?.Update(room.game.evenUpdate);
                if (!Mathf.Approximately(worldSpeed, 0f)) room.socialEventRecognizer.Update();
                int updateIndex = room.updateList.Count - 1;
                while (updateIndex >= 0)
                {
                    var ud = room.updateList[updateIndex];
                    if (ud is Player player)
                    {
                        // TODO: implement playerTPS
                        player.Update(room.game.evenUpdate);
                        if (player.dangerGraspTime > 0 && Random.value > worldSpeed) player.dangerGraspTime--;
                        player.graphicsModule.Update();
                        player.GraphicsModuleUpdated(true, room.game.evenUpdate);

                        updateIndex--;
                        continue;
                    }

                    // Freeze
                    if (worldSpeed < STOP_THRESHOLD)
                    {
                        if (ud is PhysicalObject)
                        {
                            if ((ud as PhysicalObject).grabbedBy.Count > 0) // && (updatableAndDeletable as PhysicalObject).grabbedBy[0].grabber is Player
                                goto forceUpdate;
                            else if (ud is VoidSpawn) // VoidSpawn ignore time shift
                                goto forceUpdate;
                        }
                        updateIndex--;
                        continue;

                    forceUpdate:
                        ud.Update(room.game.evenUpdate);
                        if (ud is PhysicalObject po)
                        {
                            if (po.graphicsModule != null)
                            {
                                po.graphicsModule.Update();
                                po.GraphicsModuleUpdated(true, room.game.evenUpdate);
                            }
                            else
                            {
                                po.GraphicsModuleUpdated(false, room.game.evenUpdate);
                            }
                        }
                        updateIndex--;
                        continue;
                    }

                    // Slowed
                    ud.Update(room.game.evenUpdate);
                    if (ud is PhysicalObject p)
                    {
                        if (p.graphicsModule != null)
                        {
                            p.graphicsModule.Update();
                            p.GraphicsModuleUpdated(true, room.game.evenUpdate);
                        }
                        else
                        {
                            p.GraphicsModuleUpdated(false, room.game.evenUpdate);
                        }
                    }
                    updateIndex--;
                }
                if (room.chunkGlue != null)
                {
                    foreach (ChunkGlue chunkGlue in room.chunkGlue)
                    { chunkGlue.moveChunk.pos = chunkGlue.otherChunk.pos + chunkGlue.relativePos; }
                }
                room.chunkGlue = null;
                for (int j = 1; j < room.physicalObjects.Length; j++)
                {
                    for (int k = 0; k < room.physicalObjects[j].Count; k++)
                    {
                        for (int l = k + 1; l < room.physicalObjects[j].Count; l++)
                        {
                            if (Mathf.Abs(room.physicalObjects[j][k].bodyChunks[0].pos.x - room.physicalObjects[j][l].bodyChunks[0].pos.x)
                                < room.physicalObjects[j][k].collisionRange + room.physicalObjects[j][l].collisionRange
                                && Mathf.Abs(room.physicalObjects[j][k].bodyChunks[0].pos.y - room.physicalObjects[j][l].bodyChunks[0].pos.y)
                                < room.physicalObjects[j][k].collisionRange + room.physicalObjects[j][l].collisionRange)
                            {
                                bool collided = false;
                                bool grabbed = false;
                                if (room.physicalObjects[j][k] is Creature && (room.physicalObjects[j][k] as Creature).Template.grasps > 0)
                                {
                                    foreach (Creature.Grasp grasp in (room.physicalObjects[j][k] as Creature).grasps)
                                    {
                                        if (grasp != null && grasp.grabbed == room.physicalObjects[j][l])
                                        {
                                            grabbed = true;
                                            break;
                                        }
                                    }
                                }
                                if (!grabbed && room.physicalObjects[j][l] is Creature && (room.physicalObjects[j][l] as Creature).Template.grasps > 0)
                                {
                                    foreach (Creature.Grasp grasp2 in (room.physicalObjects[j][l] as Creature).grasps)
                                    {
                                        if (grasp2 != null && grasp2.grabbed == room.physicalObjects[j][k])
                                        {
                                            grabbed = true;
                                            break;
                                        }
                                    }
                                }
                                if (!grabbed)
                                {
                                    for (int p = 0; p < room.physicalObjects[j][k].bodyChunks.Length; p++)
                                    {
                                        for (int q = 0; q < room.physicalObjects[j][l].bodyChunks.Length; q++)
                                        {
                                            if (room.physicalObjects[j][k].bodyChunks[p].collideWithObjects && room.physicalObjects[j][l].bodyChunks[q].collideWithObjects
                                                && Custom.DistLess(room.physicalObjects[j][k].bodyChunks[p].pos, room.physicalObjects[j][l].bodyChunks[q].pos,
                                                room.physicalObjects[j][k].bodyChunks[p].rad + room.physicalObjects[j][l].bodyChunks[q].rad))
                                            {
                                                float radSum = room.physicalObjects[j][k].bodyChunks[p].rad + room.physicalObjects[j][l].bodyChunks[q].rad;
                                                float dist = Vector2.Distance(room.physicalObjects[j][k].bodyChunks[p].pos, room.physicalObjects[j][l].bodyChunks[q].pos);
                                                Vector2 dir = Custom.DirVec(room.physicalObjects[j][k].bodyChunks[p].pos, room.physicalObjects[j][l].bodyChunks[q].pos) * worldSpeed;
                                                float massRatio = room.physicalObjects[j][l].bodyChunks[q].mass / (room.physicalObjects[j][k].bodyChunks[p].mass + room.physicalObjects[j][l].bodyChunks[q].mass);
                                                room.physicalObjects[j][k].bodyChunks[p].pos -= (radSum - dist) * dir * massRatio;
                                                room.physicalObjects[j][k].bodyChunks[p].vel -= (radSum - dist) * dir * massRatio;
                                                room.physicalObjects[j][l].bodyChunks[q].pos += (radSum - dist) * dir * (1f - massRatio);
                                                room.physicalObjects[j][l].bodyChunks[q].vel += (radSum - dist) * dir * (1f - massRatio);
                                                if (room.physicalObjects[j][k].bodyChunks[p].pos.x == room.physicalObjects[j][l].bodyChunks[q].pos.x)
                                                {
                                                    room.physicalObjects[j][k].bodyChunks[p].vel += Custom.DegToVec(Random.value * 360f) * 0.0001f;
                                                    room.physicalObjects[j][l].bodyChunks[q].vel += Custom.DegToVec(Random.value * 360f) * 0.0001f;
                                                }
                                                if (!collided)
                                                {
                                                    room.physicalObjects[j][k].Collide(room.physicalObjects[j][l], p, q);
                                                    room.physicalObjects[j][l].Collide(room.physicalObjects[j][k], q, p);
                                                }
                                                collided = true;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                return;
            }
        normalSpeed:
            orig(room);
        }

        private static void GameRawUpdatePatch(On.RainWorldGame.orig_RawUpdate orig, RainWorldGame game, float dt)
        {
            orig(game, dt);
            game.framesPerSecond = Mathf.Max(1, Mathf.CeilToInt(worldTPS));
        }

        private static void AbstractRoomPatch(On.AbstractRoom.orig_Update orig, AbstractRoom room, int timePassed)
        {
            int newTime = Mathf.RoundToInt(timePassed * worldSpeed);
            if (Mathf.Approximately(newTime, 0f))
            {
                if (Random.value < worldSpeed)
                    orig(room, 1);
            }
            else
                orig(room, newTime);
        }

        private static void SpriteLeaserPatch(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser leaser, float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            orig(leaser, timeStacker * worldSpeed, rCam, camPos);
        }

        private static void RainTimerPatch(On.RainCycle.orig_Update orig, RainCycle cycle)
        {
            if (worldSpeed < STOP_THRESHOLD) return;
            else if (Mathf.Approximately(worldSpeed, 1f)) { orig.Invoke(cycle); return; }
            else
            {
                if (Random.value < worldSpeed) orig(cycle);
            }
        }

        private static void ShortcutHandlerPatch(On.ShortcutHandler.orig_Update orig, ShortcutHandler handler)
        {
            for (int i = handler.transportVessels.Count - 1; i >= 0; i--)
            { if (handler.transportVessels[i].creature is Player) { orig(handler); return; } }
            if (Mathf.Approximately(worldSpeed, 1f) || (worldSpeed >= STOP_THRESHOLD && Random.value < worldSpeed))
                orig(handler);
        }

        private static void BodyChunkPatch(On.BodyChunk.orig_Update orig, BodyChunk bodyChunk)
        {
            if (Mathf.Approximately(worldSpeed, 1f))
            {
                orig(bodyChunk); return;
            }

            bool isOrRelatedToPlayer = bodyChunk.owner is Player || (bodyChunk.owner.grabbedBy.Count > 0 && bodyChunk.owner.grabbedBy[0].grabber is Player)
                || bodyChunk.setPos != null;
            if (!isOrRelatedToPlayer && worldSpeed < STOP_THRESHOLD) return;
            Vector2 origV = bodyChunk.vel;
            if (float.IsNaN(origV.y)) origV.y = 0f;
            if (float.IsNaN(origV.x)) origV.x = 0f;
            orig(bodyChunk);
            if (!isOrRelatedToPlayer && bodyChunk.owner.room != null)
            {
                bodyChunk.pos.x = Mathf.Lerp(bodyChunk.lastPos.x, bodyChunk.pos.x, worldSpeed);
                bodyChunk.pos.y = Mathf.Lerp(bodyChunk.lastPos.y, bodyChunk.pos.y, worldSpeed);
                bodyChunk.vel.x = Mathf.Lerp(origV.x, bodyChunk.vel.x, worldSpeed);
                bodyChunk.vel.y = Mathf.Lerp(origV.y, bodyChunk.vel.y, worldSpeed);
            }
        }

        private static bool CanIPickUpPatch(On.Player.orig_CanIPickThisUp orig, Player player, PhysicalObject obj)
        {
            if (worldSpeed < STOP_THRESHOLD && obj is Weapon)
            {
                if ((obj as Weapon).mode == Weapon.Mode.Thrown && !((obj as Weapon).thrownBy is Player))
                {
                    (obj as Weapon).mode = Weapon.Mode.Free;
                    if (orig(player, obj)) return true;
                    else { (obj as Weapon).mode = Weapon.Mode.Thrown; return false; }
                }
            }
            return orig(player, obj);
        }

        private static void WaterNutSwellPatch(On.WaterNut.orig_Swell orig, WaterNut nut)
        {
            if (Mathf.Approximately(worldSpeed, 1f))
                orig(nut);
        }

        private static void MicrophoneDrawPatch(On.VirtualMicrophone.orig_DrawUpdate orig, VirtualMicrophone mic, float timeStacker, float timeSpeed)
        {
            if (worldSpeed > STOP_THRESHOLD)
                orig(mic, timeStacker, timeSpeed);
            else
                orig(mic, timeStacker, timeSpeed * Mathf.Max(worldSpeed, 0.2f));
        }
    }
}