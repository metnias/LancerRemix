using LancerRemix.Cat;
using RWCustom;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal static class TutorialModify
    {
        internal static void Patch()
        {
            On.OverseerTutorialBehavior.PickupObjectInputInstructionController.Update += LancerPickupInstruction;
            On.RoomSpecificScript.SU_A23FirstCycleMessage.Update += LancerSU_A23;

            if (ModManager.MMF) OnMMFEnablePatch();
        }

        internal static void OnMMFEnablePatch()
        {
            On.MoreSlugcats.TipScreen.GetCharacterTipMeta += GetLancerTipMeta;
        }

        internal static void OnMMFDisablePatch()
        {
            On.MoreSlugcats.TipScreen.GetCharacterTipMeta -= GetLancerTipMeta;
        }

        private static string GetLancerTipMeta(On.MoreSlugcats.TipScreen.orig_GetCharacterTipMeta orig, SlugName slugcat)
        {
            if (!IsStoryLancer) return orig(slugcat);
            return "";
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static bool extraTuto = false;

        private static string Translate(string text) => Custom.rainWorld.inGameTranslator.Translate(text);

        private static void LancerPickupInstruction(On.OverseerTutorialBehavior.PickupObjectInputInstructionController.orig_Update orig, OverseerTutorialBehavior.PickupObjectInputInstructionController self)
        {
            if (IsStoryLancer && self.overseer.AI.communication != null && self.overseer.AI.communication.inputInstruction != null
                && !self.overseer.AI.communication.inputInstruction.slatedForDeletetion && self.overseer.AI.communication.inputInstruction is PickupObjectInstruction)
            {
                if (self.room.abstractRoom.name == "SU_A23")
                {
                    if (!self.textShown)
                    {
                        self.room.game.cameras[0].hud.textPrompt.AddMessage(Translate("You are too young and weak to throw a metal rebar"), 0, 300, true, true);
                        self.room.game.cameras[0].hud.textPrompt.AddMessage(Translate("But stabbing does not stun foes"), 20, 240, true, true);
                        self.room.game.cameras[0].hud.textPrompt.AddMessage(Translate("Have to find an opening"), 20, 240, true, true);
                        self.textShown = true;
                        extraTuto = false;
                    }
                }
                if (!extraTuto && self.room.abstractRoom.name == "SU_A25")
                {
                    self.room.game.cameras[0].hud.textPrompt.AddMessage(Translate("Press GRAB while holding a spear to block beforehand"), 0, 300, true, true);
                    extraTuto = true;
                }
            }
            orig(self);
        }

        private static void LancerSU_A23(On.RoomSpecificScript.SU_A23FirstCycleMessage.orig_Update orig, RoomSpecificScript.SU_A23FirstCycleMessage self, bool eu)
        {
            if (IsStoryLancer && self.room.game.session is StoryGameSession && !(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.GoExploreMessage
                && self.room.game.Players.Count > 0 && self.room.game.Players[0].realizedCreature != null
                && self.room.game.Players[0].realizedCreature.room == self.room)
            {
                (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.GoExploreMessage = true;
                var basis = GetBasis(self.room.game.StoryCharacter);
                if (basis == SlugName.Yellow)
                    self.room.game.cameras[0].hud.textPrompt.AddMessage(Translate("May you overcome this world of violence"), 20, 160, true, true);

                if (self.room.game.cameras[0].hud.textPrompt.subregionTracker != null)
                    self.room.game.cameras[0].hud.textPrompt.subregionTracker.lastShownRegion = 1;

                self.Destroy();
                return;
            }
            orig(self, eu);
        }

    }
}