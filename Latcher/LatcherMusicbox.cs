#if LATCHER

using MoreSlugcats;
using RWCustom;
using System;
using System.Collections.Generic;
using UnityEngine;
using Watcher;
using static LancerRemix.Latcher.LatcherPatch;
using Random = UnityEngine.Random;

namespace LancerRemix.Latcher
{
    internal static class LatcherMusicbox
    {
        internal static void SubPatch()
        {
            On.Room.Update += RoomUpdatePatch;
            On.MainLoopProcess.RawUpdate += GameRawUpdatePatch;
            On.RainWorldGame.GrafUpdate += GrafUpdateHalt;
            On.RoomCamera.SpriteLeaser.Update += SpriteLeaserPatch;
            On.Room.ShouldBeDeferred += TimelineDeferredPatch;
            On.RainCycle.Update += RainTimerPatch;
            On.Player.CanIPickThisUp += CanIPickUpPatch;

            On.LocustSystem.Swarm.IsTargetValid += NoLatcherLocustAttachOnRipple;

            worldTPS = playerTPS = 40f;
            latcherTimelineDrawables = new HashSet<IDrawable>();
        }

        private static float worldTPS;
        private static float playerTPS;
        private static bool didWorldTick;
        private static bool haltGrafUpdate;
        internal static float playerSlowRatio;
        private static float playerWorldRatio;
        private static float playerTimeStacker;
        private static HashSet<IDrawable> latcherTimelineDrawables;
        internal static bool IsLatcherRipple => worldTPS < 1f;

        private static void RoomUpdatePatch(On.Room.orig_Update orig, Room self)
        {
            didWorldTick = true;
            orig(self);
        }

        private static void GameRawUpdatePatch(On.MainLoopProcess.orig_RawUpdate orig, MainLoopProcess self, float dt)
        {
            haltGrafUpdate = false;
            playerWorldRatio = playerSlowRatio = 1f;
            playerTPS = worldTPS = self.framesPerSecond;
            float targetTPS = 40f;
            var ripplePlayers = new List<Player>();
            bool doSync = false;
            if (self is RainWorldGame game && IsStoryLatcher(game) && game.pauseMenu == null && game.processActive)
            {
                float maxRipple = 0f;
                var players = game.session.Players;
                if (players.Count < 1) goto normalSpeed;
                if (game.cameras[0]?.warpPointTimer != null) goto normalSpeed; // Deactivate timeline while warping

                #region CheckRippleAndTargetTPS

                for (int j = 0; j < players.Count; j++)
                {
                    if (players[j].realizedCreature is Player player)
                    {
                        if (player.room != null && player.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidMelt) > 0f)
                            targetTPS = Math.Min(targetTPS, Mathf.Lerp(targetTPS, 15f, player.room.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.VoidMelt) * Mathf.InverseLerp(-7000f, -2000f, player.mainBodyChunk.pos.y)));
                        if (IsPlayerLatcher(player) && player.camoProgress > 0f)
                        {
                            maxRipple = Mathf.Max(Mathf.Clamp01(player.camoProgress) * player.rippleLevel, maxRipple);
                            ripplePlayers.Add(player);
                        }
                        if (player.mushroomCounter > 0)
                            targetTPS = Math.Min(targetTPS, Mathf.Lerp(40f, 15f, player.Adrenaline));
                        if (player.redsIllness != null)
                            targetTPS *= player.redsIllness.TimeFactor;
                    }
                }
                if (Mathf.Approximately(maxRipple, 0f)) goto normalSpeed;

                targetTPS = Math.Min(targetTPS, 40f - game.cameras[0].ghostMode * 10f);

                #endregion CheckRippleAndTargetTPS

                #region CalculateTPSForMaxRipple

