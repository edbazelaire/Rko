using System.Collections;
using Tools;
using UnityEngine;

namespace AI
{
    public class BaseChecker : BaseNode
    {
        #region Members

        protected bool m_IsReversed;

        #endregion


        #region Init & End

        public BaseChecker(Controller controller, bool reversed = false) : base(controller) 
        { 
            m_IsReversed = reversed;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            SetNodeState(NodeState.FAILURE);
            return m_State;
        }

        public override void SetNodeState(NodeState state)
        {
            if (m_IsReversed)
            {
                if (state == NodeState.FAILURE)
                    state = NodeState.SUCCESS;

                else if (state == NodeState.SUCCESS)
                    state = NodeState.FAILURE;
            }

            base.SetNodeState(state);
        }

        #endregion
    }
}