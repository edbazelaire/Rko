using Enums;
using Game.Loaders;
using Save;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;


namespace Data
{
    [CreateAssetMenu(fileName = "BuffRune", menuName = "Game/Runes/Buff")]
    public class BuffRune : RuneData
    {
        #region Members

        [SerializeField] 
        protected List<EStateEffectProperty> m_DescriptionVariables = new List<EStateEffectProperty>();

        [SerializeField]
        protected List<SCharacterStatScaling> m_BonusStats = new List<SCharacterStatScaling>();

        public List<SCharacterStatScaling> BonusStats => m_BonusStats;

        #endregion


        #region Infos

        float GetProperty(EStateEffectProperty property)
        {
            foreach (var stat in m_BonusStats)
            {
                if (stat.StateEffectProperty == property)
                    return stat.BaseValue * Mathf.Pow(1 + stat.ScalingFactor, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);
            }

            ErrorHandler.Error("Unable to find any property " + property + " in " + name);
            return 0;  
        }

        #endregion
    }
}