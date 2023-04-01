using CatSub.Cat;
using MonoMod.RuntimeDetour;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    public static class ModifyCat
    {
        internal static void Patch()
        {
            On.Player.ctor += PlayerCtor;
            On.Player.Update += PlayerUpdate;
            On.Player.Destroy += PlayerDestroy;
            On.Player.Grabbed += PlayerGrabbed;
            On.Player.ShortCutColor += LancerShortCutColor;
            On.Player.DeathByBiteMultiplier += LancerDeathByBiteMultiplier;
            On.Player.ThrowObject += PlayerThrowObject;
            On.Player.Stun += PlayerStun;
            On.Player.Die += PlayerDie;
            On.Player.MovementUpdate += LancerMovementUpdate;

            On.PlayerGraphics.InitiateSprites += GrafInitSprite;
            On.PlayerGraphics.AddToContainer += GrafAddToContainer;
            On.PlayerGraphics.Update += GrafUpdate;
            On.PlayerGraphics.DrawSprites += GrafDrawSprite;
            On.PlayerGraphics.ApplyPalette += GrafApplyPalette;
            On.PlayerGraphics.SuckedIntoShortCut += GrafSuckedIntoShortCut;
            On.PlayerGraphics.Reset += GrafReset;
            On.PlayerGraphics.ColoredBodyPartList += ColoredLancerPartList;

            var characterForColor = new Hook(
                typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.CharacterForColor), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyCat).GetMethod(nameof(ModifyCat.LancerForColor), BindingFlags.Static | BindingFlags.NonPublic)
            );
            /*var renderAsPup = new Hook(
                typeof(PlayerGraphics).GetProperty(nameof(PlayerGraphics.RenderAsPup), BindingFlags.Instance | BindingFlags.Public).GetGetMethod(),
                typeof(ModifyCat).GetMethod(nameof(ModifyCat.RenderAsLancer), BindingFlags.Static | BindingFlags.NonPublic)
            );*/
            On.PlayerGraphics.DefaultSlugcatColor += DefaultLancerColor;

            SwapSave.SubPatch();

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
        }

        internal static void OnMSCDisablePatch()
        {
        }

        private static readonly bool[] isLancer = new bool[4];
        public static bool IsStoryLancer { get; private set; } = false;

        public static void SetIsLancer(bool story, bool[] players)
        {
            IsStoryLancer = story;
            for (int i = 0; i < 4; ++i) isLancer[i] = players[i];
        }

        public static bool IsLancer(PlayerState playerState) => isLancer[playerState.playerNumber];

        public static bool IsLancer(Player player) => IsLancer(player.playerState);

        public static bool IsLancer(PlayerGraphics playerGraphics) => IsLancer(playerGraphics.player.playerState);

        public static bool IsLancer(SlugName name) => LancerEnums.IsLancer(name);

        #region Player

        #region SubRegistry

        private static readonly ConditionalWeakTable<PlayerState, CatSupplement> catSubs
            = new ConditionalWeakTable<PlayerState, CatSupplement>();

        public static T GetSub<T>(PlayerState playerState) where T : CatSupplement
        {
            if (catSubs.TryGetValue(playerState, out var sub))
                if (sub is T) return sub as T;
            return null;
        }

        public static T GetSub<T>(Player player) where T : CatSupplement
            => GetSub<T>(player.playerState);

        public static T GetSub<T>(PlayerGraphics playerGraphics) where T : CatSupplement
           => GetSub<T>(playerGraphics.player.playerState);

        #endregion SubRegistry

        #region CatSub

        private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!IsLancer(self.playerState)) return;
            catSubs.Add(self.playerState, new LancerSupplement(self));
            catDecos.Add(self.playerState, new LancerDecoration(self));
        }

        private static void PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (IsLancer(self.playerState))
                GetSub<LancerSupplement>(self)?.Update(null, eu);
        }

        private static void PlayerDestroy(On.Player.orig_Destroy orig, Player self)
        {
            orig(self);
            if (IsLancer(self.playerState))
                GetSub<LancerSupplement>(self)?.Destroy(null);
        }

        #endregion CatSub

        private static void PlayerGrabbed(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            if (IsLancer(self))
            { GetSub<LancerSupplement>(self)?.Grabbed(orig, grasp); return; }
            orig(self, grasp);
        }

        private static Color LancerShortCutColor(On.Player.orig_ShortCutColor orig, Player self)
        {
            if (!IsLancer(self)) return orig(self);
            var lancer = self.playerState.slugcatCharacter;
            if (HasLancer(lancer)) lancer = GetLancer(lancer);
            return PlayerGraphics.SlugcatColor(lancer);
        }

        private static float LancerDeathByBiteMultiplier(On.Player.orig_DeathByBiteMultiplier orig, Player self)
        {
            if (IsLancer(self))
            {
                if (self.room?.game.IsStorySession == true)
                    return 0.2f + self.room.game.GetStorySession.difficulty / 4f;
                return 0.3f;
            }
            return orig(self);
        }

        private static void PlayerThrowObject(On.Player.orig_ThrowObject orig, Player self, int grasp, bool eu)
        {
            if (IsLancer(self))
            { GetSub<LancerSupplement>(self)?.ThrowObject(orig, grasp, eu); return; }
            orig(self, grasp, eu);
        }

        private static void PlayerStun(On.Player.orig_Stun orig, Player self, int st)
        {
            orig(self, st);
            if (IsLancer(self)) GetSub<LancerSupplement>(self)?.ReleaseLanceSpear();
        }

        private static void PlayerDie(On.Player.orig_Die orig, Player self)
        {
            orig(self);
            if (IsLancer(self)) GetSub<LancerSupplement>(self)?.ReleaseLanceSpear();
        }

        private static void LancerMovementUpdate(On.Player.orig_MovementUpdate orig, Player self, bool eu)
        {
            if (IsLancer(self))
            { GetSub<LancerSupplement>(self)?.MovementUpdate(orig, eu); return; }
            orig(self, eu);
        }

        #endregion Player

        #region PlayerGraphics

        #region DecoRegistry

        private static readonly ConditionalWeakTable<PlayerState, CatDecoration> catDecos
           = new ConditionalWeakTable<PlayerState, CatDecoration>();

        public static T GetDeco<T>(PlayerState playerState) where T : CatDecoration
        {
            if (catDecos.TryGetValue(playerState, out var deco))
                if (deco is T) return deco as T;
            return null;
        }

        public static T GetDeco<T>(Player player) where T : CatDecoration
            => GetDeco<T>(player.playerState);

        public static T GetDeco<T>(PlayerGraphics playerGraphics) where T : CatDecoration
           => GetDeco<T>(playerGraphics.player.playerState);

        #endregion DecoRegistry

        #region CatDeco

        private static void GrafInitSprite(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.InitiateSprites(null, sLeaser, rCam);
        }

        private static void GrafAddToContainer(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.AddToContainer(null, sLeaser, rCam, newContatiner);
        }

        private static void GrafUpdate(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.Update(null);
        }

        private static void GrafDrawSprite(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.DrawSprites(null, sLeaser, rCam, timeStacker, camPos);
        }

        private static void GrafApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.ApplyPalette(null, sLeaser, rCam, palette);
        }

        private static void GrafSuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, PlayerGraphics self, Vector2 shortCutPosition)
        {
            orig(self, shortCutPosition);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.SuckedIntoShortCut(null, shortCutPosition);
        }

        private static void GrafReset(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig(self);
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.Reset(null);
        }

        #endregion CatDeco

        #region Properties

        private delegate SlugName orig_CharacterForColor(PlayerGraphics self);

        private static SlugName LancerForColor(orig_CharacterForColor orig, PlayerGraphics self)
        {
            var res = orig(self);
            if (IsLancer(self))
                if (HasLancer(res)) res = GetLancer(res);
            return res;
        }

        /*
        private delegate bool orig_RenderAsPup(PlayerGraphics self);

        private static bool RenderAsLancer(orig_RenderAsPup orig, PlayerGraphics self)
        {
            if (IsLancer(self)) return true;
            return orig(self);
        }
        */

        #endregion Properties

        private static Color DefaultLancerColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugName i)
        {
            if (IsLancer(i))
            {
                var basis = GetBasis(i);
                if (defaultLancerBodyColors.TryGetValue(basis, out var res)) return res;
                return orig(basis);
            }
            return orig(i);
        }

        private static readonly Dictionary<SlugName, Color> defaultLancerBodyColors
            = new Dictionary<SlugName, Color>()
            {
                { SlugName.White, new Color(0.8f, 1.0f, 0.5f) },
                { SlugName.Yellow, new Color(1.0f, 0.9f, 0.4f) },
                { SlugName.Red, new Color(0.3f, 0.5f, 1.0f) },
                { SlugName.Night, new Color(0.8f, 0.1f, 0.3f) }
            };

        private static List<string> ColoredLancerPartList(On.PlayerGraphics.orig_ColoredBodyPartList orig, SlugName slugcatID)
        {
            if (!IsLancer(slugcatID)) return orig(slugcatID);
            var basis = GetBasis(slugcatID);
            var list = orig(basis);
            list.Add("Horn");
            return list;
        }

        #endregion PlayerGraphics
    }
}