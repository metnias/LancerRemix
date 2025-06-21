#if LATCHER

using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using RWCustom;
using System;
using System.Globalization;
using UnityEngine;
using Watcher;
using static LancerRemix.Cat.ModifyCat;
using static LancerRemix.LancerEnums;
using Random = UnityEngine.Random;

namespace LancerRemix.Latcher
{
    internal static class LatcherPatch
    {
        internal static void SubPatch()
        {
            IL.Menu.ControlMap.ctor += LatcherControlMapPatch;
            IL.Watcher.WatcherRoomSpecificScript.AddRoomSpecificScript += LatcherAddRoomSpecificScriptPatch;

            if (!ModManager.Watcher) return;
            OnWatcherEnableSubPatch();
        }

        internal static void OnWatcherEnableSubPatch()
        {
            On.DaddyCorruption.SentientRotMode += LatcherSentientRotMode;
            On.Ghost.Update += LatcherGhostTalk;
            On.HUD.HUD.InitSinglePlayerHud += LatcherAddCamoMeter;
            On.HUD.KarmaMeter.Update += LatcherKarmaMeterUpdate;
            On.StoryGameSession.ctor += LatcherStorySession;
            On.WinState.TrackerAllowedOnSlugcat += TrackerAllowedOnLatcher;
            On.World.SpawnGhost += LatcherSpawnSpinningTop;
            On.Watcher.SpinningTop.SpinningTopConversation.AddEvents += LatcherSpinningTopDialog;
            On.Watcher.SpinningTop.OnScreen += LatcherSpinningTopOnScreen;
            On.VirtualMicrophone.RippleSpaceUpdate += LatcherRippleSoundSpeedFix;
            On.RoomCamera.SpriteLeaser.ctor += ReplaceLatcherRippleShader;
            On.GraphicsModule.UpdateRippleHybrid += LatcherUpdateRippleHybrid;
            IL.Watcher.DrillCrab.Collide += DrillCrabNoAttackOnRipple;
        }

        internal static void OnWatcherDisableSubPatch()
        {
            On.DaddyCorruption.SentientRotMode -= LatcherSentientRotMode;
            On.Ghost.Update -= LatcherGhostTalk;
            On.HUD.HUD.InitSinglePlayerHud -= LatcherAddCamoMeter;
            On.HUD.KarmaMeter.Update -= LatcherKarmaMeterUpdate;
            On.StoryGameSession.ctor -= LatcherStorySession;
            On.WinState.TrackerAllowedOnSlugcat -= TrackerAllowedOnLatcher;
            On.World.SpawnGhost -= LatcherSpawnSpinningTop;
            On.Watcher.SpinningTop.OnScreen -= LatcherSpinningTopOnScreen;
            On.VirtualMicrophone.RippleSpaceUpdate -= LatcherRippleSoundSpeedFix;
            On.RoomCamera.SpriteLeaser.ctor -= ReplaceLatcherRippleShader;
            On.GraphicsModule.UpdateRippleHybrid -= LatcherUpdateRippleHybrid;
        }

        internal static bool IsStoryLatcher(RainWorldGame game)
            => ModManager.Watcher && IsStoryLancer && game != null && game.IsStorySession
            && GetBasis(game.GetStorySession.saveStateNumber) == WatcherEnums.SlugcatStatsName.Watcher;

        internal static bool IsPlayerLatcher(Player player)
            => ModManager.Watcher && IsStoryLancer && player != null && IsPlayerLancer(player)
            && GetBasis(player.SlugCatClass) == WatcherEnums.SlugcatStatsName.Watcher;

        private static bool LatcherSentientRotMode(On.DaddyCorruption.orig_SentientRotMode orig, Room rm)
        {
            if (rm != null && IsStoryLatcher(rm.game)) return true;

            return orig(rm);
        }

        private static void LatcherGhostTalk(On.Ghost.orig_Update orig, Ghost self, bool eu)
        {
            orig(self, eu);

            if (self.room != null && IsStoryLatcher(self.room.game))
            {
                if (self.onScreenCounter > 100) self.onScreenCounter = 90; // to prevent force fadeout
                if (self.room.game.Players.Count > 0)
                {
                    self.theMarkMode = true;
                    if (self.onScreenCounter > 80 && self.currentConversation == null && self.room.game.cameras[0].hud != null)
                        self.StartConversation();
                }
            }
        }

        private static void LatcherAddCamoMeter(On.HUD.HUD.orig_InitSinglePlayerHud orig, HUD.HUD self, RoomCamera cam)
        {
            if (IsPlayerLatcher(self.owner as Player))
                self.AddPart(new CamoMeter(self, self.fContainers[1]));
            orig(self, cam);
        }

