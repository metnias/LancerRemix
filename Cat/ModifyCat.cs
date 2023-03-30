using CatSub.Cat;
using System.Collections.Generic;
using System.Linq;
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
            On.Player.Grabbed += GrabbedSub;
            // TODO: hook RainWorldGame.StoryCharacter to return basis

            On.PlayerGraphics.InitiateSprites += InitSprite;

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

        #region Player

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


        private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!IsLancer(self.playerState)) return;
            catSubs.Add(self.playerState, new LancerSupplement(self));
            catDecos.Add(self.playerState, new LancerDecoration(self));
        }

        private static void GrabbedSub(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            if (IsLancer(self.playerState))
                GetSub<LancerSupplement>(self)?.Grabbed(orig, grasp);
            orig(self, grasp);
        }

        #endregion Player

        #region PlayerGraphics

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

        private static void InitSprite(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (IsLancer(self.player.playerState))
                GetDeco<LancerDecoration>(self)?.InitiateSprites(null, sLeaser, rCam);
            orig(self, sLeaser, rCam);
        }

        #endregion PlayerGraphics
    }
}