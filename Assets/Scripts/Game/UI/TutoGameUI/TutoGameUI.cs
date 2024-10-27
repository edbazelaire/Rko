using Enums;
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
        Image                       m_Background;
        ObjectifDisplayerUI         m_ObjectifDisplayer;
        Speaker                     m_Speaker;
        Speaker                     m_SpeakerEnemy;
        HandUI                      m_Hand;
        ClickInteractor             m_ClickInteractor;

        // ==========================================================================
        // Data
        public ObjectifDisplayerUI  ObjectifDisplayer       => m_ObjectifDisplayer;
        public Speaker              Speaker                 => m_Speaker;
        public Speaker              SpeakerEnemy            => m_SpeakerEnemy;
        public HandUI               Hand                    => m_Hand;
        public ClickInteractor      ClickInteractor         => m_ClickInteractor;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Background        = Finder.FindComponent<Image>(gameObject, "Background");
            m_ObjectifDisplayer = Finder.FindComponent<ObjectifDisplayerUI>(gameObject, "ObjectifDisplayer"); ;
            m_Speaker           = Finder.FindComponent<Speaker>(gameObject, "Speaker");
            m_SpeakerEnemy      = Finder.FindComponent<Speaker>(gameObject, "SpeakerEnemy");
            m_Hand              = Finder.FindComponent<HandUI>(gameObject, "Hand");
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
            m_SpeakerEnemy.Initialize();
            m_ObjectifDisplayer.Initialize();
            m_Hand.Initialize();

            m_Background.gameObject.SetActive(false);
            m_Speaker.Activate(false);
            m_SpeakerEnemy.Activate(false);
            m_Hand.Activate(false);
            gameObject.SetActive(false);
        }

        #endregion


        #region Presentation

        public IEnumerator PlayPresentation()
        {
            // activate background
            m_Background.gameObject.SetActive(true);

            // welcome text
            yield return m_Speaker.Write("Welcome to CrossFire Arena !", ECaptionType.Exclamation, true);
        }

        public IEnumerator MovementDialog()
        {
            m_Background.gameObject.SetActive(false);

            StartCoroutine(m_Speaker.Write("Use these buttons to move left and right", ECaptionType.Exclamation, false));
            yield return m_Hand.ClickOn(GameUIManager.MovementButtonsContainer.RightMovementButton);

            m_Speaker.Activate(false);
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