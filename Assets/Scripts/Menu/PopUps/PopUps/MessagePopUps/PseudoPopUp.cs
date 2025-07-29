using Assets.Scripts.Managers.Sound;
using Enums;
using Managers.MainMenu;
using MyBox;
using Save;
using Save.RSDs;
using System.Threading.Tasks;
using TMPro;
using Tools;

namespace Menu.PopUps.PopUps.MessagePopUps
{
    public class PseudoPopUp : MessagePopUp
    {
        #region Members

        TMP_InputField  m_InputField;
        TMP_Text        m_ErrorMessage;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_InputField        = Finder.FindComponent<TMP_InputField>(m_WindowContent, "InputField");
            m_ErrorMessage      = Finder.FindComponent<TMP_Text>( m_WindowContent,      "ErrorMessage");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_ErrorMessage.text = "";
            m_ErrorMessage.gameObject.SetActive(false);
        }

        #endregion


        #region GUI Manipulators

        protected override void SetUpMessage()
        {
            if (m_Message.IsNullOrEmpty())
            {
                m_MessageText.transform.parent.parent.gameObject.SetActive(false);
                return;
            }

            m_MessageText.transform.parent.parent.gameObject.SetActive(true);
            m_MessageText.text = m_Message;
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
                m_ErrorMessage.gameObject.SetActive(true);
                m_ErrorMessage.text = reason;
                return;
            }

            if (! success)
                return;

            // update value in cloud data
            ProfileCloudData.SetGamerTag(m_InputField.text);

            // diseable popup in recurrent data
            RecurrentPopupManager.Instance.Diseable(EPopUpState.PseudoPopUp);

            base.OnValidateButton();
            Exit();
        }

        #endregion
    }
}