using CatSub.Cat;
using RWCustom;
using UnityEngine;
using static DaddyGraphics;
using static LancerRemix.Cat.LancerDecoration;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal class LunterDummyDecoration : CatDecoration
    {
        public LunterDummyDecoration()
        {
        }

        public LunterDummyDecoration(HunterDummy dummy) : base(null)
        {
            this.dummy = dummy;
        }

        private readonly HunterDummy dummy;

        public override void InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(null, sLeaser, rCam);

            sprites = new FSprite[1];
            var tris = new TriangleMesh.Triangle[] { new TriangleMesh.Triangle(0, 1, 2) };
            var triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
            sprites[0] = triangleMesh;
            container.AddChild(sprites[0]);
            self.AddToContainer(sLeaser, rCam, null);
        }

        public override void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(null, sLeaser, rCam, newContatiner);
        }

        public override void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            sprites[0].color = DefaultHornColor(SlugName.Red);
        }

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (dummy?.owner?.daddy?.room == null)
            {
                container.isVisible = false;
                return;
            }
            else
            {
                container.isVisible = true;
            }

            Vector2 head = Vector2.Lerp(dummy.head.lastPos, dummy.head.pos, timeStacker);
            IntVector2 stat = HornStat(SlugName.Red);
            Vector2 draw1 = Vector2.Lerp(dummy.drawPositions[1, 1], dummy.drawPositions[1, 0], timeStacker);
            Vector2 tip = head + Custom.DirVec(draw1, head) * (float)stat.y;
            Vector2 thicc = Custom.PerpendicularVector(head, tip);
            Vector2 dir = (tip - head) * 0.6f;

            (sprites[0] as TriangleMesh).MoveVertice(0, dir + tip - camPos);
            (sprites[0] as TriangleMesh).MoveVertice(1, dir + head - thicc * (float)stat.x / 2f - camPos);
            (sprites[0] as TriangleMesh).MoveVertice(2, dir + head + thicc * (float)stat.x / 2f - camPos);
        }

    }
}
