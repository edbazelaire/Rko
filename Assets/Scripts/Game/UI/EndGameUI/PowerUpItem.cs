using Data;
using Data.DataStructures.PowerEffects;
using Enums;
using Save;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


namespace Game.UI.EndGameUI
{
    public enum ESelectionMod
    {
        None = 0,
        Selected = 1,
        NotSelected = 2,
    }

    public class PowerUpItem : MObject
    {
        #region Members

        SPowerEffect    m_PowerUpData;

        TMP_Text        m_Title;
        TMP_Text        m_Description;
        Image           m_Icon;
        Image           m_IconOverlay;
        Button          m_Button;
        Button          m_ValidationButton;
        Button          m_RefreshButton;
        TMP_Text        m_RefreshCtr;
        Image           m_Border;
        GameObject      m_Selected;
        GameObject      m_LockOverlay;

        ESelectionMod m_SelectionMod;
        Color m_OriginalBorderColor;
        int m_NRefreshes;

        public SPowerEffect PowerUpData => m_PowerUpData;
        public Button Button            => m_Button;
        public Button RefreshButton     => m_RefreshButton;
        public Button ValidationButton  => m_ValidationButton;
        public bool IsSelected => m_SelectionMod == ESelectionMod.Selected;

        
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Title             = Finder.FindComponent<TMP_Text>(gameObject, "Title");
            m_Description       = Finder.FindComponent<TMP_Text>(gameObject, "Description");
            m_Icon              = Finder.FindComponent<Image>(gameObject, "Icon");
            m_IconOverlay       = Finder.FindComponent<Image>(gameObject, "IconOverlay");
            m_LockOverlay       = Finder.Find(gameObject, "LockOverlay");
            m_Button            = Finder.FindComponent<Button>(gameObject);
            m_ValidationButton  = Finder.FindComponent<Button>(gameObject, "ValidationButton");
            m_RefreshButton     = Finder.FindComponent<Button>(gameObject, "RefreshButton");
            m_RefreshCtr        = Finder.FindComponent<TMP_Text>(m_RefreshButton.gameObject, "RefreshCtr");
            m_Border            = Finder.FindComponent<Image>(gameObject, "Border");
            m_Selected          = Finder.Find(gameObject, "Selected");

            m_OriginalBorderColor = m_Border.color;
        }

        public void Initialize(SPowerEffect powerUpData, bool withRefreshButton = false)
        {
            base.Initialize();
            RefreshUI(powerUpData);
            
            m_RefreshButton.gameObject.SetActive(withRefreshButton);

            m_ValidationButton.gameObject.SetActive(false);
            m_Selected.SetActive(false);
            m_LockOverlay.SetActive(false);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI(SPowerEffect powerUpData)
        {
            m_PowerUpData = powerUpData;

            m_Title.text            = TextHandler.SplitCamelCase(m_PowerUpData.BaseName);
            m_Description.text      = m_PowerUpData.GetDescription();
            m_Icon.sprite           = AssetLoader.LoadIcon(m_PowerUpData.BaseName);
            m_IconOverlay.sprite    = AssetLoader.LoadPowerUpIconBorder(m_PowerUpData.RuneActivation);
        }

        public void SetSelected(ESelectionMod selectionMod, bool force = false)
        {
            if (m_SelectionMod == selectionMod && !force)
                return;

            m_SelectionMod = selectionMod;

            switch (selectionMod)
            {
                case ESelectionMod.None:
                    // -- selected
                    m_Border.color = m_OriginalBorderColor;
                    m_Selected.SetActive(false);

                    // -- buttons
                    if (m_NRefreshes > 0)
                        m_RefreshButton.gameObject.SetActive(true);
                    m_ValidationButton.gameObject.SetActive(false);

                    // -- lock
                    m_LockOverlay.SetActive(false);
                    break;

                case ESelectionMod.Selected:
                    m_Border.color = Color.red;
                    m_Selected.SetActive(true);

                    // -- buttons
                    m_RefreshButton.gameObject.SetActive(false);
                    m_ValidationButton.gameObject.SetActive(true);

                    // -- lock
                    m_LockOverlay.SetActive(false);
                    break;

                case ESelectionMod.NotSelected:
                    // -- selected
                    m_Border.color = m_OriginalBorderColor;
                    m_Selected.SetActive(false);

                    // -- buttons
                    m_ValidationButton.gameObject.SetActive(false);
                    m_RefreshButton.gameObject.SetActive(false);

                    // -- lock
                    m_LockOverlay.SetActive(true);
                    break;
            }
        }

        public void UpdateRefreshCounter(int nRefreshes)
        {
            m_NRefreshes = nRefreshes;
            if (nRefreshes <= 0)
            {
                m_RefreshButton.gameObject.SetActive(false);
                return;
            }

            m_RefreshButton.gameObject.SetActive(true);
            m_RefreshCtr.text = m_NRefreshes.ToString();
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

        #endregion
    }
}
