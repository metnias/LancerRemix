using SlugBase;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugName = SlugcatStats.Name;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;

namespace LancerRemix.Cat
{
    internal static class LancerGenerator
    {
        internal static SlugName CreateLancer(SlugName basis)
        {
            // TODO: assign new slugbase character
            var data = new Dictionary<string, string>();

            // ID
            string id = $"{basis.value}Lancer";
            data.Add("id", id);

            string json = DictToJson(data);
            SlugBaseCharacter.Registry.Add(JsonAny.Parse(json).AsObject());
            return new SlugName(id, false);

            string DictToJson(Dictionary<string, string> dict)
            {
                return "{" +
                          string.Join(",",
                                      from kvp in dict
                                      select $"\"{kvp.Key}\":\"{kvp.Value}\""
                          ) +
                          "}";
            }
        }
    }
}