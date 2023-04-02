using MenuSceneID = Menu.MenuScene.SceneID;
using DreamID = DreamsState.DreamID;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;
using static CatSub.Story.SaveManager;
using Menu;
using LancerRemix.Cat;

namespace LancerRemix.Story
{
    internal class DreamHandler
    {
        internal static void Patch()
        {
            On.SaveState.ctor += AddDreamState;
            On.RainWorldGame.Win += WinLancer;
            On.DreamsState.StaticEndOfCycleProgress += LancerDreamProgress;
            On.Menu.DreamScreen.SceneFromDream += LancerSceneFromDream;

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
            On.DaddyLongLegs.Update += HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption += HunterRecognizeLancer;
        }

        internal static void OnMSCDisablePatch()
        {
            On.DaddyLongLegs.Update -= HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption -= HunterRecognizeLancer;
        }

        private const string HUNTERMEET = "LancerHunterMeet";

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void AddDreamState(On.SaveState.orig_ctor orig, SaveState self, SlugName saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (!IsStoryLancer) return;

            var basis = saveStateNumber;
            if (IsLancer(basis)) basis = GetBasis(basis);

            if (basis != SlugName.Red) return;
            self.dreamsState = new DreamsState(); // add dream state to lancer hunter
            SetProgValue(self.miscWorldSaveData, HUNTERMEET, 0); // never met
        }

        private static void WinLancer(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (IsStoryLancer)
            {
                var state = self.GetStorySession.saveState;
                if (state.dreamsState != null) LancerCheckEventDream(state);
            }
            orig(self, malnourished);

            void LancerCheckEventDream(SaveState saveState)
            {
                var basis = saveState.saveStateNumber;
                if (IsLancer(basis)) basis = GetBasis(basis);

                if (basis != SlugName.Red) return;

                var dreamsState = saveState.dreamsState;
                //if (GetProgValue<int>(saveState.miscWorldSaveData, HUNTERMEET) == 1) // met once
                {
                    dreamsState.InitiateEventDream(DreamHunterMeet);
                    SetProgValue(saveState.miscWorldSaveData, HUNTERMEET, 2); // met and dreamed
                }
            }
        }

        private static void LancerDreamProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamsState.DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
            if (!IsStoryLancer) return;

            var basis = saveState.saveStateNumber;
            if (IsLancer(basis)) basis = GetBasis(basis);

            if (basis != SlugName.Red) return;
            if (upcomingDream != DreamHunterMeet) upcomingDream = null;
        }

        private static MenuSceneID LancerSceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, DreamScreen self, DreamID dreamID)
        {
            if (dreamID == DreamHunterMeet) return SceneHunterMeet;

            return orig(self, dreamID);
        }

        private static void HunterMeetLancerTrigger(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);
            if (self.room != null || !self.HDmode || !self.room.game.IsStorySession || !IsStoryLancer) return;
            var basis = self.room.game.StoryCharacter;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red) return;
            if (GetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET) > 0) return; // already triggered
            if (!(self.room.game.FirstAlivePlayer?.realizedCreature is Player player)) return;
            if (self.room.VisualContact(self.mainBodyChunk.pos, player.mainBodyChunk.pos))
                SetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET, 1);
        }

        private static bool HunterRecognizeLancer(On.DaddyLongLegs.orig_CheckDaddyConsumption orig, DaddyLongLegs self, PhysicalObject otherObject)
        {
            var result = orig(self, otherObject);
            if (self.room != null || !self.HDmode || !self.room.game.IsStorySession || !IsStoryLancer) return result;
            var basis = self.room.game.StoryCharacter;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red) return result;
            return !(otherObject is Player);
        }
    }
}