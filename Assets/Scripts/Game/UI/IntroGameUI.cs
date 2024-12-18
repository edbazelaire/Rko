using Assets.Scripts.Managers.Sound;
using Managers;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;


namespace Game.UI
{
    public class IntroGameUI : MObject
    {
        #region Members

        PlayerIntroUI   m_PlayerIntroUILeft;
        PlayerIntroUI   m_PlayerIntroUIRight;

        TMP_Text        m_CountDown;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PlayerIntroUILeft = Finder.FindComponent<PlayerIntroUI>(gameObject, "PlayerIntroUI_Left");
            m_PlayerIntroUIRight = Finder.FindComponent<PlayerIntroUI>(gameObject, "PlayerIntroUI_Right");

            m_CountDown = Finder.FindComponent<TMP_Text>("CountDown");
        }

        public void Initialize(Dictionary<ulong, SPlayerData> playerData)
        {
            base.Initialize();

            // deactivate countdown by default
            m_CountDown.gameObject.SetActive(false);

            if (playerData.Count != 2)
            {
                ErrorHandler.Warning("Unhandled case : number of player data is " + playerData.Count);
            }

            foreach (var item in playerData)
            {
                // select intro UI according to the team
                PlayerIntroUI playerIntroUI = GameManager.Instance.Owner.Team == GameManager.Instance.GetPlayer(item.Key).Team ? m_PlayerIntroUILeft : m_PlayerIntroUIRight;
                playerIntroUI.Initialize(item.Value);
            }
        }

        /// <summary>
        /// Call for direct end of the Intro, ending all coroutines in the process
        /// </summary>
        public void Deactivate()
        {
            StopAllCoroutines();

            gameObject.SetActive(false);
        }

        #endregion


        #region Animation

        public void PlayEnterAnimation()
        {
            var fadeInL = m_PlayerIntroUILeft.gameObject.AddComponent<Fade>();
            fadeInL.Initialize(startScale: 0.8f, startOpacity: 0, duration: 0.5f);

            var fadeInR = m_PlayerIntroUIRight.gameObject.AddComponent<Fade>();
            fadeInR.Initialize(startScale: 0.8f, startOpacity: 0, duration: 0.5f);
        }

        public void PlayExitAnimation()
        {
            StartCoroutine(PlayCountDown());

            var fadeOut = gameObject.AddComponent<Fade>();
            fadeOut.Initialize(startOpacity: 1f, endOpacity: 0f, duration: 1.5f);
        }

        public IEnumerator PlayCountDown()
        {
            m_CountDown.gameObject.SetActive(true);

            int COUNTDOWN = 3;
            string text;
            for (int i = 0; i <= COUNTDOWN; i++)
            {
                if (i < COUNTDOWN)
                {
                    SoundFXManager.PlayOnce("WarDrum");
                    text = (COUNTDOWN - i).ToString();
                }
                else
                {
                    SoundFXManager.PlayOnce("SwordsCrossed");
                    text = "FIGHT !";
                }

                m_CountDown.text = text;

                // fade in counter animation
                var fadeIn = m_CountDown.AddComponent<Fade>();
                fadeIn.Initialize(startScale: 0.5f, startOpacity: 0.7f, duration: 0.3f);
                yield return new WaitUntil(() => fadeIn.IsOver);

                // wait a bit
                yield return new WaitForSeconds(0.3f);

                // fade out animation
                var fadeOut = m_CountDown.AddComponent<Fade>();
                fadeOut.Initialize(endScale: 0.9f, endOpacity: 0f, duration: 0.3f);
                yield return new WaitUntil(() => fadeIn.IsOver);

                // wait a bit
                yield return new WaitForSeconds(0.1f);
            }

            // at the end of the countdown : deactivate Intro
            gameObject.SetActive(false);
        }

        #endregion


        #region Register Listeners



        #endregion
    }
}