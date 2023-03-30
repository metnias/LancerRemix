using Menu;
using SlugBase.DataTypes;
using MenuSceneID = Menu.MenuScene.SceneID;
using DreamID = DreamsState.DreamID;
using System.Collections.Generic;
using SlugName = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugBase;
using System.Linq;
using LancerRemix.Cat;
using CatSub.Cat;

namespace LancerRemix
{
    public static class LancerEnums
    {
        private static readonly Dictionary<SlugName, SlugName> NameLancer = new Dictionary<SlugName, SlugName>();
        private static readonly Dictionary<SlugName, SlugName> NameBasis = new Dictionary<SlugName, SlugName>();
        internal static readonly HashSet<SlugName> AllLancer = new HashSet<SlugName>();
        private static readonly HashSet<SlugName> AllBasis = new HashSet<SlugName>();

        internal static bool IsLancer(SlugName name) => AllLancer.Contains(name);

        internal static bool HasLancer(SlugName basis) => AllBasis.Contains(basis);

        internal static SlugName GetLancer(SlugName basis) => NameLancer[basis];

        internal static SlugName GetBasis(SlugName lancer) => NameBasis[lancer];

        internal static void RegisterExtEnum()
        {
        }

        internal static void RegisterLancers()
        {
            if (CheckModState()) return;
            ClearLancers();
            var slugs = ExtEnumBase.GetNames(typeof(SlugName));
            foreach (var name in slugs)
            {
                var slug = new SlugName(name, false);
                if (slug.Index < 0) continue;
                // if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(slug)) continue;
                if (SlugcatStats.HiddenOrUnplayableSlugcat(slug))
                    if (!ModManager.MSC || slug != MSCName.Sofanthiel) continue;
                if (!LancerGenerator.CreateLancer(slug, out var lancer)) continue;
                AllLancer.Add(lancer);
                AllBasis.Add(slug);
                NameLancer.Add(slug, lancer);
                NameBasis.Add(lancer, slug);
                LancerPlugin.LogSource.LogMessage($"Created {lancer.value}({lancer.Index}) for {slug}({slug.Index})");
            }
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
        }

        private static HashSet<string> enabledMods = new HashSet<string>();

        private static bool CheckModState()
        {
            var curMods = new HashSet<string>();
            if (ModManager.MMF) curMods.Add(MoreSlugcats.MMF.MOD_ID);
            if (ModManager.MSC) curMods.Add(MoreSlugcats.MoreSlugcats.MOD_ID);
            if (ModManager.Expedition) curMods.Add(Expedition.Expedition.MOD_ID);
            if (ModManager.JollyCoop) curMods.Add(JollyCoop.JollyCoop.MOD_ID);
            foreach (var mod in ModManager.ActiveMods) curMods.Add(mod.id);

            if (CheckEquals()) return true;
            enabledMods = curMods;
            return false;

            bool CheckEquals()
            {
                if (curMods.Count != enabledMods.Count) return false;
                foreach (var mod in enabledMods) if (!curMods.Contains(mod)) return false;
                return true;
            }
        }

        internal static void UnregisterExtEnum()
        {
        }
    }
}