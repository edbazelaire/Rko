using Enums;
using Game.Loaders;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Tools;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI
{
    public class PlayerUI : MonoBehaviour
    {
        const string        c_HealthBar         = "HealthBar";
        const string        c_EnergyBar         = "EnergyBar";
        const string        c_StateDisplayer    = "StateDisplayer";

        string[] IGNORED_STATE_EFFECTS = { 
            EStateEffect.None.ToString(), 
            EStateEffect.Invulnerable.ToString(), 
            EStateEffect.Uncontrollable.ToString(), 
            EStateEffect.UnTargettable.ToString(), 
            EStateEffect.Jump.ToString(),
            EStateEffect.SpecialAnimation.ToString(),
            EStateEffect.Vanish.ToString(),
        };

        Controller m_Controller = null;
        [SerializeField] GameObject   m_TemplateStateEffect;

        PlayerBarUI         m_HealthBar;
        PlayerBarUI         m_ShieldBar;
        PlayerBarUI         m_EnergyBar;
        TMP_Text            m_PlayerName;
        GameObject          m_StateDisplayer;

        Dictionary<string, StateEffectUI> m_StateEffectsUI;

        #region Init & End

        public void Initialize(ulong clientId)
        {
            var controller = GameManager.Instance.GetPlayer(clientId);
            m_Controller = controller;

            // Player Name
            m_PlayerName = Finder.FindComponent<TMP_Text>(gameObject, "PlayerName");
            m_PlayerName.text = controller.PlayerName.ToString();

            if (NetworkManager.Singleton.LocalClientId == clientId)
                m_PlayerName.color = Color.green;

            // Character Icon
            GameObject playerIconSection = Finder.Find(gameObject, "PlayerIconSection");
            Image playerIcon = Finder.FindComponent<Image>(playerIconSection, "PlayerIcon");
            playerIcon.sprite = AssetLoader.LoadCharacterIcon(controller.Character);

            // Player Level
            TMP_Text playerLevelText = Finder.FindComponent<TMP_Text>(playerIconSection, "LevelValue");
            playerLevelText.text = "Level " + controller.CharacterLevel.ToString();

            // HealthBar
            m_HealthBar = Finder.FindComponent<PlayerBarUI>(gameObject, c_HealthBar);
            m_HealthBar.Initialize(controller.Life.Hp.Value, controller.Life.MaxHp.Value);
            controller.Life.MaxHp.OnValueChanged    += m_HealthBar.OnMaxValueChanged;
            controller.Life.Hp.OnValueChanged       += m_HealthBar.OnValueChanged;

            // HealthBar
            m_ShieldBar = Finder.FindComponent<PlayerBarUI>(gameObject, "ShieldBar");
            m_ShieldBar.Initialize(controller.Life.FinalShield.Value, controller.Life.MaxHp.Value);
            controller.Life.FinalShield.OnValueChanged += OnShieldChanged;

            // Energy Bar
            m_EnergyBar = Finder.FindComponent<PlayerBarUI>(gameObject, c_EnergyBar);
            m_EnergyBar.Initialize(controller.EnergyHandler.Energy.Value, controller.EnergyHandler.MaxEnergy.Value);
            controller.EnergyHandler.MaxEnergy.OnValueChanged   += m_EnergyBar.OnMaxValueChanged;
            controller.EnergyHandler.Energy.OnValueChanged      += m_EnergyBar.OnValueChanged;

            // State Displayer
            m_StateDisplayer = Finder.Find(gameObject, c_StateDisplayer);
            m_StateEffectsUI = new Dictionary<string, StateEffectUI>();
            UIHelper.CleanContent(m_StateDisplayer);
            controller.StateHandler.StateEffectListEvent                        += OnStateEvent;
            controller.StateHandler.HoldingStateEffects.OnListChanged   += OnHoldingStateEffectsChanged;
            controller.TriggerEffectHandler.QuestValueChanged           += OnQuestValueChanged;
        }

        private void OnDestroy()
        {
            if (m_Controller == null)
                return;

            m_Controller.Life.MaxHp.OnValueChanged                      -= m_HealthBar.OnMaxValueChanged;
            m_Controller.Life.Hp.OnValueChanged                         -= m_HealthBar.OnValueChanged;
            m_Controller.Life.FinalShield.OnValueChanged                -= m_ShieldBar.OnValueChanged;
            m_Controller.EnergyHandler.MaxEnergy.OnValueChanged         -= m_EnergyBar.OnMaxValueChanged;
            m_Controller.EnergyHandler.Energy.OnValueChanged            -= m_EnergyBar.OnValueChanged;
            m_Controller.StateHandler.StateEffectListEvent              -= OnStateEvent;
            m_Controller.TriggerEffectHandler.QuestValueChanged         -= OnQuestValueChanged;
        }

        #endregion


        #region State Displayer

        void OnStateEvent(EListEvent listEvent, string state, int stack, float duration)
        {
            // check that is not one of the state that are not displayed
            if (IGNORED_STATE_EFFECTS.Contains(state) || state.StartsWith("_"))
                return;

            var stateEffectData = SpellLoader.GetStateEffect(state);
            if (stateEffectData == null || stateEffectData.IsInstantanious || ! stateEffectData.IsDisplayed) 
                return;

            switch (listEvent)
            {
                case EListEvent.Add:
                    AddState(state, stack, duration);
                    break;

                case EListEvent.Remove:
                    RemoveState(state);
                    break;

                default:
                    Debug.Log("Unhandled case");
                    break;
            }
        }

        void AddState(string state, int stack, float duration)
        {
            // if already in existing state, refresh it
            if (m_StateEffectsUI.ContainsKey(state))
            {
                // initialize the state (or refresh it)
                m_StateEffectsUI[state].Refresh(duration, stack);
                return;
            }

            GameObject stateEffectUI = Instantiate(m_TemplateStateEffect, m_StateDisplayer.transform);
            m_StateEffectsUI.Add(state, stateEffectUI.GetComponent<StateEffectUI>());

            // initialize the state (or refresh it)
            m_StateEffectsUI[state].Initialize(state, stack, duration);
        }

        void RemoveState(string state)
        {
            // if not in existing state, create it and add it to the list
            if (!m_StateEffectsUI.ContainsKey(state))
            {
                ErrorHandler.Error($"Unable to find removed state {state} in list");
                return;
            }

            // destroy state and remove from list
            Destroy(m_StateEffectsUI[state].gameObject);
            m_StateEffectsUI.Remove(state);
        }

        #endregion


        #region Quest Display

        void OnQuestValueChanged(string effectName, int counter)
        {
            string effectId = TextHandler.TrimQuestEffectName(effectName, isActivated: true);
            effectName = TextHandler.TrimQuestEffectName(effectName, isActivated: counter == 0);

            // if already in existing state, refresh it
            if (m_StateEffectsUI.ContainsKey(effectId))
            {
                // initialize the state (or refresh it)
                m_StateEffectsUI[effectId].Refresh(-1, counter);

                if (counter == 0)
                    m_StateEffectsUI[effectId].ReloadIcon(effectName);

                return;
            }

            // instantiate new StateEffectUI
            GameObject stateEffectUI = Instantiate(m_TemplateStateEffect, m_StateDisplayer.transform);
            m_StateEffectsUI.Add(effectId, stateEffectUI.GetComponent<StateEffectUI>());

            // initialize the state (or refresh it)
            m_StateEffectsUI[effectId].Initialize(effectName, counter, -1);
        }

        #endregion


        #region Listeners

        void OnShieldChanged(int _, int newValue)
        {
            m_ShieldBar.OnValueChanged(0, newValue);
        }

        void OnHoldingStateEffectsChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            foreach (string stateEffect in m_StateEffectsUI.Keys)
            {
                m_StateEffectsUI[stateEffect].SetIsHolding(m_Controller.StateHandler.IsHolding(stateEffect));
            }
        }

        #endregion
    }
}