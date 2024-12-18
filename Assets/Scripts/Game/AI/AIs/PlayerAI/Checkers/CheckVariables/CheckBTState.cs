namespace AI.Checkers
{
    public class CheckBTState : BaseChecker
    {
        #region Members

        string  m_BehaviorState;

        #endregion


        #region Init & End

        public CheckBTState(Controller controller, string state) : base(controller) 
        {
            m_BehaviorState = state;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = m_Controller.BehaviorTree.State == m_BehaviorState ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}