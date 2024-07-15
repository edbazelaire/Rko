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

        /// <summary> when a cooldown is set, wait 0.2 sec before asking the server if spell is on cooldown </summary>
        const float TIME_BEFORE_ASK_SERVER = 0.2f;
        /// <summary> name of the GameObject containing the cooldown counter </summary>
        const string    c_CooldownCtr   = "CooldownCtr";

        /// <summary> Owner of this spell item (= current player) </summary>
        Controller      m_Owner;

        /// <summary> TextMeshPro of the cooldown counter </summary>
        TMP_Text        m_CooldownCtr;

        /// <summary> base cooldown of the spell </summary>
        float m_BaseCooldown;
        /// <summary> client side cooldown that handles spell cooldown display (to avoid spamming server and delays) </summary>
        float m_CooldownTimer;
        /// <summary> time to wait before asking the server if spell is on cooldown  </summary>
        float m_TimerBeforeAskServer;

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

            // not locked -> skip update
            if (m_State != EButtonState.Locked)
                return;

            UpdateState();
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
            base.Initialize();

            m_CollectableCloudData = new SCollectableCloudData(spell, level);
           
            // setup ui elements (icon, collection fillbar, ...)
            SetUpUI(true);

            m_CollectableCloudData = new SCollectableCloudData(spell, level);
            m_Owner = GameManager.Instance.Owner;

            SpellData spellData = SpellLoader.GetSpellData(m_Spell, level);
            m_BaseCooldown      = spellData.Cooldown;
            m_CooldownTimer     = 0;

            // set initial UI of Cooldowns
            SetupCooldown();

            // set initial state
            SetState(m_Owner.SpellHandler.CanSelect(m_Spell) ? EButtonState.Normal : EButtonState.Locked);
            m_BottomText.text = string.Format(LEVEL_FORMAT, m_CollectableCloudData.Level);

            // listeners
            m_Owner.SpellHandler.SelectedSpellNet.OnValueChanged    += OnSpellSelected;
            m_Owner.SpellHandler.OnSpellCasted                      += OnSpellCasted;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

           if (m_Owner == null || m_Owner.SpellHandler == null)
                return;

            m_Owner.SpellHandler.SelectedSpellNet.OnValueChanged    -= OnSpellSelected;
            m_Owner.SpellHandler.OnSpellCasted                      -= OnSpellCasted;
        }

        /// <summary> 
        /// Setup the icon of the spell in the SpellItemContainer
        /// </summary>
        public void SetupCooldown()
        {
            m_CooldownCtr.text = "";

            if (m_IsUltimateSpell)
                m_CooldownCtr.gameObject.SetActive(false);
        }

        #endregion


        #region State Manipulators 

        public override void SetState(EButtonState state)
        {
            base.SetState(state);

            switch (state)
            {
                case EButtonState.Locked:
                    if (m_CooldownTimer <= 0)
                        m_CooldownCtr.gameObject.SetActive(false);
                    break;
            }
        }


        protected override void UpdateState()
        {
            // wait a bit of time before asking server if its ok to select the spell (to avoid calling too soon)
            m_TimerBeforeAskServer -= Time.deltaTime;
            if (m_TimerBeforeAskServer > 0 && m_CooldownTimer > 0)
                return;

            if (m_Owner.SpellHandler.CanSelect(m_Spell))
                SetState(EButtonState.Normal);  
        }
        
        /// <summary>
        /// When the cooldown changes, update the cooldown if needed
        /// </summary>
        /// <param name="changeEvent"></param>
        void UpdateCooldown()
        {
            if (m_CooldownTimer <= 0)
            {
                m_CooldownCtr.gameObject.SetActive(false);
                return;
            }

            if (m_CooldownCtr == null || ! m_CooldownCtr.isActiveAndEnabled)
                return;

            // update cooldown 
            m_CooldownTimer -= Time.deltaTime; 
            if (m_CooldownTimer <= 0)
                m_CooldownTimer = 0;        // cooldown over : wait for server to say its ok before changing state

            if (m_IsUltimateSpell)
                return;

            m_CooldownCtr.text = m_CooldownTimer.ToString("0");
        }



        #endregion


        #region Listeners

        /// <summary>
        /// Ask for spell selection to the server
        /// </summary>
        protected override void OnClick()
        {
            if (! m_Owner.SpellHandler.CanSelect(m_Spell))
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
        /// When the spell selection changes, update the border if needed
        /// </summary>
        /// <param name="oldValue"></param>
        /// <param name="newValue"></param>
        void OnSpellCasted(ESpell spell)
        {
            if (spell != m_Spell)
                return;

            m_CooldownTimer = m_Owner.SpellHandler.CalculateCooldown(m_BaseCooldown);
            m_TimerBeforeAskServer = TIME_BEFORE_ASK_SERVER;

            SetState(EButtonState.Locked);
            m_CooldownCtr.gameObject.SetActive(true);
        }

        #endregion
    }
}