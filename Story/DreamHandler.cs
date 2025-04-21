using LancerRemix.Cat;
using Menu;
using System.Linq;
using static CatSub.Story.SaveManager;
using static LancerRemix.LancerEnums;
using DreamID = DreamsState.DreamID;
using MenuSceneID = Menu.MenuScene.SceneID;
using SlugName = SlugcatStats.Name;

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
            On.RainWorldGame.ExitToVoidSeaSlideShow += LancerToVoidSeaSlideShow;
            On.Menu.SlideShow.ctor += LancerRemoveOutroTree;

            LunterScripts.SubPatch();
            SLOracleModify.SubPatch();
            SSOracleModify.SubPatch();
            LurvivorScripts.SubPatch();

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
            LunterScripts.OnMSCEnableSubPatch();
            LurvivorScripts.OnMSCEnableSubPatch();
            //On.RoomSettings.ctor += SLOracleModify.LonkInvSLRoomSettings;
        }

        internal static void OnMSCDisablePatch()
        {
            LunterScripts.OnMSCDisableSubPatch();
            LurvivorScripts.OnMSCDisableSubPatch();
            //On.RoomSettings.ctor -= SLOracleModify.LonkInvSLRoomSettings;
        }

        internal const string COORDNULL = "COORDNULL";

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void AddDreamState(On.SaveState.orig_ctor orig, SaveState self, SlugName saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);
            if (!IsStoryLancer) return;

            var basis = GetBasis(saveStateNumber);

            if (basis == SlugName.Red)
            {
                self.dreamsState = new DreamsState(); // add dream state to lancer hunter
                SetProgValue(self.miscWorldSaveData, LunterScripts.HUNTERMEET, 0); // never met
                return;
            }
            if (basis == SlugName.White || basis == SlugName.Yellow)
                self.dreamsState = null; // no dream state for lancer surv/monk
        }

        private static void WinLancer(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished, bool fromWarpPoint)
        {
            if (IsStoryLancer)
            {
                var state = self.GetStorySession.saveState;
                if (state.dreamsState != null) LancerCheckEventDream(state);
            }
            orig(self, malnourished, fromWarpPoint);

            void LancerCheckEventDream(SaveState saveState)
            {
                var basis = GetBasis(saveState.saveStateNumber);

                if (basis != SlugName.Red) return;

                var dreamsState = saveState.dreamsState;
                if (GetProgValue<int>(saveState.miscWorldSaveData, LunterScripts.HUNTERMEET) == 1) // met once
                {
                    UnityEngine.Debug.Log("Trigger DreamHunterMeet");
                    dreamsState.InitiateEventDream(DreamHunterMeet);
                    SetProgValue(saveState.miscWorldSaveData, LunterScripts.HUNTERMEET, 2); // met and dreamed
                }
            }
        }

        private static void LancerDreamProgress(On.DreamsState.orig_StaticEndOfCycleProgress orig, SaveState saveState, string currentRegion, string denPosition, ref int cyclesSinceLastDream, ref int cyclesSinceLastFamilyDream, ref int cyclesSinceLastGuideDream, ref int inGWOrSHCounter, ref DreamsState.DreamID upcomingDream, ref DreamID eventDream, ref bool everSleptInSB, ref bool everSleptInSB_S01, ref bool guideHasShownHimselfToPlayer, ref int guideThread, ref bool guideHasShownMoonThisRound, ref int familyThread)
        {
            orig(saveState, currentRegion, denPosition, ref cyclesSinceLastDream, ref cyclesSinceLastFamilyDream, ref cyclesSinceLastGuideDream, ref inGWOrSHCounter, ref upcomingDream, ref eventDream, ref everSleptInSB, ref everSleptInSB_S01, ref guideHasShownHimselfToPlayer, ref guideThread, ref guideHasShownMoonThisRound, ref familyThread);
            if (!IsStoryLancer) return;

            var basis = GetBasis(saveState.saveStateNumber);

            if (basis != SlugName.Red) return;
            if (upcomingDream != DreamHunterMeet) upcomingDream = null;
        }

        private static MenuSceneID LancerSceneFromDream(On.Menu.DreamScreen.orig_SceneFromDream orig, DreamScreen self, DreamID dreamID)
        {
            if (dreamID == DreamHunterMeet) return SceneHunterMeet;

            return orig(self, dreamID);
        }

        private static void LancerToVoidSeaSlideShow(On.RainWorldGame.orig_ExitToVoidSeaSlideShow orig, RainWorldGame self)
        {
            OutroLancerFaceBasis = null;
            orig(self);
            if (!IsStoryLancer) return;
            var basis = GetBasis(self.StoryCharacter);
            if (basis == null) return;
            if (basis == SlugName.Red)
                self.manager.nextSlideshow = SlideShow.SlideShowID.RedOutro;
            else if (basis == SlugName.White)
                self.manager.nextSlideshow = SlideShow.SlideShowID.WhiteOutro;
            else if (basis == SlugName.Yellow)
                self.manager.nextSlideshow = SlideShow.SlideShowID.WhiteOutro;
            OutroLancerFaceBasis = basis;
        }

        internal static SlugName OutroLancerFaceBasis { get; private set; } = null;

        private static void LancerRemoveOutroTree(On.Menu.SlideShow.orig_ctor orig, SlideShow self,
            ProcessManager manager, SlideShow.SlideShowID slideShowID)
        {
            orig(self, manager, slideShowID);
            if (!IsStoryLancer || slideShowID != SlideShow.SlideShowID.WhiteOutro) return;

            int i = 0;
            for (; i < self.playList.Count; ++i)
                if (self.playList[i].sceneID == MenuSceneID.Outro_4_Tree) break;
            self.playList.RemoveAt(i);
            var treeScene = self.preloadedScenes[i];
            treeScene.RemoveSprites();
            var sceneList = self.preloadedScenes.ToList();
            sceneList.Remove(treeScene);
            self.preloadedScenes = sceneList.ToArray();

            --i;
            self.playList[i].fadeInDoneAt = self.ConvertTime(0, 51, 20) - 1.1f;
            self.playList[i].fadeOutStartAt = self.ConvertTime(0, 55, 60) - 1.1f;
        }

        internal static WorldCoordinate? GetMiscWorldCoord(PlayerProgression.MiscProgressionData data, string key)
        {
            try
            {
                string text = GetMiscValue<string>(data, key);
                if (text == COORDNULL) return null;
                return WorldCoordinate.FromString(text);
            }
            catch { SetMiscValue(data, key, COORDNULL); return null; }
        }

        internal static void SetMiscWorldCoord(PlayerProgression.MiscProgressionData data, string key, WorldCoordinate? coord)
            => SetMiscValue(data, key, coord.HasValue ? coord.Value.SaveToString() : COORDNULL);
    }
}