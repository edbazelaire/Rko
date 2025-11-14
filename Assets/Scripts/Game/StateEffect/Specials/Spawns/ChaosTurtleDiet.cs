using Data;
using Enums;
using System.Runtime.Remoting.Messaging;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "ChaosTurtleDiet", menuName = "Game/StateEffects/SpecialEffects/ChaosTurtleDiet")]
    public class ChaosTurtleDiet : StateEffect
    {
        #region Members

        [SerializeField] SpellData m_CounterProjectile;
        [SerializeField] int m_EnergyGainOnDeath;

        Vector3 m_BaseSize;

        #endregion


        #region Stacks Management

        protected override void OnApplied(int stacks)
        {
            m_BaseSize = m_Controller.transform.localScale;

            base.OnApplied(stacks);
        }

        protected override void OnRefreshed(int stacks)
        {
            base.OnRefreshed(stacks);

            if (m_Controller == null)
            {
                ForceEnd(false);
                return;
            }

            m_Controller.transform.localScale = m_BaseSize * (1 + m_Stacks * 0.01f);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            Controller.OnDeathEvent += OnDeathEvent;
            m_Controller.Life.OnHittedEvent += OnHit;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            Controller.OnDeathEvent         -= OnDeathEvent;

            if (m_Controller != null) 
                m_Controller.Life.OnHittedEvent -= OnHit;
        }

        void OnDeathEvent(Controller controller)
        {
            if (controller.Team != m_Controller.Team)
                return;

            Refresh(m_EnergyGainOnDeath);
        }

        void OnHit(int damage, ulong casterId, EDamageCategory damageCategory, EHitCategory spellCategory)
        {
            if (spellCategory != EHitCategory.Direct)
                return;

            var target = GameManager.Instance.GetPlayer(casterId);
            if (target.StateHandler.IsUnTargetable)
                return;

            var targetPos = target.transform.position;
            targetPos.y = 0;

            m_CounterProjectile.Cast(
                m_Controller.PlayerId,
                target: targetPos,
                recalculateTarget: false
            );
        }

        #endregion


        #region Description

        public override string GetDescription()
        {
            return base.GetDescription().Replace("[EnergyGainOnDeath]", m_EnergyGainOnDeath.ToString());
        }

        #endregion
    }
}