using Data;
using Enums;
using Menu.MainMenu;
using Tools;
using UnityEngine;
using UnityEngine.Video;

namespace Menu.PopUps
{
    public class ClipTabContent : TabContent
    {
        #region Members

        GameObject m_ErrorScreen;
        VideoPlayer m_VideoPlayer;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ErrorScreen = Finder.Find(gameObject, "ErrorScreen");
            m_VideoPlayer = Finder.FindComponent<VideoPlayer>(gameObject, "VideoPlayer");
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Display video of the spell
        /// </summary>
        public void SetUpVideo(ESpell spell)
        {
            var clip = AssetLoader.GetSpellPreviewClip(spell.ToString());
            if (clip == null)
            {
                m_VideoPlayer.gameObject.SetActive(false);
                m_ErrorScreen.SetActive(true);
                return;
            }

            m_ErrorScreen.SetActive(false);
            m_VideoPlayer.clip = clip;
            m_VideoPlayer.isLooping = true;
            m_VideoPlayer.Prepare();
        }

        public override void Activate(bool activate)
        {
            base.Activate(activate);

            if (activate && m_VideoPlayer.gameObject.activeInHierarchy)
            {
                m_VideoPlayer.Play();
            }
        }

        #endregion
    }
}