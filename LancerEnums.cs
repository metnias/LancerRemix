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
            ClearLancers();
            var slugs = ExtEnumBase.GetNames(typeof(SlugName));
            foreach (var name in slugs)
            {
                var slug = new SlugName(name, false);
                if (slug.Index < 0) continue;
                if (SlugcatStats.HiddenOrUnplayableSlugcat(slug)) continue;
                if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(slug)) continue;
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
            foreach (var lancer in AllLancer) lancer.Unregister();

            NameLancer.Clear();
            NameBasis.Clear();
            AllLancer.Clear();
            AllBasis.Clear();
        }

        internal static void UnregisterExtEnum()
        {
        }
    }
}