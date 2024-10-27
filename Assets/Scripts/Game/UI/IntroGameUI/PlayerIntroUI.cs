using Enums;
using Managers;
using Menu.Common.Buttons;
using Menu.MainMenu;
using TMPro;
using Tools;
using UnityEngine;

namespace Game.UI
{
    public class PlayerIntroUI : MObject
    {
        #region Members

        ProfileDisplayUI    m_ProfileDisplayUI;

        GameObject          m_CharacterDisplayUI;
        GameObject          m_CharacterContainer;
        TMP_Text            m_Level;
        TemplateRuneItemUI  m_RunePrimal;
        TemplateRuneItemUI  m_RuneMajor;
        TemplateRuneItemUI  m_RuneMinor;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_ProfileDisplayUI = Finder.FindComponent<ProfileDisplayUI>(gameObject);

            m_CharacterDisplayUI    = Finder.Find(gameObject, "CharacterDisplay");
            m_CharacterContainer    = Finder.Find(m_CharacterDisplayUI, "CharacterContainer");
            m_Level                 = Finder.FindComponent<TMP_Text>(m_CharacterDisplayUI, "Level");
            m_RunePrimal            = Finder.FindComponent<TemplateRuneItemUI>(gameObject, "RunePrimal");
            m_RuneMajor             = Finder.FindComponent<TemplateRuneItemUI>(gameObject, "RuneMajor");
            m_RuneMinor             = Finder.FindComponent<TemplateRuneItemUI>(gameObject, "RuneMinor");
        }

        public void Initialize(SPlayerData playerData)
        {
            base.Initialize();

            m_ProfileDisplayUI.Initialize(playerData.ProfileData);

            // init character and spell level
            UIHelper.SpawnCharacter(playerData.Character.ToString(), m_CharacterContainer, "Overlay");
            m_Level.text = "Level " + playerData.CharacterLevel;

            // init runes
            m_RunePrimal.Initialize(playerData.Runes.Length > 0 ? playerData.Runes[0] : ERune.None, true);
            m_RuneMajor.Initialize(playerData.Runes.Length > 1 ? playerData.Runes[1] : ERune.None, true);
            m_RuneMinor.Initialize(playerData.Runes.Length > 2 ? playerData.Runes[2] : ERune.None, true);
        }

        #endregion
    }
}