using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class SelectorWeight : Selector
    {
        #region Members

        Node m_SelectedNode = null;

        #endregion


        #region Core

        public SelectorWeight(List<Node> nodes, Func<float> weight = null) : base(nodes, false, weight)
        {
            
        }

        public override NodeState Evaluate()
        {
            // If a node is already selected, evaluate and return its state
            if (m_SelectedNode != null)
            {
                // reset node randomly
                if (m_State == NodeState.SUCCESS && UnityEngine.Random.value > 0.9f)
                {
                    Reset();
                }

                // Evaluate Current Node
                else
                {
                    SetNodeState(m_SelectedNode.Evaluate());
                    return m_State;
                }
            }

            // Compute total weight considering only activated nodes
            float totalWeight = 0f;
            List<(Node node, float weight)> activatedNodes = new List<(Node node, float weight)>();

            foreach (Node node in m_Children)
            {
                // Skip non-activated nodes
                if (!node.IsActivated)
                    continue;

                float weight = Mathf.Max(node.Weight, 0f);
                activatedNodes.Add((node, weight));
                totalWeight += weight;
            }

            // Return failure if no activated nodes
            if (activatedNodes.Count == 0)
            {
                m_State = NodeState.FAILURE;
                return m_State;
            }

            // Select node randomly based on weights
            float randomPoint = UnityEngine.Random.value * totalWeight;
            float currentWeight = 0f;

            foreach (var (node, weight) in activatedNodes)
            {
                currentWeight += weight;
                if (randomPoint <= currentWeight)
                {
                    m_SelectedNode = node;
                    break;
                }
            }

            m_State = m_SelectedNode != null ? m_SelectedNode.Evaluate() : NodeState.FAILURE;
            return m_State;
        }

        public override void Reset()
        {
            base.Reset();
            m_SelectedNode = null;
        }

        #endregion
    }
}
