using Assets.Scripts.Data.DataStructures.Common;
using Enums;
using System;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ToughLove", menuName = "Game/StateEffects/SpecialEffects/ToughLove")]
    public class ToughLove : StateEffect
    {
        #region Members

        [SerializeField, Tooltip("Percentage of healing converted")]
        protected SScalingStat m_HealConversion;

        #endregion


        #region Level

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_HealConversion.SetLevel(level);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_Controller != null)
                m_Controller.Life.OnHealedEvent += OnHealedEvent;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_Controller != null)
                m_Controller.Life.OnHealedEvent -= OnHealedEvent;
        }

        private void OnHealedEvent(int heal, ulong casterId)
        {
            // convert heal to damages
            GameManager.Instance.GetFirstEnemy(m_Controller.Team).Life.Hit(
                damage:         (int)Math.Round(heal * m_HealConversion.GetValue()),
                casterId:       m_Controller.PlayerId,
                source:         m_Parent,
                spellCategory:  EHitCategory.Tick,
                ignoreRes:      false
            );
        }

        #endregion


        #region Info

        public override string GetDescription()
        {
            string description = base.GetDescription();
            description = description.Replace("[HealConversion]", Mathf.Round(100 * m_HealConversion.GetValue()).ToString() + "%");
            return description;
        }

        #endregion
    }
}