using LancerRemix.Cat;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using MoreSlugcats;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using static CatSub.Story.SaveManager;
using static DaddyGraphics;
using static LancerRemix.LancerEnums;
using MSCSlugName = MoreSlugcats.MoreSlugcatsEnums.SlugcatStatsName;
using SlugName = SlugcatStats.Name;

namespace LancerRemix.Story
{
    internal static class LunterScripts
    {
        internal static void SubPatch()
        {
            On.RegionState.AdaptWorldToRegionState += AdaptWorldToRegionState;
            On.StoryGameSession.PlaceKarmaFlowerOnDeathSpot += PlaceLancerKarmaFlower;
            IL.Menu.SlugcatSelectMenu.MineForSaveData += MineForLunterData;
            On.WorldLoader.OverseerSpawnConditions += LunterOverseerSpawn;
            On.OverseerAbstractAI.SetAsPlayerGuide += LunterOverseerGuide;
            On.OverseersWorldAI.DirectionFinder.ctor += LunterOverseerDirection;
        }

        internal static void OnMSCEnableSubPatch()
        {
            On.DaddyLongLegs.ctor += LunterDaddyCtor;
            On.DaddyTentacle.ctor += LunterDaddyTentacleCtor;
            On.DaddyLongLegs.Update += HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption += HunterRecognizeLancer;
            On.DaddyLongLegs.Die += LunterDaddyDie;
            On.DaddyGraphics.ApplyPalette += LunterDaddyApplyPalette;
            On.DaddyGraphics.HunterDummy.ctor += LunterDummyCtor;
            On.DaddyGraphics.HunterDummy.InitiateSprites += LunterDummyInitSprites;
            On.DaddyGraphics.HunterDummy.AddToContainer += LunterDummyAddToContainer;
            On.DaddyGraphics.HunterDummy.DrawSprites += LunterDummyDrawSprites;
            On.DaddyGraphics.HunterDummy.ApplyPalette += LunterDummyApplyPalette;
            On.DaddyGraphics.DaddyTubeGraphic.ApplyPalette += LunterTubeApplyPalette;
            On.DaddyGraphics.DaddyDangleTube.ApplyPalette += LunterTubeDangleApplyPalette;
            On.DaddyGraphics.DaddyDeadLeg.ApplyPalette += LunterDeadLegApplyPalette;
        }

        internal static void OnMSCDisableSubPatch()
        {
            On.DaddyLongLegs.ctor -= LunterDaddyCtor;
            On.DaddyTentacle.ctor -= LunterDaddyTentacleCtor;
            On.DaddyLongLegs.Update -= HunterMeetLancerTrigger;
            On.DaddyLongLegs.CheckDaddyConsumption -= HunterRecognizeLancer;
            On.DaddyLongLegs.Die -= LunterDaddyDie;
            On.DaddyGraphics.ApplyPalette -= LunterDaddyApplyPalette;
            On.DaddyGraphics.HunterDummy.ctor -= LunterDummyCtor;
            On.DaddyGraphics.HunterDummy.InitiateSprites -= LunterDummyInitSprites;
            On.DaddyGraphics.HunterDummy.AddToContainer -= LunterDummyAddToContainer;
            On.DaddyGraphics.HunterDummy.DrawSprites -= LunterDummyDrawSprites;
            On.DaddyGraphics.HunterDummy.ApplyPalette -= LunterDummyApplyPalette;
            On.DaddyGraphics.DaddyTubeGraphic.ApplyPalette -= LunterTubeApplyPalette;
            On.DaddyGraphics.DaddyDangleTube.ApplyPalette -= LunterTubeDangleApplyPalette;
            On.DaddyGraphics.DaddyDeadLeg.ApplyPalette -= LunterDeadLegApplyPalette;
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
            if (self.room == null || !self.HDmode || !self.room.game.IsStorySession || !IsStoryLancer) return;
            var basis = GetBasis(self.room.game.StoryCharacter);
            if (basis != SlugName.Red) return;
            if (GetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET) > 0) return; // already triggered
            if (!(self.room.game.FirstAlivePlayer?.realizedCreature is Player player)) return;
            if (self.room.VisualContact(self.mainBodyChunk.pos, player.mainBodyChunk.pos))
            {
                SetProgValue<int>(self.room.game.GetStorySession.saveState.miscWorldSaveData, HUNTERMEET, 1);
                Debug.Log("Lunter meet Hunter: trigger nightmare");
            }
        }

