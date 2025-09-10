using Enums;
using Save;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "NewArenaAchievementData", menuName = "Game/Achievements/Arena_Achievement/NewDefault")]
    public class NewArenaAchievementData : AchievementData
    {
        #region Members

        [SerializeField]
        protected EArenaType m_ArenaType;

        public override string ID => m_ArenaType.ToString() + base.ID;

        #endregion


        #region Check

        public virtual bool CheckOnGameEnded(EGameMode gameMode, EGameResult gameResult)
        {
            if (gameMode != EGameMode.Arena)
                return false;

            if (ProgressionCloudData.CurrentArena.ArenaType != m_ArenaType)
                return false;

            return true;
        }

        public virtual void CheckOnArenaEnded(bool win) { }

        #endregion
    }
}