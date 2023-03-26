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

namespace LancerRemix.Cat
{
    internal static class LancerGenerator
    {
        internal static SlugName CreateLancer(SlugName basis)
        {
            var data = new Dictionary<string, string>();

            // ID
            string id = $"{basis.value}Lancer";
            data.Add("id", id.StringQuote());

            if (ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(basis))
                GetMSCLancerData(basis, ref data);
            else if (SlugBaseCharacter.Registry.TryGet(basis, out var character)) PopulateSlugBaseLancerData(character, ref data);
            else PopulateVanillaLancerData(basis, ref data);

            var pair = SlugBaseCharacter.Registry.Add(JsonAny.Parse(DictToJson(data)).AsObject());
            return pair.Key;
        }

        private static string DictToJson(Dictionary<string, string> dict) => "{" +
                      string.Join(",",
                                  from kvp in dict
                                  select $"\"{kvp.Key}\":{kvp.Value}"
                      ) + "}";

        private static string StringQuote(this string text) => $"\"{text}\"";

        private static string CustomColor(Color[] storyColors)
        {
            StringBuilder sb = new StringBuilder();
            sb.Append('[');
            for (int i = 0; i < storyColors.Length; ++i)
            {
            }
            sb.Append(']');
            return sb.ToString();
        }

        private static void PopulateVanillaLancerData(SlugName basis, ref Dictionary<string, string> data)
        {
            // name
            data.Add("name", SlugcatStats.getSlugcatName(basis).StringQuote());
            // description

            // features
            var features = new Dictionary<string, string>();

            data.Add("features", DictToJson(features));
        }

        private static void PopulateSlugBaseLancerData(SlugBaseCharacter character, ref Dictionary<string, string> data)
        {
            // name
            data.Add("name", character.DisplayName.StringQuote());
            // description
            data.Add("description", character.Description.StringQuote());

            // features
            var features = new Dictionary<string, string>();

            data.Add("features", DictToJson(features));
        }

        private static void GetMSCLancerData(SlugName basis, ref Dictionary<string, string> data)
        {
        }
    }
}