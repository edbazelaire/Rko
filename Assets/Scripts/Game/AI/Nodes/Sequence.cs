using System;
using System.Collections.Generic;

namespace AI
{
    public class Sequence : Node
    {
        protected bool m_SaveCurrentNode = false;

        public Sequence(List<Node> nodes, bool saveCurrentNode = false, Func<float> weight = null) : base(nodes, weight) 
        {
            m_SaveCurrentNode = saveCurrentNode;
        }

        public override NodeState Evaluate()
        {
            base.Evaluate();

            bool done = false;

            foreach (Node node in m_Children)
            {
                if (!node.IsActivated)
                    continue;

                if (m_CurrentNode != null && node != m_CurrentNode)
                    continue;

                // Process over - set all remaining nodes to FAILURE (reset them)
                if (done)
                {
                    node.Reset();
                    continue;
                }

                // Evaluate current node
                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        m_State = NodeState.FAILURE;
                        m_CurrentNode = null;
                        done = true;
                        break;

                    case NodeState.RUNNING:
                        m_State = NodeState.RUNNING;
                        if (m_SaveCurrentNode)
                            m_CurrentNode = node;
                        done = true;
                        break;

                    case NodeState.SUCCESS:
                        m_State = NodeState.SUCCESS;
                        m_CurrentNode = null;
                        break;

                    default:
                        m_State = NodeState.SUCCESS;
                        done = true;
                        break;
                }
            }

            return m_State;
        }

    }

}