        private static bool HunterRecognizeLancer(On.DaddyLongLegs.orig_CheckDaddyConsumption orig, DaddyLongLegs self, PhysicalObject otherObject)
        {
            var result = orig(self, otherObject);
            if (self.room == null || !self.HDmode) return result;
            if (otherObject is DaddyLongLegs ddl && ddl.HDmode) return false; // don't eat each other
            if (!self.room.game.IsStorySession || !IsStoryLancer) return result;
            var basis = GetBasis(self.room.game.StoryCharacter);
            if (basis != SlugName.Red) return result;
            return !(otherObject is Player);
        }

        private static bool IsLunter(DaddyLongLegs self)
            => self.HDmode && self.abstractCreature.ID.altSeed == LUNTERSEED;

        private static Color LunterColor => ModifyCat.defaultLancerBodyColors[SlugName.Red];

        private static void LunterDaddyCtor(On.DaddyLongLegs.orig_ctor orig, DaddyLongLegs self, AbstractCreature abstractCreature, World world)
        {
            orig(self, abstractCreature, world);
            if (IsLunter(self))
            {
                self.effectColor = LunterColor;
                self.eyeColor = self.effectColor;
            }
        }

        private static void LunterDaddyTentacleCtor(On.DaddyTentacle.orig_ctor orig, DaddyTentacle self,
            DaddyLongLegs daddy, BodyChunk chunk, float length, int tentacleNumber, Vector2 tentacleDir)
        {
            if (IsLunter(daddy)) length *= 0.7f;
            orig(self, daddy, chunk, length, tentacleNumber, tentacleDir);
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

        #region Dummy

        private static readonly ConditionalWeakTable<HunterDummy, LunterDummyDecoration> dummyDecos
            = new ConditionalWeakTable<HunterDummy, LunterDummyDecoration>();

        private static LunterDummyDecoration GetDeco(HunterDummy dummy)
            => dummyDecos.GetValue(dummy, (d) => new LunterDummyDecoration(d));

        private static void LunterDummyCtor(On.DaddyGraphics.HunterDummy.orig_ctor orig, HunterDummy self,
            DaddyGraphics dg, int startSprite)
        {
            orig(self, dg, startSprite);
            if (!IsLunter(dg.daddy)) return;
            dummyDecos.Add(self, new LunterDummyDecoration(self));

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

        private static void LunterDummyInitSprites(On.DaddyGraphics.HunterDummy.orig_InitiateSprites orig, HunterDummy self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam)
        {
            orig(self, sLeaser, rCam);
            if (IsLunter(self.owner.daddy))
                GetDeco(self).InitiateSprites(null, sLeaser, rCam);
        }

        private static void LunterDummyAddToContainer(On.DaddyGraphics.HunterDummy.orig_AddToContainer orig, HunterDummy self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, FContainer newContatiner)
        {
            orig(self, sLeaser, rCam, newContatiner);
            if (IsLunter(self.owner.daddy))
                GetDeco(self).AddToContainer(null, sLeaser, rCam, newContatiner);
        }

        private static void LunterDummyDrawSprites(On.DaddyGraphics.HunterDummy.orig_DrawSprites orig, HunterDummy self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, float timeStacker, Vector2 camPos)
        {
            orig(self, sLeaser, rCam, timeStacker, camPos);
            if (IsLunter(self.owner.daddy))
                GetDeco(self).DrawSprites(null, sLeaser, rCam, timeStacker, camPos);
            else
            {
                if (!self.owner.daddy.room.game.IsStorySession || !IsStoryLancer) return;
                var basis = GetBasis(self.owner.daddy.room.game.StoryCharacter);
                if (basis != SlugName.Red) return;
                sLeaser.sprites[self.startSprite + 5].element = Futile.atlasManager.GetElementWithName("FaceA" +
                    sLeaser.sprites[self.startSprite + 3].element.name.Substring(5));
                sLeaser.sprites[self.startSprite + 5].scaleX = sLeaser.sprites[self.startSprite + 3].scaleX;
            }
        }

        private static void LunterDummyApplyPalette(On.DaddyGraphics.HunterDummy.orig_ApplyPalette orig, HunterDummy self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsLunter(self.owner.daddy))
            {
                Color color = Color.Lerp(LunterColor, Color.gray, 0.4f);
                Color blackColor = palette.blackColor;
                for (int i = 0; i < self.numberOfSprites - 1; i++)
                {
                    sLeaser.sprites[self.startSprite + i].color = color;
                }
                sLeaser.sprites[self.startSprite + 5].color = blackColor;

                GetDeco(self).ApplyPalette(null, sLeaser, rCam, palette);
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        #endregion Dummy

        private static void LunterDaddyApplyPalette(On.DaddyGraphics.orig_ApplyPalette orig, DaddyGraphics self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            orig(self, sLeaser, rCam, palette);
            if (!IsLunter(self.daddy)) return;
            var color = Color.Lerp(LunterColor, Color.gray, 0.4f);
            for (int i = 0; i < self.daddy.bodyChunks.Length; i++)
                sLeaser.sprites[self.BodySprite(i)].color = color;
        }

        private static void LunterTubeApplyPalette(On.DaddyGraphics.DaddyTubeGraphic.orig_ApplyPalette orig, DaddyTubeGraphic self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsLunter(self.owner.daddy))
            {
                Color color = Color.Lerp(LunterColor, Color.gray, 0.4f);
                for (int i = 0; i < (sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length; i++)
                {
                    float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length - 1));
                    (sLeaser.sprites[self.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(floatPos));
                }
                int num = 0;
                for (int j = 0; j < self.bumps.Length; j++)
                {
                    sLeaser.sprites[self.firstSprite + 1 + j].color = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(self.bumps[j].pos.y));
                    if (self.bumps[j].eyeSize > 0f)
                    {
                        sLeaser.sprites[self.firstSprite + 1 + self.bumps.Length + num].color = (self.owner.colorClass ? self.owner.EffectColor : color);
                        num++;
                    }
                }
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void LunterTubeDangleApplyPalette(On.DaddyGraphics.DaddyDangleTube.orig_ApplyPalette orig, DaddyDangleTube self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsLunter(self.owner.daddy))
            {
                Color color = Color.Lerp(LunterColor, Color.gray, 0.4f);
                for (int i = 0; i < (sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length; i++)
                {
                    float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length - 1));
                    (sLeaser.sprites[self.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(floatPos));
                }
                sLeaser.sprites[self.firstSprite].color = color;
                for (int j = 0; j < self.bumps.Length; j++)
                {
                    sLeaser.sprites[self.firstSprite + 1 + j].color = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(self.bumps[j].pos.y));
                }
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        private static void LunterDeadLegApplyPalette(On.DaddyGraphics.DaddyDeadLeg.orig_ApplyPalette orig, DaddyDeadLeg self,
            RoomCamera.SpriteLeaser sLeaser, RoomCamera rCam, RoomPalette palette)
        {
            if (IsLunter(self.owner.daddy))
            {
                Color color = Color.Lerp(LunterColor, Color.gray, 0.4f);
                for (int i = 0; i < (sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length; i++)
                {
                    float floatPos = Mathf.InverseLerp(0.3f, 1f, (float)i / (float)((sLeaser.sprites[self.firstSprite] as TriangleMesh).vertices.Length - 1));
                    (sLeaser.sprites[self.firstSprite] as TriangleMesh).verticeColors[i] = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(floatPos));
                }
                int num = 0;
                for (int j = 0; j < self.bumps.Length; j++)
                {
                    sLeaser.sprites[self.firstSprite + 1 + j].color = Color.Lerp(color, self.owner.EffectColor, self.OnTubeEffectColorFac(self.bumps[j].pos.y));
                    if (self.bumps[j].eyeSize > 0f)
                    {
                        sLeaser.sprites[self.firstSprite + 1 + self.bumps.Length + num].color = (self.owner.colorClass ? (self.owner.EffectColor * Mathf.Lerp(0.5f, 0.2f, self.deadness)) : color);
                        num++;
                    }
                }
                return;
            }
            orig(self, sLeaser, rCam, palette);
        }

        #endregion DaddyCreature

        private static void AdaptWorldToRegionState(On.RegionState.orig_AdaptWorldToRegionState orig, RegionState self)
        {
            orig(self);
            var basis = GetBasis(self.saveState.saveStateNumber);
            var lancer = GetLancer(basis);
            var story = IsStoryLancer ? lancer : basis;
            var miscData = self.world.game.rainWorld.progression.miscProgressionData;
            if (miscData.redsFlower != null && self.world.IsRoomInRegion(miscData.redsFlower.Value.room))
            {
                if (ModManager.MSC) // Add normal hunter daddy
                {
                    if (LancerGenerator.IsTimelineInbetween(story, SlugName.Red, MSCSlugName.Gourmand))
                    {
                        Debug.Log($"Lancer added HunterDaddy at {miscData.redsFlower}");
                        self.world.GetAbstractRoom(miscData.redsFlower.Value)
                            .AddEntity(new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy),
                            null, miscData.redsFlower.Value, self.world.game.GetNewID()));
                    }
                }
            }
            var lancerFlower = DreamHandler.GetMiscWorldCoord(miscData, HUNTERLANCERFLOWER);
            if (lancerFlower != null && self.world.IsRoomInRegion(lancerFlower.Value.room))
            {
                if (ModManager.MSC) // Add lancer hunter daddy
                {
                    if (LancerGenerator.IsTimelineInbetween(story, GetLancer(SlugName.Red), SlugName.White))
                    {
                        Debug.Log($"Lancer added LunterDaddy at {lancerFlower}");
                        var id = self.world.game.GetNewID();
                        id.setAltSeed(LUNTERSEED);
                        self.world.GetAbstractRoom(lancerFlower.Value)
                            .AddEntity(new AbstractCreature(self.world, StaticWorld.GetCreatureTemplate(MoreSlugcatsEnums.CreatureTemplateType.HunterDaddy),
                            null, lancerFlower.Value, id));
                    }
                }
                if (story == SlugName.White || LancerGenerator.IsTimelineInbetween(story, SlugName.White, ModManager.MSC ? MSCSlugName.Rivulet : null))
                { // Add lancer hunter flower
                    Debug.Log($"Lancer added LunterFlower at {lancerFlower}");
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

        private static void MineForLunterData(ILContext il)
        {
            var cursor = new ILCursor(il);
            LancerPlugin.ILhookTry(LancerPlugin.ILhooks.MineForLunterData);

            if (!cursor.TryGotoNext(MoveType.Before,
                x => x.MatchLdsfld(typeof(ModManager).GetField(nameof(ModManager.MSC))),
                x => x.MatchBrfalse(out var _),
                x => x.MatchLdloc(4),
                x => x.MatchLdstr(">HASROBO"),
                x => x.MatchLdnull()
                )) return;

            DebugLogCursor();

            cursor.Emit(OpCodes.Ldarg_1);
            cursor.Emit(OpCodes.Ldloc, 4);
            cursor.EmitDelegate<Action<SlugName, List<SaveStateMiner.Target>>>((slugcat, list) =>
            {
                if (IsLancer(slugcat))
                {
                    var basis = GetBasis(slugcat);
                    if (basis == SlugName.Red)
                        list.Add(new SaveStateMiner.Target(">REDSDEATH", null, "<dpA>", 20));
                }
            });

            LancerPlugin.ILhookOkay(LancerPlugin.ILhooks.MineForLunterData);

            void DebugLogCursor() =>
                LancerPlugin.LogSource.LogInfo($"{cursor.Prev.OpCode.Name} > Cursor < {cursor.Next.OpCode.Name}");
        }

        #region Overseer

        private static bool LunterOverseerSpawn(On.WorldLoader.orig_OverseerSpawnConditions orig, WorldLoader self, SlugName character)
        {
            if (IsStoryLancer)
            {
                var basis = GetBasis(character);
                if (basis == SlugName.Red) return self.game.session is StoryGameSession session && !session.saveState.miscWorldSaveData.EverMetMoon;
                if (basis == SlugName.Yellow) return false;
                return orig(self, basis);
            }
            return orig(self, character);
        }

        private static void LunterOverseerGuide(On.OverseerAbstractAI.orig_SetAsPlayerGuide orig, OverseerAbstractAI self, int ownerOverride)
        {
            if (IsStoryLancer)
            {
                var basis = GetBasis(self.world.game.StoryCharacter);
                if (basis == SlugName.Red)
                {
                    ownerOverride = 2;
                    if (!self.spearmasterLockedOverseer)
                    {
                        if (self.RelevantPlayer?.Room.name == "SL_AI" || self.world.regionState.saveState.miscWorldSaveData.EverMetMoon)
                        { self.isPlayerGuide = false; return; }
                    }
                }
                else if (basis == SlugName.Yellow) { self.isPlayerGuide = false; return; }
            }
            orig(self, ownerOverride);
        }

        private static void LunterOverseerDirection(On.OverseersWorldAI.DirectionFinder.orig_ctor orig, OverseersWorldAI.DirectionFinder self, World world)
        {
            orig(self, world);
            if (IsStoryLancer)
            {
                var basis = GetBasis(world.game.StoryCharacter);
                if (basis == SlugName.Red && (world.game.session as StoryGameSession).saveState.miscWorldSaveData.EverMetMoon) self.destroy = true;
                else if (basis == SlugName.Yellow) self.destroy = true;
            }
        }

        #endregion Overseer
    }
}