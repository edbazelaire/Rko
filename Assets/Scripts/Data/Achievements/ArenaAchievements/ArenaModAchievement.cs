using Data.ArenaEffects.ArenaMods;
using Enums;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "ArenaModAchievement", menuName = "Game/Achievements/Arena_Achievement/ArenaModAchievement")]
    public class ArenaModAchievement : ArenaAchievementData
    {
        #region Members

        public List<EArenaMod> ArenaMods;
        public int MinExtraDifficulty;

        #endregion


        #region Check

        public override void CheckOnArenaEnded(bool win)
        {
            base.CheckOnArenaEnded(win);

            if (! win)
                return;

            if (ProgressionCloudData.CurrentArena.GetExtraDifficulty() < MinExtraDifficulty)
                return;

            foreach (var mod in ArenaMods)
            {
                if (!ProgressionCloudData.CurrentArena.HasMod(mod))
                    return;
            }

            OnCheckValidated();
        }

        public virtual void OnCheckValidated()
        {
            int count = (int)ProgressionCloudData.CurrentArena.GetArenaDifficulty() + 1;
            if (count >= GetCount())
                UpdateCount(count);
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            var description = $"Finish the <i>{m_ArenaType}</i> in difficulty at least <b>{(EArenaDifficulty)(Current.MaxValue - 1)}</b>";
            if (!ArenaMods.IsNullOrEmpty())
                description += $" in <b>{String.Join(", ", ArenaMods)}</b> mod";   
            if (MinExtraDifficulty > 0)
                description += $" with at least +{MinExtraDifficulty} extra difficulty";

            return CleanDescription(description);
        }

        #endregion
    }
}