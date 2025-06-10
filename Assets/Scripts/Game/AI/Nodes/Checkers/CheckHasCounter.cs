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
            SetNodeState(m_Controller.CounterHandler.HasCounter ? NodeState.SUCCESS : NodeState.FAILURE);
            return m_State;
        }

        #endregion
    }
}