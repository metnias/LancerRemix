using LancerRemix;
using Menu;
using Menu.Remix;
using RWCustom;
using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;
using MiscProgressionData = PlayerProgression.MiscProgressionData;
using SaveGameData = Menu.SlugcatSelectMenu.SaveGameData;

namespace LancerRemix
{
    public static class SaveManager
    {
        public static void SubPatch()
        {
            progDataTable = new ConditionalWeakTable<SaveState, SaveDataTable>();
            persDataTable = new ConditionalWeakTable<DeathPersistentSaveData, SaveDataTable>();
            miscDataTable = new ConditionalWeakTable<MiscProgressionData, SaveDataTable>();

            On.SaveState.ctor += ProgDataCtorPatch;
            On.SaveState.SaveToString += ProgDataToStringPatch;
            On.SaveState.LoadGame += ProgDataFromStringPatch;

            On.DeathPersistentSaveData.ctor += PersDataCtorPatch;
            On.DeathPersistentSaveData.SaveToString += PersDataToStringPatch;
            On.DeathPersistentSaveData.FromString += PersDataFromStringPatch;

            On.PlayerProgression.MiscProgressionData.ctor += MiscDataCtorPatch;
            On.PlayerProgression.MiscProgressionData.ToString += MiscDataToStringPatch;
            On.PlayerProgression.MiscProgressionData.FromString += MiscDataFromStringPatch;

            On.Menu.SlugcatSelectMenu.MineForSaveData += MineForPlanterSaveData;

            On.RainWorldGame.GoToStarveScreen += StarvePlantSaveCheck;
            On.RainWorldGame.Win += WinPlanterProcess;
            On.SaveState.SessionEnded += SessionEndedPatch;
        }

        public static readonly string Prefix = $"<Data_{LancerPlugin.PLUGIN_ID}>";

        #region InitialSave

        private static SaveDataTable CreateNewProgSaveData()
        {
            var prog = new SaveDataTable();
            prog.SetValue(PLANT_FOOD, 0);
            prog.SetValue(PLANT_LOVE, 0f);
            prog.SetValue(PLANT_MALNOURISHED, false);
            prog.SetValue(PLANT_TAKEOVER, 0);
            return prog;
        }

        internal const string PLANT_FOOD = "plantFood";
        internal const string PLANT_LOVE = "plantLikeness";
        internal const string PLANT_MALNOURISHED = "plantMalnourished";
        internal const string PLANT_TAKEOVER = "plantTakeover";

        private static SaveDataTable CreateNewPersSaveData()
        {
            var pers = new SaveDataTable();
            pers.SetValue(PLANT_SIZE, 1);
            pers.SetValue(PLANT_PENALTY, 0);
            return pers;
        }

        internal const string PLANT_SIZE = "plantSize";
        internal const string PLANT_PENALTY = "plantPenalty";

        private static SaveDataTable CreateNewMiscSaveData()
        {
            var misc = new SaveDataTable();
            misc.SetValue(BAD_FATE, false);
            return misc;
        }

        internal const string BAD_FATE = "planterBadFate";

        #endregion InitialSave

        #region ProgData

        private static ConditionalWeakTable<SaveState, SaveDataTable> progDataTable;

        private static void ProgDataCtorPatch(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);

            if (saveStateNumber != PlanterEnums.SlugPlanter) return;

            SaveDataTable prog = CreateNewProgSaveData();
            progDataTable.Add(self, prog);
        }

        private static string ProgDataToStringPatch(On.SaveState.orig_SaveToString orig, SaveState self)
        {
            if (progDataTable.TryGetValue(self, out var saveData))
            {
                var saveDataPos = -1;
                for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
                    if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                        saveDataPos = i;

                if (saveDataPos > -1)
                    self.unrecognizedSaveStrings[saveDataPos] = saveData.ToString();
                else
                    self.unrecognizedSaveStrings.Add(saveData.ToString());
            }

            return orig(self);
        }

