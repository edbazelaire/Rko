using Enums;
using Game.Spells;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Managers.Sound
{

    [Serializable]
    public struct SAppStateMusic
    {
        public EAppState State;
        public AudioClip AudioClip;
    }

    [Serializable]
    public struct SGameStateMusic
    {
        public EGameState State;
        public AudioClip AudioClip;
    }

    public class SoundFXManager : MonoBehaviour
    {
        #region Members

        public static SoundFXManager Instance;

        [Description("List of clips played based on app state")]
        [SerializeField] private List<SAppStateMusic> m_AppStateMusics;
        [SerializeField] private List<SGameStateMusic> m_GameStateMusics;

        [Header("Menu")]
        [SerializeField] private AudioClip m_ClickButtonSoundFX;
        [SerializeField] private AudioClip m_ErrorSoundFX;
        [SerializeField] private AudioClip m_LevelUpSoundFX;
        [SerializeField] private AudioClip m_OpenOverlayScreenSoundFX;

        [Header("PopUps")]
        [SerializeField] private AudioClip m_ProgressBarSoundFX;
        [SerializeField] private AudioClip m_OpenPopUpSoundFX;
        [SerializeField] private AudioClip m_GoldsCollectedSoundFX;
        [SerializeField] private AudioClip m_AchievementRewardCollectedSoundFX;
        [SerializeField] private AudioClip m_RewardCollectedSoundFX;
        [SerializeField] private AudioClip m_DefaultChestOpenSoundFX;

        [Header("Game")]
        [SerializeField] private AudioClip m_DefaultCastSoundFX;
        [SerializeField] private AudioClip m_DefaultOnHitSoundFX;
        [SerializeField] private AudioClip m_WinSoundFX;
        [SerializeField] private AudioClip m_LossSoundFX;


        private AudioSource m_AudioSource;
        private AudioSource m_FXAudioSource;

        public static AudioSource MusicAudioSource => Instance.m_AudioSource;
        public static AudioClip LevelUpSoundFX => Instance.m_LevelUpSoundFX;
        public static AudioClip ClickButtonSoundFX => Instance.m_ClickButtonSoundFX;
        public static AudioClip ErrorSoundFX => Instance.m_ErrorSoundFX;
        public static AudioClip ProgressBarSoundFX => Instance.m_ProgressBarSoundFX;
        public static AudioClip OpenOverlayScreenSoundFX => Instance.m_OpenOverlayScreenSoundFX;
        public static AudioClip OpenPopUpSoundFX => Instance.m_OpenPopUpSoundFX;
        public static AudioClip GoldsCollectedSoundFX => Instance.m_GoldsCollectedSoundFX;
        public static AudioClip AchievementRewardCollectedSoundFX => Instance.m_AchievementRewardCollectedSoundFX;
        public static AudioClip RewardCollectedSoundFX => Instance.m_RewardCollectedSoundFX;
        public static AudioClip DefaultChestOpenSoundFX => Instance.m_DefaultChestOpenSoundFX;

        // ==================================================================================================
        // GAME
        public static AudioClip DefaultCastSoundFX => Instance.m_DefaultCastSoundFX;
        public static AudioClip DefaultOnHitSoundFX => Instance.m_DefaultOnHitSoundFX;
        public static AudioClip WinSoundFX => Instance.m_WinSoundFX;
        public static AudioClip LossSoundFX => Instance.m_LossSoundFX;

        #endregion


        #region Init & End

        private void Awake()
        {
            Instance = this;
            m_AudioSource = Finder.FindComponent<AudioSource>(gameObject, "AudioSource");
            m_FXAudioSource = Finder.FindComponent<AudioSource>(gameObject, "FXAudioSource");

            RefreshMusicVolume();

            DontDestroyOnLoad(gameObject);
        }

        #endregion


        #region Base Audio
        
        public static void PlayMusic(AudioClip audioClip)
        {
            // assign clip
            Instance.m_AudioSource.clip = audioClip;

            // play it
            Instance.m_AudioSource.Play();
        }

        public static void PlayStateMusic(EAppState appState)
        {
            int index = Instance.m_AppStateMusics.FirstIndex(appStateMusic => appStateMusic.State == appState);
            if (index == -1)
                return;

            PlayMusic(Instance.m_AppStateMusics[index].AudioClip);
        }

        public static void PlayStateMusic(EGameState gameState)
        {
            int index = Instance.m_GameStateMusics.FirstIndex(stateMusic => stateMusic.State == gameState);
            if (index == -1)
                return;

            PlayMusic(Instance.m_GameStateMusics[index].AudioClip);
        }

        #endregion  


        #region Play FX

        public static AudioSource PlaySoundFXClip(AudioClip audioClip, Transform transform = null)
        {
            // spawn audio source
            AudioSource audioSource = Instantiate(Instance.m_FXAudioSource, transform);
            audioSource.loop = true;

            // assign clip
            audioSource.clip = audioClip;

            // assign volume
            audioSource.volume = GetVolume(EVolumeOption.SoundEffectsVolume);

            // play sound
            audioSource.Play();

            // return audio source
            return audioSource;
        }

        public static void PlayOnce(AudioClip audioClip)
        {
            if (audioClip == null) 
                return;

            var audioSource = PlaySoundFXClip(audioClip, null);
            audioSource.loop = false;

            Destroy(audioSource.gameObject, audioClip.length);
        }

        public static void PlayOnce(string clipName)
        {
            var audioClip = AssetLoader.Load<AudioClip>(clipName, AssetLoader.c_SoundsPath);
            if (audioClip == null)
                return;

            PlayOnce(audioClip);
        }

        #endregion


        #region Volume

        public static void RefreshMusicVolume()
        {
            Instance.m_AudioSource.volume = GetVolume(EVolumeOption.MusicVolume);
        }

        static float GetVolume(EVolumeOption volumeOption)
        {
            if (PlayerPrefsHandler.GetMuted(volumeOption) || PlayerPrefsHandler.GetMuted(EVolumeOption.MasterVolume))
                return 0f;

            return PlayerPrefsHandler.GetVolume(EVolumeOption.MasterVolume) * PlayerPrefsHandler.GetVolume(volumeOption);
        }

        #endregion


        #region Listeners


        #endregion
    }
}