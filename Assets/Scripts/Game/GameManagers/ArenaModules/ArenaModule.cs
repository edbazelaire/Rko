using Assets.Scripts.Data.GameManagement;
using Data.GameManagement;
using Enums;
using Game.GameManagers.Interfaces;
using Game.Spells;
using Game.StateEffects.Quests;
using Save;
using Save.Data.Progression.Structs;
using Tools;
using UnityEngine;


namespace Game.GameManagers.ArenaModules
{
    public class ArenaModule : MObject, IGameModule
    {
        #region Members

        protected ArenaData m_BaseArenaData;
        protected bool m_IsValid = true;

        ArenaData m_ArenaData => m_BaseArenaData;
        public bool CheckIsValid() => m_IsValid;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public virtual void Initialize(EArenaType arena, EArenaDifficulty arenaDifficulty, int extraDifficulty)
        {
            this.enabled = true;
            m_BaseArenaData = AssetLoader.LoadArenaData(arena, new SArenaDifficulty(arenaDifficulty, extraDifficulty));

            base.Initialize();
        }

        public void Deactivate()
        {
            this.enabled = false;
        }

        public void Shutdown()
        {
            UnRegisterListeners();
        }

        #endregion


        #region Timer Manipulations

        public virtual void SetUpTimer()
        {
            GameUIManager.GameTimerUI.Initialize(m_ArenaData.RoundDuration);
        }

        protected virtual void OnTimerEnd() { }

        #endregion


        #region Game Start

        protected virtual void OnGameRunning()
        {
            ApplyMetadata();
        }

        protected virtual void ApplyMetadata() { }

        #endregion


        #region Game Over

        protected virtual void OnGameOver()
        {
            SaveMetaData();
        }

        protected virtual void SaveMetaData()
        {
            foreach (StateEffect stateEffect in GameManager.Instance.Owner.StateHandler.StateEffects)
            {
                if (stateEffect is not ArenaQuestEffect arenaQuest)
                    continue;

                arenaQuest.Save();
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            GameManager.Instance.State.OnValueChanged += OnGameStateChanged;
            GameManager.TimerEndedEvent += OnTimerEnd;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            GameManager.TimerEndedEvent -= OnTimerEnd;

            if (GameManager.Exists)
                GameManager.Instance.State.OnValueChanged -= OnGameStateChanged;
        }

        #endregion


        #region GameManager State Listeners

        public virtual void OnGameStateChanged(EGameState _, EGameState newState)
        {
            switch (newState)
            {
                case EGameState.None:
                    break;

                case EGameState.WaitingForConnection:
                    OnWaitingForConnection();
                    break;

                case EGameState.PreparingGame:
                    OnPreparingGame();
                    break;

                case EGameState.Intro:
                    OnIntro();
                    break;

                case EGameState.GameRunning:
                    OnGameRunning();
                    break;

                case EGameState.GameOver:
                    OnGameOver();
                    break;
            }
        }

        protected virtual void OnWaitingForConnection() { }
        protected virtual void OnPreparingGame() { }
        protected virtual void OnIntro() { }

        #endregion
    }
}

