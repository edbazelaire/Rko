using Enums;
using MyBox;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "GameAchievementData", menuName = "Game/Achievements/Analytics/GameAchievementData")]
    public class GameAchievementData : AchievementData
    {
        #region Members

        [SerializeField]
        protected List<EGameMode> m_GameModes; 
        [SerializeField]
        protected bool m_Win;

        #endregion


        #region Check

        /// <summary>
        /// Use end game analytics data (GameAnalyticsManager) to check end game achievements
        /// </summary>
        /// <param name="spellHitSummary"></param>
        /// <param name="specialValues"></param>
        /// <param name="spellHitTypeDatas"></param>
        public virtual void Check(EGameMode gameMode, bool win)
        {
            // CHECK : correct game mode
            if (!m_GameModes.IsNullOrEmpty() && !m_GameModes.Contains(gameMode))
                return;

            if (m_Win && !win)
                return;

            Increase(1);
        }

        #endregion
    }
}