using MyBox;
using System;
using System.Collections.Generic;

namespace AI
{
    public class Selector : Node
    {
        protected bool m_IsRandom = false;

        public Selector() : base() { }
        public Selector(List<Node> children, bool random = false) : base(children) 
        {
            m_IsRandom = random;
        }

        public override NodeState Evaluate()
        {
            foreach (Node node in (m_IsRandom ? m_Children.ShuffleClone() : m_Children))
            {
                if (!node.IsActivated)
                {
                    m_State = NodeState.FAILURE;
                    return m_State;
                }

                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        continue;
                    case NodeState.SUCCESS:
                        m_State = NodeState.SUCCESS;
                        return m_State;
                    case NodeState.RUNNING:
                        m_State = NodeState.RUNNING;
                        return m_State;
                    default:
                        continue;
                }
            }

            m_State = NodeState.FAILURE;
            return m_State;
        }
    }

}
