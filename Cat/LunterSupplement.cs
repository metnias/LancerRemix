using LancerRemix.Combat;

namespace LancerRemix.Cat
{
    public class LunterSupplement : LancerSupplement
    {
        public LunterSupplement()
        {
        }

        public LunterSupplement(Player player) : base(player)
        {
            maskOnHorn = new MaskOnHorn(this);
        }

        public readonly MaskOnHorn maskOnHorn = null;

        public virtual void ObjectEaten(On.Player.orig_ObjectEaten orig, IPlayerEdible edible)
        {
            orig(self, edible);
            maskOnHorn.LockInteraction();
        }

        public override void Stun(On.Player.orig_Stun orig, int st)
        {
            base.Stun(orig, st);
            if (maskOnHorn.HasAMask && st > UnityEngine.Random.Range(40, 80))
                maskOnHorn.DropMask();
        }

        public override void Die(On.Player.orig_Die orig)
        {
            base.Die(orig);
            maskOnHorn.DropMask();
        }

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            base.Update(orig, eu);
            maskOnHorn.Update(eu);
        }
    }
}