using System;
using System.Collections.Generic;

namespace AI
{
    public class Selector : Node
    {
        /// <summary> save node currently running, to jump right into it </summary>
        protected bool m_SaveCurrentNode    = false;
        /// <summary> randomize nodes order </summary>
        protected bool m_IsRandom       = false;

        public Selector(List<Node> nodes, bool saveCurrentNode = false, bool random = false, Func<float> weight = null) : base(nodes, weight)
        {
            m_SaveCurrentNode = saveCurrentNode;
            m_IsRandom = random;
        }

        public override NodeState Evaluate()
        {
            base.Evaluate();
         
            bool done = false;

            // Check Node saved as "Running Node"
            if (m_CurrentNode != null)
            {
                switch (m_CurrentNode.Evaluate())
                {
                    case NodeState.FAILURE:
                        m_CurrentNode = null;
                        break;

                    case NodeState.RUNNING:
                        m_State = NodeState.RUNNING;
                        return m_State;

                    case NodeState.SUCCESS:
                        m_State = NodeState.RUNNING;
                        m_CurrentNode = null;
                        return m_State;
                }
                
            }

            // otherwise - check all child nodes
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
                        if (m_SaveCurrentNode)
                            m_CurrentNode = node;
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
