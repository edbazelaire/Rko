using Assets.Scripts.Managers;
using Enums;
using JetBrains.Annotations;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Infos
{
    public class InfoRowUI : MObject
    {
        #region Members

        protected Image         m_Icon;
        protected Button        m_Button;
        protected TMP_Text      m_NameText;
        protected GameObject    m_ValueContainer;
        protected TMP_Text      m_ValueText;

        protected string m_Key;
        protected string m_Name;
        protected string m_IconName;
        protected object m_Value;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            var iconContainer = Finder.Find(gameObject, "IconContainer");
            m_Icon = Finder.FindComponent<Image>(iconContainer, "Icon");
            m_Button = Finder.FindComponent<Button>(gameObject);
            m_NameText = Finder.FindComponent<TMP_Text>(gameObject, "Name");
            m_ValueContainer = Finder.Find(gameObject, "ValueContainer", false);
            m_ValueText = Finder.FindComponent<TMP_Text>(gameObject, "Value", false);
        }

        public virtual void Initialize(string key, object value, string title = "")
        {
            m_Key       = key; 
            m_Name      = key; 
            m_IconName  = key;
            m_Value     = value;

            // Title provided - force as name
            if (title != "")
            {
                m_Name = title;
            }

            // Special PropertyName - set as name
            else if (PropertyHandler.TryExtractSpecialPropertyInfo(key, out string iconName, out string prettyName))
            {
                m_Name = prettyName;
                m_IconName = iconName;
            }

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


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClick);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
        }

        protected virtual void OnClick()
        {
            if (m_Value == null)
                return;
            ScreenManager.SetPopUp(EPopUpState.PropertyInfoPopUp, m_Key, m_Value.ToString());
        }

        #endregion
    }
}