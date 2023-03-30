using CatSub.Cat;
using System;
using UnityEngine;

namespace LancerRemix.Cat
{
    internal class LancerSupplement : CatSupplement, IAmLancer
    {
        /// <summary>
        /// Slide throw backward: backflip upwards
        /// forward: hard hit & stun, but loses spear
        /// pulling spear from wall/creatures takes time
        /// 
        /// parry: grab / throw. throw parry will fling your spear. (12 ticks for now)
        /// grab parry will flip lizards
        /// 
        /// normal stab: will never stun
        /// </summary>
        public LancerSupplement(Player player) : base(player)
        {
        }

        public LancerSupplement() : base()
        {
        }

        private int parry = 0; // throw button: makes you lose spear
        private int block = 0; // grab button
        private int OnParry => Math.Max(parry, block);

        public override string TargetSubVersion => "1.0";


        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(null, eu);
            if (parry > 0) --parry;
            if (block > 0) --block;
        }

        public override void Destroy(On.Player.orig_Destroy orig)
        {
            base.Destroy(null);
        }

        public virtual void Grabbed(On.Player.orig_Grabbed orig, Creature.Grasp grasp)
        {
            if (OnParry < 1) goto NoParry;
            if (!(grasp.grabber is Lizard) && !(grasp.grabber is Vulture) && !(grasp.grabber is BigSpider) && !(grasp.grabber is DropBug)) goto NoParry;
            // Parry!

            grasp.grabber.Stun(Mathf.CeilToInt(Mathf.Lerp(80, 40, grasp.grabber.TotalMass / 10f)));

            // effect
            self.room.PlaySound(SoundID.Spear_Damage_Creature_But_Fall_Out, grasp.grabber.mainBodyChunk, false, 1.5f, 0.8f);
            parry = 0; block = 0;
            return;
        NoParry: orig(self, grasp);
        }
    }

    internal interface IAmLancer
    {
    }
}