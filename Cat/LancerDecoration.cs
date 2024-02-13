using CatSub.Cat;
using LancerRemix.LancerMenu;
using RWCustom;
using System.Collections.Generic;
using UnityEngine;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    public class LancerDecoration : CatDecoration, IAmLancer
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
            InitiateHorn(sLeaser, rCam);
        }

        protected virtual void InitiateHorn(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sprites = new FSprite[1];
            var tris = new TriangleMesh.Triangle[] { new TriangleMesh.Triangle(0, 1, 2) };
            var triangleMesh = new TriangleMesh("Futile_White", tris, false, false);
            sprites[0] = triangleMesh;
            container.AddChild(sprites[0]);
            self.AddToContainer(sLeaser, rCam, null);
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
            DrawHorn(sLeaser, timeStacker, camPos);
        }

        protected virtual void DrawHorn(RoomCamera.SpriteLeaser sLeaser, float timeStacker, Vector2 camPos)
        {
            Vector2 head = Vector2.Lerp(self.head.lastPos, self.head.pos, timeStacker);
            IntVector2 stat = HornStat(player.SlugCatClass);
            Vector2 draw1 = Vector2.Lerp(self.drawPositions[1, 1], self.drawPositions[1, 0], timeStacker);
            Vector2 tip = head + Custom.DirVec(draw1, head) * (float)stat.y;
            Vector2 thicc = Custom.PerpendicularVector(head, tip);
            Vector2 dir = (tip - head) * 0.6f;

            for (int j = 0; j < 2; ++j)
            {
                if (sLeaser.sprites.Length < 5 + j || sLeaser.sprites[5 + j] == null) break;
                Vector2 hand = Vector2.Lerp(self.hands[j].lastPos, self.hands[j].pos, timeStacker);
                sLeaser.sprites[5 + j].x = hand.x + thicc.x * (j == 0 ? 3f : -3f) - camPos.x;
                sLeaser.sprites[5 + j].y = hand.y + thicc.y * (j == 0 ? 3f : -3f) - camPos.y;
            }

            (sprites[0] as TriangleMesh).MoveVertice(0, dir + tip - camPos);
            (sprites[0] as TriangleMesh).MoveVertice(1, dir + head - thicc * (float)stat.x / 2f - camPos);
            (sprites[0] as TriangleMesh).MoveVertice(2, dir + head + thicc * (float)stat.x / 2f - camPos);
        }

        public override void ApplyPalette(On.PlayerGraphics.orig_ApplyPalette orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            base.ApplyPalette(orig, sLeaser, rCam, palette);
            ApplyHornPalette();
        }

        protected virtual void ApplyHornPalette()
        {
            sprites[0].color = GetHornColor();
        }

        public override void SuckedIntoShortCut(On.PlayerGraphics.orig_SuckedIntoShortCut orig, Vector2 shortCutPosition)
        {
            base.SuckedIntoShortCut(orig, shortCutPosition);
        }

        public override void Reset(On.PlayerGraphics.orig_Reset orig)
        {
            base.Reset(orig);
        }

        public virtual Color GetHornColor()
        {
            if (self.useJollyColor || PlayerGraphics.CustomColorsEnabled())
                return
                    self.player?.playerState == null ? DefaultHornColor(SlugName.White) :
                    HornColorPick.GetHornColor(self.player.playerState.playerNumber);

            return self.player?.SlugCatClass == null ? DefaultHornColor(SlugName.White) :
                DefaultHornColor(self.player.SlugCatClass);
        }

        public static Color DefaultHornColor(SlugName basis)
        {
            basis = GetBasis(basis);
            if (defaultHornColors.TryGetValue(basis, out var res)) return res;

            var c = PlayerGraphics.DefaultSlugcatColor(basis);
            var hsl = Custom.RGB2HSL(c);
            hsl.x = (hsl.x + 0.5f) % 1f;
            hsl.z = Mathf.Lerp(hsl.z, 0.2f, 0.3f);
            defaultHornColors.Add(basis, Custom.HSL2RGB(hsl.x, hsl.y, hsl.z));

            return defaultHornColors[basis];
        }

        public static readonly Dictionary<SlugName, Color> defaultHornColors
            = new Dictionary<SlugName, Color>()
            {
                { SlugName.White, new Color(0.1f, 0.3f, 0.0f) },
                { SlugName.Yellow, new Color(0.5f, 0.1f, 0.0f) },
                { SlugName.Red, new Color(0.0f, 0.1f, 0.5f) },
                { SlugName.Night, new Color(0.1f, 0.5f, 0.3f) }
            };

        public static IntVector2 HornStat(SlugName basis)
        {
            basis = GetBasis(basis);
            if (vanillaHornStats.TryGetValue(basis, out var res)) return res;
            return new IntVector2(3, 8);
        }

        public static readonly Dictionary<SlugName, IntVector2> vanillaHornStats
            = new Dictionary<SlugName, IntVector2>()
            {
                { SlugName.White, new IntVector2(3, 8) },
                { SlugName.Night, new IntVector2(3, 8) },
                { SlugName.Yellow, new IntVector2(4, 6) },
                { SlugName.Red, new IntVector2(3, 10) }
            };
    }
}