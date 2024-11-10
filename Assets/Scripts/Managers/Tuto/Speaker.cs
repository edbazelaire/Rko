using Enums;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Managers.Tuto
{
    public class Speaker : MObject
    {
        #region Members

        [SerializeField] ECaptionColor m_CaptionColor = ECaptionColor.White;

        Image   m_Character;
        Caption m_Caption;

        public Caption Caption => m_Caption;
        public bool IsDoneWriting => m_Caption == null || m_Caption.IsDoneWriting; 

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

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopAllCoroutines();
        }

        #endregion


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            m_Caption.Activate(false);

            ShowSpeaker(activate);
            gameObject.SetActive(activate);
        }

        public void ShowSpeaker(bool show)
        {
            m_Character.gameObject.SetActive(show);
        }

        public IEnumerator Write(string text, ECaptionType captionType = ECaptionType.None, bool showSpeaker = true)
        {
            gameObject.SetActive(true);
            m_Caption.Write(text, captionType, m_CaptionColor);
            ShowSpeaker(showSpeaker);

            yield return new WaitUntil(() => GameUIManager.TutoGameUI.ClickInteractor.Skip || IsDoneWriting);
            GameUIManager.TutoGameUI.ClickInteractor.Refresh();
        }

        public IEnumerator WriteOnce(string text, ECaptionType captionType = ECaptionType.None, bool showSpeaker = true)
        {
            yield return Write(text, captionType, showSpeaker);
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
