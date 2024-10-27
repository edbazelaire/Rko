using Enums;
using Game;
using System.Collections.Generic;

namespace AI
{
    public class CheckHasCounter : BaseChecker
    {
        #region Init & End

        public CheckHasCounter(Controller controller) : base(controller) 
        {
           
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = m_Controller.CounterHandler.HasCounter.Value ? NodeState.SUCCESS : NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}