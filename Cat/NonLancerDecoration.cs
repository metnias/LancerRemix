using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace LancerRemix.Cat
{
    internal class NonLancerDecoration : CatDecoration
    {
        public NonLancerDecoration(AbstractCreature owner) : base(owner)
        {
            isLancer = false;
        }

        public override void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            return;
        }

        public override void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            return;
        }

        public override void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            return;
        }

        public override void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            return;
        }

        public override void Reset()
        {
            return;
        }
        public override void SuckedIntoShortCut()
        {
            return;
        }
    }
}