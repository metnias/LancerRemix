using UnityEngine;
using static LancerRemix.Latcher.LatcherPatch;
using Random = UnityEngine.Random;

namespace LancerRemix.Latcher
{
    internal static class LatcherMusicbox
    {
        internal static void SubPatch()
        {
            On.Room.Update += RoomUpdatePatch;
            On.Room.ShouldBeDeferred += ShouldObjBeDefered;
            On.AbstractRoom.Update += AbstractRoomPatch;
            On.RoomCamera.SpriteLeaser.Update += SpriteLeaserPatch;
            On.ShortcutHandler.Update += ShortcutHandlerPatch;
            On.RainCycle.Update += RainTimerPatch;
            On.BodyChunk.Update += BodyChunkPatch;
            On.MainLoopProcess.RawUpdate += GameRawUpdatePatch;
            On.Player.CanIPickThisUp += CanIPickUpPatch;
            On.WaterNut.Swell += WaterNutSwellPatch;
            On.VirtualMicrophone.DrawUpdate += MicrophoneDrawPatch;
            worldSpeed = 1f; worldTPS = playerTPS = 40f;
        }

        private static float worldTPS;
        private static float playerTPS;
        private static float worldSpeed;
        private const float STOP_THRESHOLD = 0.05f;
        private static bool doWorldTick;
        internal static float playerSlowRatio;

