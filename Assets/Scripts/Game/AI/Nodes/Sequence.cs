using System;
using System.Collections.Generic;

namespace AI
{
    public class Sequence : Node
    {
        public Sequence(List<Node> nodes, Func<float> weight = null) : base(nodes, weight) { }

        public override NodeState Evaluate()
        {
            base.Evaluate();

            bool done = false;

            foreach (Node node in m_Children)
            {
                if (!node.IsActivated)
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
                        done = true;
                        break;

                    case NodeState.RUNNING:
                        m_State = NodeState.RUNNING;
                        done = true;
                        break;

                    case NodeState.SUCCESS:
                        m_State = NodeState.SUCCESS;
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
