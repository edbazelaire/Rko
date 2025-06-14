using Assets;
using Assets.Scripts.Data.PowerUps;
using Data;
using Enums;
using Save;
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

        SRunePower  m_RunePower;
        int         m_Index;

        Image       m_Background;
        Image       m_Icon;
        Button      m_Button;

        protected bool m_IsMissingData => m_Index < ProgressionCloudData.CurrentArena.Level && m_RunePower == null;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Background    = Finder.FindComponent<Image>(gameObject, "Background");
            m_Icon          = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Button        = Finder.FindComponent<Button>(gameObject);
        }

        public virtual void Initialize(SRunePower powerUpData, int index)
        {
            m_RunePower = powerUpData;
            m_Index = index;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            RefreshUI(m_RunePower);
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(SRunePower powerUpData)
        {
            m_RunePower = powerUpData;

            SetUpBackground();
            SetUpIcon();
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

            if (m_RunePower == null)
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
            if (m_RunePower == null)
            {
                m_Icon.gameObject.SetActive(false);
                return;
            }

            m_Icon.gameObject.SetActive(true);
            m_Icon.sprite = AssetLoader.LoadIcon(m_RunePower.RuneName);
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
                Main.SetPopUp(EPopUpState.PowerUpSelectionScreen, m_Index);
                return;
            }

            // Has PowerUp -> Display the Info Screen
            if (m_RunePower != null)
            {
                Main.SetPopUp(EPopUpState.PowerUpInfoScreen, m_RunePower);
                return;
            }
        }

        #endregion
    }
}