        private static void RoomUpdatePatch(On.Room.orig_Update orig, Room self)
        {
            doWorldTick = true;
            if (self.game == null || !IsStoryLatcher(self.game)
                || Mathf.Approximately(worldSpeed, 1f)) goto normalSpeed;
            doWorldTick = Random.value < worldSpeed;
        /*
        if (self.fullyLoaded && (!self.abstractRoom.gate && !self.abstractRoom.shelter))
        {
            if (self.snowSources.Count == 0) self.snow = false;
            self.UpdateWindDirection();
            if (self.waitToEnterAfterFullyLoaded > 0 && self.fullyLoaded) self.waitToEnterAfterFullyLoaded--;
            self.lastBackgroundNoise = self.backgroundNoise;
            self.backgroundNoise = Mathf.Max(0f, self.backgroundNoise - 0.05f * worldSpeed);
            if (self.game.pauseUpdate)
                self.backgroundNoise = Mathf.Lerp(self.backgroundNoise, 0f, 0.05f);
            self.aidataprepro?.Update();
            if (self.waterObject != null)
            {
                if (self.defaultWaterLevel == -2000 && self.waterObject.fWaterLevel < -400f && self.waterObject.fWaterLevel > -2000f)
                    self.waterObject.fWaterLevel = -2000f;
                self.waterObject.Update();
            }
            if (!Mathf.Approximately(worldSpeed, 0f))
            {
                self.socialEventRecognizer.Update();
            }
            self.UpdateSentientRotEffect();
            self.darkenLightsFactor = self.roomSettings.GetEffectAmount(RoomSettings.RoomEffect.Type.DarkenLights);
            if (self.DustStormIntensity > 0f)
            {
                float dustStormIntensity = self.DustStormIntensity;
                Shader.SetGlobalFloat(RainWorld.ShadPropDustWaveProgress, dustStormIntensity);
                dustStormIntensity = Mathf.InverseLerp(0.4f, 0.2f, dustStormIntensity);
                self.roomSettings.Clouds = self.cloudsNdarken.x + dustStormIntensity * (1f - self.cloudsNdarken.x);
                self.darkenLightsFactor = self.cloudsNdarken.y + dustStormIntensity * (1f - self.cloudsNdarken.y);
            }
            else
            {
                self.cloudsNdarken.x = self.roomSettings.Clouds;
                self.cloudsNdarken.y = self.darkenLightsFactor;
            }
            if (doWorldTick) self.syncTicker++;

            if (!self.game.pauseUpdate && self.BeingViewed && !self.abstractRoom.shelter && self.roomSettings.DangerType != RoomRain.DangerType.None && self.ceilingTiles.Length != 0 && Random.value < (self.gravity * worldSpeed) && (double)Random.value > Mathf.Pow((1f - Mathf.Max((1f - self.world.rainCycle.CycleStartUp) * Mathf.InverseLerp(0f, 0.5f, self.roomSettings.CeilingDrips), Mathf.Pow(self.roomSettings.CeilingDrips, 7f)) * 0.05f), self.ceilingTiles.Length))
            {
                self.AddObject(new WaterDrip(self.MiddleOfTile(self.ceilingTiles[Random.Range(0, self.ceilingTiles.Length)]) + new Vector2(Mathf.Lerp(-10f, 10f, Random.value), 9f), new Vector2(0f, 0f), false));
            }
            self.lastWaterGlitterCycle = self.waterGlitterCycle;
            self.waterGlitterCycle -= 0.0166666675f * worldSpeed;
            if (self.shortcutsBlinking != null && !self.game.pauseUpdate)
            {
                for (int l = 0; l < self.shortcutsBlinking.GetLength(0); l++)
                {
                    self.shortcutsBlinking[l, 0] = Mathf.Clamp(self.shortcutsBlinking[l, 0] - 1f / ((self.shortcutsBlinking[l, 1] > 0f) ? Mathf.Lerp(20f, (float)this.shortcuts[l].length, 0.3f) : 10f), 0f, 1f);
                    if (self.shortcutsBlinking[l, 1] > 0f)
                    {
                        self.shortcutsBlinking[l, 1] += 1f / Mathf.Lerp(20f, (float)self.shortcuts[l].length, 0.3f);
                        if (self.shortcutsBlinking[l, 1] > 1f)
                            self.shortcutsBlinking[l, 1] = 0f;
                        self.shortcutsBlinking[l, 2] = 0f;
                    }
                    if (self.shortcutsBlinking[l, 3] == 0f)
                    {
                        self.shortcutsBlinking[l, 2] = Mathf.Lerp(self.shortcutsBlinking[l, 2], 1f, 0.04f);
                        if (Random.value < 0.01f * worldSpeed)
                        {
                            self.shortcutsBlinking[l, 3] = Random.value * 20f;
                        }
                    }
                    else if (self.shortcutsBlinking[l, 3] < 0f)
                    {
                        self.shortcutsBlinking[l, 3] = Mathf.Min(self.shortcutsBlinking[l, 3] + 1f, 0f);
                    }
                    else
                    {
                        self.shortcutsBlinking[l, 3] = Mathf.Max(self.shortcutsBlinking[l, 3] - 1f, 0f);
                        self.shortcutsBlinking[l, 2] = Mathf.Lerp(self.shortcutsBlinking[l, 2], Random.value, Mathf.Lerp(0f, 0.5f, UnityEngine.Random.value));
                    }
                }
            }
            self.fliesRoomAi?.Update(self.game.evenUpdate);
            self.PERTILEVISALIZER?.Update(self);
            if (!self.game.pauseUpdate && doWorldTick)
                self.abstractRoom.UpdateCreaturesInDens(1);

            int updateIndex = self.updateList.Count - 1;
            while (updateIndex >= 0)
            {
                var ud = self.updateList[updateIndex];
                if (ud.slatedForDeletetion || ud.room != self)
                {
                    self.CleanOutObjectNotInThisRoom(ud);
                    --updateIndex;
                    continue;
                }

                if (ud is Player player)
                {
                    // TODO: implement playerTPS
                    player.Update(self.game.evenUpdate);
                    if (player.dangerGraspTime > 0 && Random.value > worldSpeed) player.dangerGraspTime--;
                    player.graphicsModule.Update();
                    player.GraphicsModuleUpdated(true, self.game.evenUpdate);

                    updateIndex--;
                    continue;
                }

                bool defered = self.ShouldBeDeferred(ud) && (!doWorldTick && !(ud is Player));
                if ((!self.game.pauseUpdate || ud is IRunDuringDialog) && !defered)
                {
                    ud.Update(self.game.evenUpdate);
                }
                if (ud.slatedForDeletetion || ud.room != self)
                {
                    self.CleanOutObjectNotInThisRoom(ud);
                }
                else if (ud is PhysicalObject po && !defered)
                {
                    if (po.graphicsModule != null)
                    {
                        po.graphicsModule.Update();
                        po.GraphicsModuleUpdated(true, self.game.evenUpdate);
                    }
                    else
                    {
                        po.GraphicsModuleUpdated(false, self.game.evenUpdate);
                    }
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
                    ud.Update(self.game.evenUpdate);
                    if (ud is PhysicalObject po)
                    {
                        if (po.graphicsModule != null)
                        {
                            po.graphicsModule.Update();
                            po.GraphicsModuleUpdated(true, self.game.evenUpdate);
                        }
                        else
                        {
                            po.GraphicsModuleUpdated(false, self.game.evenUpdate);
                        }
                    }
                    updateIndex--;
                    continue;
                }

                // Slowed
                ud.Update(self.game.evenUpdate);
                if (ud is PhysicalObject p)
                {
                    if (p.graphicsModule != null)
                    {
                        p.graphicsModule.Update();
                        p.GraphicsModuleUpdated(true, self.game.evenUpdate);
                    }
                    else
                    {
                        p.GraphicsModuleUpdated(false, self.game.evenUpdate);
                    }
                }
                updateIndex--;
            }

            if (ModManager.DLCShared && this.roomSettings.GetEffect(DLCSharedEnums.RoomEffectType.RoomWrap) != null)
            {
                foreach (AbstractCreature abstractCreature2 in this.game.Players)
                {
                    if (abstractCreature2.realizedCreature != null && abstractCreature2.realizedCreature.room == this)
                    {
                        Player player = abstractCreature2.realizedCreature as Player;
                        if (player.mainBodyChunk.pos.x < -228f)
                        {
                            player.SuperHardSetPosition(new Vector2(this.RoomRect.right + 212f, player.mainBodyChunk.pos.y));
                        }
                        if (player.mainBodyChunk.pos.x > this.RoomRect.right + 228f)
                        {
                            player.SuperHardSetPosition(new Vector2(-212f, player.mainBodyChunk.pos.y));
                        }
                        if (player.mainBodyChunk.pos.y > this.RoomRect.top + 48f)
                        {
                            player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, this.RoomRect.bottom - 72f));
                        }
                        if (player.mainBodyChunk.pos.y < this.RoomRect.bottom - 96f)
                        {
                            player.SuperHardSetPosition(new Vector2(player.mainBodyChunk.pos.x, this.RoomRect.top + 32f));
                            for (int m = 0; m < player.bodyChunks.Length; m++)
                            {
                                player.bodyChunks[m].vel.y = Mathf.Clamp(player.bodyChunks[m].vel.y, -15f, 15f);
                            }
                        }
                    }
                }
            }
            this.updateIndex = int.MaxValue;
            if (this.chunkGlue != null)
            {
                foreach (ChunkGlue chunkGlue in this.chunkGlue)
                {
                    chunkGlue.moveChunk.pos = chunkGlue.otherChunk.pos + chunkGlue.relativePos;
                }
            }
            this.chunkGlue = null;
            for (int n = 1; n < this.physicalObjects.Length; n++)
            {
                for (int num5 = 0; num5 < this.physicalObjects[n].Count; num5++)
                {
                    for (int num6 = num5 + 1; num6 < this.physicalObjects[n].Count; num6++)
                    {
                        if ((this.physicalObjects[n][num5].abstractPhysicalObject.rippleLayer == this.physicalObjects[n][num6].abstractPhysicalObject.rippleLayer || this.physicalObjects[n][num5].abstractPhysicalObject.rippleBothSides || this.physicalObjects[n][num6].abstractPhysicalObject.rippleBothSides) && Mathf.Abs(this.physicalObjects[n][num5].bodyChunks[0].pos.x - this.physicalObjects[n][num6].bodyChunks[0].pos.x) < this.physicalObjects[n][num5].collisionRange + this.physicalObjects[n][num6].collisionRange && Mathf.Abs(this.physicalObjects[n][num5].bodyChunks[0].pos.y - this.physicalObjects[n][num6].bodyChunks[0].pos.y) < this.physicalObjects[n][num5].collisionRange + this.physicalObjects[n][num6].collisionRange)
                        {
                            bool flag2 = false;
                            bool flag3 = false;
                            if (this.physicalObjects[n][num5] is Creature && (this.physicalObjects[n][num5] as Creature).Template.grasps > 0)
                            {
                                foreach (Creature.Grasp grasp in (this.physicalObjects[n][num5] as Creature).grasps)
                                {
                                    if (grasp != null && grasp.grabbed == this.physicalObjects[n][num6])
                                    {
                                        flag3 = true;
                                        break;
                                    }
                                }
                            }
                            if (!flag3 && this.physicalObjects[n][num6] is Creature && (this.physicalObjects[n][num6] as Creature).Template.grasps > 0)
                            {
                                foreach (Creature.Grasp grasp2 in (this.physicalObjects[n][num6] as Creature).grasps)
                                {
                                    if (grasp2 != null && grasp2.grabbed == this.physicalObjects[n][num5])
                                    {
                                        flag3 = true;
                                        break;
                                    }
                                }
                            }
                            if (!flag3)
                            {
                                for (int num8 = 0; num8 < this.physicalObjects[n][num5].bodyChunks.Length; num8++)
                                {
                                    for (int num9 = 0; num9 < this.physicalObjects[n][num6].bodyChunks.Length; num9++)
                                    {
                                        if (this.physicalObjects[n][num5].bodyChunks[num8].collideWithObjects && this.physicalObjects[n][num6].bodyChunks[num9].collideWithObjects && Custom.DistLess(this.physicalObjects[n][num5].bodyChunks[num8].pos, this.physicalObjects[n][num6].bodyChunks[num9].pos, this.physicalObjects[n][num5].bodyChunks[num8].rad + this.physicalObjects[n][num6].bodyChunks[num9].rad))
                                        {
                                            float num10 = this.physicalObjects[n][num5].bodyChunks[num8].rad + this.physicalObjects[n][num6].bodyChunks[num9].rad;
                                            float num11 = Vector2.Distance(this.physicalObjects[n][num5].bodyChunks[num8].pos, this.physicalObjects[n][num6].bodyChunks[num9].pos);
                                            Vector2 a = Custom.DirVec(this.physicalObjects[n][num5].bodyChunks[num8].pos, this.physicalObjects[n][num6].bodyChunks[num9].pos);
                                            float num12 = this.physicalObjects[n][num6].bodyChunks[num9].mass / (this.physicalObjects[n][num5].bodyChunks[num8].mass + this.physicalObjects[n][num6].bodyChunks[num9].mass);
                                            this.physicalObjects[n][num5].bodyChunks[num8].pos -= (num10 - num11) * a * num12;
                                            this.physicalObjects[n][num5].bodyChunks[num8].vel -= (num10 - num11) * a * num12;
                                            this.physicalObjects[n][num6].bodyChunks[num9].pos += (num10 - num11) * a * (1f - num12);
                                            this.physicalObjects[n][num6].bodyChunks[num9].vel += (num10 - num11) * a * (1f - num12);
                                            if (this.physicalObjects[n][num5].bodyChunks[num8].pos.x == this.physicalObjects[n][num6].bodyChunks[num9].pos.x)
                                            {
                                                this.physicalObjects[n][num5].bodyChunks[num8].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                                this.physicalObjects[n][num6].bodyChunks[num9].vel += Custom.DegToVec(UnityEngine.Random.value * 360f) * 0.0001f;
                                            }
                                            if (!flag2)
                                            {
                                                this.physicalObjects[n][num5].Collide(this.physicalObjects[n][num6], num8, num9);
                                                this.physicalObjects[n][num6].Collide(this.physicalObjects[n][num5], num9, num8);
                                            }
                                            flag2 = true;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }

            if (self.chunkGlue != null)
            {
                foreach (ChunkGlue chunkGlue in self.chunkGlue)
                { chunkGlue.moveChunk.pos = chunkGlue.otherChunk.pos + chunkGlue.relativePos; }
            }
            self.chunkGlue = null;
            for (int j = 1; j < self.physicalObjects.Length; j++)
            {
                for (int k = 0; k < self.physicalObjects[j].Count; k++)
                {
                    for (int l = k + 1; l < self.physicalObjects[j].Count; l++)
                    {
                        if (Mathf.Abs(self.physicalObjects[j][k].bodyChunks[0].pos.x - self.physicalObjects[j][l].bodyChunks[0].pos.x)
                            < self.physicalObjects[j][k].collisionRange + self.physicalObjects[j][l].collisionRange
                            && Mathf.Abs(self.physicalObjects[j][k].bodyChunks[0].pos.y - self.physicalObjects[j][l].bodyChunks[0].pos.y)
                            < self.physicalObjects[j][k].collisionRange + self.physicalObjects[j][l].collisionRange)
                        {
                            bool collided = false;
                            bool grabbed = false;
                            if (self.physicalObjects[j][k] is Creature && (self.physicalObjects[j][k] as Creature).Template.grasps > 0)
                            {
                                foreach (Creature.Grasp grasp in (self.physicalObjects[j][k] as Creature).grasps)
                                {
                                    if (grasp != null && grasp.grabbed == self.physicalObjects[j][l])
                                    {
                                        grabbed = true;
                                        break;
                                    }
                                }
                            }
                            if (!grabbed && self.physicalObjects[j][l] is Creature && (self.physicalObjects[j][l] as Creature).Template.grasps > 0)
                            {
                                foreach (Creature.Grasp grasp2 in (self.physicalObjects[j][l] as Creature).grasps)
                                {
                                    if (grasp2 != null && grasp2.grabbed == self.physicalObjects[j][k])
                                    {
                                        grabbed = true;
                                        break;
                                    }
                                }
                            }
                            if (!grabbed)
                            {
                                for (int p = 0; p < self.physicalObjects[j][k].bodyChunks.Length; p++)
                                {
                                    for (int q = 0; q < self.physicalObjects[j][l].bodyChunks.Length; q++)
                                    {
                                        if (self.physicalObjects[j][k].bodyChunks[p].collideWithObjects && self.physicalObjects[j][l].bodyChunks[q].collideWithObjects
                                            && Custom.DistLess(self.physicalObjects[j][k].bodyChunks[p].pos, self.physicalObjects[j][l].bodyChunks[q].pos,
                                            self.physicalObjects[j][k].bodyChunks[p].rad + self.physicalObjects[j][l].bodyChunks[q].rad))
                                        {
                                            float radSum = self.physicalObjects[j][k].bodyChunks[p].rad + self.physicalObjects[j][l].bodyChunks[q].rad;
                                            float dist = Vector2.Distance(self.physicalObjects[j][k].bodyChunks[p].pos, self.physicalObjects[j][l].bodyChunks[q].pos);
                                            Vector2 dir = Custom.DirVec(self.physicalObjects[j][k].bodyChunks[p].pos, self.physicalObjects[j][l].bodyChunks[q].pos) * worldSpeed;
                                            float massRatio = self.physicalObjects[j][l].bodyChunks[q].mass / (self.physicalObjects[j][k].bodyChunks[p].mass + self.physicalObjects[j][l].bodyChunks[q].mass);
                                            self.physicalObjects[j][k].bodyChunks[p].pos -= (radSum - dist) * dir * massRatio;
                                            self.physicalObjects[j][k].bodyChunks[p].vel -= (radSum - dist) * dir * massRatio;
                                            self.physicalObjects[j][l].bodyChunks[q].pos += (radSum - dist) * dir * (1f - massRatio);
                                            self.physicalObjects[j][l].bodyChunks[q].vel += (radSum - dist) * dir * (1f - massRatio);
                                            if (self.physicalObjects[j][k].bodyChunks[p].pos.x == self.physicalObjects[j][l].bodyChunks[q].pos.x)
                                            {
                                                self.physicalObjects[j][k].bodyChunks[p].vel += Custom.DegToVec(Random.value * 360f) * 0.0001f;
                                                self.physicalObjects[j][l].bodyChunks[q].vel += Custom.DegToVec(Random.value * 360f) * 0.0001f;
                                            }
                                            if (!collided)
                                            {
                                                self.physicalObjects[j][k].Collide(self.physicalObjects[j][l], p, q);
                                                self.physicalObjects[j][l].Collide(self.physicalObjects[j][k], q, p);
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

            if (ModManager.Expedition && self.game.rainWorld.ExpeditionMode)
                ExpeditionGame.ExSpawn(self);
            if (ModManager.Watcher)
            {
                float effectAmount = self.roomSettings.GetEffectAmount(WatcherEnums.RoomEffectType.SentientRotInfection);
                if (!self.rotPresenceInitialized && effectAmount > 0f && self.aimap != null)
                    self.InitializeSentientRotPresenceInRoom(effectAmount);
            }
            return;
        }
        */
        normalSpeed:
            orig(self);
        }

