using CatSub.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;

namespace LancerRemix.Cat
{
    internal class LancerSupplement : CatSupplement, IAmLancer
    {
        public LancerSupplement(Player player) : base(player)
        {
            lancer = player.SlugCatClass;
            player.slugcatStats.name = GetBasis(lancer);
            
        }

        internal readonly SlugName lancer;
        private readonly CatSupplement basisSub = null;

        public LancerSupplement() : base() { }

        

        protected override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(orig, eu);
        }
    }

    internal interface IAmLancer
    {

    }
}