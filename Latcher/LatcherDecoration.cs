#if LATCHER

using LancerRemix.Cat;
using RWCustom;
using UnityEngine;

namespace LancerRemix.Latcher
{
    public class LatcherDecoration : LancerDecoration
    {
        public LatcherDecoration(Player player) : base(player)
        {
        }

        protected override void InitiateHorn(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            sLeaser.sprites[0].shader = Custom.rainWorld.Shaders["PlayerCamoMaskBeforePlayer"];
            /*
            sprites[1] = new FSprite("Futile_White", true);
            sprites[1].scale = 7f;
            sprites[1].shader = Custom.rainWorld.Shaders["PlayerCamoMask"];
            */
            for (int i = 1; i < 10; i++)
                sLeaser.sprites[i].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];
            sLeaser.sprites[11].shader = Custom.rainWorld.Shaders["RippleBasicBothSides"];

            base.InitiateHorn(sLeaser, rCam);
        }

        public override void AddToContainer(On.PlayerGraphics.orig_AddToContainer orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            base.AddToContainer(orig, sLeaser, rCam, newContatiner);

            if (newContatiner == null)
                newContatiner = rCam.ReturnFContainer("Midground");
            for (int i = 0; i < sLeaser.sprites.Length; i++)
            {
                if ((i > 6 && i < 9) || i > 9)
                    rCam.ReturnFContainer("Foreground").AddChild(sLeaser.sprites[i]);
                else
                    newContatiner.AddChild(sLeaser.sprites[i]);
            }
            //(player.inVoidSea ? rCam.ReturnFContainer("Foreground") : newContatiner).AddChild(sprites[1]);
        }

        public override void Update(On.PlayerGraphics.orig_Update orig)
        {
            self.RippleTrailUpdate();
            base.Update(orig);
        }

        public override void DrawSprites(On.PlayerGraphics.orig_DrawSprites orig, RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (player != null)
            {
                if (player.inVoidSea != self.lastInVoidSea)
                    self.AddToContainer(sLeaser, rCam, null);
                self.lastInVoidSea = player.inVoidSea;
            }

            base.DrawSprites(orig, sLeaser, rCam, timeStacker, camPos);

            if (player != null)
            {
                // Set EyeColor
                var eyeColor = Color.Lerp(new Color(1f, 1f, 1f), rCam.currentPalette.blackColor, 0.3f);
                if (self.useJollyColor)
                    eyeColor = PlayerGraphics.JollyColor(player.playerState.playerNumber, 1);
                if (PlayerGraphics.CustomColorsEnabled())
                    eyeColor = PlayerGraphics.CustomColorSafety(1);
                if (self.malnourished > 0f)
                {
                    float malnourished = player.Malnourished ? self.malnourished : Mathf.Max(0f, self.malnourished - 0.005f);
                    eyeColor = Color.Lerp(eyeColor, Color.Lerp(Color.white, rCam.currentPalette.fogColor, 0.5f), 0.2f * malnourished * malnourished);
                }
                eyeColor = Color.Lerp(eyeColor, Color.white, player.camoProgress);
                sLeaser.sprites[9].color = eyeColor;

                /*
                if (sprites[1].container != sLeaser.sprites[9].container)
                    sLeaser.sprites[9].container.AddChild(sLeaser.sprites[1]);
                if (rCam.warpPointTimer == null)
                    sprites[1].MoveBehindOtherNode(sLeaser.sprites[9]);
                else
                    sprites[1].MoveInFrontOfOtherNode(sLeaser.sprites[9]);
                var mainBodyChunk = player.mainBodyChunk;
                sprites[1].x = Mathf.Lerp(mainBodyChunk.lastPos.x, mainBodyChunk.pos.x, timeStacker) - camPos.x;
                sprites[1].y = Mathf.Lerp(mainBodyChunk.lastPos.y, mainBodyChunk.pos.y, timeStacker) - camPos.y;
                sprites[1].color = new Color(player.camoProgress, 0f, 0f);
                */
                sLeaser.sprites[12].color = new Color(0f, 0f, 0f);
                self.rippleTrail?.DrawUpdate(timeStacker, rCam, camPos);

                /*
                for (int i = 0; i < self.mudSpriteCount; i++)
                {
                    FSprite fsprite = sLeaser.sprites[self.firstMudSprite + i];
                    if (fsprite.container != sprites[1].container)
                    {
                        fsprite.RemoveFromContainer();
                        sprites[1].container.AddChildAtIndex(fsprite, sprites[1].container.GetChildIndex(sprites[1]) + 1);
                    }
                    else if (sprites[1].container.GetChildIndex(fsprite) != sprites[1].container.GetChildIndex(sprites[1]) + 1)
                        fsprite.MoveInFrontOfOtherNode(sprites[1]);
                }
                */
            }
        }
    }
}

#endif