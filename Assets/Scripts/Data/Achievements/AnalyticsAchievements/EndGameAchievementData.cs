using Enums;
using MyBox;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "GameAchievementData", menuName = "Game/Achievements/Analytics/GameAchievementData")]
    public class EndGameAchievementData : DefaultAchievementData
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
        /// <param name="gameMode"></param>
        /// <param name="win"></param>
        /// <param name="save"> if the counter increase, save directly to cloud ? (can be deactivated to save all achievements at once)</param>
        public virtual bool Check(EGameMode gameMode, bool win, bool save = false)
        {
            // CHECK : correct game mode
            if (!m_GameModes.IsNullOrEmpty() && !m_GameModes.Contains(gameMode))
                return false;

            if (m_Win && !win)
                return false;

            Increase(1, save);
            return true;
        }

        #endregion
    }
}