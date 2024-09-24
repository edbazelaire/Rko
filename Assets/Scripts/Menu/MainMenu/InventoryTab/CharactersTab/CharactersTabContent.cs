using Tools;
using UnityEngine;

namespace Menu.MainMenu
{
    public class CharactersTabContent : TabContent
    {
        /// <summary> handles the display of the character template buttons </summary>
        CharacterSelectionUI m_CharacterSelectionUI;

        #region Init & End

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            m_CharacterSelectionUI = Finder.FindComponent<CharacterSelectionUI>(gameObject);
            m_CharacterSelectionUI.Initialize();
        }

        #endregion
    }
}