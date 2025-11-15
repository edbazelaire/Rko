using Assets;
using Assets.Scripts.Data.GameManagement;
using Data;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Game.Spells;
using Save;
using System;
using System.Collections;
using Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace Game.GameManagers.ArenaModules
{
    public enum EArenaMetadataKeys
    {
        CorruptionStacks    = 0,
        BossHp              = 1,
    }

    public class EternalMenagerieModule : ArenaModule
    {
        #region Members

        const float OVERTIME = 45f;

        bool m_Abort = false;

        Controller      m_PlayerController;
        Controller      m_BossController;
        SpawnerData     m_SpawnerData;
        bool            m_TimerEnded                = false;
        bool            m_BossPhaseStarted          = false;

        float           m_BossHpThreshold   => (float)(m_ArenaData.MaxLevel - m_ArenaData.CurrentLevel) / (m_ArenaData.MaxLevel + 1);
        EternalMenagerieData m_ArenaData => m_BaseArenaData as EternalMenagerieData;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public override void Initialize(EArenaType arena, EArenaDifficulty arenaDifficulty, int extraDifficulty)
        {
            base.Initialize(arena, arenaDifficulty, extraDifficulty);
            m_TimerEnded = false;
            m_BossPhaseStarted = false;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!GameManager.IsGameOver)
                ErrorHandler.Error("This module has beed destroyed before the end of the Game");
        }

        #endregion


        #region Intro

        protected override void OnIntro()
        {
            base.OnIntro();

            m_IsValid = false;

            // get Boss controller
            FindCharacters();

            if (m_Abort)
                return;

            // initialize Current Data
            InitArenaMetaData();

            // deactivate boss at the start of the fight
            m_BossController.Activate(false);

            // prepare game portal for spawns
            StartCoroutine(PreparePortal());
        }

        /// <summary>
        /// Find the Player and the Boss
        /// </summary>
        void FindCharacters()
        {
            if (m_Abort)
                return;

            m_PlayerController = null;
            m_BossController = null;
            foreach (Controller controller in GameManager.Instance.Controllers.Values)
            {
                // Find : PLAYER
                if (controller.PlayerId == 0)
                {
                    if (m_PlayerController != null)
                        ErrorHandler.Warning("Found multiple PLAYER controllers : " + controller.name + " and " + m_PlayerController.name);
                    m_PlayerController = controller;
                }

                // Find : BOSS
                if (CharacterLoader.IsBoss(controller.Character))
                {
                    if (m_BossController != null)
                        ErrorHandler.Warning("Found multiple BOSS controllers : " + controller.name + " and " + m_BossController.name);
                    m_BossController = controller;
                }
            }

            if (m_BossController == null)
            {
                GameManager.ExitWithError("Unable to find BOSS Controller during Intro");
                m_Abort = true;
                return;
            }

            if (m_PlayerController == null)
            {
                GameManager.ExitWithError("Unable to find BOSS Controller during Intro");
                m_Abort = true;
                return;
            }
        }

        /// <summary>
        /// Initialize the fight with the "CurrentArena" metadata 
        ///     - Boss HP
        ///     - Corrupted Power stacks
        /// </summary>
        void InitArenaMetaData()
        {
            // set Boss HP
            int baseBossHp = ProgressionCloudData.CurrentArena.GetMetaData<int>(EArenaMetadataKeys.BossHp.ToString());
            if (baseBossHp > 0)
                m_BossController.Life.SetHp(baseBossHp);
        }

        IEnumerator PreparePortal()
        {
            // Debug tool - skip waves and start the boss right away
            if (Main.SkipWaves)
            {
                m_IsValid = true;
                yield break;
            }

            m_SpawnerData = m_ArenaData.GetSpawnerData();

            yield return new WaitForSeconds(0.2f);

            m_IsValid = true;
        }

        #endregion


        #region OnGameRunning

        protected override void OnGameRunning()
        {
            base.OnGameRunning();

            // Debug tool - skip waves and start the boss right away
            if (Main.SkipWaves)
            {
                StartCoroutine(StartBossPhase());
                return;
            }

            // get spell when it spawns
            m_SpawnerData.OnSpellSpawn += OnSpellSpawn;

            // cast the "Spawner" spell
            m_SpawnerData.Cast(
                clientId:   m_BossController.PlayerId,
                target:     Vector3.zero
            );
        }

        protected override void ApplyMetadata()
        {
            base.ApplyMetadata();

            // set Player Corruption Stacks
            int corruptionStacks = ProgressionCloudData.CurrentArena.GetMetaData<int>(EArenaMetadataKeys.CorruptionStacks.ToString());
            if (corruptionStacks > 0)
            {
                GameManager.Instance.Owner.StateHandler.AddStateEffect(
                    EStateEffect.CorruptedPower,
                    caster: m_BossController,
                    level: m_BossController.CharacterLevel,
                    stacks: corruptionStacks,
                    origin: "",
                    force: true     // ignore requirements
                );

                // remove "N Corruption Stacks" from quantity of
                int stacks = Math.Min(ArenaManagementData.NCorruptionStacksLossOnDeath, corruptionStacks);
                ProgressionCloudData.CurrentArena.SetMetaData(EArenaMetadataKeys.CorruptionStacks.ToString(), (corruptionStacks - stacks).ToString());
            }
        }

        IEnumerator StartBossPhase()
        {
            // if timer already ended - WIN
            if (IsTimerOver(timeMarge: 10f))
            {
                GameManager.Instance.GameOver(0);
                yield break;
            }

            m_BossPhaseStarted = true;

            // PLAY BOSS ANIMATION 
            if (! Main.SkipBossAnimations)
            {
                yield return PlayBossAnimation();
            }

            // activate the boss and start the fight
            m_BossController.Activate(true);
            m_BossController.Life.Hp.OnValueChanged += OnBossHpChanged;
        }

        IEnumerator PlayBossAnimation()
        {
            GameUIManager.TutoGameUI.Initialize();
            GameUIManager.TutoGameUI.Activate(true);
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("Pathetic !", ECaptionType.Exclamation, showSpeaker: false);
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("I will handle this myself", showSpeaker: false);

            yield return SpawnBossEffect();

            yield return GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("Your end is here !", ECaptionType.Exclamation, showSpeaker: false);
        }

        IEnumerator SpawnBossEffect()
        {
            var effect = AssetLoader.Load<GameObject>("CorruptedTornado", AssetLoader.c_ParticlesPrefabPath);
            if (effect == null)
            {
                ErrorHandler.Error("Unable to find FirePortal object");
                yield break;
            }

            effect = Instantiate(effect, m_BossController.transform.position, Quaternion.identity);

            yield return new WaitForSeconds(3f);

            Destroy(effect);

            var pos = m_BossController.transform.position + new Vector3(0f, 1f, 0f);
            var explosion = Instantiate(AssetLoader.Load<GameObject>("CorruptedTornado_END", AssetLoader.c_ParticlesPrefabPath), pos, Quaternion.identity);
            m_BossController.gameObject.SetActive(true);

            yield return new WaitForSeconds(0.5f);

            Destroy(explosion);
        }

        #endregion


        #region Over Time

        IEnumerator StartOverTime()
        {
            GameUIManager.GameTimerUI.StartOverTime(OVERTIME);

            while (GameManager.IsGameRunning)
            {
                if (m_PlayerController == null || !m_PlayerController.Life.IsAlive)
                {
                    ErrorHandler.Warning("Player Controller not accessible");
                    yield break;
                }

                m_PlayerController.StateHandler.AddStateEffect(EStateEffect.CorruptedPower, m_BossController, "OverTime", stacks: 1, level: 1, force: true);
                yield return new WaitForSeconds(1f);
            }
        }

        #endregion


        #region Timer Events

        public bool IsTimerOver(float timeMarge = 0f)
        {
            if (m_ArenaData.IsLastBoss(m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage))
                return false;

            return m_TimerEnded || timeMarge > GameUIManager.GameTimerUI.Timer;
        }

        public override void SetUpTimer()
        {
            // no timer on last boss
            if (m_ArenaData.IsLastBoss(m_ArenaData.CurrentLevel, m_ArenaData.CurrentStage))
            {
                GameUIManager.GameTimerUI.gameObject.SetActive(false);
                return;
            }

            base.SetUpTimer();
        }

        protected override void OnTimerEnd()
        {
            m_TimerEnded = true;

            // timer ended during OVER TIME - LOSS
            if (GameUIManager.GameTimerUI.IsOverTimer)
            {
                GameUIManager.GameTimerUI.gameObject.SetActive(false);
                GameManager.Instance.GameOver(1);
                return;
            }

            // boss spawned - VICTORY
            if (m_BossPhaseStarted)
            {
                GameUIManager.GameTimerUI.gameObject.SetActive(false);
                GameManager.Instance.GameOver(0);
                return;
            }

            // start the overtime effect
            StartCoroutine(StartOverTime());
            return;
        }

        #endregion


        #region Game Over

        protected override void OnGameOver()
        {
            base.OnGameOver();
            SaveMetaData();
        }

        protected override void SaveMetaData()
        {
            base.SaveMetaData();

            // only save if is winning
            if (GameManager.Instance.WinningTeam != GameManager.Instance.Owner.Team)
                return;

            // calculate number of stacks to save 
            int corruptedPowerStacks = GameManager.Instance.Owner.StateHandler.GetStacks(EStateEffect.CorruptedPower);
            // -- if the boss is defeated before the timer - retrieve some stacks
            if (GameUIManager.GameTimerUI.Timer > 0)
                corruptedPowerStacks = Math.Max(0, corruptedPowerStacks - ArenaManagementData.NCorruptionStacksLossOnWin);

            // SAVE : number of Corrupted Stacks
            ProgressionCloudData.SetArenaMetaData(
                EArenaMetadataKeys.CorruptionStacks.ToString(),
                corruptedPowerStacks.ToString(),
                save: false);

            // SAVE : Boss HP
            ProgressionCloudData.SetArenaMetaData(
                EArenaMetadataKeys.BossHp.ToString(), 
                m_BossController.Life.Hp.Value.ToString(), 
                save: true);
        }


        #endregion


        #region GameManager State Events

        protected override void OnWaitingForConnection()
        {
            base.OnWaitingForConnection();
        }

        protected override void OnPreparingGame()
        {
            base.OnPreparingGame();
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        /// <summary>
        /// If boos hp < threshold - end the game
        /// </summary>
        /// <param name="_"></param>
        /// <param name="hp"></param>
        void OnBossHpChanged(int _, int hp)
        {
            // let the GameManager handle death
            if (m_BossController.Life.PercHp <= 0)
                return;

            // Above Threshold ? do nothing
            if (m_BossController.Life.PercHp > m_BossHpThreshold)
                return;

            // BELOW Threshold - end game
            GameManager.Instance.GameOver(0);
        }

        /// <summary>
        /// When the spell spawns, attach OnSpellEnd method to it
        /// </summary>
        /// <param name="spell"></param>
        void OnSpellSpawn(Spell spell)
        {
            spell.OnSpellEvent += OnSpellEnded;
        }

        /// <summary>
        /// When the spell spawns, attach OnSpellEnd method to it
        /// </summary>
        /// <param name="spell"></param>
        void OnSpellEnded(ESpellEvent spellEvent)
        {
            // await at least "OnEnd" event
            if (spellEvent != ESpellEvent.OnEnd)
                return;

            // Boss phase already started ? - exit
            if (m_BossPhaseStarted)
                return;

            // OverTime ? - win
            if (GameUIManager.GameTimerUI.IsOverTimer)
            {
                GameManager.Instance.GameOver(0);
                return;
            }

            StartCoroutine(StartBossPhase());
        }

        #endregion
    }
}

