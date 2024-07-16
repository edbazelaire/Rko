using Enums;
using NUnit.Framework.Internal;
using UnityEngine;

namespace Game.Spells
{
    public class Torment : Aoe
    {
        const int   DAMAGES_PER_STACKS = 5;
        const int   ENERGY_PER_STACKS = 2;

        #region Protected Manipulators
        
        protected override void ApplyEnemyStateEffects(Controller targetController)
        {
            // if has no cursed effect, apply it 
            if (! targetController.StateHandler.HasState(EStateEffect.Cursed))
            {
                base.ApplyEnemyStateEffects(targetController);
                return;
            }

            // if has cursed effects consumme them to apply damages and stung
            int nStacks = targetController.StateHandler.RemoveStateEffect(EStateEffect.Cursed);
            targetController.Life.Hit(nStacks * DAMAGES_PER_STACKS);
            m_Controller.EnergyHandler.AddEnergy(nStacks * ENERGY_PER_STACKS);

            // stun
            targetController.StateHandler.AddStateEffect(EStateEffect.Stun, m_Controller, duration: 2f);
        }

        #endregion
    }
}