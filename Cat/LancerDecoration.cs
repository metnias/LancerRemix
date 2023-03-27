using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LancerRemix.Cat
{
    internal class LancerDecoration : CatDecoration
    {
        public LancerDecoration(AbstractCreature owner) : base(owner)
        {
            isLancer = true;
        }
    }
}