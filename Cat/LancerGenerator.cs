using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugName = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static UnityEngine.RectTransform;
using UnityEngine;
using static LancerRemix.LancerEnums;
using CatSub.Cat;
using CatSub.Story;
using System.IO;

namespace LancerRemix.Cat
{
    internal static class LancerGenerator
    {
        internal static void Patch()
        {
            On.SlugcatStats.SlugcatUnlocked += LancerUnlocked;
            On.SlugcatStats.HiddenOrUnplayableSlugcat += DisableRegularSelect;
        }

        private static bool LancerUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugName i, RainWorld rainWorld)
        {
            if (IsLancer(i))
            {
                var basis = GetBasis(i);
                if (SlugcatStats.IsSlugcatFromMSC(basis)) return false; // TBA
                return orig(basis, rainWorld);
            }
            return orig(i, rainWorld);
        }

        private static bool DisableRegularSelect(On.SlugcatStats.orig_HiddenOrUnplayableSlugcat orig, SlugName i)
        {
            if (IsLancer(i)) return true;
            return orig(i);
        }

        internal static bool CreateLancer(SlugName basis, out SlugName lancer)
        {
            string id = GetLancerName(basis.value);
            lancer = new SlugName(id, false);
            if (lancer.Index >= 0) return false;

            if (basis == SlugName.White || basis == SlugName.Yellow || basis == SlugName.Red || basis == SlugName.Night)
                lancer = RegisterVanillaLancer(basis);
            else if (SlugcatStats.IsSlugcatFromMSC(basis))
                lancer = RegisterMSCLancer(basis);
            if (SlugBaseCharacter.TryGet(basis, out var _))
                lancer = RegisterSlugBaseLancer(basis);
            if (lancer == null || lancer.Index < 0) return false; // something went wrong

            // SubRegistry.Register(lancer, (player) => new LancerSupplement(player));
            // DecoRegistry.Register(lancer, (player) => new LancerDecoration(player));
            StoryRegistry.RegisterTimeline(new StoryRegistry.TimelinePointer(lancer, StoryRegistry.TimelinePointer.Relative.After, basis));

            return true;

            //var builder = new JsonBuilder().
            //    Value("id", $"{basis.value}Lancer");

            //if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basis))
            //    GetMSCLancerData(basis, ref builder);
            //else if (SlugBaseCharacter.Registry.TryGet(basis, out var character)) PopulateSlugBaseLancerData(character, ref builder);
            //else PopulateVanillaLancerData(basis, ref builder);

            //var pair = SlugBaseCharacter.Registry.Add(JsonAny.Parse(builder.Build()).AsObject());
            //return pair.Key;
        }

        private static string GetLancerName(string basisName) => $"{basisName}Lancer";

        internal static void DeleteLancer(SlugName lancer)
        {
            SubRegistry.Unregister(lancer);
            DecoRegistry.Unregister(lancer);
            StoryRegistry.UnregisterTimeline(lancer);
            if (SlugBaseCharacter.Registry.TryGet(lancer, out var _))
                SlugBaseCharacter.Registry.Remove(lancer);

            lancer?.Unregister();
        }

        private static SlugName RegisterVanillaLancer(SlugName basis)
        {
            return new SlugName(GetLancerName(basis.value), true);
        }

        private static SlugName RegisterMSCLancer(SlugName basis)
        {
            return new SlugName(GetLancerName(basis.value), true);
        }

        private static SlugName RegisterSlugBaseLancer(SlugName basis)
        {
            return new SlugName(GetLancerName(basis.value), true);

            /*
            if (!SlugBaseCharacter.Registry.TryGetPath(basis, out string path))
                return null;
            string data = File.ReadAllText(path);
            data = ReplaceID(data);
            var json = JsonAny.Parse(data).AsObject();
            return SlugBaseCharacter.Registry.Add(json).Key;

            string ReplaceID(string text)
            {
                string basisName = basis.value;
                string lancerName = GetLancerName(basisName);
                int index = text.IndexOf(basisName);
                if (index >= 0) return $"{text.Substring(0, index)}{lancerName}{text.Substring(index + basisName.Length)}";
                return text;
            }
            */
        }
    }
}