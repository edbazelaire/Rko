namespace AI.Checkers
{
    public class CheckPhase : BaseChecker
    {
        #region Members

        int  m_Phase;

        #endregion


        #region Init & End

        public CheckPhase(Controller controller, int phase) : base(controller) 
        {
            m_Phase = phase;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = m_Controller.BehaviorTree.Phase == m_Phase ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}