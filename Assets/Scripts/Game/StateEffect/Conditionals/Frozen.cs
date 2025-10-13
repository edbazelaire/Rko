using Data;
using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Frozen", menuName = "Game/StateEffects/ConditionalEffects/Frozen")]
    public class Frozen : StateEffect
    {
        protected override int ApplyConsumeState(int stacks, Controller caster, Controller targetController)
        {
            // refresh potential Frostbite effects
            if (targetController.StateHandler.HasState(EStateEffect.Frostbite))
                targetController.StateHandler.AddStateEffect(EStateEffect.Frostbite, caster, m_Origin, stacks: 0);

            stacks = base.ApplyConsumeState(stacks, caster, targetController);
            if (stacks > 0 && targetController.StateHandler.IsUncontrollable)
                targetController.StateHandler.AddStateEffect(EStateEffect.Frostbite, caster, m_Origin, stacks: 1, force: true);

            return stacks;
        }
    }
}