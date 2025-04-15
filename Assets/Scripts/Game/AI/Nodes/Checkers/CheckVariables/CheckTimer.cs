using System;

namespace AI.Checkers
{
    public class CheckTimer : BaseChecker
    {
        #region Members

        string      m_Id;
        Func<float> m_TimerMethod;

        float       m_Timer => m_TimerMethod.Invoke();

        #endregion


        #region Init & End

        public CheckTimer(Controller controller, string id, float timer) : base(controller) 
        {
            m_Id = id;
            m_TimerMethod = () => timer;
        }

        public CheckTimer(Controller controller, string id, Func<float> timerMethod) : base(controller) 
        {
            m_Id = id;
            m_TimerMethod = timerMethod;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = m_Controller.BehaviorTree.CheckTimer(m_Id, m_Timer) ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}