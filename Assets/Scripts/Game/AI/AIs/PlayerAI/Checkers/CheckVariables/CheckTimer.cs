namespace AI.Checkers
{
    public class CheckTimer : BaseChecker
    {
        #region Members

        string  m_Id;
        float   m_Timer;

        #endregion


        #region Init & End

        public CheckTimer(Controller controller, string id, float timer) : base(controller) 
        {
            m_Id = id;
            m_Timer = timer;
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