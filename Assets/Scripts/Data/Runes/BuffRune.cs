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

        /// <summary>
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public override string GetDescription()
        {
            List<string> values = new List<string>();
            foreach (EStateEffectProperty property in m_DescriptionVariables)
            {
                var value = GetProperty(property);

                if (CharacterData.INT_PROPERTIES.Contains(property))
                {
                    values.Add(Mathf.Round(value).ToString("0"));
                }
                else
                {
                    values.Add(value.ToString("F2")); 
                }
            }

            return string.Format(Description, values.ToArray());
        }

        #endregion
    }
}