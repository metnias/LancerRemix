﻿using System.Collections.Generic;
using DreamID = DreamsState.DreamID;
using MenuSceneID = Menu.MenuScene.SceneID;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;
using SlugTime = SlugcatStats.Timeline;

namespace LancerRemix
{
    public static class LancerEnums
    {
        #region Enums

        internal static MenuSceneID SceneHunterMeet;
        internal static MenuSceneID SceneGhostLancerWhite;
        internal static MenuSceneID SceneGhostLancerYellow;
        internal static MenuSceneID SceneGhostLancerRed;
        internal static MenuSceneID SceneOutroLHunter1Swim;

        internal static DreamID DreamHunterMeet;
        internal static Conversation.ID MoonRecieveNSHSwarmer;
        internal static SSOracleBehavior.Action MeetLonk_Images;

        internal static void RegisterExtEnum()
        {
            SceneHunterMeet = new MenuSceneID("dream - lancer hunter meet", false);
            SceneGhostLancerWhite = new MenuSceneID("white ghost lancer", false);
            SceneGhostLancerYellow = new MenuSceneID("yellow ghost lancer", false);
            SceneGhostLancerRed = new MenuSceneID("red ghost lancer", false);
            SceneOutroLHunter1Swim = new MenuSceneID("outro lhunter 1 - swim", false);

            DreamHunterMeet = new DreamID(nameof(DreamHunterMeet), true);
            MoonRecieveNSHSwarmer = new Conversation.ID(nameof(MoonRecieveNSHSwarmer), true);
            MeetLonk_Images = new SSOracleBehavior.Action(nameof(MeetLonk_Images), true);
        }

        internal static void UnregisterExtEnum()
        {
            SceneHunterMeet = null;
            SceneGhostLancerWhite = null;
            SceneGhostLancerYellow = null;
            SceneGhostLancerRed = null;
            DreamHunterMeet?.Unregister(); DreamHunterMeet = null;
            MoonRecieveNSHSwarmer?.Unregister(); MoonRecieveNSHSwarmer = null;
            MeetLonk_Images?.Unregister(); MeetLonk_Images = null;
        }

        #endregion Enums

        #region Lancers

        private static readonly Dictionary<SlugName, SlugName> NameLancer = new Dictionary<SlugName, SlugName>();
        private static readonly Dictionary<SlugName, SlugName> NameBasis = new Dictionary<SlugName, SlugName>();
        internal static readonly HashSet<SlugName> AllLancer = new HashSet<SlugName>();
        private static readonly HashSet<SlugName> AllBasis = new HashSet<SlugName>();
        internal static readonly Dictionary<SlugName, SlugTime> LancerTimes = new Dictionary<SlugName, SlugTime>();

        public static bool IsLancer(SlugName name) => AllLancer.Contains(name);

        public static bool HasLancer(SlugName basis) => AllBasis.Contains(basis);

        public static SlugName GetLancer(SlugName basis)
        {
            if (basis == null || basis.Index < 0) return basis;
            if (NameLancer.TryGetValue(basis, out var lancer)) return lancer;
            return basis;
        }

        public static SlugName GetBasis(SlugName lancer)
        {
            if (lancer == null || lancer.Index < 0) return lancer;
            if (NameBasis.TryGetValue(lancer, out var basis)) return basis;
            return lancer;
        }

        internal static void RegisterLancers()
        {
            if (CheckModChanged()) return;
            ClearLancers();
            var slugs = ExtEnumBase.GetNames(typeof(SlugName));
            foreach (var name in slugs)
            {
                var slug = new SlugName(name, false);
                if (slug.Index < 0) continue;
                if (LancerGenerator.IsCustomLancer(name)) continue;

                if (SlugcatStats.HiddenOrUnplayableSlugcat(slug))
                    if (!ModManager.MSC || slug != MSCName.Sofanthiel) continue;

                SlugName lancer;
                if (LancerGenerator.HasCustomLancer(name, out var customName))
                {
                    lancer = new SlugName(customName, false);
                    LancerPlugin.LogSource.LogMessage($"{slug}({slug.Index}) has Custom Lancer: {lancer.value}({lancer.Index})");
                    if (lancer.Index < 0) continue;
                }
                else if (!LancerGenerator.CreateLancer(slug, out lancer)) continue;
                AllLancer.Add(lancer);
                AllBasis.Add(slug);
                NameLancer.Add(slug, lancer);
                NameBasis.Add(lancer, slug);
                LancerPlugin.LogSource.LogMessage($"Created {lancer.value}({lancer.Index}) for {slug}({slug.Index})");
            }
            slugNameVersion = ExtEnumBase.GetExtEnumType(typeof(SlugName)).version;
            LancerPlugin.CheckedAnyModChanged();
        }

        internal static void ClearLancers()
        {
            foreach (var lancer in AllLancer)
            {
                if (lancer == null || lancer.Index < 0) continue;
                LancerGenerator.DeleteLancer(lancer);
            }

            NameLancer.Clear();
            NameBasis.Clear();
            AllLancer.Clear();
            AllBasis.Clear();
            LancerTimes.Clear();
        }

        private static int slugNameVersion = -1;

        private static bool CheckModChanged()
        {
            if (slugNameVersion != ExtEnumBase.GetExtEnumType(typeof(SlugName)).version) return false;
            if (!LancerPlugin.AnyModChanged) return true; // skip checking
            return false;

            /*
            var curMods = GetCurrentMods();
            if (CheckModsEquals()) return true;
            enabledMods = curMods;
            return false;

            HashSet<string> GetCurrentMods()
            {
                var mods = new HashSet<string>();
                if (ModManager.MMF) mods.Add(MoreSlugcats.MMF.MOD_ID);
                if (ModManager.MSC) mods.Add(MoreSlugcats.MoreSlugcats.MOD_ID);
                if (ModManager.Expedition) mods.Add(Expedition.Expedition.MOD_ID);
                if (ModManager.JollyCoop) mods.Add(JollyCoop.JollyCoop.MOD_ID);
                foreach (var mod in ModManager.ActiveMods) mods.Add(mod.id);
                return mods;
            }
            bool CheckModsEquals()
            {
                if (curMods.Count != enabledMods.Count) return false;
                foreach (var mod in enabledMods) if (!curMods.Contains(mod)) return false;
                return true;
            }
            */
        }

        #endregion Lancers

        /// <summary>
        /// Register custom slugcat lancer campaign. <paramref name="lancer"/> should be SlugBase character.
        /// </summary>
        public static void RegisterCustomLancer(SlugName basis, SlugName lancer)
            => LancerGenerator.RegisterCustomLancer(basis, lancer);
    }
}