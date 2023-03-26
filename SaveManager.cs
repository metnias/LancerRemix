using LancerRemix.Cat;
using Menu.Remix;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
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

            On.Menu.SlugcatSelectMenu.MineForSaveData += MineForLancerSaveData;

            On.RainWorldGame.Win += WinPlanterProcess;
            On.SaveState.SessionEnded += SessionEndedPatch;
        }

        public static readonly string Prefix = $"<Data_{LancerPlugin.PLUGIN_ID}>";

        #region InitialSave

        private static SaveDataTable CreateNewProgSaveData()
        {
            var prog = new SaveDataTable();
            return prog;
        }

        private static SaveDataTable CreateNewPersSaveData()
        {
            var pers = new SaveDataTable();
            return pers;
        }

        private static SaveDataTable CreateNewMiscSaveData()
        {
            var misc = new SaveDataTable();
            return misc;
        }

        #endregion InitialSave

        #region ProgData

        private static ConditionalWeakTable<SaveState, SaveDataTable> progDataTable;

        private static void ProgDataCtorPatch(On.SaveState.orig_ctor orig, SaveState self, SlugcatStats.Name saveStateNumber, PlayerProgression progression)
        {
            orig(self, saveStateNumber, progression);

            if (!ModifyCat.IsLancer(saveStateNumber)) return;

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

            if (!ModifyCat.IsLancer(slugcat)) return;

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

        private static SaveGameData MineForLancerSaveData(On.Menu.SlugcatSelectMenu.orig_MineForSaveData orig, ProcessManager manager, SlugcatStats.Name slugcat)
        {
            var data = orig(manager, slugcat);
            if (ModifyCat.IsLancer(slugcat))
            {
                /*
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
                            //cyclePenalty = int.Parse(res[0].data);
                        }
                    }
                }*/
            }

            return data;
        }

        #endregion SaveGameData

        private static void WinPlanterProcess(On.RainWorldGame.orig_Win orig, RainWorldGame self, bool malnourished)
        {
            if (ModifyCat.IsLancer(self.StoryCharacter))
            {
                var state = self.GetStorySession.saveState;
                Player lancer = null;
                foreach (var p in self.Players)
                {
                    lancer = p.realizedCreature as Player;
                    if (lancer.slugcatStats.name == self.StoryCharacter) break;
                }
                if (lancer.slugcatStats.name != self.StoryCharacter) goto NoLancer;

                //if (state.dreamsState != null) DreamHandler.PlanterCheckEventDream(state, target);
                orig(self, malnourished);

                return;
            }
        NoLancer: orig(self, malnourished);
        }

        private static void SessionEndedPatch(On.SaveState.orig_SessionEnded orig, SaveState self,
            RainWorldGame game, bool survived, bool newMalnourished)
        {
            if (ModifyCat.IsLancer(self.saveStateNumber))
            {
                Player lancer = null;
                foreach (var p in game.Players)
                {
                    lancer = p.realizedCreature as Player;
                    if (lancer.slugcatStats.name == self.saveStateNumber) break;
                }
                if (lancer.slugcatStats.name != self.saveStateNumber) goto NoLancer;

                if (survived)
                {
                }
                orig(self, game, survived, newMalnourished);
                return;
            }
        NoLancer:
            orig(self, game, survived, newMalnourished);
        }

        private static void UpdatePersSaveData(DeathPersistentSaveData self, ref SaveDataTable persData, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            //if (saveAsIfPlayerDied && self.reinforcedKarma)
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