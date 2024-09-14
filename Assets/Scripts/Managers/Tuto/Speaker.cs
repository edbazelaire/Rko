using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Managers.Tuto
{
    public class Speaker : MObject
    {
        #region Members

        Image   m_Character;
        Caption m_Caption;

        public Caption Caption => m_Caption;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Character = Finder.FindComponent<Image>(gameObject, "Character");
            m_Caption = Finder.FindComponent<Caption>(gameObject, "Caption");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Caption.Initialize();
            Activate(false);
        }

        #endregion


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            m_Caption.Activate(false);

            m_Character.gameObject.SetActive(activate);
            gameObject.SetActive(activate);
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
