using Menu.Common.Dots;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class LifesSectionUI : MObject
    {
        #region Members

        [SerializeField] protected Color m_EnableColor;
        [SerializeField] protected Color m_DiseableColor;

        int m_CurrentValue;
        int m_MaxValue;

        GameObject m_Container;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Container = Finder.Find(gameObject, "Container");
        }

        public void Initialize(int value, int maxValue)
        {
            m_CurrentValue = value;
            m_MaxValue = maxValue;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            DisplayDots();
        }

        #endregion


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            m_Container.SetActive(activate);
        }

        public void RefreshUI(int value, int maxValue)
        {
            m_CurrentValue = value;
            m_MaxValue = maxValue;
            DisplayDots();
        }

        void DisplayDots()
        {
            UIHelper.CleanContent(m_Container);

            for (int i = 1; i <= m_MaxValue; i++)
            {
                var dot = Instantiate(AssetLoader.LoadComponentPrefab<DotUI>("Dot"), m_Container.transform);
                dot.Initialize(i <= m_CurrentValue);
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
    
