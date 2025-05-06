using AI;
using Game.AI;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Debugs.BT
{
    public class BTDebugger : MObject
    {
        #region Members

        Button          m_DisplayButton;                // activation / deactivation button
        Transform       m_BTContainer;                  // Parent UI transform for nodes
        Slider          m_OffensiveMeterSlider;

        BehaviorTree m_BehaviorTree;             // Reference to the BehaviorTree component
        bool m_IsActive = true;
        float m_Width;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_DisplayButton         = Finder.FindComponent<Button>(gameObject, "DisplayButton");
            m_BTContainer           = Finder.Find(gameObject, "BTContainer").transform;
            m_OffensiveMeterSlider  = Finder.FindComponent<Slider>(gameObject, "OffensiveMeterSlider");

            m_Width                 = Finder.FindComponent<RectTransform>(gameObject).rect.width;
        }

        public void Initialize(Controller controller)
        {
            if (controller.IsPlayer)
            {
                ErrorHandler.Error("trying to display BT of a Player");
                return;
            }

            m_BehaviorTree = controller.BehaviorTree;

            base.Initialize();

            Activate(false, false);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetupNodes();
        }

        void SetupNodes()
        {
            if (m_BehaviorTree == null || m_BehaviorTree.RootNode == null)
            {
                ErrorHandler.Error("Behavior tree or root node is null");
                return;
            }

            // clean content before spawning
            UIHelper.CleanContent(m_BTContainer.gameObject);

            // Start recursion from root node
            InstantiateRootNode();
        }

        void InstantiateRootNode()
        {
            // instantiate first node as ROOT node
            Instantiate(AssetLoader.Load<BTNodeUI>("BTNodeUI", AssetLoader.c_GameUIContentPath), m_BTContainer).Initialize(m_BehaviorTree.RootNode);
            
            // instantiate a slector node that will recursivly insantiate the rest of the nodes
            Instantiate(AssetLoader.Load<NodeContainer>("SelectorContainer", AssetLoader.c_GameUIContentPath), m_BTContainer).Initialize(m_BehaviorTree.RootNode);
        }

        #endregion


        #region Update

        void Update()
        {
            // Update Offensive Meter display
            if (m_BehaviorTree is CharacterBT characterBT)
                m_OffensiveMeterSlider.value = characterBT.OffensiveMeter;
        }

        #endregion


        #region GUI Manipulators

        void ToggleActivation()
        {
            Debug.Log("ToggleActivation");
            Activate(!m_IsActive);
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
            endPos.x = activate ? -m_Width / 2 : m_Width / 2;

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

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_DisplayButton.onClick.AddListener(ToggleActivation);
        }

        #endregion
    }
}