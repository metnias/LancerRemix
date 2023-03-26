using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using UnityEngine;
using static LancerRemix.LancerEnums;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Cat
{
    public static class ModifyCat
    {
        public static void SubPatch()
        {
            On.SlugcatStats.getSlugcatTimelineOrder += AppendTimelineOrder;
            On.RainWorldGame.ctor += StartGamePatch;
            On.Player.ctor += CtorPatch;
            On.Player.Update += UpdatePatch;
            On.Player.Destroy += DestroyPatch;
            On.PlayerGraphics.InitiateSprites += InitSprPatch;
            On.PlayerGraphics.Reset += ResetPatch;
            On.PlayerGraphics.SuckedIntoShortCut += SuckedIntoShortCutPatch;
            On.PlayerGraphics.DrawSprites += DrawSprPatch;
            On.PlayerGraphics.AddToContainer += AddToCtnrPatch;
            On.PlayerGraphics.ApplyPalette += PalettePatch;
            On.PlayerGraphics.Update += GrafUpdatePatch;

            subs = new CatSupplement[4]; ghostSubs = new ConditionalWeakTable<AbstractCreature, CatSupplement>();
            decos = new CatDecoration[4]; ghostDecos = new ConditionalWeakTable<AbstractCreature, CatDecoration>();

            if (ModManager.MSC) OnMSCEnablePatch();
        }

        internal static void OnMSCEnablePatch()
        {
        }

        internal static void OnMSCDisablePatch()
        {
        }

        private static SlugName[] AppendTimelineOrder(On.SlugcatStats.orig_getSlugcatTimelineOrder orig)
        {
            LinkedList<SlugName> list = new LinkedList<SlugName>(orig());
            var node = list.First;
            while (node.Next != null)
            {
                if ((ModManager.MSC && SlugcatStats.IsSlugcatFromMSC(node.Value)) || !HasLancer(node.Value))
                { node = node.Next; continue; }
                list.AddAfter(node, GetLancer(node.Value));
                node = node.Next;
            }

            return list.ToArray();
        }

        public static bool IsLancer(Player self) => IsLancer(self.SlugCatClass);

        public static bool IsLancer(SlugName name) => LancerEnums.IsLancer(name);

        private static CatSupplement[] subs;
        private static ConditionalWeakTable<AbstractCreature, CatSupplement> ghostSubs;

        public static void ClearSubsAndDecos()
        {
            for (int i = 0; i < subs.Length; i++) subs[i] = null;
            // ghostSubs.Clear();
            for (int i = 0; i < decos.Length; i++) decos[i] = null;
            // ghostDecos.Clear();
        }

        #region Player

        public static CatSupplement GetSub(AbstractCreature self)
        {
            if (!(self.state is PlayerState pState)) return null;
            if (!pState.isGhost) { return subs[pState.playerNumber]; }
            if (ghostSubs.TryGetValue(self, out var sub)) return sub;
            return null;
        }

        private static void StartGamePatch(On.RainWorldGame.orig_ctor orig, RainWorldGame self, ProcessManager manager)
        {
            ClearSubsAndDecos(); // clear
            orig(self, manager);
            /*
            if (!self.IsStorySession) return;
            for (int i = 0; i < self.Players.Count; i++)
                if (self.world.GetAbstractRoom(self.Players[i].pos) != null)
                    if (self.world.GetAbstractRoom(self.Players[i].pos).shelter) continue;
                    else if (self.world.GetAbstractRoom(self.Players[i].pos).name == "LF_A11") // Sporecat
                        self.Players[i].pos.Tile = new IntVector2(11, 30);
            */
        }

        private static void CtorPatch(On.Player.orig_ctor orig, Player self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (IsLancer(self))
            {
                if (!self.playerState.isGhost)
                {
                    if (GetSub(self.abstractCreature) != null) return;
                    subs[self.playerState.playerNumber] = new LancerSupplement(self.abstractCreature);
                    decos[self.playerState.playerNumber] = new LancerDecoration(self.abstractCreature);
                }
                else
                {
                    if (GetSub(self.abstractCreature) != null) return;
                    ghostSubs.Add(self.abstractCreature, new LancerSupplement(self.abstractCreature));
                    ghostDecos.Add(self.abstractCreature, new LancerDecoration(self.abstractCreature));
                }
            }
        }

        private static void UpdatePatch(On.Player.orig_Update orig, Player self, bool eu)
        {
            orig(self, eu);
            if (IsLancer(self)) GetSub(self.abstractCreature).Update();
        }

        private static void DestroyPatch(On.Player.orig_Destroy orig, Player self)
        {
            orig(self);
            if (IsLancer(self)) GetSub(self.abstractCreature).Destroy();
        }

        #endregion Player

        #region Graphics

        private static CatDecoration[] decos;
        private static ConditionalWeakTable<AbstractCreature, CatDecoration> ghostDecos;

        public static CatDecoration GetDeco(AbstractCreature self)
        {
            if (!(self.state is PlayerState pState)) return null;
            if (!pState.isGhost) { return decos[pState.playerNumber]; }
            if (ghostDecos.TryGetValue(self, out var deco)) return deco;
            return null;
        }

        private static void GrafUpdatePatch(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
        {
            orig(self);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).Update();
        }

        private static void InitSprPatch(On.PlayerGraphics.orig_InitiateSprites orig, PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).InitiateSprites(sLeaser, rCam);
        }

        private static void SuckedIntoShortCutPatch(On.PlayerGraphics.orig_SuckedIntoShortCut orig,
            PlayerGraphics self, Vector2 shortCutPosition)
        {
            orig(self, shortCutPosition);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).SuckedIntoShortCut();
        }

        private static void ResetPatch(On.PlayerGraphics.orig_Reset orig, PlayerGraphics self)
        {
            orig.Invoke(self);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).Reset();
        }

        private static void DrawSprPatch(On.PlayerGraphics.orig_DrawSprites orig, PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).DrawSprites(sLeaser, rCam, timeStacker, camPos);
        }

        private static void AddToCtnrPatch(On.PlayerGraphics.orig_AddToContainer orig, PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).AddToContainer(sLeaser, rCam, newContatiner);
        }

        private static void PalettePatch(On.PlayerGraphics.orig_ApplyPalette orig, PlayerGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (IsLancer(self.player)) GetDeco(self.player.abstractCreature).ApplyPalette(sLeaser, rCam, palette);
        }

        #endregion Graphics
    }
}