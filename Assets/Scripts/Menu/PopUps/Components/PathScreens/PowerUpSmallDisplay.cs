using Assets;
using Assets.Scripts.Managers;
using Data;
using Data.DataStructures;
using Data.DataStructures.PowerEffects;
using Enums;
using Game.Character;
using Game.Loaders;
using Game.StateEffects.Quests;
using Managers;
using MyBox;
using Save;
using System;
using System.Linq;
using TMPro;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class PowerUpSmallDisplay : MObject
    {
        #region Members

        SPowerEffect    m_PowerEffect;
        int             m_Index;

        Image           m_Background;
        Image           m_Icon;
        Image           m_Overlay;
        Button          m_Button;
        GameObject      m_StacksOverlay;
        TMP_Text        m_StacksCounter;
        GameObject      m_DeactivatedOverlay;

        protected bool m_IsMissingData => m_Index < ProgressionCloudData.CurrentArena.Level && m_PowerEffect == null;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Background            = Finder.FindComponent<Image>(gameObject, "Background");
            m_Icon                  = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Overlay               = Finder.FindComponent<Image>(gameObject, "Overlay");
            m_Button                = Finder.FindComponent<Button>(gameObject);
            m_StacksOverlay         = Finder.Find(gameObject, "StacksOverlay");
            m_StacksCounter         = Finder.FindComponent<TMP_Text>(m_StacksOverlay, "StacksCounter");
            m_DeactivatedOverlay    = Finder.Find(gameObject, "DeactivatedOverlay");
        }

        public virtual void Initialize(SPowerEffect powerUpData, int index)
        {
            m_PowerEffect = powerUpData;
            m_Index = index;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            RefreshUI(m_PowerEffect);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(SPowerEffect powerUpData)
        {
            m_PowerEffect = powerUpData;
            m_DeactivatedOverlay.gameObject.SetActive(false);

            SetUpBackground();
            SetUpIcon();

            RefreshStacks();
            RefreshActivation();
        }

        void SetUpBackground()
        {
            // missing power up : set to green and with pulse animation
            if (m_IsMissingData)
            {
                m_Background.color = new Color(0.7f , 1f, 0.7f);
                var animation = m_Background.AddComponent<Pulse>();
                animation.Initialize();
                return;
            }

            if (m_PowerEffect == null)
            {
                m_Background.color = new Color(0.2f, 0.2f, 0.2f);
                return;
            }

            // no missing power up : reset animation and color
            m_Background.color = new Color(1f, 1f, 1f);
            var pulse = m_Background.GetComponent<Pulse>();
            if (pulse != null)
                GameObject.Destroy(pulse);
        }

        void SetUpIcon()
        {
            if (m_PowerEffect == null)
            {
                m_Icon.gameObject.SetActive(false);
                m_Overlay.gameObject.SetActive(false);
                return;
            }

            m_Icon.gameObject.SetActive(true);
            m_Icon.sprite = AssetLoader.LoadIcon(m_PowerEffect.BaseName);
            m_Overlay.gameObject.SetActive(true);
            m_Overlay.sprite = AssetLoader.LoadPowerUpIconBorder(m_PowerEffect.RuneActivation);
        }


        /// <summary>
        /// Refresh number of stacks displayed
        /// </summary>
        void RefreshStacks()
        {
            m_StacksOverlay.gameObject.SetActive(false);

            if (m_PowerEffect == null || m_PowerEffect.TriggerEffects.IsNullOrEmpty())
                return;

            foreach (STriggerEffect triggerEffect in m_PowerEffect.TriggerEffects)
            {
                if (!SpellLoader.IsStateEffect(triggerEffect.SpellDataName))
                    continue;

                if (!ProgressionCloudData.CurrentArena.HasMetaData(triggerEffect.SpellDataName))
                    continue;

                m_StacksOverlay.gameObject.SetActive(true);
                m_StacksCounter.text = ProgressionCloudData.CurrentArena.GetMetaData<int>(triggerEffect.SpellDataName).ToString();
                return;
            }
        }

        /// <summary>
        /// If this power up is a Rune that is already in the current build - set deactivated
        /// </summary>
        void RefreshActivation()
        {
            // CHECK : has data
            if (m_PowerEffect == null)
                return;

            // CHECK : it is a Rune
            if (m_PowerEffect == null || ! Enum.TryParse(m_PowerEffect.BaseName, out ERune rune))
            {
                SetActive(true);
                return;
            }

            // CHECK : is in build
            SBuildData buildData = ProgressionCloudData.CurrentArena.HasBuildData() ? ProgressionCloudData.CurrentArena.BuildData : CharacterBuildsCloudData.CurrentBuild;
            SetActive(!buildData.Runes.Contains(rune));
        }

        void SetActive(bool active)
        {
            var color = active ? new Color(1f, 1f, 1f) : new Color(0.2f, 0.2f, 0.2f);
            m_Icon.color = color;
            m_Overlay.color = color;
            m_Background.color = color;

            m_DeactivatedOverlay.SetActive(! active);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OnClickButton);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
        }

        protected void OnClickButton()
        {
            // Do not have a PowerUp BUT SHOULD -> Display the Selection Screen
            if (m_IsMissingData)
            {
                ScreenManager.PowerUpSelectionScreen(ProgressionCloudData.CurrentArena.ArenaType, m_Index);
                return;
            }

            // Has PowerUp -> Display the Info Screen
            if (m_PowerEffect != null)
            {
                Main.SetPopUp(EPopUpState.PowerUpInfoScreen, m_PowerEffect, m_Index);
                return;
            }
        }

        #endregion
    }
}

