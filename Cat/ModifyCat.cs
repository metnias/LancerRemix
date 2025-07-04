﻿using CatSub.Cat;
using LancerRemix.LancerMenu;
using LancerRemix.Latcher;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MonoMod.RuntimeDetour;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using Watcher;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    public static class ModifyCat
    {
        internal static void Patch()
        {
            On.Player.ctor += PlayerCtor;
            On.Player.GetInitialSlugcatClass += GetInitialLancerClass;
            On.Player.Update += PlayerUpdate;
            On.Player.Destroy += PlayerDestroy;
            On.Player.Grabbed += PlayerGrabbed;
            On.Player.ShortCutColor += LancerShortCutColor;
            On.Player.DeathByBiteMultiplier += LancerDeathByBiteMultiplier;
            On.Player.ThrowObject += PlayerThrowObject;
            On.Player.CanIPickThisUp += PlayerCanIPickThisUp;
            On.Player.Stun += PlayerStun;
            On.Player.Die += PlayerDie;
            On.Player.Deafen += PlayerDeafen;
            On.Player.UpdateMSC += PlayerUpdateMSC;
            On.Player.MovementUpdate += LancerMovementUpdate;
            IL.Player.EatMeatUpdate += LonkEatMeatUpdate;
            On.Player.GraphicsModuleUpdated += LunterGrafModuleUpdated;
            On.Player.SetMalnourished += LancerSetMalnourished;
            On.Player.TerrainImpact += LancerTerrainImpact;

            On.PlayerGraphics.ctor += GrafCtor;
            On.PlayerGraphics.InitiateSprites += GrafInitSprite;
            On.PlayerGraphics.AddToContainer += GrafAddToContainer;
            On.PlayerGraphics.Update += GrafUpdate;
            On.PlayerGraphics.DrawSprites += GrafDrawSprite;
            On.PlayerGraphics.ApplyPalette += GrafApplyPalette;
            On.PlayerGraphics.SuckedIntoShortCut += GrafSuckedIntoShortCut;
            On.PlayerGraphics.Reset += GrafReset;

            var characterForColor = new Hook(
                typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.CharacterForColor), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyCat).GetMethod(nameof(ModifyCat.LancerForColor), BindingFlags.Static | BindingFlags.NonPublic)
            );
            var renderAsPup = new Hook(
                typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.RenderAsPup), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyCat).GetMethod(nameof(ModifyCat.RenderAsLancer), BindingFlags.Static | BindingFlags.NonPublic)
            );
            On.PlayerGraphics.DefaultSlugcatColor += DefaultLancerColor;

            SwapSave.SubPatch();
#if LATCHER
            Latcher.ModifyLatcher.SubPatch();