        private static void ProgDataFromStringPatch(On.SaveState.orig_LoadGame orig, SaveState self, string str, RainWorldGame game)
        {
            orig(self, str, game);

            if (!progDataTable.TryGetValue(self, out var saveData)) return;

            var saveDataPos = -1;
            for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
            {
                if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                    saveDataPos = i;
            }

            if (saveDataPos > -1)
                saveData.FromString(self.unrecognizedSaveStrings[saveDataPos]);
        }

        internal static T GetProgValue<T>(SaveState data, string key)
            => progDataTable.TryGetValue(data, out var table) ? table.GetValue<T>(key) : default;

        internal static void SetProgValue<T>(SaveState data, string key, T value)
        { if (progDataTable.TryGetValue(data, out var table)) table.SetValue(key, value); }

        #endregion ProgData

        #region PersData

        private static ConditionalWeakTable<DeathPersistentSaveData, SaveDataTable> persDataTable;

        private static void PersDataCtorPatch(On.DeathPersistentSaveData.orig_ctor orig, DeathPersistentSaveData self, SlugcatStats.Name slugcat)
        {
            orig(self, slugcat);

            if (slugcat != PlanterEnums.SlugPlanter) return;

            SaveDataTable pers = CreateNewPersSaveData();
            persDataTable.Add(self, pers);
        }

