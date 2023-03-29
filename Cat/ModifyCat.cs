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


        private static void GrabbedSub(On.Player.orig_Grabbed orig, Player self, Creature.Grasp grasp)
        {
            if (IsLancer(self.SlugCatClass) && SubRegistry.TryGetSub(self.playerState, out CatSupplement sub))
            {
                if (sub is LancerSupplement lancerSub) lancerSub.Grabbed(orig, grasp);
                return;
            }
            orig(self, grasp);
        }



    }
}