                if (maxRipple <= 2.5f)
                {
                    float t = Mathf.Clamp01(maxRipple * 2f);
                    playerTPS = Mathf.Lerp(40f, 25f, t);
                    worldTPS = Mathf.Lerp(40f, 25f, t);
                }
                else if (maxRipple <= 3.5f)
                {
                    // x2.0
                    float t = Mathf.Clamp01((maxRipple - 2.5f) * 2f);
                    playerTPS = Mathf.Lerp(25f, 30f, t);
                    worldTPS = Mathf.Lerp(25f, 15f, t);
                }
                else if (maxRipple <= 4.0f)
                {
                    // x3.0
                    float t = Mathf.Clamp01((maxRipple - 3.5f) * 2f);
                    playerTPS = Mathf.Lerp(30f, 33f, t);
                    worldTPS = Mathf.Lerp(15f, 11f, t);
                }
                else if (maxRipple <= 4.5f)
                {
                    // x4.0
                    float t = Mathf.Clamp01((maxRipple - 4.0f) * 2f);
                    playerTPS = Mathf.Lerp(33f, 36f, t);
                    worldTPS = Mathf.Lerp(11f, 9f, t);
                }
                else
                {
                    // xinf
                    float t = Mathf.Clamp01((maxRipple - 4.5f) * 2f);
                    playerTPS = Mathf.Lerp(36f, 40f, t);
                    worldTPS = Mathf.Lerp(9f, 0f, t);
                }

                #endregion CalculateTPSForMaxRipple

                worldTPS = Mathf.Min(targetTPS, worldTPS);
                playerTPS = Mathf.Min(targetTPS, playerTPS);
                if (ModManager.MMF)
                {
                    float slowFactor = 1f / Mathf.Max(MMF.cfgSlowTimeFactor.Value, .01f);
                    worldTPS *= slowFactor;
                    playerTPS *= slowFactor;
                    targetTPS *= slowFactor;
                }
                playerWorldRatio = playerTPS / Mathf.Max(worldTPS, 8f);
                playerSlowRatio = targetTPS / playerTPS;
                if (playerWorldRatio % 1f < .01f) doSync = true;

                if (game.devToolsActive)
                {
                    if (game.devToolsActive && Input.GetKey("a"))
                    {
                        self.framesPerSecond = 10;
                        goto normalSpeed;
                    }
                    else if (Input.GetKey("s") && game.devToolsActive)
                    {
                        self.framesPerSecond = 400;
                        goto normalSpeed;
                    }
                }

                haltGrafUpdate = playerWorldRatio > 1f;
                self.framesPerSecond = Mathf.Max(8, Mathf.CeilToInt(worldTPS));

                //Debug.Log($"Ripple{maxRipple:0.00} target{targetTPS:0.0} w{worldTPS:0.0}/p{playerTPS:0.0} (w{playerWorldRatio:0.00};p{playerSlowRatio:0.00})");
            }

