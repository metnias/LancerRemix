using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LancerRemix.Cat
{
    internal class NonLancerSupplement : CatSupplement
    {
        public NonLancerSupplement(AbstractCreature owner) : base(owner)
        {
            isLancer = false;
        }

        public override void Update()
        {
            return;
        }

        public override void Destroy()
        {
            return;
        }
    }
}