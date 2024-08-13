using Assets.Scripts.Managers.Sound;
using MyBox;
using Save;
using Save.RSDs;
using TMPro;
using Tools;

namespace Menu.PopUps.PopUps.MessagePopUps
{
    public class PseudoPopUp : MessagePopUp
    {
        #region Members

        TMP_InputField  m_InputField;
        TMP_InputField  m_TokenInputField;
        TMP_Text        m_ErrorMessage;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_InputField        = Finder.FindComponent<TMP_InputField>(m_WindowContent, "InputField");
            m_TokenInputField   = Finder.FindComponent<TMP_InputField>(m_WindowContent, "TokenInputField");
            m_ErrorMessage      = Finder.FindComponent<TMP_Text>( m_WindowContent,      "ErrorMessage");

            if (ProfileCloudData.Token != "")
                m_TokenInputField.gameObject.SetActive(false);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_ErrorMessage.text = "";
        }

        #endregion


        #region GUI Manipulators

        protected override void SetUpMessage()
        {
            if (m_Message.IsNullOrEmpty())
                return;

            m_MessageText.text = m_Message;
        }

        #endregion


        #region Buttons

        protected override void OnUIButton(string bname)
        {
            switch (bname)
            {
                case "Background":
                    break;

                case "ValidateButton":
                    OnValidateButton();
                    break;

                default:
                    base.OnUIButton(bname);
                    break;
            }
        }

        #endregion


        #region Listeners

        protected async override void OnValidateButton()
        {
            // CHECK : Gamer tag
            (bool success, string reason) = await ProfileCloudData.IsGamerTagValid(m_InputField.text);
            if (! success)
            {
                // play error sound
                SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);

                // display why is not valid
                m_ErrorMessage.text = reason;
                return;
            }

            // CHECK : Token
            if (ProfileCloudData.Token == "")
            {
                (success, reason) = await ProfileCloudData.IsTokenValid(m_TokenInputField.text);
                if (!success)
                {
                    // play error sound
                    SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);

                    // display why is not valid
                    m_ErrorMessage.text = reason;
                    return;
                }

                ProfileCloudData.SetToken(m_TokenInputField.text);
            }

            ProfileCloudData.SetGamerTag(m_InputField.text);

            base.OnValidateButton();
            Exit();
        }

        #endregion
    }
}