using Assets;
using Assets.Scripts.Managers.Sound;
using Enums;
using Network;
using Save;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.MainMenu.MainTab
{
    public class MainTab : MainMenuTabContent
    {
        #region Members

        const string            c_CharacterPreviewSection           = "CharacterPreviewSection";
        const string            c_PlayButton                        = "PlayButton";
        const string            c_Dropdown                          = "Dropdown";

        CharacterPreviewSectionUI   m_CharacterPreviewSection;
        CharacterSelectionWindow    m_CharacterSelection;
        GameSectionUI               m_GameSectionUI;
        Button                      m_PlayButton;
        TMP_Dropdown                m_GameTypeDropDown;

        #endregion


        #region Init & End 

        protected override void FindComponents()
        {
            base.FindComponents(); 

            m_CharacterPreviewSection           = Finder.FindComponent<CharacterPreviewSectionUI>(gameObject, c_CharacterPreviewSection);
            m_CharacterSelection                = Finder.FindComponent<CharacterSelectionWindow>(gameObject, "CharacterSelectionWindow");
            m_GameSectionUI                     = Finder.FindComponent<GameSectionUI>(gameObject, "GameSection");
            m_PlayButton                        = Finder.FindComponent<Button>(gameObject, c_PlayButton);
            m_GameTypeDropDown                  = Finder.FindComponent<TMP_Dropdown>(gameObject, c_Dropdown);
        }

        public override void Initialize(TabButton tabButton, AudioClip activationSoundFX)
        {
            base.Initialize(tabButton, activationSoundFX);

            // initialize GameSectionUI
            m_GameSectionUI.Initialize();

            // create all buttons for characters
            m_CharacterSelection.Initialize();
            m_CharacterSelection.gameObject.SetActive(false);

            // set game modes
            UIHelper.SetUpDropdown<EGameMode>(m_GameTypeDropDown, PlayerPrefsHandler.GetGameMode(), OnDropDown);

            // register to events
            m_PlayButton.onClick.AddListener(OnPlay);

            // initialize character preview section (with a delay to avoid issue with size)
            CoroutineManager.DelayMethod(m_CharacterPreviewSection.Initialize);
            // delay register method by one frame (to avoid issue with Awake() order)
            CoroutineManager.DelayMethod(() => { m_CharacterPreviewSection.CharacterPreviewButton.onClick.AddListener(ToggleCharacterSelection); });

            // check if player is in a game currently
            CheckCurrentGameId();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            m_PlayButton.onClick.RemoveAllListeners();
        }

        #endregion


        #region Activation / Deactivation

        public override void Activate(bool activate)
        {
            base.Activate(activate);

            m_CharacterPreviewSection.Activate(activate);

            if (!activate)
                OnDeactivation();
        }

        void OnDeactivation()
        {
            if (m_CharacterSelection.IsOpened)
                m_CharacterSelection.Close();
        }

        #endregion


        #region Checkers

        async void CheckCurrentGameId()
        {
            if (PlayerPrefs.GetString(EPlayerPref.CurrentGameId.ToString(), "") == "")
                return;

            var joined = await LobbyHandler.Instance.JoinLobby(PlayerPrefs.GetString(EPlayerPref.CurrentGameId.ToString()));
            if (! joined)
            {
                ErrorHandler.Warning("Unable to join lobby with id : " + PlayerPrefs.GetString(EPlayerPref.CurrentGameId.ToString()));
                PlayerPrefs.SetString(EPlayerPref.CurrentGameId.ToString(), "");
                return;
            }

            // TODO : reconnect to the game
            ErrorHandler.Error("[TODO] Joined lobby with id : " + PlayerPrefs.GetString(EPlayerPref.CurrentGameId.ToString()));
        }

        bool CheckBeforePlaying()
        {
            if (!CharacterBuildsCloudData.IsCurrentBuildOk)
            {
                SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);
                Main.ErrorMessagePopUp("Current build is not valid");
                return false;
            }

            if (PlayerPrefsHandler.GetGameMode() == EGameMode.Arena && ProgressionCloudData.IsArenaCompleted(PlayerPrefsHandler.GetArenaType()))
            {
                SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);
                Main.ErrorMessagePopUp("This arena has already beed completed");
                return false;
            }

            if (PlayerPrefsHandler.GetGameMode() == EGameMode.Arena && ProgressionCloudData.IsArenaDifficultyCompleted(PlayerPrefsHandler.GetArenaType()))
            {
                SoundFXManager.PlayOnce(SoundFXManager.ErrorSoundFX);
                Main.ErrorMessagePopUp("You need to collect your rewards to unlock the next level of difficulty.\nClick the Arena button to display the Arena Path of Rewards and click rewards to collect them !");
                return false;
            }

            return true;
        }

        #endregion


        #region Lobby

        async void JoinLobby()
        {
            // set the button as selected
            await LobbyHandler.Instance.QuickJoinLobby();
        }

        void LeaveLobby()
        {
            LobbyHandler.Instance.LeaveLobby();
            Main.SetState(EAppState.MainMenu);
        }

        #endregion


        #region Event Listeners

        /// <summary>
        /// Activate / Deactivate character selection
        /// </summary>
        public void ToggleCharacterSelection()
        {
            if (m_CharacterSelection == null)
                return;

            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            if (!m_CharacterSelection.gameObject.activeInHierarchy)
                m_CharacterSelection.Open();
            else
                m_CharacterSelection.Close();
        }

        /// <summary>
        /// Action enabled when the play button is clicked : quick / leave lobby
        /// </summary>
        void OnPlay()
        {
            SoundFXManager.PlayOnce(SoundFXManager.ClickButtonSoundFX);

            // check that everyting is working properly
            if (!CheckBeforePlaying())
                return;

            if (Main.State == EAppState.Lobby)
            {
                LeaveLobby();
                return;
            }

            Main.SetPopUp(EPopUpState.LobbyScreen);

            JoinLobby();
        }

        void OnDropDown(EGameMode gameMode)
        {
            if (gameMode == PlayerPrefsHandler.GetGameMode())
                return;

            PlayerPrefsHandler.SetGameMode(gameMode);
        }

        #endregion
    }
}
