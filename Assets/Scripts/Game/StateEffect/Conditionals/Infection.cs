using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Infection", menuName = "Game/StateEffects/ConditionalEffects/Infection")]
    public class Infection : StateEffect
    {
        protected override bool CheckBeforeGraphicInit()
        {
            base.CheckBeforeGraphicInit();

            if (m_Controller.StateHandler.GetStacks(EStateEffect.Poison) < 10)
                return false;

            return true;
        }

        protected override void OnStart()
        {
            base.OnStart();

            m_Controller.StateHandler.AddStateEffect(new SStateEffectData(EStateEffect.Infected, m_Stacks), m_Caster, m_Level, m_Origin);
        }
    }
}