using Assets.Scripts.Managers.Sound;
using UnityEngine;

namespace Menu.MainMenu
{
    public class TabContent : MObject
    {
        #region Members

        [SerializeField] protected AudioClip m_ActivationSoundFX = null;

        /// <summary> button linked to this tab </summary>
        protected TabButton     m_TabButton;
        /// <summary> is the table currently activater ? </summary>
        protected bool          m_Activated;
        /// <summary> Content getting turned on/off during the activation </summary>
        protected virtual GameObject m_ActivationContent => gameObject;    

        public TabButton TabButton => m_TabButton;

        #endregion


        #region Init & End

        /// <summary>
        /// Called when the tab is getting initialized, during registration by the "TabsManager"
        /// </summary>
        /// <param name="tabButton"></param>
        public virtual void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize();

            m_TabButton = tabButton;

            if (m_ActivationSoundFX == null)
                m_ActivationSoundFX = activationSoundFX;  
        }

        /// <summary>
        /// Called when the tab gets activated / deactivated as currently displayed tab
        /// </summary>
        /// <param name="activate"></param>
        public virtual void Activate(bool activate)
        {
            m_Activated = activate;
            m_TabButton?.Activate(activate);

            if (m_ActivationSoundFX != null && activate)
            {
                SoundFXManager.PlayOnce(m_ActivationSoundFX);
            }

            if (m_ActivationContent != null && m_ActivationContent.activeSelf != activate)
                m_ActivationContent.SetActive(activate);
        }

        /// <summary>
        /// Called on beeing un-registered from the "TabsManager"
        /// </summary>
        public virtual void UnRegister() 
        {
            UnRegisterListeners();  
            Destroy(gameObject);
        }

        #endregion
    }
}