using Data.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.AI;

namespace AI
{
    public enum NodeState
    {
        RUNNING,
        SUCCESS,
        FAILURE
    }

    public class Node
    {
        protected NodeState m_State;

        public Node m_Parent;
        protected bool m_IsActivated = true;
        protected List<Node> m_Children = new List<Node>();

        private Dictionary<string, object> m_DataContext = new Dictionary<string, object>();

        public bool IsActivated => m_IsActivated;

        public Node()
        {
            m_Parent = null;
        }
        public Node(List<Node> children)
        {
            foreach (Node child in children)
                _Attach(child);
        }

     
        private void _Attach(Node node)
        {
            node.m_Parent = this;
            m_Children.Add(node);
        }

        public virtual NodeState Evaluate() => NodeState.FAILURE;

        public virtual void SetNodeState(NodeState nodeState)
        {
            m_State = nodeState;
        }

        public void SetData(string key, object value)
        {
            m_DataContext[key] = value;
        }

        public object GetData(string key)
        {
            if (m_DataContext.TryGetValue(key, out object value))
                return value;

            Node node = m_Parent;
            while (node != null)
            {
                value = node.GetData(key);
                if (value != null)
                    return value;
                node = node.m_Parent;
            }

            return null;
        }

        public bool ClearData(string key)
        {
            if (m_DataContext.ContainsKey(key))
            {
                m_DataContext.Remove(key);
                return true;
            }

            Node node = m_Parent;
            while (node != null)
            {
                bool cleared = node.ClearData(key);
                if (cleared)
                    return true;
                node = node.m_Parent;
            }
            return false;
        }
    }

}
