using Data;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Save;
using TMPro;
using Tools;
using UnityEngine;

namespace Game.UI
{
    public class SpellItemUI : TemplateSpellButton
    {
        #region Members

        /// <summary> name of the GameObject containing the cooldown counter </summary>
        const string    c_CooldownCtr   = "CooldownCtr";

        /// <summary> Owner of this spell item (= current player) </summary>
        Controller      m_Owner;

        /// <summary> TextMeshPro of the cooldown counter </summary>
        TMP_Text        m_CooldownCtr;

        // ============================================================================================================
        // LOCAL DATA
        /// <summary> base cooldown of the spell </summary>
        float m_BaseCooldown;
        /// <summary> client side cooldown that handles spell cooldown display (to avoid spamming server and delays) </summary>
        float m_CooldownTimer;

        ESpell m_Spell => (ESpell)m_CollectableCloudData.GetCollectable();
        bool m_IsUltimateSpell => m_Owner.SpellHandler.Ultimate == m_Spell;

        #endregion


        #region Inherited Manipulators

        private void Update()
        {
            // not initialized yet : skip update
            if (!m_IsInitialized)
                return;

            // game over : stop updating
            if (GameManager.IsGameOver)
                return;

            
            UpdateCooldown();            
        }

        #endregion


        #region Initialization & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CooldownCtr = Finder.FindComponent<TMP_Text>(m_LockState, c_CooldownCtr);
        }

        /// <summary>
        /// Initialize the GameObject : graphics, button, members, listeners
        /// </summary>
        /// <param name="spell"></param>
        public void Initialize(ESpell spell, int level)
        {
            // init data
            m_CollectableCloudData = new SCollectableCloudData(spell, level);
            m_Owner = GameManager.Instance.Owner;

            SpellData spellData = SpellLoader.GetSpellData(m_Spell, level, destroy: true);
            m_BaseCooldown = spellData.Cooldown;
            m_CooldownTimer = 0;

            // call base init 
            base.Initialize();

            // setup ui elements (icon, collection fillbar, ...)
            SetUpUI(true);

            // set initial UI of Cooldowns
            SetupCooldown();

            // set initial state
            SetState(spellData.EnergyCost <= 0 ? EButtonState.Normal : EButtonState.Locked);
            m_BottomText.text = string.Format(LEVEL_FORMAT, m_CollectableCloudData.Level);
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

           if (m_Owner == null || m_Owner.SpellHandler == null)
                return;
        }

        #endregion


        #region Activation / Deactivation

        public void Activate(bool activate)
        {
            if (activate)
            {
                RegisterListeners();
                RefreshState();
                return;
            }

            // DEACTIVATE 
            UnRegisterListeners();
            SetState(EButtonState.Locked);
        }

        #endregion


        #region Cooldowns

        /// <summary> 
        /// Setup the icon of the spell in the SpellItemContainer
        /// </summary>
        public void SetupCooldown()
        {
            m_CooldownCtr.text = "";

            if (m_IsUltimateSpell)
                m_CooldownCtr.gameObject.SetActive(false);
        }

        /// When the cooldown changes, update the cooldown if needed
        /// </summary>
        /// <param name="changeEvent"></param>
        void UpdateCooldown()
        {
            // not locked -> skip update cooldown
            if (m_State != EButtonState.Locked)
                return;

            // cooldown <= 0 : keep this LOCK state (without cooldown display) until Server changes the State
            if (m_CooldownTimer <= 0)
            {
                m_CooldownCtr.gameObject.SetActive(false);
                return;
            }

            // update cooldown 
            m_CooldownTimer -= Time.deltaTime;
            if (m_CooldownTimer <= 0)
                m_CooldownTimer = 0;        // cooldown over : wait for server to say its ok before changing state

            if (m_CooldownCtr == null)
                return;

            m_CooldownCtr.text = m_CooldownTimer.ToString("0");
        }


        #endregion


        #region State Manipulators 

        public override void SetState(EButtonState state)
        {
            base.SetState(state);

            switch (state)
            {
                case EButtonState.Normal:
                    m_CooldownTimer = 0;
                    break;

                case EButtonState.Locked:
                    if (m_CooldownTimer <= 0)
                        m_CooldownCtr.gameObject.SetActive(false);
                    else
                        m_CooldownCtr.gameObject.SetActive(true);
                    break;
            }
        }

        protected override void UpdateState() { }

        protected void RefreshState()
        {
            OnSpellSelectionStateChanged(m_Spell, m_Owner.SpellHandler.GetSpellSelectionState(m_Spell));
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            // listeners
            m_Owner.SpellHandler.SelectedSpellNet.OnValueChanged    += OnSpellSelected;
            m_Owner.SpellHandler.SpellSelectionEvent                += OnSpellSelectionStateChanged;
            m_Owner.SpellHandler.OnCooldownEvent                    += OnCooldownChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Owner.SpellHandler.SelectedSpellNet.OnValueChanged    -= OnSpellSelected;
            m_Owner.SpellHandler.SpellSelectionEvent                -= OnSpellSelectionStateChanged;
            m_Owner.SpellHandler.OnCooldownEvent                    -= OnCooldownChanged;
        }

        /// <summary>
        /// Ask for spell selection to the server
        /// </summary>
        protected override void OnClick()
        {
            if (m_State != EButtonState.Normal)
                // TODO : CantSelectSpellFeedback()
                return;

            SetSelected(true);
            m_Owner.SpellHandler.AskSpellSelectionServerRPC(m_Spell);
        }

        /// <summary>
        /// When the spell selection changes, update the border if needed
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnSpellSelected(int oldValue, int newValue)
        {
            SetSelected((ESpell)newValue == m_Spell);
        }

        /// <summary>
        /// When spell selection state changed, display the corresponding UI
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="spellSelectionState"></param>
        void OnSpellSelectionStateChanged(ESpell spell, ESpellSelectionState spellSelectionState)
        {
            if (spell != m_Spell)
                return;

            switch (spellSelectionState)
            {
                case ESpellSelectionState.None:
                    SetState(EButtonState.Normal);
                    break;

                case ESpellSelectionState.Inactive:
                    SetState(EButtonState.Locked);
                    m_CooldownCtr.gameObject.SetActive(false);
                    break;

                case ESpellSelectionState.Cooldown:
                    m_CooldownTimer = m_Owner.SpellHandler.CalculateCooldown(m_BaseCooldown);
                    SetState(EButtonState.Locked);
                    break;

                default:
                    ErrorHandler.Warning("Unhandled case : " + spellSelectionState);
                    break;
            }
        }

        /// <summary>
        /// Update the UI with new spell cooldown value
        /// </summary>
        /// <param name="spell"></param>
        /// <param name="spellSelectionState"></param>
        void OnCooldownChanged(ESpell spell, float newCooldown)
        {
            if (spell != m_Spell)
                return;

            // =====================================================================
            // TODO : Remove
            Debug.Log("OnCooldownChanged("+ spell + ") : " + newCooldown);
            // =====================================================================

            m_CooldownTimer = newCooldown;
        }

        #endregion
    }
}