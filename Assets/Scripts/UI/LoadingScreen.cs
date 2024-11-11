using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Assets.Scripts.UI
{
    public class LoadingScreen : MonoBehaviour
    {
        #region Members

        const string c_LoadingBar = "LoadingBar";

        Image m_LoadingBar;
        TMP_Text m_ProgressText;
        TMP_Text m_InfoText;

        #endregion


        // Use this for initialization
        void Awake()
        {
            m_LoadingBar = Finder.FindComponent<Image>(gameObject, c_LoadingBar);
            m_ProgressText = Finder.FindComponent<TMP_Text>(gameObject, "ProgressText");
            m_InfoText = Finder.FindComponent<TMP_Text>(gameObject, "InfoText");

            DontDestroyOnLoad(gameObject);
        }

        public void SetProgress(float progress)
        {
            if (m_LoadingBar == null)
                return;

            m_LoadingBar.fillAmount = progress;

            if (m_ProgressText != null)
                m_ProgressText.text = Mathf.Round(progress * 100).ToString() + "%";
        }

        public void SetInfoText(string infoText)
        {
            if (m_InfoText == null)
                return;

            if (infoText != "")
                infoText = " - " + infoText;

            m_InfoText.text = infoText;
        }

        public void Display(bool display)
        {
            gameObject.SetActive(display);
        }
    }
}