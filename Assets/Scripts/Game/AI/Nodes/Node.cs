using Data.AI;
using System;
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
        protected bool m_IsEvaluated = false;
        protected List<Node> m_Children = new List<Node>();
        protected Func<float> m_WeightMethod = null;

        private Dictionary<string, object> m_DataContext = new Dictionary<string, object>();

        public NodeState State          => m_State;
        public bool IsEvaluated         => m_IsEvaluated;
        public bool IsActivated         => m_IsActivated;
        public List<Node> Children      => m_Children;
        public Func<float> WeightMethod => m_WeightMethod;
        public float Weight             => m_WeightMethod == null ? 0f : m_WeightMethod.Invoke();

        public Node()
        {
            m_Parent = null;
        }

        public Node(List<Node> children, Func<float> weight = null)
        {
            m_WeightMethod = weight;

            if (children == null)
                children = new List<Node>();

            foreach (Node child in children)
                _Attach(child);
        }

        protected void _Attach(Node node)
        {
            node.m_Parent = this;
            m_Children.Add(node);
        }

        public virtual NodeState Evaluate()
        {
            m_State = NodeState.FAILURE;
            return m_State;
        }

        public virtual void Reset()
        {
            m_State = NodeState.FAILURE;
            foreach (var child in m_Children)
                child.Reset();
        }

        #region State Management

        public virtual void SetNodeState(NodeState nodeState)
        {
            if (m_State == nodeState)
                return;

            switch (nodeState)
            {
                case NodeState.FAILURE:
                    OnFailure();
                    break;

                case NodeState.RUNNING:
                    OnRunning();
                    break;

                case NodeState.SUCCESS:
                    OnSuccess();
                    break;
            }

            m_State = nodeState;
        }

        protected virtual void OnFailure()
        {
            Reset();
        }

        protected virtual void OnRunning() { }

        protected virtual void OnSuccess() { }

        #endregion


        #region Data Management


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

        #endregion


        #region Debug

        public virtual string GetInfo()
        {
            return null;
        }

        #endregion
    }


}
