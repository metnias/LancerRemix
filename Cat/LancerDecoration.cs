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
            lancerName = player.SlugCatClass;
            player.playerState.slugcatCharacter = GetBasis(lancerName);
            player.slugcatStats.name = GetBasis(lancerName);
            if (DecoRegistry.TryMakeDeco(player, out CatDecoration deco))
                basisDeco = deco;
        }

        internal readonly SlugName lancerName;
        private readonly CatDecoration basisDeco = null;

        public LancerDecoration() : base() { }


        public override void InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (basisDeco != null) basisDeco.InitiateSprites(orig, sLeaser, rCam);
            else base.InitiateSprites(orig, sLeaser, rCam);
        }

        public override void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (basisDeco != null) basisDeco.AddToContainer(orig, sLeaser, rCam, newContatiner);
            else base.AddToContainer(orig, sLeaser, rCam, newContatiner);
        }

        public override void Update(On.PlayerGraphics.orig_Update orig)
        {
            if (basisDeco != null) basisDeco.Update(orig);
            else base.Update(orig);
        }

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (basisDeco != null) basisDeco.DrawSprites(orig, sLeaser, rCam, timeStacker, camPos);
            else base.DrawSprites(orig, sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (basisDeco != null) basisDeco.ApplyPalette(orig, sLeaser, rCam, palette);
            else base.ApplyPalette(orig, sLeaser, rCam, palette);
        }

        public override void SuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, Vector2 shortCutPosition)
        {
            if (basisDeco != null) basisDeco.SuckedIntoShortCut(orig, shortCutPosition);
            else base.SuckedIntoShortCut(orig, shortCutPosition);
        }

        public override void Reset(On.PlayerGraphics.orig_Reset orig)
        {
            if (basisDeco != null) basisDeco.Reset(orig);
            else base.Reset(orig);
        }

    }
}