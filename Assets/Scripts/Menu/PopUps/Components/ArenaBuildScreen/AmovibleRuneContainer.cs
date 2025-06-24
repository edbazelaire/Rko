using Enums;
using Menu.Common.Buttons;
using System;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.PopUps.Components
{
    public class AmovibleRuneContainer : MObject
    {
        #region Members

        public Action<int, int> ButtonClickedEvent;

        GameObject m_TemplateContainer;
        Button m_UpperButton;
        Button m_LowerButton;

        TemplateRuneItemUI m_TemplateRuneItemUI;
        int m_Index;

        public Button UpperButton => m_UpperButton;
        public Button LowerButton => m_LowerButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateContainer = Finder.Find(gameObject, "TemplateContainer");
            m_UpperButton = Finder.FindComponent<Button>(gameObject, "UpperButton");
            m_LowerButton = Finder.FindComponent<Button>(gameObject, "LowerButton");
        }

        public virtual void Initialize(TemplateRuneItemUI template, int index)
        {
            m_TemplateRuneItemUI = template;
            m_Index = index;

            m_TemplateRuneItemUI.SetRuneActivation((ERuneActivation)(3 - index));

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            UIHelper.CleanContent(m_TemplateContainer);
            m_TemplateRuneItemUI.transform.SetParent(m_TemplateContainer.transform, false);

            SetUpButtons();
        }

        #endregion


        #region GUI Manipulators

        void SetUpButtons()
        {
            m_UpperButton.gameObject.SetActive(m_Index > 0);
            m_LowerButton.gameObject.SetActive(m_Index < 2);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            // NOTE : increment of UPPER button is -1 because Primal rune is at index 0, and minor rune is at index 2.
            // so, if you INCREASE a rune activation, you DECRESE its index
            m_UpperButton.onClick.AddListener(() => ButtonClickedEvent?.Invoke(m_Index, -1));
            m_LowerButton.onClick.AddListener(() => ButtonClickedEvent?.Invoke(m_Index, 1));
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            m_UpperButton.onClick.RemoveAllListeners();
            m_LowerButton.onClick.RemoveAllListeners();
        }

        #endregion
    }
}


