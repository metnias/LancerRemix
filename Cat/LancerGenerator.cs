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

namespace LancerRemix.Cat
{
    internal static class LancerGenerator
    {
        internal static void SubPatch()
        {
            On.SlugcatStats.getSlugcatTimelineOrder += AppendTimelineOrder;
            On.SlugcatStats.HiddenOrUnplayableSlugcat += DisableRegularSelect;
        }

        private static SlugName[] AppendTimelineOrder(On.SlugcatStats.orig_getSlugcatTimelineOrder orig)
        {
            LinkedList<SlugName> list = new LinkedList<SlugName>(orig());
            var node = list.First;
            while (node.Next != null)
            {
                if ((ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(node.Value)) || !HasLancer(node.Value))
                { node = node.Next; continue; }
                list.AddAfter(node, GetLancer(node.Value));
                node = node.Next;
            }

            return list.ToArray();
        }

        private static bool DisableRegularSelect(On.SlugcatStats.orig_HiddenOrUnplayableSlugcat orig, SlugName i)
        {
            if (IsLancer(i)) return true;
            return orig(i);
        }

        internal static SlugName CreateLancer(SlugName basis)
        {
            string id = $"{basis.value}Lancer";
            var prebuilt = new SlugName(id, false);
            if (prebuilt.Index >= 0) return prebuilt;
            return new SlugName(id, true);

            //var builder = new JsonBuilder().
            //    Value("id", $"{basis.value}Lancer");

            //if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basis))
            //    GetMSCLancerData(basis, ref builder);
            //else if (SlugBaseCharacter.Registry.TryGet(basis, out var character)) PopulateSlugBaseLancerData(character, ref builder);
            //else PopulateVanillaLancerData(basis, ref builder);

            //var pair = SlugBaseCharacter.Registry.Add(JsonAny.Parse(builder.Build()).AsObject());
            //return pair.Key;
        }

        /*
        private static void PopulateVanillaLancerData(SlugName basis, ref JsonBuilder builder)
        {
            // name
            builder.Value("name", SlugcatStats.getSlugcatName(basis));
            // description

            // features
            builder.Object("features", features =>
            {
                features.Value("color", "FFFFFF");
            });
        }

        private static void PopulateSlugBaseLancerData(SlugBaseCharacter character, ref JsonBuilder builder)
        {
            // name
            builder.Value("name", character.DisplayName);
            // description
            builder.Value("description", character.Description);

            // features
            builder.Object("features", features =>
            {
                foreach (var feature in character.Features)
                {
                    features.Value(feature.ID, feature.ToString());
                }
            });
        }

        private static void GetMSCLancerData(SlugName basis, ref JsonBuilder builder)
        {
        }
        */
    }
}