﻿using CatSub.Story;
using LancerRemix.Cat;
using LancerRemix.LancerMenu;
using Menu;
using RWCustom;
using SlugBase;
using System.Collections.Generic;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix
{
    internal static class LancerGenerator
    {
        internal static void Patch()
        {
            On.SlugcatStats.ctor += LancerStats;
            On.SlugcatStats.SlugcatFoodMeter += LancerFoodMeter;
            On.SlugcatStats.SlugcatUnlocked += LancerUnlocked;
            On.SlugcatStats.NourishmentOfObjectEaten += LancerNourishmentOfObjectEaten;
            On.Menu.MenuScene.UseSlugcatUnlocked += UseLancerUnlocked;
            On.SlugcatStats.HiddenOrUnplayableSlugcat += DisableLancerRegularSelect;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static bool LancerUnlocked(On.SlugcatStats.orig_SlugcatUnlocked orig, SlugName i, RainWorld rainWorld)
        {
            if (IsLancer(i))
            {
                i = GetBasis(i);
                if (SlugcatStats.IsSlugcatFromMSC(i)) return LancerPlugin.MSCLANCERS && HasCustomLancer(i.value, out var _);
            }
            return orig(i, rainWorld);
        }

        private static bool UseLancerUnlocked(On.Menu.MenuScene.orig_UseSlugcatUnlocked orig, MenuScene self, SlugName slugcat)
        {
            if (self.owner is SelectMenuPatch.LancerPageNewGame || self.owner is SelectMenuPatch.LancerPageContinue)
                return ((self.owner as SlugcatSelectMenu.SlugcatPage).menu as SlugcatSelectMenu).SlugcatUnlocked(GetLancer(slugcat));
            return orig(self, slugcat);
        }

        private static bool DisableLancerRegularSelect(On.SlugcatStats.orig_HiddenOrUnplayableSlugcat orig, SlugName i)
        {
            if (IsLancer(i)) return true;
            return orig(i);
        }

        private static void LancerStats(On.SlugcatStats.orig_ctor orig, SlugcatStats self, SlugName slugcat, bool malnourished)
        {
            if (IsCustomLancer(slugcat)) { orig(self, slugcat, malnourished); return; }
            var basis = GetBasis(slugcat);
            orig(self, basis, malnourished);
            if (!IsStoryLancer && !IsLancer(slugcat)) return;
            if (!lancerModifiers.TryGetValue(basis, out var mod)) return;

            self.lungsFac *= mod.lungsFac;
            self.poleClimbSpeedFac *= mod.poleClimbSpeedFac;
            self.corridorClimbSpeedFac *= mod.corridorClimbSpeedFac;
            self.runspeedFac *= mod.runspeedFac;
            self.bodyWeightFac *= mod.bodyWeightFac;
            self.loudnessFac *= mod.loudnessFac;

            self.generalVisibilityBonus += mod.generalVisibilityBonus;
            self.visualStealthInSneakMode += mod.visualStealthInSneakMode;

            if (basis == SlugName.Yellow) { self.foodToHibernate = 2; self.maxFood = 3; }
        }

        private static IntVector2 LancerFoodMeter(On.SlugcatStats.orig_SlugcatFoodMeter orig, SlugName slugcat)
        {
            if (IsCustomLancer(slugcat)) return orig(slugcat);
            var basis = GetBasis(slugcat);
            var res = orig(basis);
            if (!IsStoryLancer && !IsLancer(slugcat)) return res;
            if (basis == SlugName.Yellow) return new IntVector2(3, 2);
            return res;
        }

        private static int LancerNourishmentOfObjectEaten(On.SlugcatStats.orig_NourishmentOfObjectEaten orig, SlugName slugcatIndex, IPlayerEdible eatenObject)
        {
            if (IsCustomLancer(slugcatIndex)) return orig(slugcatIndex, eatenObject);
            var basis = GetBasis(slugcatIndex);
            var res = orig(basis, eatenObject);
            //Debug.Log($"{slugcatIndex}({IsStoryLancer}) Nourishment: {res}");
            if (!IsStoryLancer && !IsLancer(slugcatIndex)) return res;
            if (basis == SlugName.Yellow) res >>= 2;
            return res;
        }

        internal static bool CreateLancer(SlugName basis, out SlugName lancer)
        {
            string id = GetLancerName(basis.value);
            lancer = new SlugName(id, false);
            if (lancer.Index >= 0) return false;
            lancerModifiers.Remove(basis);

            if (basis == SlugName.White || basis == SlugName.Yellow || basis == SlugName.Red || basis == SlugName.Night)
                lancer = RegisterVanillaLancer(basis);
            else if (SlugcatStats.IsSlugcatFromMSC(basis) && !HasCustomLancer(basis.value, out var _))
                lancer = RegisterVanillaLancer(basis);

            if (SlugBaseCharacter.TryGet(basis, out var _))
            {
                if (lancer.Index >= 0 && SlugBaseCharacter.TryGet(lancer, out var _)) RegisterCustomLancer(basis, lancer);
                else lancer = RegisterSlugBaseLancer(basis);
            }
            if (lancer == null || lancer.Index < 0) return false; // something went wrong

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

        internal static string GetLancerName(string basisName)
        {
            if (CustomLancerDictionary.TryGetValue(basisName, out var lancer)) return lancer;
            return $"{basisName}Lancer";
        }

        internal static bool HasCustomLancer(string basisName, out string lancerName)
            => CustomLancerDictionary.TryGetValue(basisName, out lancerName);

        internal static bool IsCustomLancer(string lancerName)
            => CustomLancers.Contains(lancerName);

        internal static bool IsCustomLancer(SlugName lancerName)
            => IsCustomLancer(lancerName.value);

        private static readonly Dictionary<string, string> CustomLancerDictionary = new Dictionary<string, string>();
        private static readonly HashSet<string> CustomLancers = new HashSet<string>();

        internal static void DeleteLancer(SlugName lancer)
        {
            StoryRegistry.UnregisterTimeline(lancer);
            if (SlugBaseCharacter.Registry.TryGet(lancer, out var _))
                SlugBaseCharacter.Registry.Remove(lancer);
            lancerModifiers.Remove(GetBasis(lancer));

            if (IsCustomLancer(lancer.value)) return;
            lancer?.Unregister();
        }

        private static SlugName RegisterVanillaLancer(SlugName basis)
        {
            var lancer = new SlugName(GetLancerName(basis.value), true);
            if (basis == SlugName.Yellow)
                StoryRegistry.RegisterTimeline(new StoryRegistry.TimelinePointer(lancer, StoryRegistry.TimelinePointer.Relative.Before, SlugName.Red));
            else
                StoryRegistry.RegisterTimeline(new StoryRegistry.TimelinePointer(lancer, StoryRegistry.TimelinePointer.Relative.After, basis));
            var modifier = new StatModifier
            {
                lungsFac = 0.6f,
                poleClimbSpeedFac = 1.1f,
                corridorClimbSpeedFac = 1.05f,
                runspeedFac = 1.2f,
                bodyWeightFac = 0.8f,
                loudnessFac = 0.8f,

                generalVisibilityBonus = -0.1f,
                visualStealthInSneakMode = 0.2f
            };
            lancerModifiers.Add(basis, modifier);

            return lancer;
        }

        internal static void RegisterCustomLancer(SlugName basis, SlugName lancer)
        {
            var modifier = new StatModifier
            {
                lungsFac = 1f,
                poleClimbSpeedFac = 1f,
                corridorClimbSpeedFac = 1f,
                runspeedFac = 1f,
                bodyWeightFac = 1f,
                loudnessFac = 1f,

                generalVisibilityBonus = 0f,
                visualStealthInSneakMode = 0f
            };
            lancerModifiers.Add(basis, modifier);
            if (CustomLancers.Contains(lancer.value)) CustomLancerDictionary.Remove(basis.value);
            CustomLancers.Add(lancer.value);
            CustomLancerDictionary.Add(basis.value, lancer.value);
            LancerPlugin.LogSource.LogMessage($"Registered Custom Lancer {lancer}({lancer.Index}) for {basis}({basis.Index})");
        }

        private static SlugName RegisterSlugBaseLancer(SlugName basis)
        {
            var lancer = new SlugName(GetLancerName(basis.value), true);
            StoryRegistry.RegisterTimeline(new StoryRegistry.TimelinePointer(lancer, StoryRegistry.TimelinePointer.Relative.After, basis));
            var modifier = new StatModifier
            {
                lungsFac = 1.1f,
                poleClimbSpeedFac = 1.1f,
                corridorClimbSpeedFac = 1.05f,
                runspeedFac = 1.2f,
                bodyWeightFac = 0.8f,
                loudnessFac = 0.8f,

                generalVisibilityBonus = -0.1f,
                visualStealthInSneakMode = 0.2f
            };
            lancerModifiers.Add(basis, modifier);
            return lancer;

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

        internal static SlugName GetStoryBasisForLancer(SlugName lancer)
        {
            var basis = GetBasis(lancer);
            if (basis == SlugName.Yellow) basis = SlugName.Red;
            return basis;
        }

        internal static bool IsTimelineInbetween(SlugName check, SlugName leftExclusive, SlugName rightExclusive)
        {
            var timeline = SlugcatStats.getSlugcatTimelineOrder();
            int c = -1, l = -1, r = timeline.Length;
            for (int i = 0; i < timeline.Length; ++i)
            {
                if (timeline[i] == check) c = i;
                if (timeline[i] == leftExclusive) l = i;
                if (timeline[i] == rightExclusive) r = i;
            }
            //Debug.Log($"Timeline Check: {l}<{c}<{r}");
            return l < c && c < r;
        }

        private static readonly Dictionary<SlugName, StatModifier> lancerModifiers
            = new Dictionary<SlugName, StatModifier>();

        private struct StatModifier
        {
            // multipliers
            public float lungsFac;

            public float poleClimbSpeedFac;
            public float corridorClimbSpeedFac;
            public float runspeedFac;
            public float bodyWeightFac;
            public float loudnessFac;

            // adders
            public float generalVisibilityBonus;

            public float visualStealthInSneakMode;

            // public int throwingSkill;
        }
    }
}