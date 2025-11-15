using System.Collections.Generic;
using Tools;

namespace AI
{
    public enum EGlobalState
    {
        None,
        Casting,
        Stun,
        Silence,
        Airborne,
    }

    public class CheckState : BaseChecker
    {
        #region Global States

        List<EGlobalState> m_GlobalStates;
        bool m_Allowed;

        #endregion


        #region Init & End

        public CheckState(Controller controller, List<EGlobalState> globalStates, bool allowed = true) : base(controller)
        {
            m_GlobalStates = globalStates;
            m_Allowed = allowed;
        }

        public CheckState(Controller controller, EGlobalState globalState, bool allowed = true) : base(controller) 
        {
            m_GlobalStates = new List<EGlobalState>() { globalState };
            m_Allowed = allowed;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            foreach (var state in m_GlobalStates)
            {
                if (CheckHasState(state))
                {
                    m_State = m_Allowed ? NodeState.SUCCESS : NodeState.FAILURE;
                    return m_State;
                }
            }

            // if the states we are looking for were not allowed, this is a SUCCESS
            m_State = m_Allowed ? NodeState.FAILURE : NodeState.SUCCESS;
            return m_State;
        }


        public bool CheckHasState(EGlobalState state)
        {
            switch(state)
            {
                case EGlobalState.None:
                    return true;

                case EGlobalState.Casting:
                    return m_Controller.SpellHandler.IsCasting;

                case EGlobalState.Stun:
                    return m_Controller.StateHandler.IsStunned;

                case EGlobalState.Airborne:
                    return m_Controller.StateHandler.IsAirborned;

                case EGlobalState.Silence:
                    return m_Controller.StateHandler.IsSilenced;

                default:
                    ErrorHandler.Error("Unhandled case");
                    return false;
            }
        }

        #endregion
    }
}