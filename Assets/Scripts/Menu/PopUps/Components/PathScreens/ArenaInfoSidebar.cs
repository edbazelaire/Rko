using Data.GameManagement;
using Game.Loaders;
using MyBox;
using Save;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.PopUps
{
    public class ArenaInfoSidebar : MObject
    {
        #region Members

        [SerializeField] protected GameObject m_LossKnob;
        [SerializeField] protected GameObject m_PowerUpSmallDisplayTemplate;

        // ================================================================================================
        // Data
        bool m_IsActive;
        float m_Width;

        // ================================================================================================
        // GameObjects & Components
        List<PowerUpSmallDisplay>   m_PowerUpsSmallDisplayers;
        Button                      m_DisplayButton;
        Button                      m_AbandonButton;
        Button                      m_RefreshLifeButton;
        GameObject                  m_LossesContainer;

        // Public Accessors
        public Button AbandonButton => m_AbandonButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PowerUpsSmallDisplayers   = Finder.FindComponents<PowerUpSmallDisplay>(gameObject);
            m_DisplayButton             = Finder.FindComponent<Button>(gameObject, "DisplayButton");
            m_AbandonButton             = Finder.FindComponent<Button>(gameObject, "AbandonButton");
            m_RefreshLifeButton         = Finder.FindComponent<Button>(gameObject, "RefreshLifeButton");
            m_LossesContainer           = Finder.Find(gameObject, "LossesContainer");

            m_Width = Finder.FindComponent<RectTransform>(gameObject).rect.width;
        }

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_RefreshLifeButton.gameObject.SetActive(ProfileCloudData.IsAdmin);

            m_IsActive = true;
            RefreshUI();
        }

        #endregion


        #region GUI Manipulators

        void ToggleActivation()
        {
            Activate(! m_IsActive);
        }

        void Activate(bool activate = true, bool withAnimation = true)
        {
            if (m_IsActive == activate)
                return;

            m_IsActive = activate;

            // Ensure the component is a RectTransform for UI elements
            var rectTransform = GetComponent<RectTransform>();
            if (rectTransform == null)
            {
                Debug.LogError("No RectTransform component found on this GameObject.");
                return;
            }

            // Cancel animation component (if any)
            var animationComponent = Finder.FindComponent<MoveAnimation>(gameObject, throwError: false);
            if (animationComponent != null)
            {
                Destroy(animationComponent);
            }

            // Calculate desired end position using anchoredPosition for UI elements
            Vector2 endPos = rectTransform.anchoredPosition;
            endPos.x = activate ? 0 : 25 - m_Width;

            // Rotate the display button
            m_DisplayButton.transform.rotation = Quaternion.Euler(0f, activate ? 0f : 180f, 0f);

            // Instant activation without animation
            if (!withAnimation)
            {
                rectTransform.anchoredPosition = endPos;
                return;
            }

            // Add new MoveAnimation component and initialize it with anchored positions
            animationComponent = gameObject.AddComponent<MoveAnimation>();
            animationComponent.Initialize("SideBar", 0.5f, rectTransform.anchoredPosition, endPos, checkRectTransform: true);
        }

        void RefreshUI()
        {
            SetupPowerUps();
            SetUpLosses();
        }

        void SetupPowerUps()
        {
            for (int i = 0; i < m_PowerUpsSmallDisplayers.Count;  i++)
            {
                if (ProgressionCloudData.CurrentArena.Level > i)
                {
                    string powerUpName = "";
                    if (ProgressionCloudData.CurrentArena.GetPowerUps().Length <= i)
                    {
                        ErrorHandler.Error("Number of max PowerUps " + ProgressionCloudData.CurrentArena.GetPowerUps().Length + " is <= to expeted index " + i);
                    }
                    else
                    {
                        powerUpName = ProgressionCloudData.CurrentArena.GetPowerUps()[i];
                    }

                    var powerUpData = powerUpName.IsNullOrEmpty() ? null : SpellLoader.GetPowerUp(powerUpName, InventoryCloudData.Instance.GetCollectable(CharacterBuildsCloudData.SelectedCharacter).Level);
                    m_PowerUpsSmallDisplayers[i].Initialize(powerUpData, i);
                } 
                else
                {
                    m_PowerUpsSmallDisplayers[i].Initialize(null, i);
                }
            }
        }

        /// <summary>
        /// Display number of losses
        /// </summary>
        void SetUpLosses()
        {
            UIHelper.CleanContent(m_LossesContainer);

            for (int i = 0; i < ArenaData.MAX_LOSSES; i++)
            {
                m_LossKnob = Instantiate(m_LossKnob, m_LossesContainer.transform);
                if (ProgressionCloudData.CurrentArena.Losses >= ArenaData.MAX_LOSSES - i)
                {
                    Finder.FindComponent<Image>(m_LossKnob).color = new Color(0.3f, 0.3f, 0.3f);
                }
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_DisplayButton.onClick.AddListener(OnDisplayButtonClicked);
            m_RefreshLifeButton.onClick.AddListener(OnRefreshLifeButtonClicked);
            ProgressionCloudData.CurrentArenaDataChangedEvent += RefreshUI;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_DisplayButton.onClick.RemoveListener(OnDisplayButtonClicked);
            m_RefreshLifeButton.onClick.RemoveListener(OnRefreshLifeButtonClicked);
            ProgressionCloudData.CurrentArenaDataChangedEvent -= RefreshUI;
        }

        void OnDisplayButtonClicked()
        {
            ToggleActivation();
        }

        /// <summary>
        /// [CHEAT BUTTON]
        /// </summary>
        void OnRefreshLifeButtonClicked()
        {
            ProgressionCloudData.AddArenaLoss(-3);
        }

        #endregion
    }

}