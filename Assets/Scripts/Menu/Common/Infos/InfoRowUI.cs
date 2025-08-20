using TMPro;
using Tools;
using UnityEngine.UI;

namespace Menu.Common.Infos
{
    public class InfoRowUI : MObject
    {
        #region Members

        protected Image     m_Icon;
        protected TMP_Text  m_NameText;
        protected TMP_Text  m_ValueText;

        protected string m_Name;
        protected object m_Value;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            var iconContainer = Finder.Find(gameObject, "IconContainer");
            m_Icon = Finder.FindComponent<Image>(iconContainer, "Icon");
            m_NameText = Finder.FindComponent<TMP_Text>(gameObject, "Name");
            m_ValueText = Finder.FindComponent<TMP_Text>(gameObject, "Value", false);
        }

        public virtual void Initialize(string name, object value)
        {
            // setup name of the property
            if (!TextHandler.IsSpecialPropertyName(name, out string propertyName, out string specialCondition))
                propertyName = name;

            m_Name = propertyName;
            m_Value = value;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            // setup name and color of the info row title
            
            Refresh();
        }

        #endregion


        #region GUI Manipulators

        public virtual void Refresh()
        {
            if (CheckSpecialCases())
                return;

            SetUpName(m_Name);
            SetUpIcon();
            RefreshValue(m_Value);
        }

        public virtual void SetUpName(string name)
        {
            m_Name = name;

            // set name of the row
            m_NameText.text = TextLocalizer.SplitCamelCase(TextLocalizer.LocalizeText(name));
        }

        protected virtual void SetUpIcon()
        {
            
        }


        public virtual void RefreshValue(object value, object newValue = null)
        {
            
        }

        protected virtual bool CheckSpecialCases()
        {
            return false;
        }

        #endregion


        #region Format



        #endregion
    }
}