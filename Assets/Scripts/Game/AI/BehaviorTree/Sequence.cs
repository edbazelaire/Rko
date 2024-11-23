using System.Collections.Generic;

namespace AI
{
    public class Sequence : Node
    {
        public Sequence() : base() { }
        public Sequence(List<Node> children) : base(children) { }

        public override NodeState Evaluate()
        {
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

                    case NodeState.RUNNING:
                        return m_State;

                    case NodeState.SUCCESS:
                        continue;

                    default:
                        m_State = NodeState.SUCCESS;
                        return m_State;
                }
            }

            return m_State;
        }

    }

}
