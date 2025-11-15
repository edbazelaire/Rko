using Assets.Scripts.Data.GameManagement;
using Assets.Scripts.UI;
using Data;
using Data.DataStructures.PowerEffects;
using Enums;
using Game.Loaders;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Game.UI.EndGameUI
{
    public class PowerUpSection : MObject
    {
        #region Members

        const int NUM_POWER_UPS = 3;

        public Action OnEndEvent;

        GameObject m_PowerUpContainer;
        List<PowerUpItem> m_PowerUpItems = new();
        ERuneActivation? m_RuneActivation = null;
        List<string> m_UsedPowerUpData;

        /// <summary> index of the arena power up to replace in cloud data </summary>
        int m_CurrentArenaPowerUpIndex;

        public ERuneActivation RuneActivation => m_RuneActivation.Value;
        int m_Refreshes => ProgressionCloudData.CurrentArena.RefreshTokens;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerUpContainer = gameObject;
        }

        public void Initialize(int index = -1, ERuneActivation? runeActivation = null)
        {
            m_CurrentArenaPowerUpIndex = index >= 0 ? index : ProgressionCloudData.CurrentArena.Level;
            if (runeActivation != null)
                m_RuneActivation = runeActivation;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        public void Activate(bool activate)
        {
            gameObject.SetActive(activate);

            if (!activate)
                return;
            
            RefreshPowerUps(m_RuneActivation);
        }

        public void RefreshPowerUps(ERuneActivation? runeActivation = null)
        {
            // clean potential previous content
            UIHelper.CleanContent(m_PowerUpContainer);
            m_PowerUpItems.Clear();

            // select a random rune activation (= rarety of the PowerUps)
            if (runeActivation == null)
                SelectRuneActivation();
            else
                m_RuneActivation = runeActivation.Value;

            // get name of Used Power Ups
            m_UsedPowerUpData = ProgressionCloudData.CurrentArena.GetActivePowerUps()
                .Select(s => s.Split('-')[0].Trim()) 
                .ToList();

            // load template of PowerUpItem
            var template = AssetLoader.LoadPowerUpItem(m_RuneActivation.Value);                   
            for (int i = 0; i < NUM_POWER_UPS; i++)
            {
                // select a random power up based on context
                var powerUpData = SelectRandomPowerUp(i);
                if (powerUpData == null)
                {
                    ErrorHandler.Error("Unable to find powerUp data for rarety " + m_RuneActivation + " at index " + i);
                    continue;
                }

                // set first PowerUpValue as default PowerUp
                if (i == 0)
                {
                    ProgressionCloudData.SetCurrentArenaPowerUp(powerUpData.Name, m_CurrentArenaPowerUpIndex);
                }

                // instantiate the PowerUpItem with the provided data
                var powerUpItem = Instantiate(template, m_PowerUpContainer.transform);
                powerUpItem.Initialize(powerUpData, withRefreshButton: true);

                powerUpItem.Button.onClick.AddListener(OnClickPowerUpCallback(i));
                powerUpItem.ValidationButton.onClick.AddListener(OnValidatePowerUpCallback(i));
                powerUpItem.RefreshButton.onClick.AddListener(OnClickRefreshCallback(i));

                // -- display the number of available refreshes
                powerUpItem.UpdateRefreshCounter(m_Refreshes);

                // add to list of current items
                m_PowerUpItems.Add(powerUpItem);
            }

            // force rebuild layout to make sure it's clean
            LayoutRebuilder.ForceRebuildLayoutImmediate(m_PowerUpContainer.GetComponent<RectTransform>());
        }

        void ConsumeRefresh()
        {
            ProgressionCloudData.AddCurrentArenaRefreshToken(-1);

            if (m_Refreshes < 0)
                ErrorHandler.Warning("Refreshes (" + m_Refreshes + ") are < 0, this should never happen");

            UpdateRefreshCounters();
        }

        void UpdateRefreshCounters()
        {
            for (int i = 0; i < m_PowerUpItems.Count; i++)
            {
                m_PowerUpItems[i].UpdateRefreshCounter(m_Refreshes);
            }
        }

        #endregion


        #region Power Up Selection

        void SelectRuneActivation()
        {
            // select a rarety for the PowerUps
            m_RuneActivation = ArenaManagementData.SelectRandomActivation(ProgressionCloudData.CurrentArena.Level - 1);
        }

        /// <summary>
        /// Select a Random power matching the context
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        SPowerEffect SelectRandomPowerUp(int index)
        {
            SPowerEffect powerUpData = new();
            // choose a random powerup filling criteria
            if (index <= 1 || m_RuneActivation == ERuneActivation.Primal)
            {
                powerUpData = SpellLoader.GetRandomPowerUp(
                    level:                  ProfileCloudData.AccountLevel,
                    runeActivationFilter:   new List<ERuneActivation>() { m_RuneActivation.Value },
                    forcedType:             "PowerUp",
                    notAllowedFilter:       m_UsedPowerUpData
                );
            } 
            else
            {
                powerUpData = SpellLoader.GetRandomPowerUp(
                    level:                  ProfileCloudData.AccountLevel,
                    runeActivationFilter:   new List<ERuneActivation>() { m_RuneActivation.Value + 1 },
                    forcedType:             "Rune",
                    notAllowedFilter:       m_UsedPowerUpData
                );
            }
           

            if (powerUpData == null)
            {
                return null;
            }

            m_UsedPowerUpData.Add(powerUpData.Name);

            return powerUpData;
        }

        #endregion


        #region Animation

        IEnumerator SelectPowerUpAnimation(int index)
        {
            for (int i = 0; i < m_PowerUpItems.Count; i++)
            {
                // deactivate buttons during animation
                m_PowerUpItems[i].Button.interactable = false;
                m_PowerUpItems[i].RefreshButton.gameObject.SetActive(false);
                m_PowerUpItems[i].ValidationButton.gameObject.SetActive(false);

                // if not selected : Hide() animation
                if (i != index)
                {
                    StartCoroutine(Hide(m_PowerUpItems[i]));
                    continue;
                }

                // if selected : Select() animation
                StartCoroutine(Select(m_PowerUpItems[i]));
            }

            yield return new WaitForSeconds(1.5f);

            OnEndEvent?.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator Hide(PowerUpItem item)
        {
            Fade fade = item.AddComponent<Fade>();
            fade.Initialize(duration: 1f, endOpacity: 0f);

            var move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 1f, startPos: item.transform.position, endPos: item.transform.position + new Vector3(0f, -0.5f, 0f));

            yield return new WaitUntil(() => fade.IsOver);
        }

        IEnumerator Select(PowerUpItem powerUpItem)
        {
            Fade fade = powerUpItem.AddComponent<Fade>();
            fade.Initialize(duration: 1.5f, endOpacity: 0f);

            var card = powerUpItem.Content;
            var rotate = card.AddComponent<RotateAnimation>();
            rotate.Initialize(duration: 1.5f, rotation: new Vector3(0f, 1040f, 0f));

            var move = card.AddComponent<MoveAnimation>();
            move.Initialize(duration: 1.5f, startPos: card.transform.position, endPos: card.transform.position + new Vector3(0f, 1f, 0f));

            yield return new WaitUntil(() => fade.IsOver);
        }

        IEnumerator RefreshPowerUp(int index)
        {
            PowerUpItem item = m_PowerUpItems[index];
            var basePos = item.transform.position;

            // deactivate buttons during animation
            item.Button.interactable = false;
            item.RefreshButton.interactable = false;

            // HIDE BUTTON ======================================
            Fade fade = item.AddComponent<Fade>();
            fade.Initialize(duration: 0.5f, endOpacity: 0f, endWhenOver: false);

            var move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 0.5f, startPos: item.transform.position, endPos: item.transform.position + new Vector3(0f, -0.3f, 0f));

            yield return new WaitUntil(() => fade.IsOver);

            // SETUP new value ======================================
            item.RefreshUI(SelectRandomPowerUp(index));

            // SHOW BUTTON ======================================
            fade.FadeBack();
            move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 0.5f, startPos: item.transform.position, endPos: basePos);

            yield return new WaitUntil(() => fade.IsOver);

            // re-activate buttons after animation
            item.Button.interactable = true;
            item.RefreshButton.interactable = true;
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

        UnityEngine.Events.UnityAction OnClickPowerUpCallback(int index)
        {
            return () =>
            {
                bool isSelected = m_PowerUpItems[index].IsSelected;
                for (int i = 0; i < m_PowerUpItems.Count; i++)
                {
                    // WAS SELECTED : deselect all
                    if (isSelected)
                    {
                        m_PowerUpItems[i].SetSelected(ESelectionMod.None);
                        continue;
                    }

                    // WAS NOT SELECTED : select it and set all others to not selected
                    if (i == index)
                    {
                        m_PowerUpItems[i].SetSelected(ESelectionMod.Selected);
                    } else
                    {
                        m_PowerUpItems[i].SetSelected(ESelectionMod.NotSelected);
                    }
                }
            };
            
        }

        UnityEngine.Events.UnityAction OnValidatePowerUpCallback(int index)
        {
            return () =>
            {
                ProgressionCloudData.SetCurrentArenaPowerUp(m_PowerUpItems[index].PowerUpData.Name, m_CurrentArenaPowerUpIndex, true);
                StartCoroutine(SelectPowerUpAnimation(index));
            };
            
        }

        UnityEngine.Events.UnityAction OnClickRefreshCallback(int index)
        {
            return () =>
            {
                ConsumeRefresh();
                StartCoroutine(RefreshPowerUp(index));
            };
            
        }

        #endregion
    }
}

