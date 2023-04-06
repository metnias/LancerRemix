using LancerRemix.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;

namespace LancerRemix.Story
{
    internal static class TutorialModify
    {
        internal static void Patch()
        {
        }

        /*
        public static Dictionary<string, int> swapDict = null;

        public static void GenDictionary()
        {
            swapDict = new Dictionary<string, int>
            {
                { "You are hungry, find food", 1 },
                { "Three is enough to hibernate", 2 },
                { "Additional food (above three) is kept for later", 3 },
                { "You are full", 4 }
            };
        }

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        private static void LunterLurvivorTutorial(On.OverseerTutorialBehavior.orig_TutorialText orig, OverseerTutorialBehavior self, string text, int wait, int time, bool hideHud)
        {
            if (!IsStoryLancer) goto NoLancer;
            if (swapDict == null) GenDictionary();
            if (swapDict.TryGetValue(text, out int trigger))
            {
                switch (trigger)
                {
                    case 1:
                        orig.Invoke(self, self.overseer.room.game.rainWorld.inGameTranslator.Translate("You are too young and weak to throw a metal rebar"), 10, 160, true);
                        orig.Invoke(self, self.overseer.room.game.rainWorld.inGameTranslator.Translate("Have to find an opening"), 10, 200, true);
                        return;

                    case 2:
                        if (GetBasis(self.player.slugcatStats.name) == SlugName.Yellow)
                        {
                            orig.Invoke(self, self.overseer.room.game.rainWorld.inGameTranslator.Translate("You have to keep up with"), 10, 120, true);
                            return;
                        }
                        break;

                    case 3:
                        if (GetBasis(self.player.slugcatStats.name) == SlugName.Yellow)
                        {
                            orig.Invoke(self, self.overseer.room.game.rainWorld.inGameTranslator.Translate("You will now last a bit longer"), 10, 120, true);
                            return;
                        }
                        break;

                    case 4:
                        if (GetBasis(self.player.slugcatStats.name) == SlugName.Yellow) return;
                        break;
                }
            }
            NoLancer:
            orig.Invoke(self, text, wait, time, hideHud);
        }

        private static void PickupUpdatePatch(On.OverseerTutorialBehavior.PickupObjectInputInstructionController.orig_Update orig, OverseerTutorialBehavior.PickupObjectInputInstructionController instance)
        {
            if (LancerMod.IsMelee && instance.overseer.AI.communication != null && instance.overseer.AI.communication.inputInstruction != null
                && !instance.overseer.AI.communication.inputInstruction.slatedForDeletetion && instance.overseer.AI.communication.inputInstruction is PickupObjectInstruction)
            {
                if (instance.room.abstractRoom.name == "SU_A23")
                {
                    if (!instance.textShown)
                    {
                        instance.room.game.cameras[0].hud.textPrompt.AddMessage(LancerMod.Translate("You can sharpen your spear by double tap pick up button while holding rock and spear"), 0, 300, true, true);
                        instance.room.game.cameras[0].hud.textPrompt.AddMessage(LancerMod.Translate("Sharpened spear will last longer before it gets stuck in creatures"), 20, 240, true, true);
                        instance.room.game.cameras[0].hud.textPrompt.AddMessage(LancerMod.Translate("Then, hold up and pick up to pull out the spear from them"), 20, 240, true, true);
                        instance.textShown = true;
                        extraTuto = false;
                    }
                }
                if (!extraTuto && instance.room.abstractRoom.name == "SU_A25")
                {
                    instance.room.game.cameras[0].hud.textPrompt.AddMessage(LancerMod.Translate("You can only stay in close combat for so long"), 0, 300, true, true);
                    instance.room.game.cameras[0].hud.textPrompt.AddMessage(LancerMod.Translate("Once enervated, retreat and recompose"), 20, 240, true, true);
                    extraTuto = true;
                }
            }
            orig.Invoke(instance);
        }

        private static void SU_A23Patch(On.RoomSpecificScript.SU_A23FirstCycleMessage.orig_Update orig, RoomSpecificScript.SU_A23FirstCycleMessage self, bool eu)
        {
            if (IsStoryLancer && self.room.game.session is StoryGameSession && !(self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.GoExploreMessage
                && self.room.game.Players.Count > 0 && self.room.game.Players[0].realizedCreature != null
                && self.room.game.Players[0].realizedCreature.room == self.room)
            {
                (self.room.game.session as StoryGameSession).saveState.deathPersistentSaveData.GoExploreMessage = true;
                var basis = GetBasis( self.room.game.StoryCharacter);
                if (basis == SlugName.White)
                {
                    self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("New adventure begins"), 20, 160, true, true);
                }
                else if (basis == SlugName.Yellow)
                {
                    self.room.game.cameras[0].hud.textPrompt.AddMessage(self.room.game.rainWorld.inGameTranslator.Translate("May you overcome this world of violence"), 20, 160, true, true);
                }
                if (self.room.game.cameras[0].hud.textPrompt.subregionTracker != null)
                {
                    self.room.game.cameras[0].hud.textPrompt.subregionTracker.lastShownRegion = 1;
                }
                self.Destroy();
                return;
            }
            orig.Invoke(self, eu);
        }
        */
    }
}