using UnityEngine;

namespace LancerRemix.Cat
{
    public class LunterDecoration : LancerDecoration
    {
        public LunterDecoration()
        {
        }

        public LunterDecoration(Player player) : base(player)
        {
        }

        protected bool hornOverlay = true;

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(orig, sLeaser, rCam, timeStacker, camPos);
            if (hornOverlay == ModifyCat.GetSub<LunterSupplement>(self)?.maskOnHorn.HasAMask)
            {
                hornOverlay = !hornOverlay;
                SwitchHornOverlap(rCam, hornOverlay);
            }
        }

        protected void SwitchHornOverlap(RoomCamera rCam, bool newOverlap)
        {
            container.RemoveFromContainer();
            FContainer newContainer = rCam.ReturnFContainer(newOverlap ? "Midground" : "Foreground");
            newContainer.AddChild(container);
        }
    }
}