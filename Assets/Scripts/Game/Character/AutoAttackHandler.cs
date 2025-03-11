using System;
using Tools;
using Unity.Netcode;
using UnityEngine;

namespace Game.Character
{
    public class AutoAttackHandler : NetworkBehaviour
    {
        #region Members

        public Action AutoAttackEvent;

        Controller m_Controller;
        float m_Interval = 0f;
        float m_IntervalTimer = 0f;

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Interval = 0f;
        }
        
        public void Activate(bool activate, float? interval = null)
        {
            if (!activate)
            {
                StopAllCoroutines();

                // deactivate but is currently casting autoattack
                if (m_Controller.SpellHandler.IsCasting && m_Controller.SpellHandler.SelectedSpell == m_Controller.SpellHandler.AutoAttack)
                    m_Controller.SpellHandler.CancelCast();
            }
            else
            {
                // set new interval
                if (interval != null)
                    m_Interval = interval.Value;
            }

            this.enabled = activate;
        }

        #endregion


        #region Update

        private void Update()
        {
            if (! IsServer)
                return;

            if (m_IntervalTimer > 0)
            {
                m_IntervalTimer -= Time.deltaTime;
                return;
            }

            if (! CanCastAutoAttack)
                return;

            bool success = m_Controller.SpellHandler.TryStartCastSpell(m_Controller.SpellHandler.AutoAttack);
            if (! success)
                return;

            m_IntervalTimer = m_Interval;
            AutoAttackEvent?.Invoke();
        }

        bool CanCastAutoAttack
        {
            get
            {
                if (! m_Controller.GameRunning)
                    return false;

                if (m_Controller.Movement.IsMoving)
                    return false;

                if (m_Controller.SpellHandler.IsCasting)
                    return false;

                return true;
            }
        }

        #endregion


        #region Listeners

        #endregion
    }
}