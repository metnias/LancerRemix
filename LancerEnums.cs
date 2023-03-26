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
        public static Dictionary<SlugName, SlugName> NameLancer;
        public static HashSet<SlugName> AllLancer;

        internal static void RegisterExtEnum()
        {
            AllLancer = new HashSet<SlugName>();
            NameLancer = new Dictionary<SlugName, SlugName>();
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
            }
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