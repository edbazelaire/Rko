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

        Button m_DisplayButton;              
        Transform m_EmotsContainer;  
        EmotButton m_EmotButtonTemplate;

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
            m_Width = Finder.FindComponent<RectTransform>(gameObject).rect.width;

            m_EmotButtonTemplate = AssetLoader.Load<EmotButton>("EmotButton", AssetLoader.c_EmotsSectionUIPath);
        }

        public void Initialize(List<EEmot> emots)
        {
            m_Emots = emots;

            base.Initialize();

            Activate(false, false);
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
            UIHelper.CleanContent(m_EmotsContainer.gameObject);

            foreach (EEmot emot in m_Emots)
            {
                Instantiate(m_EmotButtonTemplate, m_EmotsContainer).Initialize(emot);
            }
        }

        #endregion


        #region Activation / Deactivation

        void ToggleActivation()
        {
            Debug.Log("ToggleActivation");
            Activate(!m_IsActive);
        }

        void Activate(bool activate = true, bool withAnimation = true)
        {
            if (m_IsActive == activate)
                return;

            m_IsActive = activate;

            // Ensure the component is a RectTransform for UI elements
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("No RectTransform component found on this GameObject.");
                return;
            }

            // Cancel animation component (if any)
            var animationComponent = Finder.FindComponent<MoveAnimation>(gameObject, throwError: false);
            if (animationComponent != null)
            {
                Destroy(animationComponent);
            }

            // Calculate desired end position using anchoredPosition for UI elements
            Vector2 endPos = rectTransform.anchoredPosition;
            endPos.x = activate ? -m_Width / 2 : m_Width / 2;

            // Instant activation without animation
            if (!withAnimation)
            {
                rectTransform.anchoredPosition = endPos;
                return;
            }

            // Add new MoveAnimation component and initialize it with anchored positions
            animationComponent = gameObject.AddComponent<MoveAnimation>();
            animationComponent.Initialize("SideBar", 0.5f, rectTransform.anchoredPosition, endPos, checkRectTransform: true);
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
