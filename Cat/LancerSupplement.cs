using CatSub.Cat;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SlugName = SlugcatStats.Name;
using static LancerRemix.LancerEnums;
using CatSub.Story;

namespace LancerRemix.Cat
{
    internal class LancerSupplement : CatSupplement, IAmLancer
    {
        public LancerSupplement(Player player) : base(player)
        {
            lancer = player.SlugCatClass;
            player.slugcatStats.name = GetBasis(lancer);
            if (SubRegistry.TryMakeSub(player, out CatSupplement sub))
                basisSub = sub;
            player.SlugCatClass = lancer; // for lancerdeco
        }

        internal readonly SlugName lancer;
        private readonly CatSupplement basisSub = null;

        public LancerSupplement() : base() { }

        public override void Update(On.Player.orig_Update orig, bool eu)
        {
            if (basisSub != null) basisSub.Update(orig, eu);
            else base.Update(orig, eu);
        }

        public override void Destroy(On.Player.orig_Destroy orig)
        {
            if (basisSub != null) basisSub.Destroy(orig);
            else base.Destroy(orig);
        }

        public override SaveDataTable AppendNewProgSaveData()
        {
            SaveDataTable prog;
            if (basisSub != null) prog = basisSub.AppendNewProgSaveData();
            else prog = base.AppendNewProgSaveData();
            return prog;
        }

        public override SaveDataTable AppendNewPersSaveData()
        {
            SaveDataTable pers;
            if (basisSub != null) pers = basisSub.AppendNewPersSaveData();
            else pers = base.AppendNewPersSaveData();
            return pers;
        }

        public override void UpdatePersSaveData(ref SaveDataTable table, DeathPersistentSaveData data, bool saveAsIfPlayerDied, bool saveAsIfPlayerQuit)
        {
            if (basisSub != null) basisSub.UpdatePersSaveData(ref table, data, saveAsIfPlayerDied, saveAsIfPlayerQuit);
            base.UpdatePersSaveData(ref table, data, saveAsIfPlayerDied, saveAsIfPlayerQuit);
        }

        public override SaveDataTable AppendNewMiscSaveData()
        {
            SaveDataTable misc;
            if (basisSub != null) misc = basisSub.AppendNewMiscSaveData();
            else misc = base.AppendNewMiscSaveData();
            return misc;
        }

    }

    internal interface IAmLancer
    {

    }
}