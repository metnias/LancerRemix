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

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
        }

        internal static void OnMSCDisablePatch()
        {
        }

        private static bool[] isLancer = new bool[4];

        public static void SetIsLancer(bool[] players)
        {
            for (int i = 0; i < 4; ++i) isLancer[i] = players[i];
        }

        public static bool IsLancer(PlayerState playerState) => isLancer[playerState.playerNumber];

        #region Player

        private static readonly ConditionalWeakTable<PlayerState, CatSupplement> catSubs
            = new ConditionalWeakTable<PlayerState, CatSupplement>();

        private static T GetSub<T>(PlayerState playerState) where T : CatSupplement
        {
            if (catSubs.TryGetValue(playerState, out var sub))
                if (sub is T) return sub as T;
            return default;
        }

        private static void PlayerCtor(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (!IsLancer(self.playerState)) return;
            catSubs.Add(self.playerState, new LancerSupplement(self));
        }

        private static void GrabbedSub(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            if (IsLancer(self.playerState))
            {
                GetSub<LancerSupplement>(self.playerState).Grabbed(orig, grasp);
                return;
            }
            orig(self, grasp);
        }

        #endregion Player
    }
}