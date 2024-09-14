using Managers.Tuto;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Game.UI
{
    public class TutoGameUI : MObject
    {
        #region Members

        // ==========================================================================
        // GameObject & Components
        Image m_Background;
        Speaker m_Speaker;
        Image m_Hand;

        // ==========================================================================
        // Data
        ClickInteractor m_ClickInteractor;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Background = Finder.FindComponent<Image>(gameObject, "Background");
            m_Speaker = Finder.FindComponent<Speaker>(gameObject, "Speaker");
            m_Hand = Finder.FindComponent<Image>(gameObject, "Hand");
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        public void Activate(bool activate)
        {
            gameObject.SetActive(activate);

            if (m_ClickInteractor == null)
                m_ClickInteractor = ClickInteractor.Create();
            m_ClickInteractor.Activate(activate);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Speaker.Initialize();

            m_Background.gameObject.SetActive(false);
            m_Hand.gameObject.SetActive(false);
            m_Speaker.gameObject.SetActive(false);
            gameObject.SetActive(false);
        }

        #endregion


        #region Presentation

        public IEnumerator PlayPresentation()
        {
            // activate background
            m_Background.gameObject.SetActive(true);

            // activate speaker
            m_Speaker.Activate(true);

            // todo => Animation with Coroutine ?
            m_Speaker.Caption.Write("Welcome to CrossFire Arena !");

            // wait skip
            yield return new WaitUntil(() => m_ClickInteractor.Skip);
            m_ClickInteractor.Skip = false;

            // todo => Animation with Coroutine ?
            m_Speaker.Caption.Write("You are here to shit blood !");

            // wait skip
            yield return new WaitUntil(() => m_ClickInteractor.Skip);
            m_ClickInteractor.Skip = false;

            // deactivate speaker
            m_Speaker.Activate(false);
            m_Background.gameObject.SetActive(false);
        }

        #endregion


        #region Hand Movements

        public void ClickOn(GameObject gameObject)
        {

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