#endif

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
        }

        internal static void OnMSCDisablePatch()
        {
        }

        private static bool[] isPlayerLancer = new bool[RainWorld.PlayerObjectBodyColors.Length];
        public static bool IsStoryLancer { get; private set; } = false;

        public static void SetIsPlayerLancer(bool story, bool[] players)
        {
            IsStoryLancer = story;
            if (players.Length > isPlayerLancer.Length)
            {
                Array.Resize(ref isPlayerLancer, players.Length);
                HornColorPick.ResizeHornColors(players.Length);
            }
            for (int i = 0; i < players.Length; ++i) isPlayerLancer[i] = players[i];
        }

        public static bool IsPlayerLancer(int playerNumber) => isPlayerLancer[playerNumber];

        public static bool IsPlayerLancer(Player player) => !player.isNPC && IsPlayerLancer(player.playerState.playerNumber);

        public static bool IsPlayerLancer(PlayerGraphics playerGraphics) => IsPlayerLancer(playerGraphics.player);

        public static bool IsPlayerCustomLancer(SlugName name) => LancerGenerator.IsCustomLancer(name.value);

        public static bool IsPlayerCustomLancer(Player player) => !player.isNPC && IsPlayerCustomLancer(player.SlugCatClass);

        public static bool IsPlayerCustomLancer(PlayerGraphics playerGraphics) => IsPlayerCustomLancer(playerGraphics.player);

        public static bool IsLancer(SlugName name) => LancerEnums.IsLancer(name);

        #region Player

        #region SubRegistry

        private static readonly ConditionalWeakTable<PlayerState, CatSupplement> catSubs
            = new ConditionalWeakTable<PlayerState, CatSupplement>();

        public static T GetSub<T>(Player player) where T : CatSupplement
        {
            if (catSubs.TryGetValue(player.playerState, out var sub))
                if (sub is T) return sub as T;
            if (IsPlayerCustomLancer(player) && AppendCatSub.TryGetSub(player, out var customSub))
                return customSub as T;

            return null;
        }

        public static T GetSub<T>(PlayerGraphics playerGraphics) where T : CatSupplement
           => GetSub<T>(playerGraphics.player);

        #endregion SubRegistry

        #region CatSub

        private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (IsPlayerCustomLancer(self))
            {
                isPlayerLancer[self.playerState.playerNumber] = true;
                return; // CatSub will add supplements
            }
            if (!IsPlayerLancer(self)) return;
            var basis = GetBasis(self.SlugCatClass);
            if (SlugcatStats.IsSlugcatFromMSC(basis) && (!LancerPlugin.MSCLANCERS || !IsPlayerCustomLancer(self.SlugCatClass)))
            {
                isPlayerLancer[self.playerState.playerNumber] = false;
                SelectMenuPatch.SetLancerPlayers(self.playerState.playerNumber, false);
                SelectMenuPatch.SaveLancerPlayers(world.game.rainWorld.progression.miscProgressionData);
                return;
            }
            if (basis == SlugName.Red)
            {
                catSubs.Add(self.playerState, new LunterSupplement(self));
                catDecos.Add(self.playerState, new LunterDecoration(self));
            }
#if LATCHER
            else if (ModManager.Watcher && basis == WatcherEnums.SlugcatStatsName.Watcher)
            {
                catSubs.Add(self.playerState, new LatcherSupplement(self));
                catDecos.Add(self.playerState, new LatcherDecoration(self));
            }
