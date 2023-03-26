using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using UnityEngine;
using RWCustom;
using Menu;

namespace LancerRemix
{
    public static class TutorialPatch
    {
        public static void SubPatch()
        {
            On.Menu.ControlMap.ctor += new On.Menu.ControlMap.hook_ctor(ControlMapPatch);
        }

        public static void ControlMapPatch(On.Menu.ControlMap.orig_ctor orig, ControlMap map, Menu.Menu menu, MenuObject owner, Vector2 pos, Options.ControlSetup.Preset preset, bool showPickupInstructions)
        {
            orig.Invoke(map, menu, owner, pos, preset, showPickupInstructions);
            if (!(Custom.rainWorld.processManager.currentMainLoop is RainWorldGame rwg)) return;
            //if (rwg.StoryCharacter != PlanterEnums.SlugPlanter) return;
            if (showPickupInstructions)
            {
                /*
                string text = string.Empty;
                text = menu.Translate("Planter interactions:") + Environment.NewLine + Environment.NewLine;
                text += "- " + menu.Translate("Sporecat's diet is exclusively insectivore, regardless of the prey's size") + Environment.NewLine;
                text += "- " + menu.Translate("Hold UP and press PICK UP to grab a Puffball from the tail") + Environment.NewLine;
                text += "- " + menu.Translate("Hold DOWN and PICK UP for charged explosion") + Environment.NewLine;
                text += "- " + menu.Translate("However, using too many Puffballs costs hunger");
                Vector2 position = map.pickupButtonInstructions.pos;
                map.RemoveSubObject(map.pickupButtonInstructions);
                map.pickupButtonInstructions = new MenuLabel(menu, map, text, position, new Vector2(100f, 20f), false);
                map.pickupButtonInstructions.label.alignment = FLabelAlignment.Left;
                map.subObjects.Add(map.pickupButtonInstructions);
                */
            }
        }
    }
}