using SlugName = SlugcatStats.Name;
using MSCSlugName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using static LancerRemix.LancerEnums;
using static CatSub.Story.SaveManager;
using LancerRemix.Cat;
using MoreSlugcats;
using UnityEngine;
using System.Collections.Generic;

namespace LancerRemix.Story
{
    internal static class HunterLancerScripts
    {
        internal static void SubPatch()
        {
            On.RegionState.AdaptWorldToRegionState += AdaptWorldToRegionState;
            On.StoryGameSession.PlaceKarmaFlowerOnDeathSpot += PlaceLancerKarmaFlower;
        }

        internal static void OnMSCEnableSubPatch()
        {
            On.DaddyLongLegs.ctor += LunterDaddyCtor;
            On.DaddyLongLegs.Update += HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption += HunterRecognizeLancer;
            On.DaddyLongLegs.Die += LunterDaddyDie;
            On.DaddyGraphics.ApplyPalette += LunterDaddyApplyPalette;
            On.DaddyGraphics.HunterDummy.ctor += LunterDummyCtor;
            On.DaddyGraphics.HunterDummy.ApplyPalette += LunterDummyApplyPalette;
        }

        internal static void OnMSCDisableSubPatch()
        {
            On.DaddyLongLegs.ctor -= LunterDaddyCtor;
            On.DaddyLongLegs.Update -= HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption -= HunterRecognizeLancer;
            On.DaddyLongLegs.Die -= LunterDaddyDie;
            On.DaddyGraphics.ApplyPalette -= LunterDaddyApplyPalette;
            On.DaddyGraphics.HunterDummy.ctor -= LunterDummyCtor;
            On.DaddyGraphics.HunterDummy.ApplyPalette -= LunterDummyApplyPalette;
        }

        internal const string HUNTERMEET = "LancerHunterMeet";
        internal const string HUNTERLANCERFLOWER = "LancerHunterFlower";
        internal const int LUNTERSEED = 1004;

        private static bool IsStoryLancer => ModifyCat.IsStoryLancer;

        #region LunterDaddy

        #region DaddyCreature

