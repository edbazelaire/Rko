using Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures.SpellRequirement
{
    [Serializable]
    public class SpellRequirements
    {
        #region Members

        [SerializeField, Description("List of state effects and stacks required to apply spell or effect")] 
        protected List<SStateEffectRequirement>   m_StateEffectRequirements;

        protected int m_Level;

        public List<SStateEffectRequirement> StateEffectRequirements => m_StateEffectRequirements;

        public List<SRequirement> Requirements {
            get
            {
                var requirements = new List<SRequirement>();
                requirements.AddRange(m_StateEffectRequirements);
                return requirements;
            }
            
        }

        #endregion


        #region Level

        public void SetLevel(int level)
        {
            m_Level = level;

            for (int i = 0; i < m_StateEffectRequirements.Count; i++)
            {
                m_StateEffectRequirements[i].SetLevel(level);
            }
        }

        #endregion



        #region Checkers

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public bool CheckRequirement(Controller caster, Controller targetController)
        {
            foreach (var requirement in Requirements)
            {
                if (! requirement.CheckRequirement(requirement.CalculateTarget(caster, targetController)))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Check that SpellRequirements are met
        /// </summary>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public bool TryApplyRequirements(Controller caster, Controller targetController)
        {
            foreach (var requirement in Requirements)
            {
                if (! requirement.TryApplyRequirements(requirement.CalculateTarget(caster, targetController)))
                    return false;
            }

            return true;
        }

        #endregion


        #region Info & Description

        public string GetDescription()
        {
            var description = "";
            foreach (var requirement in Requirements)
            {
                description += requirement.GetDescription();
            }

            return TextHandler.ReplaceStateEffectTokens(description);
        }

        #endregion
    }
}