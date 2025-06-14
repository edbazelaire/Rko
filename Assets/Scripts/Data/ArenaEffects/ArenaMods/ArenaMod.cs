using System;
using Tools;
using UnityEngine;

namespace Data.ArenaEffects.ArenaMods
{
    [Serializable, CreateAssetMenu(fileName = "ArenaMod", menuName = "Game/ArenaMods/ArenaMod")]
    public class ArenaMod : ScriptableObject
    {
        #region Members

        [SerializeField]
        protected string m_Description;
        [SerializeField]
        protected float m_BonusPower;

        public float BonusPower => m_BonusPower;

        #endregion


        #region Activate

        public void Activate() { }

        #endregion


        #region Description
        
        public string GetDescription()
        {
            return TextHandler.ReplaceStateEffectTokens(m_Description);
        }

        #endregion
    }
}

