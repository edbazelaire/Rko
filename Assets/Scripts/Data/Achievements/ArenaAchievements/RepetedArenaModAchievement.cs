using Enums;
using Save;
using System;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "RepetedArenaModAchievement", menuName = "Game/Achievements/Arena_Achievement/RepetedArenaModAchievement")]
    public class RepetedArenaModAchievement : ArenaModAchievement
    {
        #region Members

        EArenaDifficulty m_RequiredArenaDifficulty => (EArenaDifficulty)CurrentIndex;

        #endregion


        #region Check

        public override void OnCheckValidated()
        {
            // check is min required arena difficulty
            if (ProgressionCloudData.CurrentArena.GetArenaDifficulty() >= m_RequiredArenaDifficulty)
                UpdateCount(GetCount() + 1);
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            return CleanDescription($"Finish {Current.Value.MaxValue} times the <i>{m_ArenaType}</i> in difficulty at least <b>{m_RequiredArenaDifficulty}</b> in <b>{String.Join(", ", ArenaMods)}</b> mod");
        }

        #endregion
    }
}