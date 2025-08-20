using Enums;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "GreatIceLancePassive", menuName = "Game/StateEffects/SpecialEffects/Passives/GreatIceLancePassive")]
    public class GreatIceLancePassive : StateEffect
    {
        [SerializeField] protected float m_CooldownReduction;

        protected override void OnRefreshed(int stacks)
        {
            base.OnRefreshed(stacks);

            Debug.Log("GreatIceLancePassive - OnRefreshed() : " + stacks);

            m_Controller.SpellHandler.ReduceCooldown(ESpell.GreatIceLance, stacks * m_CooldownReduction);
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