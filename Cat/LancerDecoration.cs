using CatSub.Cat;
using UnityEngine;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;

namespace LancerRemix.Cat
{
    internal class LancerDecoration : CatDecoration
    {
        public LancerDecoration(Player player) : base(player)
        {
        }

        public LancerDecoration() : base()
        {
        }

        public override void InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(orig, sLeaser, rCam);
        }

        public override void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(orig, sLeaser, rCam, newContatiner);
        }

        public override void Update(On.PlayerGraphics.orig_Update orig)
        {
            base.Update(orig);
        }

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(orig, sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(orig, sLeaser, rCam, palette);
        }

        public override void SuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, Vector2 shortCutPosition)
        {
            base.SuckedIntoShortCut(orig, shortCutPosition);
        }

        public override void Reset(On.PlayerGraphics.orig_Reset orig)
        {
            base.Reset(orig);
        }
    }
}