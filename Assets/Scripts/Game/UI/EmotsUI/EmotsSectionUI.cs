using AI;
using Game.AI;
using System.Collections;
using Tools.Animations;
using Tools.Debugs.BT;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using Enums;
using System.Collections.Generic;


namespace Game.UI
{
    public class EmotsSectionUI : MObject
    {
        #region Members

        Button                  m_DisplayButton;              
        Transform               m_EmotsContainer;
        List<EmotSelectionUI>   m_EmotButtons;

        List<EEmot> m_Emots;
        bool m_IsActive = true;
        float m_Width;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DisplayButton = Finder.FindComponent<Button>(gameObject, "DisplayButton");
            m_EmotsContainer = Finder.Find(gameObject, "EmotsContainer").transform;
            m_EmotButtons = Finder.FindComponents<EmotSelectionUI>(m_EmotsContainer.gameObject);
            m_Width = Finder.FindComponent<RectTransform>(gameObject).rect.width;
        }

        public void Initialize(List<EEmot> emots)
        {
            m_Emots = emots;

            base.Initialize();

            Activate(false);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetUpEmots();
        }

        #endregion


        #region GUI Manipulators

        void SetUpEmots()
        {
            for (int i = 0; i < m_EmotButtons.Count; i++)
            {
                if (i >= m_Emots.Count)
                {
                    ErrorHandler.Warning("Not enough Emots to fill all buttons : " + m_Emots.Count);
                    break;
                }
                m_EmotButtons[i].Initialize(m_Emots[i]);
            }
        }

        #endregion


        #region Activation / Deactivation

        void ToggleActivation()
        {
            Debug.Log("ToggleActivation");
            Activate(!m_IsActive);
        }

        void Activate(bool activate = true)
        {
            if (m_IsActive == activate)
                return;

            m_IsActive = activate;
            m_EmotsContainer.gameObject.SetActive(activate);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_DisplayButton.onClick.AddListener(ToggleActivation);
        }

        #endregion
    }
}
