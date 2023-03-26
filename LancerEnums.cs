using Menu;
using SlugBase.DataTypes;
using MenuSceneID = Menu.MenuScene.SceneID;
using DreamID = DreamsState.DreamID;
using System.Collections.Generic;
using SlugName = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;

namespace LancerRemix
{
    public static class LancerEnums
    {
        private static Dictionary<SlugName, SlugName> NameLancer;
        private static Dictionary<SlugName, SlugName> NameBasis;
        private static HashSet<SlugName> AllLancer;

        internal static bool IsLancer(SlugName name) => AllLancer.Contains(name);

        internal static bool HasLancer(SlugName basis) => NameLancer.ContainsKey(basis);

        internal static SlugName GetLancer(SlugName basis) => NameLancer[basis];

        internal static SlugName GetBasis(SlugName lancer) => NameBasis[lancer];

        internal static void RegisterExtEnum()
        {
            AllLancer = new HashSet<SlugName>();
            NameLancer = new Dictionary<SlugName, SlugName>();
            NameBasis = new Dictionary<SlugName, SlugName>();
        }

        internal static void RegisterLancers()
        {
            ClearLancers();
            var slugs = ExtEnumBase.GetNames(typeof(SlugName));
            foreach (var name in slugs)
            {
                var slug = new SlugName(name, false);
                if (slug.Index < 0) continue;
                if (SlugcatStats.HiddenOrUnplayableSlugcat(slug)) continue;
                if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(slug)) continue;
                var lancer = CreateLancer(slug);
                AllLancer.Add(lancer);
                NameLancer.Add(slug, lancer);
                NameBasis.Add(lancer, slug);
            }
        }

        private static SlugName CreateLancer(SlugName basis)
        {
            // TODO: assign new slugbase character
            return new SlugName(basis.value + "Lancer", false);
        }

        internal static void ClearLancers()
        {
            NameLancer.Clear();
            AllLancer.Clear();
        }

        internal static void UnregisterExtEnum()
        {
        }
    }
}