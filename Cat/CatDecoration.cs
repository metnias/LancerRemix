using RWCustom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static PlayerGraphics;

namespace LancerRemix.Cat
{
    public abstract class CatDecoration
    {
        public CatDecoration(AbstractCreature owner)
        {
            this.owner = owner;
        }

        public readonly AbstractCreature owner;
        public Player player => owner.realizedCreature as Player;
        protected internal PlayerGraphics OwnerGraphic => player.graphicsModule as PlayerGraphics;
        // private SporeCatSupplement OwnerSub => ModifyCat.GetSub(owner.player);

        protected internal FSprite[] sprites;
        protected internal FContainer container;

        public virtual void Update()
        {
            if (player == null || player.room == null || OwnerGraphic == null) return;
        }

#pragma warning disable IDE0060
        //private float[] dbg = new float[4];

        public virtual void InitiateSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            if (this.container != null) { this.container.RemoveAllChildren(); this.container.RemoveFromContainer(); }
            this.container = new FContainer();
            //this.AddToContainer(sLeaser, rCam, null);
        }

        public virtual void SuckedIntoShortCut()
        {
            this.container.RemoveFromContainer();
        }

        public virtual void Reset()
        {
        }

        internal Vector2 GetPos(int idx, float timeStacker) => idx < 1 ? Vector2.Lerp(OwnerGraphic.drawPositions[idx, 1], this.OwnerGraphic.drawPositions[idx, 0], timeStacker) :
                Vector2.Lerp(OwnerGraphic.tail[idx - 1].lastPos, OwnerGraphic.tail[idx - 1].pos, timeStacker);

        internal Vector2 GetPos(float idx, float timeStacker) => Vector2.Lerp(GetPos(Mathf.FloorToInt(idx), timeStacker), GetPos(Mathf.FloorToInt(idx) + 1, timeStacker), idx - Mathf.FloorToInt(idx));

        internal float GetRad(int idx) => idx < 1 ? player.bodyChunks[0].rad : OwnerGraphic.tail[idx - 1].StretchedRad;

        internal float GetRad(float idx) => Mathf.Lerp(GetRad(Mathf.FloorToInt(idx)), GetRad(Mathf.FloorToInt(idx) + 1), idx - Mathf.FloorToInt(idx));

        internal Vector2 GetDir(float idx, float timeStacker) =>
            Custom.DirVec(GetPos(Mathf.FloorToInt(idx), timeStacker), GetPos(Mathf.FloorToInt(idx) + 1, timeStacker));

        public virtual void DrawSprites(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            if (player == null || player.room == null || OwnerGraphic == null)
            { container.isVisible = false; return; }
            container.isVisible = true;
        }

        public Color GetBodyColor() => bodyColor;

        public Color GetFaceColor() => faceColor;

        public Color GetThirdColor() =>
            ModManager.CoopAvailable && OwnerGraphic.useJollyColor
            ? JollyColor(player.playerState.playerNumber, 2) :
            CustomColorsEnabled() ? CustomColorSafety(2) : thirdColor;

        private Color bodyColor = Color.white;
        private Color faceColor = new Color(0.01f, 0.01f, 0.01f);
        protected Color thirdColor = new Color(0.01f, 0.01f, 0.01f);

        public virtual void ApplyPalette(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            bodyColor = sLeaser.sprites[0].color;
            faceColor = sLeaser.sprites[9].color;
        }

        public virtual void AddToContainer(RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            if (this.container == null) { return; }
            if (newContatiner == null) { newContatiner = rCam.ReturnFContainer("Midground"); }
            this.container.RemoveFromContainer();
            newContatiner.AddChild(this.container);
        }
    }
}