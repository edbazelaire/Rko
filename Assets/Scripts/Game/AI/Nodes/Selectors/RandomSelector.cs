using System;
using System.Collections.Generic;

namespace AI
{
    public class RandomSelector : Selector
    {
        #region Members

        protected Node m_SelectedNode = null;

        #endregion


        #region Core

        public RandomSelector(List<Node> nodes, bool saveCurrentNode, Func<float> weight = null) : base(nodes, saveCurrentNode, false, weight)
        {
            
        }

        public override NodeState Evaluate()
        {
            // If no node already selected or current node is not running - select new node
            if (m_SelectedNode == null || m_State != NodeState.RUNNING)
                SelectNode();

            var state = m_SelectedNode != null ? m_SelectedNode.Evaluate() : NodeState.FAILURE;
            SetNodeState(state);
            return m_State;
        }

        protected virtual void SelectNode() { }

        public override void Reset()
        {
            base.Reset();
            m_SelectedNode = null;
        }

        #endregion
    }
}
