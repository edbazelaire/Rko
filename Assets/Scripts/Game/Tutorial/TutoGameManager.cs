using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using NUnit.Framework.Constraints;
using Save;
using System;
using System.Collections;
using Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace Game
{
    public class TutoGameManager : MObject
    {
        #region Members 

        static TutoGameManager s_Instance;

        public static Vector3 EnemySpawnPosition => new Vector3(5f, 0.8f, 0f);

        // =====================================================================
        // GameObjects & Components
        FocusManager    m_FocusManager;
        PositionMarker  m_PositionMarker;

        // =====================================================================
        // Data
        Controller      m_Controller;
        TutorialBT      m_Enemy;
        string          m_CurrentCoroutineName;

        bool m_IsWaitingForSpell = false;
        bool m_EndStarted = false;
        bool m_HealAnimationDone = false;

        bool m_IsWaiting => m_IsWaitingForSpell;

        public static TutoGameManager Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = FindAnyObjectByType<TutoGameManager>();
                if (s_Instance == null)
                    s_Instance = new GameObject("TutoGameManager").AddComponent<TutoGameManager>();

                if (!s_Instance.m_Initialized)
                    s_Instance.Initialize();

                return s_Instance;
            }
        }

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_FocusManager = Finder.FindComponent<FocusManager>(GameUIManager.TutoGameUI.gameObject);
            m_PositionMarker = AssetLoader.Load<PositionMarker>("PositionMarker", AssetLoader.c_TutoGameObjectsPath);
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            StopAllCoroutines();
        }

        #endregion


        #region Activation

        public void Activate(Controller controller, Controller enemyController)
        {
            // save controller
            m_Controller = controller;
            m_Enemy = enemyController.BehaviorTree as TutorialBT;

            // make sure this character never dies
            controller.Life.Hp.OnValueChanged += OnHpChanged;

            // activate TutoManager
            gameObject.SetActive(true);

            // activate TutoUI
            GameUIManager.TutoGameUI.gameObject.SetActive(true);
            GameUIManager.TutoGameUI.Initialize();

            // setup controller for Tutorial (remove actions and spells)
            foreach (ESpell spell in m_Controller.SpellHandler.Spells)
            {
                m_Controller.SpellHandler.SetSpellSelection(spell, ESpellSelectionState.Inactive);
            }
            m_Controller.SpellHandler.Activate(false);
            m_Controller.AutoAttackHandler.Activate(false);

            // start the tutorial coroutine
            StartCoroutine(StartTuto());
        }

        IEnumerator StartTuto()
        {
            // activate Tuorial UI
            GameUIManager.TutoGameUI.Activate(true);

            // hide the enemy
            m_Enemy.Hide();                                     

            // presentation
            yield return GameUIManager.TutoGameUI.PlayPresentation();

            // start game
            GameManager.Instance.SetState(EGameState.GameRunning);

            // deactivate spells
            LockSpells();

            // deactivate auto attacks
            m_Controller.AutoAttackHandler.Activate(false);     

            // movement tutorial
            yield return StartMovementTuto();

            // Spawn Enemy
            yield return SpawnEnemy();

            // Start Fight
            yield return StartFight();
        }

        #endregion


        #region Movement Tutorial

        void Pause(bool pause)
        {
            m_Enemy.Pause(pause);

            if (pause)
            {
                LockSpells();
                m_Controller.AnimationHandler.CancelCurrentAnimation();
            }

            m_Controller.AutoAttackHandler.Activate(!pause);
            m_Controller.Movement.Activate(!pause);
        }

        IEnumerator StartMovementTuto()
        {
            yield return GameUIManager.TutoGameUI.MovementDialog();

            PositionMarker marker = SpawnPositionMarker(-1.5f);
            yield return new WaitUntil(() => marker == null || marker.IsDestroyed());

            StartCoroutine(GameUIManager.TutoGameUI.Speaker.Write("Well Played ! Now get this one !", showSpeaker: false));

            marker = SpawnPositionMarker(-5.5f);
            yield return new WaitUntil(() => marker == null || marker.IsDestroyed());

            yield return GameUIManager.TutoGameUI.Speaker.Write("Well Done !", showSpeaker: false);
        }

        PositionMarker SpawnPositionMarker(float xPos)
        {
            return Instantiate(m_PositionMarker, new Vector3(xPos, 0f, 0f), Quaternion.identity);
        }

        #endregion


        #region Enemy Management

        IEnumerator SpawnEnemy()
        {
            // dialog 
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("... !", showSpeaker: false);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Who goes there !");

            StartCoroutine(ActivatePortal());
            yield return new WaitForSeconds(1f);

            // Spawn Kahnan and make him move to the spawn position
            yield return m_Enemy.StartMovement();
        }

        IEnumerator ActivatePortal()
        {
            var portal = AssetLoader.Load<GameObject>("FirePortal", AssetLoader.c_ParticlesPrefabPath);
            if (portal == null)
            {
                ErrorHandler.Error("Unable to find FirePortal object");
                yield break;
            }

            portal = Instantiate(portal, EnemySpawnPosition, Quaternion.identity);

            yield return new WaitForSeconds(3f);

            Destroy(portal);
        }

        IEnumerator StartFight()
        {
            // -- Khanan
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.Write("I am Kahnan !", ECaptionType.Normal);
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.Write("Humble servent of the true King", ECaptionType.Normal);
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("Kneel or burn !", ECaptionType.Exclamation);

            // -- Alexander
            yield return GameUIManager.TutoGameUI.Speaker.Write("He is about to attack us !", ECaptionType.Normal);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Dodge !", ECaptionType.Exclamation, showSpeaker: false);

            // 1 - stage : 3 AutoAttacks
            yield return AttackUntilDodged(3, 2f);

            // 2 - stage : Fire Barrage
            // -- Alexander
            yield return GameUIManager.TutoGameUI.Speaker.Write("Well done !", ECaptionType.Exclamation, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Oh oh what is this..", ECaptionType.Normal, showSpeaker: true);

            yield return DodgeFireBarrage();

            // 3 - stage : Heal
            //yield return StartEnemyUltimateAnimation();
            if (!m_HealAnimationDone)
                yield return StartHealAnimation();

            // SET PAUSE
            Pause(true);

            // 4 - stage : Activate AutoAttacks
            // -- Alexander
            yield return GameUIManager.TutoGameUI.Speaker.Write("Now this is OUR turn !", ECaptionType.Exclamation, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Stay still to attack", ECaptionType.Normal, showSpeaker: true);

            yield return WaitUntilShot(5);

            // 5 - stage : Use special ability
            // -- Alexander
            yield return GameUIManager.TutoGameUI.Speaker.Write("Well done !", ECaptionType.Exclamation, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Now use a Special Ability !", ECaptionType.Normal, showSpeaker: true);
            GameUIManager.TutoGameUI.Speaker.Activate(false);

            StartCoroutine(WaitForSpell(ESpell.RockShower, m_Controller, ESpellEvent.OnEnd, 1));
            yield return LockOnSpell(ESpell.RockShower);

            yield return new WaitUntil(() => m_IsWaiting);
            yield return new WaitForSeconds(0.5f);

            // 6 - stage : FIGHT
            yield return GameUIManager.TutoGameUI.Speaker.Write("You've done it", ECaptionType.Normal, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Now... Let's FIGHT !", ECaptionType.Exclamation, showSpeaker: true);

            // give full freedom to player and AI
            m_Enemy.Controller.Life.Hp.OnValueChanged += OnEnemyHpValueChanged;
            UnlockSpells();            // -- unlock all spells after that
            Pause(false);
        }

        public IEnumerator AttackUntilDodged(int n = -1, float interval = 0f)
        {
            int counter = 0;
            int counterHit = 0;

            void OnSpellEvent(ESpellEvent spellEvent)
            {
                if (spellEvent == ESpellEvent.OnEnd)
                {
                    counter++;
                    GameUIManager.TutoGameUI.ObjectifDisplayer.UpdateObjectif(counter - counterHit);
                }

                if (spellEvent == ESpellEvent.OnHit)
                {
                    counterHit++;
                }
            }

            void OnSpellSpawn(Spell spell)
            {
                // check is right spell
                if (spell.SpellData.Spell != m_Enemy.Controller.SpellHandler.AutoAttack || spell.Caster.PlayerId != m_Enemy.Controller.PlayerId)
                    return;

                spell.OnSpellEvent += OnSpellEvent;
            }

            Spell.OnSpellSpawn += OnSpellSpawn;

            GameUIManager.TutoGameUI.ObjectifDisplayer.SetObjectif("Dodge", n);

            counter = 0;
            counterHit = 0;

            yield return m_Enemy.Attack(-1, interval);

            yield return new WaitUntil(() => counter - counterHit >= n);

            m_Enemy.StopAttacking();
            GameUIManager.ClearAllSpells();

            StartCoroutine(GameUIManager.TutoGameUI.ObjectifDisplayer.Completed());

            Spell.OnSpellSpawn -= OnSpellSpawn;
        }

        public IEnumerator DodgeFireBarrage()
        {
            int n = ((MultiProjectilesData)SpellLoader.GetSpellData(ESpell.FireBarrage)).NProjectiles;
            int ctr = 0;
            int ctrHit = 0;

            void OnSpellEvent(ESpellEvent spellEvent)
            {
                switch (spellEvent)
                {
                    case ESpellEvent.OnHit:
                        ctrHit++;
                        break;

                    case ESpellEvent.OnEnd:
                        ctr++;
                        break;
                }
            }

            void OnSpellSpawn(Spell spell)
            {
                // check is right spell
                if (spell.SpellData.Spell != ESpell.FireBarrage || spell.Caster.PlayerId != m_Enemy.Controller.PlayerId)
                    return;

                spell.OnSpellEvent += OnSpellEvent;
            }

            Spell.OnSpellSpawn += OnSpellSpawn;

            m_Enemy.Cast(ESpell.FireBarrage);

            yield return new WaitUntil(() => ctr == n);

            Spell.OnSpellSpawn -= OnSpellSpawn;

            yield return new WaitForSeconds(1f);
        }

        public IEnumerator WaitUntilShot(int n)
        {
            int counter = 0;
            GameUIManager.TutoGameUI.ObjectifDisplayer.SetObjectif("Attack", n);

            m_Controller.AutoAttackHandler.Activate(true);
            m_Controller.Movement.Activate(true);

            StartCoroutine(WaitForSpell(m_Controller.SpellHandler.AutoAttack, m_Controller, ESpellEvent.OnHit, n, () => GameUIManager.TutoGameUI.ObjectifDisplayer.UpdateObjectif(++counter)));

            yield return new WaitUntil(() => !m_IsWaiting);

            m_Controller.AutoAttackHandler.Activate(false);
            m_Controller.Movement.Activate(false);

            yield return GameUIManager.TutoGameUI.ObjectifDisplayer.Completed();
        }

        #endregion


        #region Special Animations

        IEnumerator StartHealAnimation()
        {
            m_CurrentCoroutineName = "StartHealAnimation";
            m_HealAnimationDone = true;

            // start the pause
            Pause(true);

            // -- dialog
            yield return GameUIManager.TutoGameUI.Speaker.Write("Arrgh i am not feeling well", ECaptionType.Normal, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("I need to heal", ECaptionType.Normal, showSpeaker: true);

            // WAIT until Player uses HealSpell
            StartCoroutine(WaitForSpell(ESpell.Heal, m_Controller, ESpellEvent.OnEnd, 1));
            yield return LockOnSpell(ESpell.Heal);
            yield return new WaitUntil(() => !m_IsWaiting);

            // -- dialog
            yield return GameUIManager.TutoGameUI.Speaker.Write("So much better !", ECaptionType.Normal, showSpeaker: true);

            // stop the pause
            Pause(false);
        }

        IEnumerator StartEnemyUltimateAnimation()
        {
            m_CurrentCoroutineName = "StartEnemyUltimateAnimation";

            // Make enemy cast undodgeable ULTIMATE
            yield return GameUIManager.TutoGameUI.Speaker.Write("Well done Hero !", ECaptionType.Normal, showSpeaker: true);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("Oh no... What's now !?", ECaptionType.Exclamation, showSpeaker: true);

            StartCoroutine(WaitForSpell(m_Enemy.Controller.SpellHandler.Ultimate, m_Enemy.Controller, ESpellEvent.OnHit));
            m_Enemy.Controller.EnergyHandler.AddEnergy(100);
            m_Enemy.Cast(m_Enemy.Controller.SpellHandler.Ultimate);

            yield return new WaitUntil(() => m_IsWaiting);
            yield return new WaitForSeconds(0.5f);
        }

        IEnumerator StartFinishAnimation()
        {
            m_CurrentCoroutineName = "StartFinishAnimation";
            
            m_EndStarted = true;

            Pause(true);

            m_Controller.EnergyHandler.AddEnergy(100);

            // FOCUS : Energy Bar
            m_FocusManager.Focus(GameUIManager.Instance.GetPlayerUI(m_Controller.PlayerId).EnergyBar.gameObject);
            yield return GameUIManager.TutoGameUI.Speaker.Write("You energy bar is full", ECaptionType.Normal, showSpeaker: false);
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("This means... ", ECaptionType.Normal, showSpeaker: false);
            m_FocusManager.RemoveFocus();

            // USE ULTIMATE
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("You can use your ULTIMATE ability", ECaptionType.Normal, showSpeaker: false);
            StartCoroutine(WaitForSpell(m_Controller.SpellHandler.Ultimate, m_Controller));
            yield return LockOnSpell(m_Controller.SpellHandler.Ultimate);

            yield return new WaitUntil(() => ! m_IsWaiting);

            GameUIManager.TutoGameUI.Speaker.ShowSpeaker(false);

            // -- dialog : Kahnan
            yield return GameUIManager.TutoGameUI.SpeakerEnemy.Write("Arrrgh !", ECaptionType.Exclamation, showSpeaker: true);
            StartCoroutine(GameUIManager.TutoGameUI.SpeakerEnemy.WriteOnce("I'll be... baaaack !", ECaptionType.Exclamation, showSpeaker: true));
            // -- dialog : Alexander
            yield return GameUIManager.TutoGameUI.Speaker.WriteOnce("We've done it Hero !", ECaptionType.Exclamation, true);

            // call game over
            GameManager.Instance.GameOver(m_Controller.Team);
        }

        #endregion


        #region Spell Management

        IEnumerator WaitForSpell(ESpell spell, Controller controller, ESpellEvent spellEvent = ESpellEvent.OnEnd, int nTimes = 1, Action callback = null)
        {
            m_IsWaitingForSpell = true;
            int counter = 0;
            void OnSpellEvent(ESpellEvent currentSpellEvent)
            {
                if (currentSpellEvent != spellEvent)
                    return;
                
                counter++;
                callback?.Invoke();
                GameUIManager.TutoGameUI.ObjectifDisplayer.UpdateObjectif(counter);
            }

            void OnSpellSpawn(Spell spellObject)
            {
                // check is right spell
                if (spellObject.SpellData.Name != spell.ToString() || spellObject.Caster != controller)
                    return;

                spellObject.OnSpellEvent += OnSpellEvent;
            }

            Spell.OnSpellSpawn += OnSpellSpawn;

            while (counter < nTimes)
            {
                yield return null;
            }

            m_IsWaitingForSpell = false;
        }

        IEnumerator LockOnSpell(ESpell spell)
        {
            var spellItemUI = GameUIManager.Instance.GetSpellItemUI(spell);
            if (spellItemUI == null)
            {
                ErrorHandler.Error("Unable to lock on spell : " + spell + " - this spell is not in the spell data");
                yield break;
            }

            // lock the view to the specific spell to use
            LockSpells();
            UnlockSpell(spell);

            // play "ClickOn" animation until the player clicks
            yield return GameUIManager.TutoGameUI.Hand.ClickOn(spellItemUI.gameObject);
        }

        void LockSpells()
        {
            foreach (ESpell spell in m_Controller.SpellHandler.Spells)
            {
                if (spell == m_Controller.SpellHandler.AutoAttack)
                    continue;

                LockSpell(spell);
            }
        }

        void LockSpell(ESpell spell)
        {
            GameUIManager.Instance.GetSpellItemUI(spell).Activate(false);
        }

        void UnlockSpells()
        {
            foreach (ESpell spell in m_Controller.SpellHandler.Spells)
            {
                if (spell == m_Controller.SpellHandler.AutoAttack)
                    continue;

                UnlockSpell(spell);
            }
        }

        void UnlockSpell(ESpell spell)
        {
            m_Controller.SpellHandler.SetCooldown(spell.ToString(), 0);
            GameUIManager.Instance.GetSpellItemUI(spell).Activate(true);
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

        protected void RegisterSpellEventMethod(Action<ESpellEvent> onSpellEvent, string spellName = "")
        {
            void OnSpellSpawn(Spell spell)
            {
                if (spellName == "" || spell.SpellData.Name == spellName)
                    spell.OnSpellEvent += onSpellEvent;
            }

            Spell.OnSpellSpawn += OnSpellSpawn;
        }

        protected void OnHpChanged(int _, int hp)
        {
            if (hp > 500)
                return;

            if (m_CurrentCoroutineName == "StartHealAnimation")
                return;

            StartCoroutine(StartHealAnimation());
        }

        protected void OnEnemyHpValueChanged(int _, int hp)
        {
            if (hp > 500 || m_EndStarted)
                return;

            StartCoroutine(StartFinishAnimation());
        }

        #endregion
    }
}
