using Enums;
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

        public override string ID => m_ArenaType.ToString() + base.ID;

        #endregion


        #region Check

        public virtual bool Check(EGameMode gameMode, EGameResult gameResult, EArenaType arenaType, EArenaDifficulty arenaDifficulty, List<EArenaMod> arenaMods, bool save = false)
        {
            if (gameMode != EGameMode.Arena)
                return false;

            if (Current == null)
                return false;

            // CHECK : Specific requirements of each achievement thresholds from the current index 
            bool test = false;
            for (int i = CurrentIndex; i < AchievementSubData.Count; i++)
            {
                if (! AchievementSubData[i].Check(arenaType, arenaDifficulty, arenaMods))
                    break;

                IncreaseAtIndex(i, save: save);
                test = true;
            }

            return test;
        }

        #endregion


        #region Description

        public void OverrideSubAchievements()
        {
            foreach (var subAchievement in AchievementSubData)
            {
                subAchievement.Override(
                    arenaType:              m_ArenaType,
                    arenaDifficulty:        m_ArenaDifficulty,
                    arenaMods:              m_ArenaMods
                ); 
            }
        }

        public override string GetDescription()
        {
            var description = base.GetDescription();
            description += Current.GetDescription();
            return CleanDescription(description);
        }

        #endregion
    }
}