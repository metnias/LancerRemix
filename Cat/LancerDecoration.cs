using CatSub.Cat;
using UnityEngine;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;
using RWCustom;
using System.Collections.Generic;

namespace LancerRemix.Cat
{
    internal class LancerDecoration : CatDecoration, IAmLancer
    {
        public LancerDecoration(Player player) : base(player)
        {
        }

        public LancerDecoration() : base()
        {
        }

        public override void InitiateSprites(On.PlayerGraphics.orig_InitiateSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            base.InitiateSprites(null, sLeaser, rCam);
        }

        public override void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(null, sLeaser, rCam, newContatiner);
        }

        public override void Update(On.PlayerGraphics.orig_Update orig)
        {
            base.Update(null);
        }

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            base.DrawSprites(null, sLeaser, rCam, timeStacker, camPos);
        }

        public override void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(null, sLeaser, rCam, palette);
        }

        public override void SuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, Vector2 shortCutPosition)
        {
            base.SuckedIntoShortCut(null, shortCutPosition);
        }

        public override void Reset(On.PlayerGraphics.orig_Reset orig)
        {
            base.Reset(null);
        }


        public static Color DefaultHornColor(SlugName basis)
        {
            if (defaultHornColors.TryGetValue(basis, out var res)) return res;

            var c = PlayerGraphics.DefaultSlugcatColor(basis);
            var hsl = Custom.RGB2HSL(c);
            hsl.x = (hsl.x + 0.5f) % 1f;
            hsl.z = Mathf.Lerp(hsl.z, 0.2f, 0.3f);
            defaultHornColors.Add(basis, Custom.HSL2RGB(hsl.x, hsl.y, hsl.z));

            return defaultHornColors[basis];
        }

        private static readonly Dictionary<SlugName, Color> defaultHornColors
            = new Dictionary<SlugName, Color>()
            {
                { SlugName.White, new Color(0.1f, 0.3f, 0.0f) },
                {SlugName.Yellow, new Color(0.5f, 0.1f, 0.0f) },
                {SlugName.Red, new Color(0.0f, 0.1f, 0.5f) },
                {SlugName.Night, new Color(0.1f, 0.5f, 0.3f) }
            };

    }
}