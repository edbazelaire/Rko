using Enums;
using MyBox;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "ArenaAchievementData", menuName = "Game/Achievements/Arena_Achievement/Default")]
    public class ArenaAchievementData : AchievementData<SArenaAchievementSubData>
    {
        #region Members

        [SerializeField]
        protected EArenaType        m_ArenaType;
        [SerializeField]
        protected EArenaDifficulty  m_ArenaDifficulty;
        [SerializeField]
        public List<EArenaMod>      m_ArenaMods;
        [SerializeField]
        public int                  m_MinExtraDifficulty;

        public override string ID => m_ArenaType.ToString() + base.ID;

        #endregion


        #region Check

        public virtual bool Check(EGameMode gameMode, EGameResult gameResult, EArenaType arenaType, EArenaDifficulty arenaDifficulty, int extraDifficulty, List<EArenaMod> arenaMods)
        {
            if (gameMode != EGameMode.Arena)
                return false;

            if (Current == null)
                return false;

            // CHECK : BASE requirements of the Achievements
            if (! CheckRequirements(arenaType, arenaDifficulty, extraDifficulty, arenaMods))
                return false;

            // CHECK : Specific requirements of each achievement thresholds from the current index 
            bool test = false;
            for (int i = CurrentIndex; i < AchievementSubData.Count; i++)
            {
                if (! AchievementSubData[i].Check(arenaType, arenaDifficulty, extraDifficulty, arenaMods))
                    break;

                IncreaseAtIndex(i);
                test = true;
            }

            return test;
        }

        protected virtual bool CheckRequirements(EArenaType arenaType, EArenaDifficulty arenaDifficulty, int extraDifficulty, List<EArenaMod> arenaMods)
        {
            if (arenaType != EArenaType.None && arenaType != m_ArenaType)
                return false;

            if (arenaDifficulty < m_ArenaDifficulty)
                return false;

            if (extraDifficulty < m_MinExtraDifficulty)
                return false;

            if (m_ArenaMods.IsNullOrEmpty())
                return true;

            foreach (EArenaMod mod in m_ArenaMods)
            {
                if (!arenaMods.Contains(mod))
                    return false;
            }

            return true;
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            var description = base.GetDescription();

            description += $"Finish the <i>{m_ArenaType}</i> in difficulty at least <b>{(EArenaDifficulty)(Current.MaxValue - 1)}</b>";
            if (!m_ArenaMods.IsNullOrEmpty())
                description += $" in <b>{String.Join(", ", m_ArenaMods)}</b> mod";
            if (m_MinExtraDifficulty > 0)
                description += $" with at least +{m_MinExtraDifficulty} extra difficulty";

            return CleanDescription(description);
        }

        #endregion
    }
}