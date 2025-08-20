using System;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu
{
    public class TabButton : MonoBehaviour
    {
        #region Members

        [SerializeField] protected Color m_BorderColorActivated;
        [SerializeField] protected Color m_BackgroundColorActivated;
        [SerializeField] protected Color m_ColorActivated;
        [SerializeField] protected Color m_TextSelectedColor;

        public Action TabButtonClickedEvent;

        protected Button    m_Button;
        protected Image     m_Border;
        protected Image     m_BackgroundImage;
        protected Image     m_Icon;
        protected Mask      m_IconMask;
        protected TMP_Text  m_Text;

        protected Color m_BaseBorderColor;
        protected Color m_BaseBackgroundColor;
        protected Color m_BaseColor;
        protected Color m_BaseTextColor;

        protected bool m_Activated;

        // ========================================================================================
        // Public accessors
        public Button Button => m_Button;
        public Image BackgroundImage => m_BackgroundImage;

        #endregion


        #region Init & End

        protected virtual void FindComponents()
        {
            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_Border            = Finder.FindComponent<Image>(gameObject,       "Border",           false);
            m_BackgroundImage   = Finder.FindComponent<Image>(gameObject,       "BackgroundImage",  false);
            m_IconMask          = Finder.FindComponent<Mask>(gameObject,        "IconMask",         false);
            m_Icon              = Finder.FindComponent<Image>(gameObject,       "TabIcon",          false);
            m_Text              = Finder.FindComponent<TMP_Text>(gameObject,    "TabText",          false);

            if (m_Border != null)
                m_BaseBorderColor = m_Border.color;
            if (m_BackgroundImage != null)
                m_BaseBackgroundColor = m_BackgroundImage.color;
            if (m_ColorActivated != null)
            {
                if (m_Text != null)
                    m_BaseTextColor = m_Text.color;
                
                if (m_Icon != null)
                    m_BaseColor = m_Icon.color;
            }
                

            m_Button.onClick.AddListener(OnButtonClicked);
        }

        public virtual void Initialize()
        {
            FindComponents();
        }

        public virtual void Activate(bool activate)
        {
            // do nothing on activation beeing the same as current activation
            if (m_Activated == activate)
                return;

            if (m_Border != null && m_BorderColorActivated != default)
                m_Border.color                  = activate ? m_BorderColorActivated : m_BaseBorderColor;
            if (m_BackgroundImage != null && m_BackgroundColorActivated != default)
                m_BackgroundImage.color         = activate ? m_BackgroundColorActivated : m_BaseBackgroundColor;

            SetActivationColor(activate);
            SetActivationSize(activate);

            m_Activated = activate;
        }

        protected virtual void OnDestroy()
        {
            if (m_Button != null)
                m_Button.onClick.RemoveAllListeners();
        }

        #endregion


        #region GUI Manipulators

        protected virtual void SetActivationSize(bool activate)
        {
            if (m_IconMask != null)
                m_IconMask.transform.localScale = activate ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;
            else if (m_Icon != null)
                m_Icon.transform.localScale = activate ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;

            if (m_Text != null)
            {
                m_Text.transform.localScale = activate ? new Vector3(1.2f, 1.2f, 1f) : Vector3.one;
            }
                
        }

        protected virtual void SetActivationColor(bool activate)
        {
            if (m_ColorActivated == default)
                return;

            if (m_Icon != null)
            {
                m_Icon.color = activate ? m_ColorActivated : m_BaseColor;
            }

            if (m_Text != null && m_TextSelectedColor != default)
            {
                m_Text.color = activate ? m_TextSelectedColor : m_BaseTextColor;
            }
        }

        #endregion


        #region Listeners

        protected virtual void OnButtonClicked()
        {
            TabButtonClickedEvent?.Invoke();
        }

        #endregion
    }
}