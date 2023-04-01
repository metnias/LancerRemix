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

            On.PlayerGraphics.InitiateSprites += GrafInitSprite;
            On.PlayerGraphics.AddToContainer += GrafAddToContainer;
            On.PlayerGraphics.Update += GrafUpdate;
            On.PlayerGraphics.DrawSprites += GrafDrawSprite;
            On.PlayerGraphics.ApplyPalette += GrafApplyPalette;
            On.PlayerGraphics.SuckedIntoShortCut += GrafSuckedIntoShortCut;
            On.PlayerGraphics.Reset += GrafReset;
            On.PlayerGraphics.ctor += GrafCtor;

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
            if (IsLancer(self.playerState))
            { GetSub<LancerSupplement>(self)?.Grabbed(orig, grasp); return; }
            orig(self, grasp);
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

        private static void GrafCtor(On.PlayerGraphics.orig_ctor orig, PlayerGraphics self, PhysicalObject ow)
        {
            orig(self, ow);
            // if (IsLancer(self)) GetDeco<LancerDecoration>(self)?.Ctor(self);
        }

        #region Properties

        private delegate SlugName orig_CharacterForColor(PlayerGraphics self);

        private static SlugName LancerForColor(orig_CharacterForColor orig, PlayerGraphics self)
        {
            var res = orig(self);
            if (IsLancer(self))
                if (HasLancer(res)) res = GetLancer(res);
            return res;
        }

        private delegate bool orig_RenderAsPup(PlayerGraphics self);

        private static bool RenderAsLancer(orig_RenderAsPup orig, PlayerGraphics self)
        {
            if (IsLancer(self)) return true;
            return orig(self);
        }

        #endregion Properties

        private static Color DefaultLancerColor(On.PlayerGraphics.orig_DefaultSlugcatColor orig, SlugName i)
        {
            if (LancerEnums.IsLancer(i))
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
                {SlugName.White, new Color(0.8f, 1.0f, 0.5f) },
                {SlugName.Yellow, new Color(1.0f, 0.9f, 0.4f)},
                {SlugName.Red, new Color(0.3f, 0.5f, 1.0f)},
                {SlugName.Night, new Color(0.8f, 0.1f, 0.3f) }
            };

        #endregion PlayerGraphics
    }
}