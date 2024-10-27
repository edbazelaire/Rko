using Data.GameManagement;
using Game.Loaders;
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

            RefreshUI();
            Activate(true, false);
        }

        #endregion


        #region GUI Manipulators

        void ToggleActivation()
        {
            Activate(! m_IsActive);
        }

        void Activate(bool activate = true, bool withAnimation = true)
        {
            m_IsActive = activate;

            // cancel animation component (if any)
            var animationComponent = Finder.FindComponent<MoveAnimation>(gameObject, throwError: false);
            if (animationComponent != null)
                Destroy(animationComponent);

            // calculate desired end pos
            var endPos = transform.position;
            if (activate)
            {
                endPos.x = 0;
                m_DisplayButton.transform.rotation = Quaternion.Euler(0f, 0f, 0f);
            }
            else
            {
                endPos.x = -m_Width;
                m_DisplayButton.transform.rotation = Quaternion.Euler(0f, 180f, 0f);
            }

            // instant activation (no animation)
            if (! withAnimation)
            {
                transform.position = endPos;
                return;
            }

            animationComponent = gameObject.AddComponent<MoveAnimation>();
            animationComponent.Initialize("SideBar", 2.5f, transform.position, endPos);
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
                if (ProgressionCloudData.CurrentArena.PowerUps.Count > i)
                {
                    m_PowerUpsSmallDisplayers[i].Initialize(SpellLoader.PowerUpsData[ProgressionCloudData.CurrentArena.PowerUps[i]]);
                    continue;
                }

                m_PowerUpsSmallDisplayers[i].Initialize(null);
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