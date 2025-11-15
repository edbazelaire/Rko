using Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "SpellPassive", menuName = "Game/StateEffects/SpecialEffects/Passives/SpellPassive")]
    public class SpellPassive : StateEffect
    {
        [SerializeField] protected float m_CooldownReduction;
        [SerializeField] protected List<ESpell> m_SpellsAffected;

        protected override void OnRefreshed(int stacks)
        {
            base.OnRefreshed(stacks);
            foreach (var spell in m_SpellsAffected)
            {
                ApplyEffect(spell, stacks);
            }
        }

        protected virtual void ApplyEffect(ESpell spell, int stacks)
        {
            if (! m_Controller.SpellHandler.Spells.Contains(spell) || m_CooldownReduction <= 0)
                return;
            m_Controller.SpellHandler.ReduceCooldown(spell, stacks * m_CooldownReduction);
        }

        public override bool TryGetSpecialPropertyValue(string propertyName, out object value)
        {
            value = null;
            if (propertyName == "CooldownReduction")
            {
                value = m_CooldownReduction;
                return true;
            }

            return false;
        }
    }
}