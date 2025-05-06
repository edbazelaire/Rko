using Assets.Scripts.Managers;
using Enums;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;


namespace Managers.MainMenu
{
    [Serializable]
    public class SRecurrentPopUpDisplay
    {
        public EPopUpState PopUp;
        public List<int> Tresholds;

        public int CurrentTreshold { 
            get { return PlayerPrefs.GetInt(PopUp.ToString()+"Treshold", -1); } 
            set { PlayerPrefs.SetInt(PopUp.ToString()+"Treshold", value); } 
        }

        public bool Enabled
        {
            get { return PlayerPrefs.GetInt(PopUp.ToString() + "Enabled", 1) != 0; }
            set { PlayerPrefs.SetInt(PopUp.ToString() + "Enabled", value ? 1 : 0); }
        }
    }

    public class RecurrentPopupManager : MonoBehaviour
    {
        #region Members

        public const string KEY_POPUP_COUNTER   = "PopUpCounter";
        public const string KEY_POPUP_COOLDOWN  = "PopUpCooldown";
        public const int POPUP_COOLDOWN         = 0;

        [SerializeField] List<SRecurrentPopUpDisplay> m_PopUpDisplays;

        static RecurrentPopupManager s_Instance;
        public static RecurrentPopupManager Instance => s_Instance;

        public int PopUpCounter
        {
            get { return PlayerPrefs.GetInt(KEY_POPUP_COUNTER, 0); }
            set { PlayerPrefs.SetInt(KEY_POPUP_COUNTER, value); }
        }

        public int PopUpCooldown
        {
            get { return PlayerPrefs.GetInt(KEY_POPUP_COOLDOWN, 0); }
            set { PlayerPrefs.SetInt(KEY_POPUP_COOLDOWN, value); }
        }

        public bool IsInCooldown => PopUpCooldown > 0;

        #endregion


        #region Init & End

        private void Awake()
        {
            s_Instance = this;

            UpdateCounter();
            CheckPopUpToDisplay();
        }

        #endregion


        #region Counters

        void UpdateCounter()
        {
            PopUpCounter += 1;

            // reduce cooldown before displaying next popup
            if (IsInCooldown)
                PopUpCooldown -= 1;
        }

        void RefreshCooldown()
        {
            PopUpCooldown = POPUP_COOLDOWN;
        }

        void CheckPopUpToDisplay()
        {
            // no display if in cooldown
            if (IsInCooldown)
                return;

            // get first available popup do display
            foreach (var popupDisplay in m_PopUpDisplays)
            {
                // check specifics for each popups
                CheckSpecialCases(popupDisplay);

                if (!popupDisplay.Enabled)
                    continue;

                int currentTreshold = popupDisplay.CurrentTreshold;
                int nextThreshold = popupDisplay.Tresholds.FirstOrDefault(t => t > currentTreshold);

                // check max threshold reached
                if (nextThreshold <= currentTreshold)
                {
                    popupDisplay.Enabled = false;
                    continue;
                }

                // check if counter value is sup to next display threshold
                if (PopUpCounter < nextThreshold)
                    continue;

                // update variables
                popupDisplay.CurrentTreshold = nextThreshold;
                RefreshCooldown();

                // display the popup
                DisplayPopUp(popupDisplay.PopUp);
                break;
            }
        }

        #endregion


        #region Helpers

        public void CheckSpecialCases(SRecurrentPopUpDisplay popUpDisplay)
        {
            switch (popUpDisplay.PopUp)
            {
                case EPopUpState.PseudoPopUp:
                    popUpDisplay.Enabled = ProfileCloudData.HasDefaultPseudo;
                    break;

                case EPopUpState.LoginPopUp:
                    popUpDisplay.Enabled = ! AuthManager.Instance.IsLoggedIn;
                    break;
            }
        }

        public void Enable(EPopUpState popUpState, bool enable = true)
        {
            m_PopUpDisplays.Where(t => t.PopUp == popUpState);
            if (m_PopUpDisplays.Count == 0)
            {
                ErrorHandler.Warning("Popup " + popUpState + " not found");
                return;
            }

            if (m_PopUpDisplays.Count > 1)
            {
                ErrorHandler.Warning("Multiple Popups " + popUpState + " found");
            }

            m_PopUpDisplays[0].Enabled = enable;
        }

        public void Diseable(EPopUpState popUpState)
        {
            Enable(popUpState, false);
        }

        #endregion


        #region Displayig

        void DisplayPopUp(EPopUpState popUpState)
        {
            ScreenManager.SetPopUp(popUpState);
        }

        #endregion

    }
}
