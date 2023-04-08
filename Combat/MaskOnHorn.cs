using LancerRemix.Cat;
using RWCustom;
using UnityEngine;
using BodyModeIndex = Player.BodyModeIndex;
using AnimationIndex = Player.AnimationIndex;

namespace LancerRemix.Combat
{
    internal class MaskOnHorn
    {
        internal MaskOnHorn(LancerSupplement owner)
        {
            this.owner = owner;
        }

        internal readonly LancerSupplement owner;
        public Player player => owner.self;
        public VultureMask Mask { get; private set; }
        private bool increment;
        private bool interactionLocked;
        internal AbstractOnHornStick abstractStick;
        private int counter;

        internal Color color;
        internal Color blackColor;
        internal HSLColor ColorA;
        internal HSLColor ColorB;

        public void LockInteraction()
        {
            increment = false;
            interactionLocked = true;
        }

        public bool CanPutMaskOnHorn()
        {
            return !HasAMask && (player.grasps[0]?.grabbed is VultureMask || player.grasps[1]?.grabbed is VultureMask);
        }

        public bool CanRetrieveMaskFromHorn()
        {
            int grasp = -1;
            for (int i = 0; i < 2; i++)
            {
                if (player.grasps[i] == null) { grasp = i; continue; }
                if (player.grasps[i]?.grabbed is IPlayerEdible || player.grasps[i].grabbed is Spear) { return false; }
                if ((int)player.Grabability(player.grasps[i].grabbed) >= 3) { return false; }
            }
            if (player.spearOnBack != null && player.spearOnBack.HasASpear) { return false; }
            return HasAMask && grasp > -1;
        }

        public bool HasAMask => Mask != null;

        public void Update(bool eu)
        {
            increment = player.input[0].pckp && !interactionLocked
                && (CanPutMaskOnHorn() || CanRetrieveMaskFromHorn());
            if (player.input[0].pckp && player.grasps[0] != null && player.grasps[0].grabbed is Creature
                && player.CanEatMeat(player.grasps[0].grabbed as Creature) && (player.grasps[0].grabbed as Creature).Template.meatPoints > 0)
                LockInteraction();
            else if (player.swallowAndRegurgitateCounter > 90)
                LockInteraction();

            if (!interactionLocked && increment)
            {
                ++counter;
                if (Mask != null && counter > 20)
                {
                    MaskToHand(eu);
                    counter = 0;
                }
                else if (Mask == null && counter > 20)
                {
                    for (int i = 0; i < player.grasps.Length; ++i)
                    {
                        if (player.grasps[i] != null && player.grasps[i].grabbed is VultureMask)
                        {
                            player.bodyChunks[0].pos += Custom.DirVec(player.grasps[i].grabbed.firstChunk.pos, player.bodyChunks[0].pos) * 2f;
                            MaskToHorn(player.grasps[i].grabbed as VultureMask);
                            counter = 0;
                            break;
                        }
                    }
                }
            }
            else counter = 0;
            if (!player.input[0].pckp)
                interactionLocked = false;
            increment = false;
        }

        public void GraphicsModuleUpdated(bool actuallyViewed, bool eu)
        {
            if (Mask == null) return;
            if (Mask.slatedForDeletetion)
            {
                abstractStick?.Deactivate();
                Mask = null;
                return;
            }
            Vector2 pos0 = player.mainBodyChunk.pos;
            //Vector2 pos1 = owner.bodyChunks[1].pos;
            Vector2 vel0 = player.mainBodyChunk.vel;
            if (player.graphicsModule != null && actuallyViewed)
            {
                pos0 = (player.graphicsModule as PlayerGraphics).head.pos;
                vel0 = (player.graphicsModule as PlayerGraphics).head.vel;
                //pos1 = (owner.graphicsModule as PlayerGraphics).drawPositions[1, 0];
            }

            float to = Custom.LerpMap(player.eatCounter, 35f, 15f, 0f, 10f);
            float to2 = 0f;
            Mask.CollideWithTerrain = false;
            Mask.CollideWithObjects = false;
            if (player.standing && (player.bodyMode != BodyModeIndex.ClimbingOnBeam || player.animation == AnimationIndex.StandOnBeam)
                && player.bodyMode != BodyModeIndex.Swimming)
                if (player.input[0].x != 0 && Mathf.Abs(player.bodyChunks[1].lastPos.x - player.bodyChunks[1].pos.x) > 2f)
                    to2 = (float)player.input[0].x;
            //if (dir.y < 0) { dir.y = -dir.y; }
            vel -= Mathf.Max(Mathf.Abs(rot), 5f) * Mathf.Sign(rot) * Mask.room.gravity;
            vel += vel0.x * 9f * (Mask.room.gravity * 0.5f + 0.5f);
            vel *= 0.95f;
            if (to2 != 0f && Random.value < 0.05f)
                rot += to2 * Random.value * 30f;
            rot += vel;
            if (rot < 1f) { rot = 0f; }
            if (vel < 1f) { vel = 0f; }
            Vector2 dir = Custom.RotateAroundOrigo(new Vector2(0f, 1f), rot);
            //dir *= Mathf.Sign(Custom.DistanceToLine(mask.firstChunk.pos, owner.bodyChunks[0].pos, owner.bodyChunks[1].pos));
            //dir = Custom.RotateAroundOrigo(dir, to2 * -30f);

            Mask.firstChunk.MoveFromOutsideMyUpdate(eu, pos0);
            Mask.firstChunk.vel = player.mainBodyChunk.vel;
            Mask.rotationA = Vector3.Slerp(Mask.rotationA, dir, 0.5f);
            Mask.rotationB = Vector2.up;
            Mask.donned = Custom.LerpAndTick(Mask.donned, to, 0.11f, 0.0333333351f);
            Mask.viewFromSide = Custom.LerpAndTick(Mask.viewFromSide, to2, 0.11f, 0.0333333351f);
            Mask.Forbid();
        }