        private static void LatcherKarmaMeterUpdate(On.HUD.KarmaMeter.orig_Update orig, HUD.KarmaMeter self)
        {
            orig(self);
            if (self.hud.owner is Player player && IsPlayerLatcher(player))
            {
                if (player.activateDynamicWarpTimer > 5)
                {
                    self.forceVisibleCounter = Math.Max(self.forceVisibleCounter, 10);
                    float ripplePower = player.rippleLevel / 5f;
                    float warpTimer = player.activateDynamicWarpTimer / (float)player.activateDynamicWarpDuration;
                    float warpTimerPow = Mathf.Pow(warpTimer, 3f);
                    Color color = Color.Lerp(self.baseColor, Color.white, ((Mathf.Max(Mathf.Sin(self.wrapCreatingFlicker), -1f) + 1f) / 2f * (1f - warpTimerPow) * warpTimer + warpTimerPow) * (ripplePower - 0.4f) / 0.6f);
                    if (self.showAsReinforced)
                    {
                        self.pos += Random.insideUnitCircle * warpTimerPow * warpTimer * 5f * (ripplePower - 0.4f) / 0.6f;
                    }
                    else
                    {
                        ripplePower = 0.4f;
                        self.pos += Custom.RNV() * warpTimerPow * warpTimer * 5f;
                        color = Color.Lerp(self.baseColor, new Color(1f, 0f, 0f), warpTimerPow * 0.5f);
                    }
                    self.wrapCreatingFlicker += warpTimer * 1.5f * ripplePower;
                    self.rad = Mathf.Lerp(self.rad, self.rad * (1f - warpTimerPow) + 15f * warpTimerPow, warpTimer * (ripplePower - 0.2f) / 0.6f);
                    self.karmaSprite.color = color;
                    if (self.ringSprite != null) self.ringSprite.color = color;
                    self.glowSprite.color = color;
                }
            }
        }

        private static void LatcherControlMapPatch(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LatcherControlMapPatch);

