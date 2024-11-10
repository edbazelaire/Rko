using Assets.Scripts.Data.GameManagement;
using Enums;
using Game.Loaders;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.UI.EndGameUI
{
    public class PowerUpSection : MObject
    {
        #region Members

        const int NUM_POWER_UPS = 3;

        public Action OnEndEvent;

        GameObject m_PowerUpContainer;
        List<PowerUpItem> m_PowerUpItems = new();

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerUpContainer = gameObject;
        }

        public override void Initialize()
        {
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
            
            RefreshPowerUps();
        }

        void RefreshPowerUps()
        {
            // clean potential previous content
            UIHelper.CleanContent(m_PowerUpContainer);
            m_PowerUpItems.Clear();

            // select a rarety for the PowerUps
            var runeActivation = ArenaManagementData.SelectRandomActivation(ProgressionCloudData.CurrentArena.Level - 1);

            List<string> usedPowerUpData = new();       // init list of already used power up data
            var template = AssetLoader.LoadPowerUpItem(runeActivation);  // load template of PowerUpItem
            for (int i = 0; i < NUM_POWER_UPS; i++)
            {
                // choose a random powerup filling criteria
                var powerUpData = SpellLoader.GetRandomPowerUp(new List<ERuneActivation>() { runeActivation }, notAllowedFilter: usedPowerUpData);
                if (powerUpData == null)
                {
                    ErrorHandler.Error("Unable to find powerUp data for rarety " + runeActivation + " at index " +  i);
                    continue;
                }

                // set first PowerUpValue as default PowerUp
                if (i == 0)
                {
                    ProgressionCloudData.AddCurrentArenaPowerUp(powerUpData.Name);
                }

                // add to list of already selected power ups
                usedPowerUpData.Add(powerUpData.Name);

                // instantiate the PowerUpItem with the provided data
                var powerUpItem = Instantiate(template, m_PowerUpContainer.transform);
                powerUpItem.Initialize(powerUpData);
                powerUpItem.Button.onClick.AddListener(OnClickPowerUpCallback(i));

                // add to list of current items
                m_PowerUpItems.Add(powerUpItem);
            }
        }

        #endregion


        #region Animation

        IEnumerator SelectPowerUpAnimation(int index)
        {
            for (int i = 0; i < m_PowerUpItems.Count; i++)
            {
                // deactivate buttons during animation
                m_PowerUpItems[i].Button.interactable = false;

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

        IEnumerator Select(PowerUpItem item)
        {
            Fade fade = item.AddComponent<Fade>();
            fade.Initialize(duration: 1.5f, endOpacity: 0f);

            var rotate = item.AddComponent<RotateAnimation>();
            rotate.Initialize(duration: 1.5f, rotation: new Vector3(0f, 1040f, 0f));

            var move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 1.5f, startPos: item.transform.position, endPos: item.transform.position + new Vector3(0f, 1f, 0f));

            yield return new WaitUntil(() => fade.IsOver);
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
                ProgressionCloudData.AddCurrentArenaPowerUp(m_PowerUpItems[index].PowerUpData.Name);
                StartCoroutine(SelectPowerUpAnimation(index));
            };
            
        }

        #endregion
    }
}

