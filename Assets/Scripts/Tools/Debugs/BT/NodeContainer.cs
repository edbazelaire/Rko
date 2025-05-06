using AI;
using System.Collections;
using UnityEngine;


namespace Tools.Debugs.BT
{
    public class NodeContainer : MObject
    {
        #region Members

        NodeContainer m_SequencePrefab;      // Prefab for SEQUENCE node 
        NodeContainer m_SelectorPrefab;      // Prefab for SELECTOR node 
        BTNodeUI m_NodeUIPrefab;             // Prefab for SIMPLE node UI

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_NodeUIPrefab          = AssetLoader.Load<BTNodeUI>("BTNodeUI", AssetLoader.c_GameUIContentPath);
            m_SequencePrefab        = AssetLoader.Load<NodeContainer>("SequenceContainer", AssetLoader.c_GameUIContentPath);
            m_SelectorPrefab        = AssetLoader.Load<NodeContainer>("SelectorContainer", AssetLoader.c_GameUIContentPath);
        }

        public virtual void Initialize(Node node)
        {
            base.Initialize();
            UIHelper.CleanContent(gameObject);

            InstantiateNodeRecursively(node);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        void InstantiateNodeRecursively(Node node)
        {
            // instantiate nodes recursively
            foreach (var childNode in node.Children)
            {
                if (childNode is Sequence)
                {
                    Instantiate(m_SequencePrefab, gameObject.transform).Initialize(childNode);
                }

                //else if (childNode is SelectorWeight)
                //{
                //    var nodeContainer = Instantiate(m_SequencePrefab, gameObject.transform);
                //    Instantiate(m_NodeUIPrefab, nodeContainer.transform).Initialize(node);
                //    nodeContainer.Initialize(childNode);
                //}

                else if (childNode is Selector)
                {
                    Instantiate(m_SelectorPrefab, gameObject.transform).Initialize(childNode);
                }

                else
                {
                    // Instantiate UI representation of the current node
                    Instantiate(m_NodeUIPrefab, gameObject.transform).Initialize(childNode);
                }
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
