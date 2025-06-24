using Data.ArenaEffects.ArenaMods;
using Enums;
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

        #endregion


        #region Check

        public override void CheckOnArenaEnded(bool win)
        {
            Debug.Log("CheckOnArenaEnded() : " + win);

            base.CheckOnArenaEnded(win);

            if (! win)
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
            return CleanDescription($"Finish the <i>{m_ArenaType}</i> in difficulty at least <b>{(EArenaDifficulty)(Current.Value.MaxValue - 1)}</b> in <b>{String.Join(", ", ArenaMods)}</b> mod");
        }

        #endregion
    }
}