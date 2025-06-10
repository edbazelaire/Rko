using Data;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Scorched", menuName = "Game/StateEffects/Scorched")]
    public class Scorched : TickDamageEffect
    {
        /// <summary>
        /// Check that Frost state is applied, otherwise apply it instead of Frozen state
        /// </summary>
        /// <returns></returns>
        protected override bool CheckBeforeGraphicInit()
        {
            bool test = false;

            // can only apply to enemy with state "Burn"
            if (m_Controller.StateHandler.HasState(EStateEffect.Burn))
            {
                // if already has burn, then
                test = true;
            }

            // re-apply burn effect anyway
            m_Controller.StateHandler.AddStateEffect(EStateEffect.Burn.ToString(), m_Caster, m_Level, m_Origin);

            return test;
        }
    }
}