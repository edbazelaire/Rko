using Data.DataStructures;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "TriggerRune", menuName = "Game/Runes/TriggerRune")]
    public class TriggerRune : RuneData
    {
        #region Members

        [SerializeField]
        protected List<STriggerEffect> m_TriggerEffects;

        public List<STriggerEffect> TriggerEffects => m_TriggerEffects;

        #endregion
    }
}