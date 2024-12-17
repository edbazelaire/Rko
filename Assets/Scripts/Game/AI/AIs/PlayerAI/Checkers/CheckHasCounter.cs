using Tools;

namespace AI
{
    public class CheckHasCounter : BaseChecker
    {
        #region Init & End

        public CheckHasCounter(Controller controller, bool reversed = false) : base(controller, reversed) 
        {

        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            SetNodeState(m_Controller.CounterHandler.HasCounter.Value ? NodeState.SUCCESS : NodeState.FAILURE);

            ErrorHandler.Log("CheckHasCounter "+ (m_IsReversed ? "(reversed) " : "") +": " + m_State);

            return m_State;
        }

        #endregion
    }
}