            if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchLdcI4(0),
                x => x.MatchStloc(1)
                )) return;

            DebugLogCursor();
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Menu.Menu, bool>>(
                (menu) =>
                {
                    if (!(menu.manager.currentMainLoop is RainWorldGame game)) return false;
                    if (!IsStoryLatcher(game)) return false;
                    return true;
                }
                );
            cursor.Emit(OpCodes.Stloc_1);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LatcherControlMapPatch);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static void LatcherStorySession(On.StoryGameSession.orig_ctor orig, StoryGameSession self,
            SlugcatStats.Name saveStateNumber, RainWorldGame game)
        {
            orig(self, saveStateNumber, game);
            if (IsStoryLatcher(game))
            {
                self.rippleLevelAtStartOfCycle = self.saveState.deathPersistentSaveData.rippleLevel;
                self.minimumRippleLevelAtStartOfCycle = self.saveState.deathPersistentSaveData.minimumRippleLevel;
                self.maximumRippleLevelAtStartOfCycle = self.saveState.deathPersistentSaveData.maximumRippleLevel;
            }
        }

        private static void LatcherAddRoomSpecificScriptPatch(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LatcherAddRoomSpecificScriptPatch);

            if (!cursor.TryGotoNext(MoveType.After,
                x => x.MatchRet()
                )) return;

            DebugLogCursor();
            var lblOkay = cursor.DefineLabel();
            cursor.Emit(OpCodes.Nop);
            cursor.MarkLabel(lblOkay);
            DebugLogCursor();

            cursor.Index = 0;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.EmitDelegate<Func<Room, bool>>((room) =>
            {
                //Debug.Log($"Latcher Add Specific Room code: {IsStoryLatcher(room.game)}");
                return IsStoryLatcher(room.game);
            }
                );
            cursor.Emit(OpCodes.Brtrue, lblOkay);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LatcherAddRoomSpecificScriptPatch);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        private static bool TrackerAllowedOnLatcher(On.WinState.orig_TrackerAllowedOnSlugcat orig, WinState.EndgameID trackerId, SlugcatStats.Name slugcat)
        {
            if (ModManager.Watcher && GetBasis(slugcat) == WatcherEnums.SlugcatStatsName.Watcher)
            {
                if (ModManager.MSC)
                {
                    if (trackerId == MoreSlugcatsEnums.EndgameID.Pilgrim) return false;
                    if (trackerId == MoreSlugcatsEnums.EndgameID.Nomad) return false;
                }
                if (trackerId == WinState.EndgameID.Traveller) return false;
                if (trackerId == WinState.EndgameID.Scholar) return false;
            }
            return orig(trackerId, slugcat);
        }

        private static void LatcherSpawnSpinningTop(On.World.orig_SpawnGhost orig, World self)
        {
            if (ModManager.Watcher && IsStoryLatcher(self.game))
            {
                self.spinningTopPresences.Clear();
                if (self.game.rainWorld.regionSpinningTopRooms.ContainsKey(self.region.name.ToLowerInvariant()))
                {
                    var list = self.game.rainWorld.regionSpinningTopRooms[self.region.name.ToLowerInvariant()];
                    for (int i = 0; i < list.Count; i++)
                    {
                        var array = list[i].Split(new char[] { ':' });
                        if (array.Length >= 2)
                        {
                            string room = array[0];
                            int num = int.Parse(array[1], NumberStyles.Any, CultureInfo.InvariantCulture);
                            AbstractRoom abstractRoom = self.GetAbstractRoom(room);
                            if (abstractRoom != null && !self.game.GetStorySession.saveState.deathPersistentSaveData.spinningTopEncounters.Contains(num))
                            {
                                var ghostWorldPresence = new GhostWorldPresence(self, WatcherEnums.GhostID.SpinningTop, num);
                                ghostWorldPresence.ghostRoom = abstractRoom;
                                self.spinningTopPresences.Add(ghostWorldPresence);
                            }
                        }
                    }
                }
            }
            orig(self);
        }

        private static void LatcherSpinningTopDialog(On.Watcher.SpinningTop.SpinningTopConversation.orig_AddEvents orig, SpinningTop.SpinningTopConversation self)
        {
            if (self.ghost.room != null && IsStoryLatcher(self.ghost.room.game))
            {
                int eventFile = -1;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_V1) eventFile = 200;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_V2) eventFile = 201;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_V3) eventFile = 202;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N1) eventFile = 203;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N2) eventFile = 204;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N3) eventFile = 205;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N4) eventFile = 206;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N5) eventFile = 211;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N6) eventFile = 212;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_N7) eventFile = 213;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_RIP1) eventFile = 207;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_ROT1) eventFile = 208;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_AU1) eventFile = 210;
                if (self.id == WatcherEnums.ConversationID.Ghost_ST_AU2) eventFile = 209;

                if (eventFile > 0)
                    self.LoadEventsFromFile(eventFile, GetLancer(WatcherEnums.SlugcatStatsName.Watcher), false, 0);
                return;
            }

            orig(self);
        }

        private static bool LatcherSpinningTopOnScreen(On.Watcher.SpinningTop.orig_OnScreen orig, SpinningTop self)
        {
            var res = orig(self);
            if (res) return res;
            if (self.SpecialData.rippleWarp && self.room.game.FirstAlivePlayer.realizedCreature is Player player
                && IsPlayerLatcher(player) && self.room.VisibleInAnyCameraScreenBounds(self.pos) && LatcherMusicbox.IsLatcherRipple)
            {
                self.timeSinceLastTauntLaugh = 0; // also stop laughing
                return true;
            }

            return res;
        }

        private static void LatcherRippleSoundSpeedFix(On.VirtualMicrophone.orig_RippleSpaceUpdate orig, VirtualMicrophone self,
            float timeStacker, float timeSpeed, Vector2 currentListenerPos)
        {
            if (LatcherMusicbox.IsLatcherRipple) timeSpeed = (timeSpeed + 2f) / 3f;
            orig(self, timeStacker, timeSpeed, currentListenerPos);
        }

        private static void ReplaceLatcherRippleShader(On.RoomCamera.SpriteLeaser.orig_ctor orig,
            RoomCamera.SpriteLeaser self, IDrawable obj, RoomCamera rCam)
        {
            orig(self, obj, rCam);

            if (rCam.rippleData != null
                && rCam.room?.game != null && IsStoryLatcher(rCam.room.game))
            {
                ReplaceRippleShader(self.sprites);
            }

            void ReplaceRippleShader(FSprite[] sprites)
            {
                if (sprites == null || sprites.Length == 0) return;
                var fshader = Custom.rainWorld.Shaders["LatcherRippleGolden"]; // "Basic"
                int index = Custom.rainWorld.Shaders["RippleBasic"].index;
                foreach (FSprite fsprite in sprites)
                {
                    if (fsprite != null && fsprite.shader.index == index)
                        fsprite.shader = fshader;
                }
            }
        }

        private static void LatcherUpdateRippleHybrid(On.GraphicsModule.orig_UpdateRippleHybrid orig,
            GraphicsModule self, RoomCamera.SpriteLeaser sLeaser, Room room)
        {
            if (self.owner?.room != null && IsStoryLatcher(self.owner.room.game))
            {
                if (self.rippleHybrid != null) self.DeactivateRippleHybrid();
                return;
            }
            orig(self, sLeaser, room);
        }

        private static void DrillCrabNoAttackOnRipple(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.DrillCrabNoAttackOnRipple);

            if (!cursor.TryGotoNext(MoveType.After,
                i => i.MatchCall(nameof(PhysicalObject), nameof(PhysicalObject.Collide)))) return;

            var resumeLabel = cursor.DefineLabel();
            DebugLogCursor();
            cursor.EmitDelegate<Func<bool>>(() => LatcherMusicbox.IsLatcherRipple);
            cursor.Emit(OpCodes.Brfalse_S, resumeLabel);
            cursor.Emit(OpCodes.Ret);
            cursor.MarkLabel(resumeLabel);
            //DebugLogCursor();

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.DrillCrabNoAttackOnRipple);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }
    }
}

#endif