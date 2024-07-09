using System.Collections;
using Tools;
using UnityEngine;

namespace AI
{
    public class BaseChecker : BaseNode
    {
        #region Init & End

        public BaseChecker(Controller controller) : base(controller) { }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.FAILURE;
            return m_State;
        }

        #endregion
    }
}