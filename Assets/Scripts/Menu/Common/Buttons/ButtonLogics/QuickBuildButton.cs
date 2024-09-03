using Save;
using System.Collections;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Buttons
{
    public class QuickBuildButton : OnOffButton
    {
        #region Members

        [SerializeField] Color ActivatedBackgroundColor;
        [SerializeField] Color ActivatedBorderColor;
        [SerializeField] Color ActivatedTextColor;

        int m_Index = -1;
        Color m_BaseBackgroundColor;
        Color m_BaseBorderColor;
        Color m_BaseTextColor;

        Button m_Button;
        Image m_Background;
        Image m_Border;
        TMP_Text m_Text;

        #endregion


        #region Init & End

        void Start()
        {
            m_Button        = Finder.FindComponent<Button>(gameObject);
            m_Background    = Finder.FindComponent<Image>(gameObject, "Background");
            m_Border        = Finder.FindComponent<Image>(gameObject, "Border");
            m_Text          = Finder.FindComponent<TMP_Text>(gameObject, "Text");

            m_BaseBackgroundColor   = m_Background.color;
            m_BaseBorderColor       = m_Border.color;
            m_BaseTextColor         = m_Text.color;

            SetupIndex();
            if (m_Index < 0)
                return;

            // check if is current build
            Activate(CharacterBuildsCloudData.CurrentBuildIndex == m_Index);

            // register listeners
            m_Button.onClick.AddListener(OnClick);
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent += OnCurrentBuildChanged;
            CharacterBuildsCloudData.SelectedCharacterChangedEvent += OnCurrentBuildChanged;    // also check when a character changes, the build index might change aswell
        }

        private void OnDestroy()
        {
            m_Button.onClick.RemoveAllListeners();
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent -= OnCurrentBuildChanged;
            CharacterBuildsCloudData.SelectedCharacterChangedEvent -= OnCurrentBuildChanged;
        }

        #endregion


        #region GUI Manipulators

        public override void Activate(bool activate = true)
        {
            base.Activate(activate);

            m_Background.color  = activate ? ActivatedBackgroundColor   : m_BaseBackgroundColor;
            m_Border.color      = activate ? ActivatedBorderColor       : m_BaseBorderColor;
            m_Text.color        = activate ? ActivatedTextColor         : m_BaseTextColor;
        }

        #endregion


        #region Private Methods

        void SetupIndex()
        {
            // try parse content into index
            if (!int.TryParse(m_Text.text, out m_Index))
            {
                ErrorHandler.Error("Unable to identitfy index of QuickBuild button : " + m_Text.text);
                m_Index = -1;
                return;
            }

            // remove one for true index
            m_Index -= 1;

            if (m_Index < 0 || m_Index >= CharacterBuildsCloudData.N_BUILDS)
            {
                ErrorHandler.Error("Unable to identitfy index of QuickBuild button : " + m_Text.text);
                m_Index = -1;
                return;
            }
        }

        #endregion


        #region Listeners

        void OnClick()
        {
            if (CharacterBuildsCloudData.CurrentBuildIndex == m_Index)
                return;

            CharacterBuildsCloudData.SelectBuild(m_Index);
        }

        void OnCurrentBuildChanged()
        {
            Activate(CharacterBuildsCloudData.CurrentBuildIndex == m_Index);
        }

        #endregion
    }
}