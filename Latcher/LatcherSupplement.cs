using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using Random = UnityEngine.Random;

namespace LancerRemix.Latcher
{
    public class LatcherSupplement : LancerSupplement
    {
        public LatcherSupplement(Player player) : base(player)
        {
            if (self.room.game.session is StoryGameSession sgs && self.rippleLevel > 0f)
                sgs.saveState.theGlow = true;
        }

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(orig, eu);

            if (self.sporeParticleTicks > 0)
            {
                if (self.sporeParticleTicks % 4 == 0)
                {
                    var bodyChunk = self.bodyChunks[Random.Range(0, self.bodyChunks.Length)];
                    Vector2 vel = bodyChunk.vel * 0.5f + Custom.RNV() * Random.Range(0f, 3f);
                    var sporeCloud = new SporeCloud(bodyChunk.pos, vel, new Color(0.02f, 0.1f, 0.08f), Random.Range(0.65f, 0.8f), null, 0, null, self.abstractPhysicalObject.rippleLayer)
                    {
                        pos = bodyChunk.pos + Random.insideUnitCircle * bodyChunk.rad,
                        nonToxic = true
                    };
                    self.room.AddObject(sporeCloud);
                }
            }
        }
    }
}