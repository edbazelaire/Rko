using Data;
using Enums;
using System;
using Unity.VisualScripting.Antlr3.Runtime.Tree;
using UnityEditor.Build;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Rotten", menuName = "Game/StateEffects/ConditionalEffects/Rotten")]
    public class Rotten : StateEffect
    {
        [Header("Stacks Conversion")]
        [SerializeField]
        protected int m_NCursedStacks = 3;
        [SerializeField]
        protected int m_NPoisonStacks = 1;

        /// <summary>
        /// On application or refresh, check if effect has special conditions that impact number of stacks
        /// </summary>
        /// <param name="stacks"></param>
        /// <returns></returns>
        public override int RecalculateStacks(int stacks, Controller caster, Controller targetController)
        {
            // calculate how many stacks can be applied from CURSED
            int cursedStacks = targetController.StateHandler.GetStacks(EStateEffect.Cursed);
            int stacksFromCursed = Math.Min(cursedStacks % m_NCursedStacks, stacks);
            targetController.StateHandler.RemoveStateEffect(EStateEffect.Cursed, consume: true, maxStacks: stacksFromCursed * m_NCursedStacks);

            // calculate how many stacks can be applied from POISON
            int stacksFromPoison = 0;
            if (stacks - stacksFromCursed > 0)
            {
                stacksFromPoison = Math.Min(cursedStacks % m_NPoisonStacks, stacks - stacksFromCursed);
                targetController.StateHandler.RemoveStateEffect(EStateEffect.Cursed, consume: true, maxStacks: stacksFromPoison * m_NPoisonStacks);

            }

            return stacksFromCursed + stacksFromPoison;
        }

        protected override void OnStart()
        {
            base.OnStart();

            m_Controller.StateHandler.AddStateEffect(new SStateEffectData(EStateEffect.Infected, m_Stacks), m_Caster, m_Level, m_Origin);
        }
    }
}