        normalSpeed:
            latcherTimelineDrawables.Clear();
            didWorldTick = false;
            orig(self, dt);
            if (didWorldTick && doSync)
            {
                playerTimeStacker = self.myTimeStacker;
            }
            else if (playerWorldRatio > 1f)
            {
                playerTimeStacker += dt * playerTPS;
                //Debug.Log($"{worldTPS:0}/{playerTPS:0}>{playerWorldRatio:0.00}) ts{self.myTimeStacker:0.00}/{playerTimeStacker:0.00} worldTicked{(didWorldTick ? "O" : "X")}");
                int updateCount = 0;
                var rippleRooms = new HashSet<Room>();
                while (playerTimeStacker > 1f)
                {
                    PlayerUpdate();
                    playerTimeStacker -= 1f;
                    updateCount++;
                    if (updateCount > 2) playerTimeStacker = 0f;
                    if (playerTimeStacker > 1f) self.manager.rainWorld.RunRewiredUpdate();
                }

                void PlayerUpdate()
                {
                    var rwg = self as RainWorldGame;
                    if (rwg.cameras[0]?.room != null)
                    {
                        rippleRooms.Add(rwg.cameras[0].room);
                    }
                    foreach (var player in ripplePlayers)
                    {
                        if (player.room == null || player.room.game == null || !player.room.readyForAI) continue;
                        if (!rippleRooms.Contains(player.room))
                            rippleRooms.Add(player.room);
                    }

                    // Force player update
                    foreach (var room in rippleRooms)
                    {
                        // Update
                        int updateIndex = room.updateList.Count - 1;
                        while (updateIndex >= 0)
                        {
                            var ud = room.updateList[updateIndex];
                            if (ud.slatedForDeletetion || ud.room != room)
                            {
                                room.CleanOutObjectNotInThisRoom(ud);
                                --updateIndex;
                                continue;
                            }

                            if (!InLatcherTimeline(ud))
                            {
                                if (ud is PhysicalObject po)
                                {
                                    var backupRng = Random.state;
                                    Random.InitState(0);
                                    if (po.graphicsModule != null)
                                    {
                                        po.graphicsModule.Update();
                                        po.GraphicsModuleUpdated(true, room.game.evenUpdate);
                                    }
                                    else
                                    {
                                        if (ud is IDrawable id) latcherTimelineDrawables.Add(id);
                                        po.GraphicsModuleUpdated(false, room.game.evenUpdate);
                                    }
                                    Random.state = backupRng;
                                }

                                --updateIndex;
                                continue;
                            }

                            if (!room.game.pauseUpdate || ud is IRunDuringDialog)
                            {
                                ud.Update(room.game.evenUpdate);
                            }
                            if (ud.slatedForDeletetion || ud.room != room)
                            {
                                room.CleanOutObjectNotInThisRoom(ud);
                            }
                            else if (ud is PhysicalObject po)
                            {
                                if (po.graphicsModule != null)
                                {
                                    latcherTimelineDrawables.Add(po.graphicsModule);
                                    po.graphicsModule.Update();
                                    po.GraphicsModuleUpdated(true, room.game.evenUpdate);
                                }
                                else
                                {
                                    if (po is IDrawable id) latcherTimelineDrawables.Add(id);
                                    po.GraphicsModuleUpdated(false, room.game.evenUpdate);
                                }
                            }
                            else if (ud is IDrawable id) latcherTimelineDrawables.Add(id);

                            --updateIndex;
                        }

                        if (room.chunkGlue != null)
                        {
                            foreach (var chunkGlue in room.chunkGlue)
                                chunkGlue.moveChunk.pos = chunkGlue.otherChunk.pos + chunkGlue.relativePos;
                        }
                        room.chunkGlue = null;

                        // Collision
                        for (int p = 1; p < room.physicalObjects.Length; p++)
                        {
                            for (int q = 0; q < room.physicalObjects[p].Count; q++)
                            {
                                for (int r = q + 1; r < room.physicalObjects[p].Count; r++)
                                {
                                    if ((InLatcherTimeline(room.physicalObjects[p][q]) || InLatcherTimeline(room.physicalObjects[p][r])) &&
                                        (room.physicalObjects[p][q].abstractPhysicalObject.rippleLayer == room.physicalObjects[p][r].abstractPhysicalObject.rippleLayer || room.physicalObjects[p][q].abstractPhysicalObject.rippleBothSides || room.physicalObjects[p][r].abstractPhysicalObject.rippleBothSides) && Mathf.Abs(room.physicalObjects[p][q].bodyChunks[0].pos.x - room.physicalObjects[p][r].bodyChunks[0].pos.x) < room.physicalObjects[p][q].collisionRange + room.physicalObjects[p][r].collisionRange && Mathf.Abs(room.physicalObjects[p][q].bodyChunks[0].pos.y - room.physicalObjects[p][r].bodyChunks[0].pos.y) < room.physicalObjects[p][q].collisionRange + room.physicalObjects[p][r].collisionRange)
                                    {
                                        bool collided = false;
                                        bool grasped = false;
                                        if (room.physicalObjects[p][q] is Creature pqCrit && pqCrit.Template.grasps > 0)
                                        {
                                            foreach (var g in pqCrit.grasps)
                                            {
                                                if (g != null && g.grabbed == room.physicalObjects[p][r])
                                                {
                                                    grasped = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!grasped && room.physicalObjects[p][r] is Creature prCrit && prCrit.Template.grasps > 0)
                                        {
                                            foreach (var g in prCrit.grasps)
                                            {
                                                if (g != null && g.grabbed == room.physicalObjects[p][q])
                                                {
                                                    grasped = true;
                                                    break;
                                                }
                                            }
                                        }
                                        if (!grasped)
                                        {
                                            for (int t = 0; t < room.physicalObjects[p][q].bodyChunks.Length; t++)
                                            {
                                                for (int u = 0; u < room.physicalObjects[p][r].bodyChunks.Length; u++)
                                                {
                                                    if (room.physicalObjects[p][q].bodyChunks[t].collideWithObjects && room.physicalObjects[p][r].bodyChunks[u].collideWithObjects && Custom.DistLess(room.physicalObjects[p][q].bodyChunks[t].pos, room.physicalObjects[p][r].bodyChunks[u].pos, room.physicalObjects[p][q].bodyChunks[t].rad + room.physicalObjects[p][r].bodyChunks[u].rad))
                                                    {
                                                        float radSum = room.physicalObjects[p][q].bodyChunks[t].rad + room.physicalObjects[p][r].bodyChunks[u].rad;
                                                        float dist = Vector2.Distance(room.physicalObjects[p][q].bodyChunks[t].pos, room.physicalObjects[p][r].bodyChunks[u].pos);
                                                        Vector2 dir = Custom.DirVec(room.physicalObjects[p][q].bodyChunks[t].pos, room.physicalObjects[p][r].bodyChunks[u].pos);
                                                        float massRatio = room.physicalObjects[p][r].bodyChunks[u].mass / (room.physicalObjects[p][q].bodyChunks[t].mass + room.physicalObjects[p][r].bodyChunks[u].mass);
                                                        if (InLatcherTimeline(room.physicalObjects[p][q]))
                                                        {
                                                            room.physicalObjects[p][q].bodyChunks[t].pos -= (radSum - dist) * dir * massRatio;
                                                            room.physicalObjects[p][q].bodyChunks[t].vel -= (radSum - dist) * dir * massRatio;
                                                        }
                                                        if (InLatcherTimeline(room.physicalObjects[p][r]))
                                                        {
                                                            room.physicalObjects[p][r].bodyChunks[u].pos += (radSum - dist) * dir * (1f - massRatio);
                                                            room.physicalObjects[p][r].bodyChunks[u].vel += (radSum - dist) * dir * (1f - massRatio);
                                                        }
                                                        if (room.physicalObjects[p][q].bodyChunks[t].pos.x == room.physicalObjects[p][r].bodyChunks[u].pos.x)
                                                        {
                                                            if (InLatcherTimeline(room.physicalObjects[p][q])) room.physicalObjects[p][q].bodyChunks[t].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                                            if (InLatcherTimeline(room.physicalObjects[p][r])) room.physicalObjects[p][r].bodyChunks[u].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                                        }
                                                        if (!collided)
                                                        {
                                                            if (InLatcherTimeline(room.physicalObjects[p][q])) room.physicalObjects[p][q].Collide(room.physicalObjects[p][r], t, u);
                                                            if (InLatcherTimeline(room.physicalObjects[p][r])) room.physicalObjects[p][r].Collide(room.physicalObjects[p][q], u, t);
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
                    }

                    // Force HUD update
                    for (int i = 0; i < rwg.cameras.Length; i++)
                    {
                        if (rwg.cameras[i].hud == null) continue;
                        if (rwg.cameras[i].hud.owner.GetOwnerType() == HUD.HUD.OwnerType.Player)
                            rwg.cameras[i].hud.Update();
                        /*
                        foreach (var part in rwg.cameras[i].hud.parts)
                            if (part is DialogBox || part is Map) part.Update();
                            */
                    }

                    // Force Shortcut update with Player
                    if (rwg.shortcuts != null)
                    {
                        bool updateShortcut = false;
                        foreach (var vessel in rwg.shortcuts.transportVessels)
                        { if (vessel.creature is Player) { updateShortcut = true; break; } }
                        if (updateShortcut)
                        {
                            ++rwg.updateShortCut;
                            if (rwg.updateShortCut > 2)
                            {
                                rwg.updateShortCut = 0;
                                rwg.shortcuts.Update();
                            }
                        }
                        else if (IsLatcherRipple)
                            rwg.updateShortCut = 0; // disallow shortcut update while ripple
                    }

                    //Debug.Log($"{worldTPS:0}/{playerTPS:0}>{playerWorldRatio:0.00}) update{updateUDCount} graf{playerTimelineDrawables.Count}");
                }
                //Debug.Log($"{worldTPS:0}/{playerTPS:0}>{playerWorldRatio:0.00}) ts{self.myTimeStacker:0.00}/{playerTimeStacker:0.00} graf{playerTimelineDrawables.Count}");
            }

            if (haltGrafUpdate)
            {
                haltGrafUpdate = false;
                self.GrafUpdate(self.myTimeStacker);
            }
        }

        private static bool InLatcherTimeline(UpdatableAndDeletable ud)
        {
            if (ud == null) return false;
            if (ud is ISpecialWarp) return true;
            if (ud is IRunDuringDialog)
            {
                if (ud is CosmeticSprite cs)
                {
                    if (cs is CosmeticInsect) return false; // fast escape for commoners
                    if (cs is Ghost) return true;
                    if (cs is RippleRing) return true;
                    if (cs is RippleDeathEffect) return true;
                    if (cs is AdrenalineEffect) return true;
                    if (cs is ShockWave shockWave) return shockWave.life < .2f;

                    return false;
                }
                return true;
            }
            if (ud is CosmeticRipple) return true;
            if (ud is Explosion explosion) return explosion.frame < 8;
            if (ud is KarmicShockwave kShockWave) return kShockWave.frame < 8;
            if (ud is PoisonInjecter poisonInjecter) return InLatcherTimeline(poisonInjecter.crit);
            //if (ud is Conversation.IOwnAConversation) return true;
            if (ud is PhysicalObject po)
            {
                if (po.abstractPhysicalObject.rippleLayer == 1
                    || po.abstractPhysicalObject.rippleBothSides) return true;
                //if (po is VoidSpawn) return true;
                if (po is Player player) return IsPlayerLatcher(player);
                if (po.grabbedBy?.Count > 0 && po.grabbedBy[0].grabber is Player grabber)
                    return IsPlayerLatcher(grabber);
                if (po is Weapon w && w.mode == Weapon.Mode.Thrown && w.thrownBy is Player thrower)
                    return IsPlayerLatcher(thrower);
                if (po is PlayerCarryableItem pci && pci.forbiddenToPlayer > 0)
                    return true;
                return false;
            }
            return false;
        }

        private static void GrafUpdateHalt(On.RainWorldGame.orig_GrafUpdate orig, RainWorldGame self, float timeStacker)
        {
            if (haltGrafUpdate) return;
            orig(self, timeStacker);
        }

        private static void SpriteLeaserPatch(On.RoomCamera.SpriteLeaser.orig_Update orig, RoomCamera.SpriteLeaser self,
            float timeStacker, RoomCamera rCam, Vector2 camPos)
        {
            bool fixTime = false;
            var backupRng = Random.state;
            if (playerWorldRatio > 1f)
            {
                // Check whether replace timeStacker
                if (self.drawableObject != null && latcherTimelineDrawables.Contains(self.drawableObject))
                    timeStacker = playerTimeStacker;
                else if (IsLatcherRipple)
                {
                    timeStacker = 0f; // remove jitter
                    fixTime = true;
                    Random.InitState(0);
                }
            }
            orig(self, timeStacker, rCam, camPos);
            if (fixTime) Random.state = backupRng;
        }

        private static bool TimelineDeferredPatch(On.Room.orig_ShouldBeDeferred orig, Room self, UpdatableAndDeletable obj)
        {
            var res = orig(self, obj);
            if (res) return res;
            if (IsLatcherRipple && !InLatcherTimeline(obj) && !(obj is SoundEmitter)) return true;
            return res;
        }

        private static void RainTimerPatch(On.RainCycle.orig_Update orig, RainCycle cycle)
        {
            if (IsLatcherRipple) return;
            orig.Invoke(cycle);
        }

        private static bool CanIPickUpPatch(On.Player.orig_CanIPickThisUp orig, Player player, PhysicalObject obj)
        {
            if (playerWorldRatio > 1f)
            {
                if (!didWorldTick && obj is PlayerCarryableItem pci)
                {
                    if (pci.forbiddenToPlayer > 0) --pci.forbiddenToPlayer; // manual reduce
                    if (pci.blink > 0) --pci.blink;
                }
                if (IsLatcherRipple && obj is Weapon weapon)
                {
                    if (weapon.mode == Weapon.Mode.Thrown && !(weapon.thrownBy is Player))
                    {
                        weapon.mode = Weapon.Mode.Free;
                        if (orig(player, obj)) return true;
                        else { weapon.mode = Weapon.Mode.Thrown; return false; }
                    }
                }
            }
            return orig(player, obj);
        }

        private static bool NoLatcherLocustAttachOnRipple(On.LocustSystem.Swarm.orig_IsTargetValid orig, LocustSystem.Swarm self)
        {
            var res = orig(self);
            if (!res) return res;
            if (self.target is Player player && IsPlayerLatcher(player) && IsLatcherRipple)
                return false;
            return res;
        }
    }
}

#endif