        private static string PersDataToStringPatch(On.DeathPersistentSaveData.orig_SaveToString orig, DeathPersistentSaveData self, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (persDataTable.TryGetValue(self, out var saveData))
            {
                UpdatePersSaveData(self, ref saveData, saveAsIfPlayerDied, saveAsIfPlayerQuit);

                var saveDataPos = -1;
                for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
                    if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                        saveDataPos = i;

                if (saveDataPos > -1)
                    self.unrecognizedSaveStrings[saveDataPos] = saveData.ToString();
                else
                    self.unrecognizedSaveStrings.Add(saveData.ToString());

                PlanterPlugin.LogSource.LogInfo($"DPS saved (died? {saveAsIfPlayerDied}) karma: {self.karma}, plantSize: {GetPersValue<int>(self, PLANT_SIZE)}");
            }

            return orig(self, saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }

        private static void PersDataFromStringPatch(On.DeathPersistentSaveData.orig_FromString orig, DeathPersistentSaveData self, string s)
        {
            orig(self, s);

            if (!persDataTable.TryGetValue(self, out var saveData)) return;

            var saveDataPos = -1;
            for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
            {
                if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                    saveDataPos = i;
            }

            if (saveDataPos > -1)
                saveData.FromString(self.unrecognizedSaveStrings[saveDataPos]);

            PlanterPlugin.LogSource.LogInfo($"DPS loaded: karma: {self.karma}, plantSize: {GetPersValue<int>(self, PLANT_SIZE)}");
        }

        internal static T GetPersValue<T>(DeathPersistentSaveData data, string key)
            => persDataTable.TryGetValue(data, out var table) ? table.GetValue<T>(key) : default;

        internal static void SetPersValue<T>(DeathPersistentSaveData data, string key, T value)
        { if (persDataTable.TryGetValue(data, out var table)) table.SetValue(key, value); }

        #endregion PersData

        #region MiscData

        private static ConditionalWeakTable<MiscProgressionData, SaveDataTable> miscDataTable;

        private static void MiscDataCtorPatch(On.PlayerProgression.MiscProgressionData.orig_ctor orig, MiscProgressionData self, PlayerProgression owner)
        {
            orig(self, owner);

            SaveDataTable misc = CreateNewMiscSaveData();
            miscDataTable.Add(self, misc);
        }

        private static string MiscDataToStringPatch(On.PlayerProgression.MiscProgressionData.orig_ToString orig, MiscProgressionData self)
        {
            if (miscDataTable.TryGetValue(self, out var saveData))
            {
                var saveDataPos = -1;
                for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
                    if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                        saveDataPos = i;

                if (saveDataPos > -1)
                    self.unrecognizedSaveStrings[saveDataPos] = saveData.ToString();
                else
                    self.unrecognizedSaveStrings.Add(saveData.ToString());
            }

            return orig(self);
        }

        private static void MiscDataFromStringPatch(On.PlayerProgression.MiscProgressionData.orig_FromString orig, MiscProgressionData self, string s)
        {
            orig(self, s);

            if (!miscDataTable.TryGetValue(self, out var saveData)) return;

            var saveDataPos = -1;
            for (var i = 0; i < self.unrecognizedSaveStrings.Count; i++)
            {
                if (self.unrecognizedSaveStrings[i].StartsWith(Prefix))
                    saveDataPos = i;
            }

            if (saveDataPos > -1)
                saveData.FromString(self.unrecognizedSaveStrings[saveDataPos]);
        }

        internal static T GetMiscValue<T>(MiscProgressionData data, string key)
            => miscDataTable.TryGetValue(data, out var table) ? table.GetValue<T>(key) : default;

        internal static void SetMiscValue<T>(MiscProgressionData data, string key, T value)
        { if (miscDataTable.TryGetValue(data, out var table)) table.SetValue(key, value); }

        #endregion MiscData

        #region SaveGameData

        private static int cyclePenalty = 0;

        internal static int CyclePenalty() => cyclePenalty;

        private static SaveGameData MineForPlanterSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
        {
            var data = orig(manager, slugcat);
            if (slugcat == PlanterEnums.SlugPlanter)
            {
                cyclePenalty = 0;
                if (data != null)
                {
                    // update planter cyclePenalty
                    var progLines = manager.rainWorld.progression.GetProgLinesFromMemory();
                    for (int i = 0; i < progLines.Length; ++i)
                    {
                        string[] array = Regex.Split(progLines[i], "<progDivB>");
                        if (array.Length != 2 || array[0] != "SAVE STATE" || BackwardsCompatibilityRemix.ParseSaveNumber(array[1]) != slugcat)
                            continue;

                        var list = new List<SaveStateMiner.Target>
                        {
                            new SaveStateMiner.Target(PLANT_PENALTY, SaveDataTable.DIV.ToString(), SaveDataTable.SPR.ToString(), 30)
                        };
                        var res = SaveStateMiner.Mine(manager.rainWorld, array[1], list);
                        if (res.Count > 0)
                        {
                            cyclePenalty = int.Parse(res[0].data);
                            PlanterPlugin.LogSource.LogInfo($"Planter cyclePenalty: {cyclePenalty}");
                        }
                    }
                }
            }

            return data;
        }

        #endregion SaveGameData

        private static void StarvePlantSaveCheck(On.RainWorldGame.orig_GoToStarveScreen orig, RainWorldGame self)
        {
            if (self.StoryCharacter == PlanterEnums.SlugPlanter)
            {
                var state = self.GetStorySession.saveState;
                Player planter = null;
                foreach (var p in self.Players)
                {
                    planter = p.realizedCreature as Player;
                    if (planter.slugcatStats.name == PlanterEnums.SlugPlanter) break;
                }
                if (planter.slugcatStats.name != PlanterEnums.SlugPlanter) goto NoPlanter;

                // if Planter can survive with Dave's help, return
                int slugFood = planter.FoodInRoom(planter.room, false);
                int slugNeed = state.malnourished ? 1 : planter.slugcatStats.maxFood;
                int plantFood = GetProgValue<int>(state, PLANT_FOOD);
                int plantSize = GetPersValue<int>(state.deathPersistentSaveData, PLANT_SIZE);
                int plantFoodToHibernate = GetProgValue<bool>(state, PLANT_MALNOURISHED) ?
                    DaveState.MaxFood(plantSize) : DaveState.RequiredFood(plantSize);
                plantFood -= plantFoodToHibernate;
                if (plantFood < 0) goto NoPlanter; // Plant is also starving; cannot help
                slugFood -= slugNeed; // -required
                if (plantSize - 1 + slugFood >= 0) // Plant rescue
                {
                    planter.playerState.foodInStomach -= slugFood;
                    plantFood += slugFood;
                    plantSize += slugFood;
                    SetProgValue(state, PLANT_FOOD, plantFood);
                    SetPersValue(state.deathPersistentSaveData, PLANT_SIZE, Math.Max(plantSize, 0));
                    return;
                }
            }
        NoPlanter: orig(self);
        }

        private static void WinPlanterProcess(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (self.StoryCharacter == PlanterEnums.SlugPlanter)
            {
                //PlanterPlugin.LogSource.LogInfo("Planter Win!");
                var state = self.GetStorySession.saveState;
                Player planter = null;
                foreach (var p in self.Players)
                {
                    planter = p.realizedCreature as Player;
                    if (planter.slugcatStats.name == PlanterEnums.SlugPlanter) break;
                }
                if (planter.slugcatStats.name != PlanterEnums.SlugPlanter) goto NoPlanter;
                //PlanterPlugin.LogSource.LogInfo($"Win Planter found: {planter.playerState.playerNumber}");

                // dave malnourish check
                int plantFood = GetProgValue<int>(state, PLANT_FOOD);
                int plantSize = GetPersValue<int>(state.deathPersistentSaveData, PLANT_SIZE);
                bool plantLastMalnourished = GetProgValue<bool>(state, PLANT_MALNOURISHED);
                int plantFoodToHibernate = plantLastMalnourished ?
                    DaveState.MaxFood(plantSize) : DaveState.RequiredFood(plantSize);
                int slugFoodLeft = planter.FoodInRoom(planter.room, false) - self.GetStorySession.characterStats.foodToHibernate;
                //PlanterPlugin.LogSource.LogInfo($"plantFood: {plantFood}, Size: {plantSize}, foodToHibernate: {plantFoodToHibernate}");
                SetProgValue(state, PLANT_MALNOURISHED, false);
                if (plantFood < plantFoodToHibernate) // dave starve!
                {
                    if (plantFood + slugFoodLeft <= 0) // slugcat cannot save plant
                    { self.GoToStarveScreen(); return; }
                    if (plantFood + slugFoodLeft < plantFoodToHibernate) // plant can survive malnourished
                    {
                        if (plantLastMalnourished) // cannot starve plant twice
                        { self.GoToStarveScreen(); return; }
                        slugFoodLeft = 0; plantFood = plantFoodToHibernate;
                        SetProgValue(state, PLANT_MALNOURISHED, true);
                    }
                    else // slugcat can save plant
                    {
                        slugFoodLeft -= plantFoodToHibernate - plantFood;
                        plantFood = plantFoodToHibernate;
                    }
                }
                else if (malnourished) // slugcat starve!
                {
                    if (plantSize - 1 + slugFoodLeft >= 0) // plant can save slugcat
                    {
                        plantFood += slugFoodLeft;
                        plantSize += slugFoodLeft;
                        slugFoodLeft = 0;
                        malnourished = false;
                    }
                }
                else ++plantSize;

                bool dave = PlanterPlugin.GetDisplayCycle(state.cycleNumber) <= 0;
                target = new PlanterTargetValues()
                {
                    dave = dave,
                    plantFood = plantFood - plantFoodToHibernate,
                    slugFood = slugFoodLeft,
                    takenOver = GetProgValue<int>(state, PLANT_TAKEOVER) >= 20 && !dave,
                    plantSize = dave || target.takenOver ? 3 : plantSize
                };
                //PlanterPlugin.LogSource.LogInfo($"plantFood: {target.plantFood}, Size: {target.plantSize}, slugFoodLeft: {target.slugFood}");

                if (state.dreamsState != null) DreamHandler.PlanterCheckEventDream(state, target);
                orig(self, malnourished);
                // SetProgValue(state, PLANT_FOOD, plantFood - plantFoodToHibernate);
                // state.food = slugFoodLeft;
                // PlanterPlugin.LogSource.LogInfo($"plantFood Left: {plantFood - plantFoodToHibernate}, slugFoodLeft: {slugFoodLeft}");

                return;
            }
        NoPlanter: orig(self, malnourished);
        }

        private static PlanterTargetValues target = new PlanterTargetValues()
        {
            plantFood = 0,
            slugFood = 0,
            plantSize = 1,
            takenOver = false,
            dave = false
        };

        internal struct PlanterTargetValues
        {
            public int plantFood;
            public int slugFood;
            public int plantSize;
            public bool takenOver;
            public bool dave;
        }

        private static void SessionEndedPatch(On.SaveState.orig_SessionEnded orig, SaveState self,
            RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (game.StoryCharacter == PlanterEnums.SlugPlanter)
            {
                PlanterPlugin.LogSource.LogInfo("Planter session ended!");
                Player planter = null;
                foreach (var p in game.Players)
                {
                    planter = p.realizedCreature as Player;
                    if (planter.slugcatStats.name == PlanterEnums.SlugPlanter) break;
                }
                if (planter.slugcatStats.name != PlanterEnums.SlugPlanter) goto NoPlanter;
                PlanterPlugin.LogSource.LogInfo($"Sesson Planter found: {planter.playerState.playerNumber}");

                if (survived)
                {
                    SetProgValue(self, PLANT_FOOD, target.plantFood);
                    int plantSize = Custom.IntClamp(target.plantSize, 1, 3);
                    SetPersValue(self.deathPersistentSaveData, PLANT_SIZE, plantSize);
                    int penalty = GetPersValue<int>(self.deathPersistentSaveData, PLANT_PENALTY);
                    if (target.takenOver) SetPersValue(self.deathPersistentSaveData, PLANT_PENALTY, ++penalty);
                    cyclePenalty = penalty;
                }
                //else
                //{
                // SetPersValue(self.deathPersistentSaveData, PLANT_SIZE, 1);
                // SetProgValue(self, PLANT_MALNOURISHED, false);
                //}
                PlanterPlugin.LogSource.LogInfo($"plantFood: {GetProgValue<int>(self, PLANT_FOOD)}, Size: {GetPersValue<int>(self.deathPersistentSaveData, PLANT_SIZE)}, cyclePenalty: {cyclePenalty}");

                orig(self, game, survived, newMalnourished);
                self.food = target.slugFood;
                return;
            }
        NoPlanter:
            orig(self, game, survived, newMalnourished);
        }

        private static void UpdatePersSaveData(DeathPersistentSaveData self, ref SaveDataTable persData, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (saveAsIfPlayerDied && !target.dave && !self.reinforcedKarma)
                persData.SetValue(PLANT_SIZE, 1); // reduce size upon death
        }

        public class SaveDataTable
        {
            public Dictionary<string, string> table = new Dictionary<string, string>();

            internal const char SPR = '|';
            internal const char DIV = '~';

            public SaveDataTable()
            {
            }

            public T GetValue<T>(string key) => ValueConverter.ConvertToValue<T>(table[key]);

            public void SetValue<T>(string key, T value)
            {
                var t = ValueConverter.ConvertToString(value);
                if (table.ContainsKey(key)) table[key] = t;
                else table.Add(key, t);
            }

            public void FromString(string text)
            {
                text = text.Substring(Prefix.Length);
                var data = text.Split(SPR);
                foreach (var d in data)
                {
                    var e = d.Split(DIV);
                    if (e.Length < 2) continue;
                    SetValue(e[0], e[1]);
                }
            }

            public override string ToString()
            {
                var text = new StringBuilder(Prefix);

                foreach (var d in table)
                {
                    text.Append(d.Key);
                    text.Append(DIV);
                    text.Append(d.Value);
                    text.Append(SPR);
                }

                return text.ToString();
            }
        }
    }
}