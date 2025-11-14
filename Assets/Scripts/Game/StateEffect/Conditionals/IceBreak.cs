using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "IceBreak", menuName = "Game/StateEffects/ConditionalEffects/IceBreak")]
    public class IceBreak : StateEffect
    {
        protected override int ApplyConsumeState(int stacks, Controller caster, Controller targetController)
        {
            // consume both Frostbite and Frozen
            int finalStacks = targetController.StateHandler.RemoveStateEffect(EStateEffect.Frostbite, consume: true);
            finalStacks += targetController.StateHandler.RemoveStateEffect(EStateEffect.Frozen, consume: true);

            // add DefaultState state (if any)
            if (finalStacks <= 0)
                targetController.StateHandler.AddStateEffect(m_DefaultState.ToString(), caster, m_Level, m_Origin);

            return finalStacks;
        }
    }
}