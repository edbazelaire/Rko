using System.Collections.Generic;
using UnityEngine;


namespace Data
{
    [CreateAssetMenu(fileName = "BuffRune", menuName = "Game/Runes/Buff")]
    public class BuffRune : RuneData
    {
        #region Members

        [SerializeField]
        protected List<SCharacterStatScaling> m_BonusStats = new List<SCharacterStatScaling>();

        public List<SCharacterStatScaling> BonusStats => m_BonusStats;

        #endregion


        #region Update



        #endregion
    }
}