        private static void HunterMeetLancerTrigger(On.DaddyLongLegs.orig_Update orig, DaddyLongLegs self, bool eu)
        {
            orig(self, eu);
            if (self.room != null || !self.HDmode || !self.room.game.IsStorySession || !IsStoryLancer) return;
            var basis = self.room.game.StoryCharacter;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red) return;
            if (GetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET) > 0) return; // already triggered
            if (!(self.room.game.FirstAlivePlayer?.realizedCreature is Player player)) return;
            if (self.room.VisualContact(self.mainBodyChunk.pos, player.mainBodyChunk.pos))
                SetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET, 1);
        }

        private static bool HunterRecognizeLancer(On.DaddyLongLegs.orig_CheckDaddyConsumption orig, DaddyLongLegs self, PhysicalObject otherObject)
        {
            var result = orig(self, otherObject);
            if (self.room != null || !self.HDmode || !self.room.game.IsStorySession || !IsStoryLancer) return result;
            var basis = self.room.game.StoryCharacter;
            if (IsLancer(basis)) basis = GetBasis(basis);
            if (basis != SlugName.Red) return result;
            return !(otherObject is Player);
        }

        private static bool IsLunter(DaddyLongLegs self)
            => self.HDmode && self.abstractCreature.ID.altSeed == LUNTERSEED;

        private static void LunterDaddyCtor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (IsLunter(self))
            {
                self.effectColor = new Color(0.3f, 0.5f, 1.0f);
                self.eyeColor = self.effectColor;
            }
        }

        private static void LunterDaddyDie(On.DaddyLongLegs.orig_Die orig, DaddyLongLegs self)
        {
            if (IsLunter(self))
            {
                if (!self.dead && self.abstractCreature.world.game.IsStorySession && self.killTag?.realizedCreature is Player)
                {
                    DreamHandler.SetMiscWorldCoord(self.abstractCreature.world.game.manager.rainWorld.progression.miscProgressionData, HUNTERLANCERFLOWER, null);
                    self.isHD = false;
                    orig(self);
                    self.isHD = true; return;
                }
            }
            orig(self);
        }

        private static void LunterDummyCtor(On.DaddyGraphics.HunterDummy.orig_ctor orig, DaddyGraphics.HunterDummy self,
            DaddyGraphics dg, int startSprite)
        {
            orig(self, dg, startSprite);
            if (!IsLunter(dg.daddy)) return;

            var partList = new List<BodyPart>();
            foreach (var part in self.bodyParts)
                if (!(part is TailSegment)) partList.Add(part);

            self.tail = new TailSegment[4];
            self.tail[0] = new TailSegment(self.owner, 6f, 2f, null, 0.85f, 1f, 1f, true);
            self.tail[1] = new TailSegment(self.owner, 4f, 3.5f, self.tail[0], 0.85f, 1f, 0.5f, true);
            self.tail[2] = new TailSegment(self.owner, 2.5f, 3.5f, self.tail[1], 0.85f, 1f, 0.5f, true);
            self.tail[3] = new TailSegment(self.owner, 1f, 3.5f, self.tail[2], 0.85f, 1f, 0.5f, true);
            foreach (var tail in self.tail) partList.Add(tail);
            self.bodyParts = partList.ToArray();
        }

        private static void LunterDaddyApplyPalette(On.DaddyGraphics.orig_ApplyPalette orig, DaddyGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (!IsLunter(self.daddy)) return;
            var color = Color.Lerp(new Color(0.3f, 0.5f, 1.0f), Color.gray, 0.4f);
            for (int i = 0; i < self.daddy.bodyChunks.Length; i++)
                sLeaser.sprites[self.BodySprite(i)].color = color;
        }

        private static void LunterDummyApplyPalette(On.DaddyGraphics.HunterDummy.orig_ApplyPalette orig, DaddyGraphics.HunterDummy self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsLunter(self.owner.daddy))
            {
                Color color = Color.Lerp(new Color(0.3f, 0.5f, 1.0f), Color.gray, 0.4f);
                Color blackColor = palette.blackColor;
                for (int i = 0; i < self.numberOfSprites - 1; i++)
                {
                    sLeaser.sprites[self.startSprite + i].color = color;
                }
                sLeaser.sprites[self.startSprite + 5].color = blackColor;
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        #endregion DaddyCreature

        private static void AdaptWorldToRegionState(On.RegionState.orig_AdaptWorldToRegionState orig, RegionState self)
        {
            orig(self);
            var basis = self.saveState.saveStateNumber;
            if (IsLancer(basis)) basis = GetBasis(basis);
            var lancer = GetLancer(basis);
            var story = IsStoryLancer ? lancer : basis;
            var miscData = self.world.game.rainWorld.progression.miscProgressionData;
            if (miscData.redsFlower != null && self.world.IsRoomInRegion(miscData.redsFlower.Value.room))
            {
                if (ModManager.MSC) // Add normal hunter daddy
                {
                    if (LancerGenerator.IsTimelineInbetween(story, SlugName.Red, MSCSlugName.Gourmand))
                        self.world.GetAbstractRoom(miscData.redsFlower.Value)
                            .AddEntity(new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy),
                            null, miscData.redsFlower.Value, self.world.game.GetNewID()));
                }
            }
            var lancerFlower = DreamHandler.GetMiscWorldCoord(miscData, HUNTERLANCERFLOWER);
            if (lancerFlower != null && self.world.IsRoomInRegion(lancerFlower.Value.room))
            {
                if (ModManager.MSC) // Add lancer hunter daddy
                {
                    if (LancerGenerator.IsTimelineInbetween(story, GetLancer(SlugName.Red), SlugName.White))
                    {
                        var id = self.world.game.GetNewID();
                        id.setAltSeed(LUNTERSEED);
                        self.world.GetAbstractRoom(lancerFlower.Value)
                            .AddEntity(new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy),
                            null, lancerFlower.Value, id));
                    }
                }
                if (story == SlugName.White || LancerGenerator.IsTimelineInbetween(story, SlugName.White, ModManager.MSC ? MSCSlugName.Rivulet : null))
                { // Add lancer hunter flower
                    self.world.GetAbstractRoom(lancerFlower.Value)
                        .AddEntity(new AbstractConsumable(self.world, AbstractPhysicalObject.AbstractObjectType.KarmaFlower,
                        null, lancerFlower.Value, self.world.game.GetNewID(), -1, -1, null));
                }
            }
        }

        private static void PlaceLancerKarmaFlower(On.StoryGameSession.orig_PlaceKarmaFlowerOnDeathSpot orig, StoryGameSession self)
        {
            if (self.RedIsOutOfCycles && IsStoryLancer)
            {
                DreamHandler.SetMiscWorldCoord(self.game.manager.rainWorld.progression.miscProgressionData, HUNTERLANCERFLOWER,
                    (self.Players[0].realizedCreature as Player).karmaFlowerGrowPos.Value);
                return;
            }
            orig(self);
        }

        #endregion LunterDaddy
    }
}