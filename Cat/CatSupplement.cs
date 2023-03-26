using HUD;
using Noise;
using UnityEngine;

namespace LancerRemix.Cat
{
    public abstract class CatSupplement
    {
        public CatSupplement(AbstractCreature owner)
        {
            this.owner = owner;
        }

        public readonly AbstractCreature owner;
        public Player player => owner.realizedCreature as Player;
        public static FoodMeter meter;
        protected internal ChunkSoundEmitter soundLoop;

        public virtual void Update()
        {
            if (player.room == null) return;
        }

        public virtual void Destroy()
        {
        }
    }
}