        private static bool ShouldObjBeDefered(On.Room.orig_ShouldBeDeferred orig, Room self, UpdatableAndDeletable obj)
        {
            var res = orig(self, obj);
            if (res) return res;
            if (obj is Player p && p.camoProgress > 0f)
            {
                return false;
            }
            if (!doWorldTick)
            {
                if (!(obj is PhysicalObject po)) return res;
                if (po.grabbedBy.Count > 0 && po.grabbedBy[0].grabber is Player) return false;
                if (po is Weapon w && w.mode == Weapon.Mode.Thrown && w.thrownBy is Player) return false;
                return true;
            }
            return res;
        }

        private static void GameRawUpdatePatch(On.MainLoopProcess.orig_RawUpdate orig, MainLoopProcess self, float dt)
        {
            worldSpeed = playerSlowRatio = 1f;
            playerTPS = worldTPS = self.framesPerSecond;
            if (self is RainWorldGame game)
            {
                #region CheckRipple

                var players = game.Players;
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
                worldTPS = Mathf.Min(self.framesPerSecond, worldTPS);
                playerTPS = Mathf.Min(self.framesPerSecond, playerTPS);
                worldSpeed = worldTPS / playerTPS;
                playerSlowRatio = self.framesPerSecond / playerTPS;

            #endregion CheckRipple

            normalSpeed:
                self.framesPerSecond = Mathf.Max(1, Mathf.CeilToInt(playerTPS));
            }

            orig(self, dt);
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