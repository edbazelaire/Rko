using Google.Apis.Sheets.v4.Data;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;


namespace Menu.Common.Displayers
{
    public class LootInfoRow : MObject
    {
        #region Members

        TMP_Text m_Value;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Value = Finder.FindComponent<TMP_Text>(gameObject, "Value");
        }

        public void Initialize(string value)
        {
            base.Initialize();

            m_Value.text = value;
        }

        #endregion


        #region GUI Manipulators

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
