using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.XR;


namespace Game.UI
{
    public class ObjectifDisplayerUI : MObject
    {
        #region Members

        TMP_Text m_Objectif;

        // ===============================================================================
        // Data
        string m_ObjectifName;
        int m_ObjectifValue;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Objectif = Finder.FindComponent<TMP_Text>(gameObject, "Objectif");
        }

        public override void Initialize()
        {
            base.Initialize();

            Activate(false);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            gameObject.SetActive(activate);
        }

        public void SetObjectif(string name, int value)
        {
            m_ObjectifName = name;
            m_ObjectifValue = value;

            UpdateObjectif(0);
            Activate(true);
        }

        public void UpdateObjectif(int counter)
        {
            m_Objectif.text = string.Format("{0} : {1} / {2}", m_ObjectifName, counter, m_ObjectifValue);
        }

        #endregion


        #region Animation

        public IEnumerator Completed()
        {
            yield return new WaitForSeconds(0.5f);

            // TODO : Completed Animation
            Activate(false);
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
