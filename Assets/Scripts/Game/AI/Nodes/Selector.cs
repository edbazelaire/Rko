using System;
using System.Collections.Generic;

namespace AI
{
    public class Selector : Node
    {
        protected bool m_IsRandom = false;

        public Selector(List<Node> nodes, bool random = false, Func<float> weight = null) : base(nodes, weight)
        {
            m_IsRandom = random;
        }

        public override NodeState Evaluate()
        {
            base.Evaluate();
         
            bool done = false;
            foreach (Node node in (m_IsRandom ? m_Children.ShuffleClone() : m_Children))
            {
                // current child node not activated ? - skip
                if (! node.IsActivated)
                    continue;

                // Process over - set all remaining nodes to FAILURE (reset them)
                if (done)
                {
                    node.Reset();
                    continue;
                }

                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue;

                    case NodeState.RUNNING:
                        m_State = NodeState.RUNNING;
                        done = true;
                        break;

                    case NodeState.SUCCESS:
                        m_State = NodeState.SUCCESS;
                        done = true;
                        break;

                    default:
                        continue;
                }
            }

            if (! done)
                m_State = NodeState.FAILURE;

            return m_State;
        }
    }

}
