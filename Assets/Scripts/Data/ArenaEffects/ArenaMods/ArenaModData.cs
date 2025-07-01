using Enums;
using MyBox;
using System;
using Tools;
using UnityEngine;

namespace Data.ArenaEffects.ArenaMods
{
    [Serializable, CreateAssetMenu(fileName = "ArenaModData", menuName = "Game/ArenaMods/ArenaMod")]
    public class ArenaModData : ScriptableObject
    {
        #region Members

        [SerializeField]
        protected string m_Description;
        [SerializeField]
        protected string m_RewardDescription;
        [SerializeField]
        protected float m_BonusPower;

        public float BonusPower => m_BonusPower;
        public EArenaMod ArenaMod => Enum.TryParse(name, out EArenaMod arenaMod) ? arenaMod : EArenaMod.None;

        #endregion


        #region Activate

        public void Activate() { }

        #endregion


        #region Description
        
        public string GetRewardDescription()
        {
            if (m_RewardDescription.IsNullOrEmpty())
                return $"Gain +{Mathf.Round(m_BonusPower * 100)}% bonus rewards on your orb at the end of the Arena";
                
            return m_RewardDescription;
        }
        
        public string GetDescription()
        {
            return TextHandler.ReplaceStateEffectTokens(m_Description);
        }

        #endregion
    }
}

