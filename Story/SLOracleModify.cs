using LancerRemix.Cat;
using RWCustom;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using static LancerRemix.LancerEnums;
using static LancerRemix.LancerGenerator;
using MSCName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal static class SLOracleModify
    {
        internal static void SubPatch()
        {
            On.SLOrcacleState.ForceResetState += LancerMoonState;
            On.SLOracleBehaviorHasMark.NameForPlayer += NameForLancer;
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void LancerMoonState(On.SLOrcacleState.orig_ForceResetState orig, SLOrcacleState self, SlugName saveStateNumber)
        {
            orig(self, saveStateNumber);
            var basis = saveStateNumber;
            if (IsLancer(basis)) basis = GetBasis(basis);
            var lancer = GetLancer(basis);
            var story = IsStoryLancer ? lancer : basis;

            if (IsTimelineInbetween(story, ModManager.MSC ? MSCName.Spear : null, SlugName.Red))
                self.neuronsLeft = 0; // dead after spear and before red
            else if (IsTimelineInbetween(story, SlugName.Red, SlugName.White))
                self.neuronsLeft = TryMineRedData(); // dead if red has not succeed and before white

            int TryMineRedData()
            {
                var progLines = Custom.rainWorld.progression?.GetProgLinesFromMemory();
                if (progLines == null || progLines.Length == 0) return 0;
                for (int i = 0; i < progLines.Length; ++i)
                {
                    var array = Regex.Split(progLines[i], "<progDivB>");
                    if (array.Length != 2 || array[0] != "SAVE STATE" || BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) != SlugName.Red) continue;

                    const string MOONREVIVED = ">MOONREVIVED";
                    var mineTarget = new List<SaveStateMiner.Target>()
                    { new SaveStateMiner.Target(MOONREVIVED, null, "<mwA>", 20) };
                    var mineResult = SaveStateMiner.Mine(Custom.rainWorld, array[1], mineTarget);
                    if (mineResult.Count > 0 && mineResult[0].name == MOONREVIVED) return 5;
                    return 0;
                }
                return 0;
            }
        }

        private static string NameForLancer(On.SLOracleBehaviorHasMark.orig_NameForPlayer orig, SLOracleBehaviorHasMark self, bool capitalized)
        {
            if (!IsStoryLancer && self.player?.playerState.isPup != true) return orig(self, capitalized);
            const string PREFIX = "lancersl-";
            string name = PREFIX + "animal";
            bool damaged = self.DamagedMode && Random.value < 0.5f;
            if (Random.value > 0.3f)
            {
                if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Likes)
                {
                    if (self.State.totalPearlsBrought > 5 && !self.DamagedMode)
                        name = PREFIX + "student";
                    else
                        name = PREFIX + "child";
                }
                else if (self.State.GetOpinion == SLOrcacleState.PlayerOpinion.Dislikes)
                    name = PREFIX + "imp";
                else
                    name = "creature";
            }
            if (self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage == InGameTranslator.LanguageID.Portuguese && (name == "friend" || name == "creature"))
            {
                string porName = self.Translate(name);
                if (porName.StartsWith(PREFIX)) porName = porName.Substring(PREFIX.Length);
                if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
                    porName = char.ToUpper(porName[0]).ToString() + porName.Substring(1);
                return porName;
            }
            string transName = self.Translate(name);
            if (transName.StartsWith(PREFIX)) transName = transName.Substring(PREFIX.Length);
            string little = self.Translate(PREFIX + "tiny");
            if (little.StartsWith(PREFIX)) little = little.Substring(PREFIX.Length);
            if (capitalized && InGameTranslator.LanguageID.UsesCapitals(self.oracle.room.game.rainWorld.inGameTranslator.currentLanguage))
                little = char.ToUpper(little[0]).ToString() + little.Substring(1);
            return little + (damaged ? "... " : " ") + transName;
        }



    }
}
