namespace AI.Checkers
{
    public class CheckCounter : BaseChecker
    {
        #region Members

        string  m_Id;
        int     m_MaxCounter;
        int     m_Counter;

        #endregion


        #region Init & End

        public CheckCounter(Controller controller, string id, int maxCounter, int counter = 0) : base(controller) 
        {
            m_Id            = id;
            m_MaxCounter    = maxCounter;
            m_Counter       = counter;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = m_Controller.BehaviorTree.CheckTimer(m_Id, m_MaxCounter) ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}