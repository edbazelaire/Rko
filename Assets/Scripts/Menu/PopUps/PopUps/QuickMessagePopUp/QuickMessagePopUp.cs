using Assets.Scripts.Managers.Sound;
using Enums;
using System;
using System.Collections;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class QuickMessagePopUp : OverlayScreen
    {
        #region Members

        // ==========================================================================================
        // GameObjects & Components
        protected GameObject    m_PopUpWindow;
        protected TMP_Text      m_MessageText;
        protected Image         m_Background;

        // Data
        string m_Message;
        float m_Duration;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PopUpWindow       = Finder.Find(gameObject, "PopUpWindow");
            m_MessageText       = Finder.FindComponent<TMP_Text>(gameObject, "MessageText");
            m_Background        = Finder.FindComponent<Image>(gameObject, "Background");
        }

        public virtual void Initialize(string message, float duration = 3f)
        {
            m_Message = message;
            m_Duration = duration;

            base.Initialize();
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            CoroutineManager.DelayMethod(() => m_MessageText.text = m_Message);
        }

        protected override void OnInitializationCompleted()
        {
            base.OnInitializationCompleted();

            if (m_Duration >= 0)
                StartCoroutine(ActivateTimer());
        }

        protected override void OnExit()
        {
            StopAllCoroutines();

            base.OnExit();
        }

        #endregion


        #region Animations

        protected IEnumerator ActivateTimer()
        {
            yield return new WaitForSeconds(m_Duration);

            Exit();
        }

        protected override void EnterAnimation()
        {
            var fadeIn = m_PopUpWindow.AddComponent<Fade>();
            fadeIn.Initialize("", duration: 0.2f, startScale: 0);
        }


        protected override IEnumerable ExitAnimation()
        {
            // set fade animation
            var fadeOut = m_PopUpWindow.AddComponent<Fade>();
            fadeOut.Initialize("", duration: 0.2f, endScale: 0);

            yield return new WaitUntil(() => fadeOut.IsOver);
        }

        #endregion


        #region Inherited Manipulators

        protected override void PlaySoundFX()
        {
            SoundFXManager.PlayOnce(SoundFXManager.OpenPopUpSoundFX);
        }

        #endregion


        #region Listeners


        #endregion
    }
}