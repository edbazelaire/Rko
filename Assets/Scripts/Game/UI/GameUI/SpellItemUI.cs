using Data;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Save;
using TMPro;
using Tools;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

namespace Game.UI
{
    public class SpellItemUI : TemplateSpellButton
    {
        #region Members

        /// <summary> name of the GameObject containing the cooldown counter </summary>
        const string    c_CooldownCtr   = "CooldownCtr";

        /// <summary> Index of the spell in the list of spell data </summary>
        int m_Index;
        /// <summary> Owner of this spell item (= current player) </summary>
        Controller      m_Owner;
        /// <summary> TextMeshPro of the cooldown counter </summary>
        TMP_Text        m_CooldownCtr;
        /// <summary> NCharges counter </summary>
        GameObject      m_NCharges;
        /// <summary> TextMeshPro of the number of charges </summary>
        TMP_Text        m_NChargesCtr;

        // ============================================================================================================
        // LOCAL DATA
        /// <summary> base cooldown of the spell </summary>
        float m_BaseCooldown;
        /// <summary> client side cooldown that handles spell cooldown display (to avoid spamming server and delays) </summary>
        float m_CooldownTimer;
        /// <summary> max number of charges for the spell </summary>
        int m_MaxCharges;
        /// <summary> current number of charges for the spell </summary>
        int m_CurrentCharges;

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
            m_NCharges = Finder.Find(gameObject, "NCharges");
            m_NChargesCtr = Finder.FindComponent<TMP_Text>(m_NCharges, "NChargesCtr");
        }

        /// <summary>
        /// Initialize the GameObject : graphics, button, members, listeners
        /// </summary>
        /// <param name="spell"></param>
        public void Initialize(ESpell spell, int level, int index)
        {
            // init data
            m_CollectableCloudData = new SCollectableCloudData(spell, level);
            m_Owner = GameManager.Instance.Owner;
            m_Index = index;

            SpellData spellData = SpellLoader.GetSpellData(m_Spell, level, destroy: true);
            m_BaseCooldown = spellData.Cooldown;
            m_CooldownTimer = 0;
            m_CurrentCharges = spellData.Charges;
            m_MaxCharges = spellData.Charges;

            // call base init 
            base.Initialize();

            // setup ui elements (icon, collection fillbar, ...)
            SetUpUI(true);

            // set initial UI of Cooldowns
            SetupCooldown();

            // only one max charge - no need for NCharges display
            if (m_MaxCharges <= 1)
                m_NCharges.SetActive(false);
            else
                SetNCharges(m_MaxCharges);

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

        protected override void RefreshUI()
        {
            base.RefreshUI();

            // refresh UI with current number of charges
            SetNCharges(m_CurrentCharges);
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

        void SetNCharges(int nCharges)
        {
            m_CurrentCharges = nCharges;

            if (m_MaxCharges <= 1)
                return;

            // no more charge : set UI for cooldown
            if (nCharges <= 0)
            {
                m_NCharges.SetActive(false);
                return;
            }

            // display current number of charges
            m_NCharges.SetActive(true);
            m_NChargesCtr.text = nCharges.ToString();
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
            m_Owner.SpellHandler.SelectedSpellIndexNet.OnValueChanged   += OnSpellIndexSelected;
            m_Owner.SpellHandler.SpellSelectionEvent                    += OnSpellSelectionStateChanged;
            m_Owner.SpellHandler.OnCooldownEvent                        += OnCooldownChanged;
            m_Owner.SpellHandler.NChargesNet.OnListChanged              += OnNChargesChanged;
            m_Owner.SpellHandler.SpellOverrideEvent                     += OnSpellOverride;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Owner.SpellHandler.SelectedSpellIndexNet.OnValueChanged   -= OnSpellIndexSelected;
            m_Owner.SpellHandler.SpellSelectionEvent                    -= OnSpellSelectionStateChanged;
            m_Owner.SpellHandler.OnCooldownEvent                        -= OnCooldownChanged;
            m_Owner.SpellHandler.NChargesNet.OnListChanged              -= OnNChargesChanged;
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
        void OnSpellIndexSelected(int oldValue, int newValue)
        {
            SetSelected(newValue == m_Index);
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

            // update timer
            m_CooldownTimer = newCooldown;

            // check if cooldown timer is set
            if (m_State == EButtonState.Locked && !m_CooldownCtr.gameObject.activeInHierarchy)
                m_CooldownCtr.gameObject.SetActive(true);
        }

        void OnNChargesChanged(NetworkListEvent<int> changeEvent)
        {
            if (changeEvent.Index != m_Index)
                return;

            SetNCharges(changeEvent.Value);
        }

        void OnSpellOverride(string spellName, ESpellProperty spellProperty, int value)
        {
            if (Spell.ToString() != spellName)
                return;

            switch (spellProperty)
            {
                case ESpellProperty.Charges:
                    if (value <= 0)
                    {
                        ErrorHandler.Warning($"Trying to override {spellProperty} of {m_Spell} with {value}. Value must be > 0");
                        return;
                    }

                    m_MaxCharges = value;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled override case : " + spellProperty + " with value " + value);
                    break;
            }

            RefreshUI();
        }

        #endregion
    }
}