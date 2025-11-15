using AI;
using Menu.Common.Dots;
using MyBox;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Debugs.BT
{
    public class BTNodeUI : MObject
    {
        #region Members

        [SerializeField] Color m_InactiveColor = new Color(0.7f, 0.7f, 0.7f);
        [SerializeField] Color m_RunningColor = new Color(0.5f, 0.5f, 0.3f);
        [SerializeField] Color m_ActiveColor = new Color(0.3f, 1f, 0.3f);

        Node m_Node;

        TMP_Text    m_NodeName;
        Image       m_Selection;
        Image       m_Background;
        TMP_Text    m_Weight;
        TMP_Text    m_Info;
        bool        m_HasWeight = false;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_NodeName      = Finder.FindComponent<TMP_Text>(gameObject,    "NodeName");
            m_Selection     = Finder.FindComponent<Image>(gameObject,       "Selection");
            m_Background    = Finder.FindComponent<Image>(gameObject,       "Background");
            m_Weight        = Finder.FindComponent<TMP_Text>(gameObject,    "Weight");
            m_Info          = Finder.FindComponent<TMP_Text>(gameObject,    "Info");

            m_HasWeight = m_Node.WeightMethod != null;
        }

        public void Initialize(Node node)
        {
            m_Node = node;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_NodeName.text = m_Node.GetType().Name;

            if (! m_HasWeight)
                m_Weight.gameObject.SetActive(false);

            string info = m_Node.GetInfo();
            if (info.IsNullOrEmpty())
                m_Info.gameObject.SetActive(false);
            else
            {
                m_Info.gameObject.SetActive(true);
            }
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!m_Initialized)
                return;

            RefreshActiveState();
            RefreshIsEvaluated();
            RefreshInfo();

            if (m_HasWeight)
                RefreshWeight();
        }

        #endregion


        #region GUI Manipulators

        void RefreshActiveState()
        {
            switch(m_Node.State)
            {
                case NodeState.FAILURE:
                    m_Background.color = m_InactiveColor;
                    break;

                case NodeState.RUNNING:
                    m_Background.color = m_RunningColor;
                    break;

                case NodeState.SUCCESS:
                    m_Background.color = m_ActiveColor;
                    break;
            }
        }

        void RefreshIsEvaluated()
        {
            m_Selection.gameObject.SetActive(m_Node.IsEvaluated);
        }

        void RefreshWeight()
        {
            m_Weight.text = m_Node.Weight.ToString("F2");
        }

        void RefreshInfo()
        {
            string info = m_Node.GetInfo();
            if (info.IsNullOrEmpty())
                return;

            m_Info.text = info;
        }

        #endregion

    }
}