using Enums;
using Game.StateEffects.Quests;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "HardSteel", menuName = "Game/StateEffects/SpecialEffects/PowerUps/HardSteel")]
    public class HardSteel : ArenaQuestEffect
    {
        #region Members

        [SerializeField] protected int m_DamageThreshold = 1000;

        int m_DamageCounter;

        #endregion


        #region Init & End

        protected override void OnStart()
        {
            base.OnStart();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Controller.Life.OnHittedEvent += OnHit;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller != null)
                m_Controller.Life.OnHittedEvent -= OnHit;
        }

        void OnHit(int damage, ulong casterId, EDamageCategory damageCategory, EHitCategory spellCategory)
        {
            if (spellCategory != EHitCategory.Direct)
                return;

            if (damageCategory != EDamageCategory.Physical)
                return;

            m_DamageCounter += damage;
            while (m_DamageCounter >= m_DamageThreshold)
            {
                m_DamageCounter -= m_DamageThreshold;
                Refresh(stacks: 1);
            }
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            string description =  base.GetDescription();
            description = description.Replace("[DamageThreshold]", m_DamageThreshold.ToString());
            return description;
        }

        #endregion
    }
}