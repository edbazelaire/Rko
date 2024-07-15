using Enums;
using System.ComponentModel;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "AutoAttackRune", menuName = "Game/Runes/AutoAttack")]
    public class AutoAttackRune : RuneData
    {
        [Description("State effect applying on activation")]
        public EStateEffect StateEffect;

        public void ApplyOnHit(ref Controller controller, Controller caster)
        {
            controller.StateHandler.AddStateEffect(StateEffect, caster, m_Level);
        }
    }
}