#endif
            else
            {
                catSubs.Add(self.playerState, new LancerSupplement(self));
                catDecos.Add(self.playerState, new LancerDecoration(self));
                if (basis == SlugName.Yellow && self.room?.game.session is StoryGameSession && self.room.abstractRoom?.name == "SU_C04")
                { // Give first food for Lonk
                    self.playerState.foodInStomach = 1;
                }
            }
        }

        private static void GetInitialLancerClass(On.Player.orig_GetInitialSlugcatClass orig, Player self)
        {
            orig(self);
            if (IsPlayerLancer(self.playerState.playerNumber) && IsPlayerCustomLancer(GetLancer(self.SlugCatClass)))
                self.SlugCatClass = GetLancer(self.SlugCatClass);
        }

        private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetSub<LancerSupplement>(self)?.Update(null, eu);
        }

        private static void PlayerDestroy(On.Player.orig_Destroy orig, Player self)
        {
            orig(self);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetSub<LancerSupplement>(self)?.Destroy(null);
        }

        #endregion CatSub

        private static void PlayerGrabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.Grabbed(orig, grasp); return; }
            orig(self, grasp);
        }

        private static Color LancerShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
        {
            if (!IsPlayerLancer(self) || self.abstractCreature.world.game.IsArenaSession) return orig(self);
            if (IsPlayerCustomLancer(self)) return orig(self);
            return PlayerGraphics.SlugcatColor(GetLancer(self.playerState.slugcatCharacter));
        }

        private static float LancerDeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
            {
                if (self.abstractCreature.world.game.IsStorySession)
                    return 0.2f + self.abstractCreature.world.game.GetStorySession.difficulty / 4f;
                return 0.3f;
            }
            return orig(self);
        }

        private static void PlayerThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.ThrowObject(orig, grasp, eu); return; }
            orig(self, grasp, eu);
        }

        private static bool PlayerCanIPickThisUp(On.Player.orig_CanIPickThisUp orig, Player self, PhysicalObject obj)
        {
            if (IsPlayerLancer(self))
            {
                var res = GetSub<LancerSupplement>(self)?.CanIPickThisUp(orig, obj);
                if (res.HasValue) return res.Value;
            }
            return orig(self, obj);
        }

        private static void PlayerThrowToGetFree(On.Player.orig_ThrowToGetFree orig, Player self, bool eu)
        {
            if (IsPlayerLancer(self)) GetSub<LancerSupplement>(self)?.ThrowToGetFree(orig, eu);
            orig(self, eu);
        }

        private static void PlayerStun(On.Player.orig_Stun orig, Player self, int st)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.Stun(orig, st); return; }
            orig(self, st);
        }

        private static void PlayerDie(On.Player.orig_Die orig, Player self)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.Die(orig); return; }
            orig(self);
        }

        private static void PlayerDeafen(On.Player.orig_Deafen orig, Player self, int df)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.Deafen(orig, df); return; }
            orig(self, df);
        }

        private static void PlayerUpdateMSC(On.Player.orig_UpdateMSC orig, Player self)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.UpdateMSC(orig); return; }
            orig(self);
        }

        private static void LancerMovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.MovementUpdate(orig, eu); return; }
            orig(self, eu);
        }

        private static void LonkEatMeatUpdate(ILContext il)
        {
            var cursor = new ILCursor(il);

            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.LonkEatMeatUpdate);

            ILLabel foodAdded = null;
            if (!cursor.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.AddQuarterFood))),
                x => x.MatchBr(out foodAdded))) return;

            if (!cursor.TryGotoNext(MoveType.AfterLabel,
                x => x.MatchLdarg(0),
                x => x.MatchLdcI4(1),
                x => x.MatchCallOrCallvirt(typeof(Player).GetMethod(nameof(Player.AddFood))))) return;

            cursor.Emit(OpCodes.Ldarg_0);
            cursor.Emit(OpCodes.Ldarg_1);
            cursor.EmitDelegate<Func<Player, int, bool>>((self, graspIndex) =>
                {
                    if (IsPlayerLancer(self) && GetBasis(self.SlugCatClass) == SlugName.Yellow)
                    {
                        self.AddQuarterFood();
                        return true;
                    }
                    return false;
                });
            cursor.Emit(OpCodes.Brtrue, foodAdded);

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.LonkEatMeatUpdate);
        }

        private static void LunterGrafModuleUpdated(On.Player.orig_GraphicsModuleUpdated orig, Player self, bool actuallyViewed, bool eu)
        {
            if (IsPlayerLancer(self)) GetSub<LunterSupplement>(self)?.maskOnHorn.GraphicsModuleUpdated(actuallyViewed, eu);
            orig(self, actuallyViewed, eu);
        }

        private static void LancerSetMalnourished(On.Player.orig_SetMalnourished orig, Player self, bool m)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.SetMalnourished(orig, m); return; }
            orig(self, m);
        }

        private static void LancerTerrainImpact(On.Player.orig_TerrainImpact orig, Player self, int chunk, IntVector2 direction, float speed, bool firstContact)
        {
            if (IsPlayerLancer(self))
            { GetSub<LancerSupplement>(self)?.TerrainImpact(orig, chunk, direction, speed, firstContact); return; }
            orig(self, chunk, direction, speed, firstContact);
        }

        #endregion Player

        #region PlayerGraphics

        #region DecoRegistry

        private static readonly ConditionalWeakTable<PlayerState, CatDecoration> catDecos
           = new ConditionalWeakTable<PlayerState, CatDecoration>();

        public static T GetDeco<T>(Player player) where T : CatDecoration
        {
            if (catDecos.TryGetValue(player.playerState, out var deco))
                if (deco is T) return deco as T;
            if (IsPlayerCustomLancer(player) && AppendCatDeco.TryGetDeco(player.graphicsModule as PlayerGraphics, out var customDeco))
                return customDeco as T;
            return null;
        }

        public static T GetDeco<T>(PlayerGraphics playerGraphics) where T : CatDecoration
           => GetDeco<T>(playerGraphics.player);

        #endregion DecoRegistry

        #region CatDeco

        private static void GrafCtor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            if (IsPlayerLancer(self))
            {
                var bodyParts = self.bodyParts.ToList();
                bodyParts.Remove(self.tail[0]);
                bodyParts.Remove(self.tail[1]);
                bodyParts.Remove(self.tail[2]);
                bodyParts.Remove(self.tail[3]);

                self.tail = new TailSegment[4];
                self.tail[0] = new TailSegment(self, 6f, 2f, null, 0.85f, 1f, 1f, true);
                self.tail[1] = new TailSegment(self, 4f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
                self.tail[2] = new TailSegment(self, 2.5f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
                self.tail[3] = new TailSegment(self, 1f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);

                bodyParts.Add(self.tail[0]);
                bodyParts.Add(self.tail[1]);
                bodyParts.Add(self.tail[2]);
                bodyParts.Add(self.tail[3]);
            }
        }

        private static void GrafInitSprite(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.InitiateSprites(null, sLeaser, rCam);
        }

        private static void GrafAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.AddToContainer(null, sLeaser, rCam, newContatiner);
        }

        private static void GrafUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.Update(null);
        }

        private static void GrafDrawSprite(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.DrawSprites(null, sLeaser, rCam, timeStacker, camPos);
        }

        private static void GrafApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.ApplyPalette(null, sLeaser, rCam, palette);
        }

        private static void GrafSuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, PlayerGraphics self, Vector2 shortCutPosition)
        {
            orig(self, shortCutPosition);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.SuckedIntoShortCut(null, shortCutPosition);
        }

        private static void GrafReset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (IsPlayerLancer(self) && !IsPlayerCustomLancer(self))
                GetDeco<LancerDecoration>(self)?.Reset(null);
        }

        #endregion CatDeco

        #region Properties

        private delegate SlugName orig_CharacterForColor(PlayerGraphics self);

        private static SlugName LancerForColor(orig_CharacterForColor orig, PlayerGraphics self)
        {
            var res = orig(self);
            if (IsPlayerLancer(self) && !self.owner.abstractPhysicalObject.world.game.IsArenaSession) res = GetLancer(res);
            return res;
        }

        private delegate bool orig_RenderAsPup(PlayerGraphics self);

        private static bool RenderAsLancer(orig_RenderAsPup orig, PlayerGraphics self)
        {
            if (IsPlayerLancer(self)) return true;
            return orig(self);
        }

        #endregion Properties

        private static Color DefaultLancerColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugName i)
        {
            if (IsLancer(i) && !IsPlayerCustomLancer(i))
            {
                var basis = GetBasis(i);
                if (ModManager.Watcher && basis == WatcherEnums.SlugcatStatsName.Watcher)
                    return new Color(0.8f, 0.1f, 0.3f);

                if (defaultLancerBodyColors.TryGetValue(basis, out var res)) return res;
                return orig(basis);
            }
            return orig(i);
        }

        internal static readonly Dictionary<SlugName, Color> defaultLancerBodyColors
            = new Dictionary<SlugName, Color>()
            {
                { SlugName.White, new Color(0.8f, 1.0f, 0.5f) },
                { SlugName.Yellow, new Color(1.0f, 0.9f, 0.4f) },
                { SlugName.Red, new Color(0.3f, 0.5f, 1.0f) },
                { SlugName.Night, new Color(0.8f, 0.1f, 0.3f) }
            };

        #endregion PlayerGraphics
    }
}