        private float vel;
        private float rot;

        public void MaskToHand(bool eu)
        {
            if (Mask == null) return;
            for (int i = 0; i < player.grasps.Length; i++)
            {
                if (player.grasps[i] != null)
                {
                    if ((int)player.Grabability(this.player.grasps[i].grabbed) >= 3) { return; }
                }
            }
            int num = -1;
            int num2 = 0;
            while (num2 < 2 && num == -1)
            {
                if (player.grasps[num2] == null) num = num2;
                ++num2;
            }
            if (num == -1) return;
            if (player.graphicsModule != null)
                Mask.firstChunk.MoveFromOutsideMyUpdate(eu, (player.graphicsModule as PlayerGraphics).hands[num].pos);
            player.SlugcatGrab(Mask, num);
            Mask = null;
            interactionLocked = true;
            player.noPickUpOnRelease = 20;
            player.room.PlaySound(SoundID.Vulture_Mask_Pick_Up, player.mainBodyChunk);
            abstractStick?.Deactivate();
            abstractStick = null;
        }

        public void MaskToHorn(VultureMask mask)
        {
            if (Mask != null) return;
            for (int i = 0; i < player.grasps.Length; ++i)
            {
                if (player.grasps[i] != null && player.grasps[i].grabbed == mask)
                {
                    player.ReleaseGrasp(i);
                    //mask.grabbedBy[0] = new Creature.Grasp(owner, mask, i, 0, Creature.Grasp.Shareability.CanNotShare, 1f, true);
                    break;
                }
            }
            Mask = mask;
            color = mask.maskGfx.color;
            blackColor = mask.maskGfx.blackColor;
            ColorA = mask.maskGfx.ColorA;
            ColorB = mask.maskGfx.ColorB;

            //mask.ChangeMode(Weapon.Mode.OnBack);
            interactionLocked = true;
            player.noPickUpOnRelease = 20;
            player.room.PlaySound(SoundID.Slugcat_Stash_Spear_On_Back, player.mainBodyChunk);
            abstractStick?.Deactivate();
            abstractStick = new AbstractOnHornStick(player.abstractPhysicalObject, Mask.abstractPhysicalObject, this);
            rot = 0f;
            vel = 0f;
        }

        public void DropMask()
        {
            if (Mask == null) return;
            Mask.firstChunk.vel = player.mainBodyChunk.vel + Custom.RNV() * 3f * Random.value;
            Mask = null;
            abstractStick?.Deactivate();
            abstractStick = null;
        }

        internal class AbstractOnHornStick : AbstractPhysicalObject.AbstractObjectStick
        {
            public AbstractOnHornStick(AbstractPhysicalObject player, AbstractPhysicalObject mask, MaskOnHorn maskOnHorn) : base(player, mask)
            {
                this.maskOnHorn = maskOnHorn;
            }

            public readonly MaskOnHorn maskOnHorn;

            public AbstractPhysicalObject Player
            {
                get { return A; }
                set { A = value; }
            }

            public AbstractPhysicalObject Mask
            {
                get { return B; }
                set { B = value; }
            }

            public override string SaveToString(int roomIndex)
            {
                return string.Concat(new string[]
                {
                    roomIndex.ToString(),
                    "<stkA>gripStk<stkA>",
                    A.ID.ToString(),
                    "<stkA>",
                    B.ID.ToString(),
                    "<stkA>",
                    "2",
                    "<stkA>",
                    "1"
                });
            }
        }
    }
}