using Enums;
using Game.Loaders;
using Game.StateEffects.Quests;
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
            EStateEffect.BlockCast.ToString(),
            EStateEffect.BlockMovement.ToString(),
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

        public PlayerBarUI HealthBar => m_HealthBar;
        public PlayerBarUI ShieldBar => m_ShieldBar;
        public PlayerBarUI EnergyBar => m_EnergyBar;

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
            controller.EnergyHandler.MaxEnergy.OnValueChanged           += m_EnergyBar.OnMaxValueChanged;
            controller.EnergyHandler.Energy.OnValueChanged              += m_EnergyBar.OnValueChanged;

            // State Displayer
            m_StateDisplayer = Finder.Find(gameObject, c_StateDisplayer);
            m_StateEffectsUI = new Dictionary<string, StateEffectUI>();
            UIHelper.CleanContent(m_StateDisplayer);
            controller.StateHandler.StateEffectEvent                    += OnStateEffectEvent;
            controller.StateHandler.QuestThresholdEvent                 += OnQuestTreshold;
            controller.StateHandler.HoldingStateEffects.OnListChanged   += OnHoldingStateEffectsChanged;
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
            m_Controller.StateHandler.StateEffectEvent                  -= OnStateEffectEvent;
            m_Controller.StateHandler.QuestThresholdEvent               -= OnQuestTreshold;
            m_Controller.StateHandler.HoldingStateEffects.OnListChanged += OnHoldingStateEffectsChanged;

        }

        #endregion


        #region State Displayer

        void OnStateEffectEvent(EStateEffectEvent stateEffectEvent, string state, int stacks, int maxStacks, float duration)
        {
            if (!isActiveAndEnabled)
                return;

            // check that is not one of the state that are not displayed
            if (IGNORED_STATE_EFFECTS.Contains(state) || state.StartsWith("_"))
                return;

            var stateEffectData = SpellLoader.GetStateEffect(state);
            if (stateEffectData == null || stateEffectData.IsInstantanious || ! stateEffectData.IsDisplayed) 
                return;

            switch (stateEffectEvent)
            {
                case EStateEffectEvent.OnApplied:
                case EStateEffectEvent.OnActivated:
                    AddState(state, stacks, maxStacks, duration, stateEffectData.StartingStacks);
                    break;

                case EStateEffectEvent.OnRefreshed:
                    UpdateStacks(state, stacks, maxStacks, duration, stateEffectData.StartingStacks);
                    break;

                case EStateEffectEvent.OnRemoved:
                case EStateEffectEvent.OnConsumed:
                    UpdateStacks(state, -stacks, maxStacks, duration, stateEffectData.StartingStacks);
                    break;

                case EStateEffectEvent.OnDeactivated:
                    RemoveState(state);
                    break;

                case EStateEffectEvent.OnEnd:
                    RemoveState(state);
                    break;

                case EStateEffectEvent.OnTick:
                    break;

                default:
                    Debug.Log("Unhandled case : " + stateEffectEvent);
                    break;
            }
        }

        void AddState(string stateEffect, int stacks, int maxStacks, float duration, int startingStacks)
        {
            // if already in existing state, refresh it
            if (m_StateEffectsUI.ContainsKey(stateEffect))
            {
                // initialize the state (or refresh it)
                m_StateEffectsUI[stateEffect].Refresh(duration, stacks, maxStacks);
                return;
            }

            GameObject stateEffectUI = Instantiate(m_TemplateStateEffect, m_StateDisplayer.transform);
            m_StateEffectsUI.Add(stateEffect, stateEffectUI.GetComponent<StateEffectUI>());

            // initialize the state (or refresh it)
            m_StateEffectsUI[stateEffect].Initialize(stateEffect, stacks, maxStacks, duration, startingStacks);
        }

        void UpdateStacks(string stateEffect, int stacks, int maxStacks, float duration, int startingStacks)
        {
            if (! m_StateEffectsUI.ContainsKey(stateEffect))
            {
                ErrorHandler.Warning($"UpdateStacks ({stacks}) of {stateEffect} but this state effect UI was not found");
                AddState(stateEffect, stacks, maxStacks, duration, startingStacks);
                return;
            }

            m_StateEffectsUI[stateEffect].AddStacks(stacks, maxStacks, duration);
        }

        void RemoveState(string stateEffect)
        {
            // if not in existing state, create it and add it to the list
            if (!m_StateEffectsUI.ContainsKey(stateEffect))
            {
                ErrorHandler.Error($"Unable to find removed state {stateEffect} in list");
                return;
            }

            // destroy state and remove from list
            Destroy(m_StateEffectsUI[stateEffect].gameObject);
            m_StateEffectsUI.Remove(stateEffect);
        }

        #endregion


        #region Listeners

        void OnShieldChanged(int _, int newValue)
        {
            if (!isActiveAndEnabled)
                return;

            m_ShieldBar.OnValueChanged(0, newValue);
        }

        void OnHoldingStateEffectsChanged(NetworkListEvent<FixedString64Bytes> changeEvent)
        {
            if (!isActiveAndEnabled)
                return;

            foreach (string stateEffect in m_StateEffectsUI.Keys)
            {
                m_StateEffectsUI[stateEffect].SetIsHolding(m_Controller.StateHandler.IsHolding(stateEffect));
            }
        }

        void OnQuestTreshold(string stateEffectName, int index)
        {
            // CHECK : UI is enabled
            if (!isActiveAndEnabled)
                return;

            // CHECK : is in our list of effects
            if (! m_StateEffectsUI.ContainsKey(stateEffectName))
            {
                ErrorHandler.Warning($"Unable to find {stateEffectName} in list of effects");
                return;
            }

            // CHECK : StateEffect is indeed Quest
            var stateEffect = SpellLoader.GetStateEffect(stateEffectName);
            if (stateEffect is not QuestEffect questEffect)
            {
                ErrorHandler.Warning($"Call OnQuestTreshold() on state effect {stateEffectName} but it is not recognized as QuestEffect");
                return;
            }

            // CHECK : index is allowed
            if (index >= questEffect.QuestThresholds.Count)
            {
                ErrorHandler.Warning($"Call OnQuestTreshold() on state effect {stateEffectName} at index {index} but max index is {questEffect.QuestThresholds.Count}");
                return;
            }

            // Select icon
            Sprite replacementIcon;
            if (index >= 0)
            {
                // CHECK : requires special ICON
                replacementIcon = questEffect.QuestThresholds[index].ReplacementIcon;
                if (replacementIcon == null)
                    return;
            } else
            {
                replacementIcon = null;
            }
            
            // change the Icon of the state effect
            m_StateEffectsUI[stateEffectName].ReloadIcon(replacementIcon);
        }

        #endregion
    }
}