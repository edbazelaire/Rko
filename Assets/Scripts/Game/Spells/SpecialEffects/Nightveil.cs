using Tools;
using UnityEngine;

namespace Game.Spells.SpecialEffects
{
    public class Nightveil : SpecialEffect
    {
        #region Members

        [SerializeField] protected int m_BonusShield = 250;
        [SerializeField] protected int m_HitShield = 150;

        Counter m_NightVeilSpell;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_NightVeilSpell = Finder.FindComponent<Counter>(gameObject);
        }


        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            Spell.OnSpellSpawn += OnSpellSpawn;
        }

        protected override void UnRegisterListeners()
        {
            base.RegisterListeners();

            Spell.OnSpellSpawn -= OnSpellSpawn;
        }

        void OnSpellSpawn(Spell spell)
        {
            if (spell.SpellData.Name.EndsWith("VoidMineAoe"))
                m_NightVeilSpell.AddShield(m_BonusShield);

            if (spell.SpellData.Name.EndsWith("VoidMine"))
                m_NightVeilSpell.HitShield(m_BonusShield);
        }



        #endregion
    }
}