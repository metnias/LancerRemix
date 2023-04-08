using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LancerRemix.Cat
{
    internal class LonkSupplement : LancerSupplement
    {
        public LonkSupplement()
        {
        }

        public LonkSupplement(Player player) : base(player)
        {
        }

        protected override void SetLanceCooltime()
        {
            base.SetLanceCooltime();
        }
    }
}