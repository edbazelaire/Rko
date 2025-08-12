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
    public class PromoCodePopUp : PopUp
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
            m_ErrorMessage      = Finder.FindComponent<TMP_Text>(m_WindowContent,      "ErrorMessage");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_ErrorMessage.text = "";
        }

        #endregion


        #region GUI Manipulators


        #endregion


        #region Listeners

        protected async override void OnValidateButton()
        {
            // CHECK : Code
            (bool success, string reason) = GiftCodeRSD.Instance.IsPromoCodeValid(m_InputField.text.Trim(), out SPromoCodeData data);

            // TRY : collection
            if (success)
            {
                // Collect code rewards
                (success, reason) = await GiftCodeRSD.Instance.Collect(data);
            }

            if (!success)
            {
                // play error sound
                SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);

                // display why is not valid
                m_ErrorMessage.text = reason;
                return;
            }

            base.OnValidateButton();
        }

        #endregion
    }
}