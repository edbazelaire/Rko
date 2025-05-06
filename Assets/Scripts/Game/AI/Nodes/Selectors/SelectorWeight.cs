using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace AI
{
    public class SelectorWeight : RandomSelector
    {
        #region Members

        #endregion


        #region Core

        public SelectorWeight(List<Node> nodes, Func<float> weight = null) : base(nodes, weight)
        {
            
        }

        protected override void SelectNode()
        {
            m_SelectedNode = null;

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
                SetNodeState(NodeState.FAILURE);
                return;
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
        }

        #endregion
    }
}
