using System.Collections.Generic;

namespace AI
{
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
            bool anyChildIsRunning = false;

            foreach (Node node in m_Children)
            {
                if (! node.IsActivated)
                {
                    m_State = NodeState.FAILURE;
                    return m_State;
                }

                switch (node.Evaluate())
                {
                    case NodeState.FAILURE:
                        m_State = NodeState.FAILURE;
                        return m_State;
                    case NodeState.SUCCESS:
                        continue;
                    case NodeState.RUNNING:
                        anyChildIsRunning = true;
                        continue;
                    default:
                        m_State = NodeState.SUCCESS;
                        return m_State;
                }
            }

            m_State = anyChildIsRunning ? NodeState.RUNNING : NodeState.SUCCESS;
            return m_State;
        }

    }

}
