using Tools;
using Unity.Netcode;

namespace Game.Character
{
    public class AutoAttackHandler : NetworkBehaviour
    {
        #region Members

        Controller m_Controller;

        bool m_IsMoving     = false;
        bool m_IsCasting    => m_Controller.SpellHandler.IsCasting;

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Controller.Movement.MoveX.OnValueChanged          += OnMovementValueChanged;
        }
        
        public void Activate(bool activate)
        {
            if (!activate)
                StopAllCoroutines();

            this.enabled = activate;
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!IsServer)
                return;

            if (!m_Controller.GameRunning)
                return;

            if (!CanCastAutoAttack)
                return;

            if (m_IsMoving || m_IsCasting)
            {
                ErrorHandler.Error("CanCastAutoAttack okayed but m_IsMoving and m_IsCasting not set to false");
                return;
            }

            m_Controller.SpellHandler.TryStartCastSpell(m_Controller.SpellHandler.AutoAttack);
        }


        bool CanCastAutoAttack
        {
            get
            {
                return m_Controller.Movement.MoveX.Value == 0
                    && ! m_Controller.SpellHandler.IsCasting;
            }
        }

        #endregion


        #region Listeners

        void OnMovementValueChanged(int old, int newValue)
        {
            m_IsMoving = newValue != 0;
        }

        #endregion
    }
}