using Menu.MainMenu;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Menu
{
    public class TabsManager : MonoBehaviour
    {
        #region Members

        // =======================================================================================
        // Actions
        public Action<string> TabSelectedEvent;

        // =======================================================================================
        // GameObjects & Components
        protected const string      c_TabButtonsContainer       = "TabButtonsContainer";
        protected const string      c_TabButtonSuffix           = "Button";
        protected const string      c_TabsContainer             = "TabsContainer";
        protected const string      c_TabsContainerContent      = "Content";

        [SerializeField] protected AudioClip m_ChangeTabSoundFX = null;

        /// <summary> </summary>
        protected GameObject        m_TabButtonsContainer;
        /// <summary> </summary>
        protected GameObject        m_TabsContainer;
        /// <summary> </summary>
        protected GameObject        m_TabsContainerContent;

        // =======================================================================================
        // Data
        /// <summary> enum of all the tabs </summary>
        protected virtual Type m_TabEnumType { get; set; } = null;
        /// <summary> default tab opened </summary>
        protected virtual Enum m_DefaultTab { get; set; } = null;

        /// <summary> dict of all Tabs buttons linked to their enum value </summary>
        protected Dictionary<Enum, TabButton> m_TabButtons = new();
        /// <summary> dict of all Tabs tabs linked to their enum value </summary>
        protected Dictionary<Enum, TabContent> m_Tabs;
        /// <summary> default tab displayed on entering the menu </summary>
        /// <summary> current tab displayed </summary>
        protected Enum m_CurrentTab = null;

        /// <summary> tab content currently displayed </summary>
        protected TabContent m_CurrentTabContent => m_CurrentTab != null ? m_Tabs[m_CurrentTab] : null;

        public T GetCurrentTab<T>() where T : TabContent
        {
            if (m_CurrentTabContent == null)
            {
                ErrorHandler.Error("No tab is currently selected");
                return null;
            }

            try
            {
                return (T)m_CurrentTabContent;
            } 
            catch (Exception e) 
            {
                ErrorHandler.Error("Unable to parse current tab content (" + m_CurrentTab.ToString() + ") as " + typeof(T));
                ErrorHandler.Error(e.Message);
                return null;
            }
        }

        public TabButton GetTabButton(Enum tab) => m_TabButtons[tab];

        #endregion


        #region Init & End

        protected virtual void Awake()
        {
            InitTabsContent();
            InitTabsButtonContainer();

            RegisterTabs();
        }

        public virtual void Initialize() { }

        protected virtual void OnDestroy() 
        {
            UnRegisterTabs();
        }

        #endregion


        #region Initializing Components

        protected virtual void InitTabsContent()
        {
            m_TabsContainer = Finder.Find(gameObject, c_TabsContainer);
            m_TabsContainerContent = Finder.Find(m_TabsContainer, c_TabsContainerContent, false);

            if (m_TabsContainerContent == null)
                m_TabsContainerContent = m_TabsContainer;
        }

        protected virtual void InitTabsButtonContainer()
        {
            // can be null
            m_TabButtonsContainer = Finder.Find(gameObject, c_TabButtonsContainer, false);
        }

        #endregion


        #region Tabs Management

        /// <summary>
        /// Register and init all TabsButton and TabsContent
        /// </summary>
        protected virtual void RegisterTabs()
        {
            m_Tabs = new Dictionary<Enum, TabContent>();
            m_TabButtons = new Dictionary<Enum, TabButton>();
            if (m_TabEnumType == null)
            {
                ErrorHandler.FatalError("Enum of tabs was not provided for " + name + " : set m_TabEnumType");
                return;
            }

            foreach (Enum tab in Enum.GetValues(m_TabEnumType))
            {
                RegisterTab(tab);
            }

            if (m_Tabs.Count == 0)
            {
                ErrorHandler.Error("No tabs found for this tab manager : " + name);
                return;
            }

            // if no default tab provided OR if the default tab is not accessible : find first available default table
            if (m_DefaultTab == null || ! m_Tabs.ContainsKey(m_DefaultTab))
            {
                for (int i = 0; i < m_Tabs.Count; i++)
                {
                    m_DefaultTab = (Enum)Enum.GetValues(m_TabEnumType).GetValue(0);
                    if (m_Tabs.ContainsKey(m_DefaultTab))
                        break;
                }
            }

            // select default tab (without animation)
            SelectTab(m_DefaultTab, false);
        }

        /// <summary>
        /// Register & Init the requested tab
        /// </summary>
        /// <param name="tab"></param>
        protected virtual void RegisterTab(Enum tab)
        {
            var tabObject = Finder.FindComponent<TabContent>(m_TabsContainerContent, tab.ToString(), false);
            if (tabObject == null)
            {
                //ErrorHandler.Error("Unable to find tab " + tab.ToString() + " in " + name);
                return;
            }

            // find tab button (if tab button container was found)
            TabButton tabButton = null;
            if (m_TabButtonsContainer != null)
            {
                tabButton = Finder.FindComponent<TabButton>(m_TabButtonsContainer, tab.ToString() + c_TabButtonSuffix, false);
                tabButton?.Initialize();
            }

            // initialize tab content (with tab button if any)
            tabObject.Initialize(tabButton, m_ChangeTabSoundFX);

            // register to tab button event
            if (tabButton != null)
            {
                // register tab button click
                tabButton.TabButtonClickedEvent += () => { SelectTab(tab, withAnim: false); };
                // deactivate tab button by default
                tabButton.Activate(false);
                // add to list of tab buttons
                m_TabButtons[tab] = tabButton;
            }

            // deactivate tab by default
            tabObject.Activate(false);

            // add to dict of tabs
            m_Tabs.Add(tab, tabObject);
        }

        /// <summary>
        /// Select and Activate the requested tab
        /// </summary>
        /// <param name="tabIndex"></param>
        /// <param name="withAnim"></param>
        public virtual void SelectTab(Enum tabIndex, bool withAnim = true)
        {
            if (m_CurrentTab != null && m_CurrentTab.Equals(tabIndex))
                return;

            // deactivate current tab window
            m_CurrentTabContent?.Activate(false);

            // set new current tab
            m_CurrentTab = tabIndex;

            // adjust position of the new tab to match the viewport
            DisplayCurrentTab(withAnim);

            // activate new window tab
            m_CurrentTabContent.Activate(true);

            // call event that the tab was selected
            TabSelectedEvent?.Invoke(tabIndex.ToString());
        }

        /// <summary>
        /// Unregister all tabs, unregister listeners
        /// </summary>
        protected virtual void UnRegisterTabs()
        {
            foreach (var tab in m_Tabs)
            {
                UnRegisterTab(tab.Key);
            }
        }

        protected virtual void UnRegisterTab(Enum tab)
        {
            // unregister from tab button event
            if (m_Tabs[tab].TabButton != null)
                m_Tabs[tab].TabButton.TabButtonClickedEvent -= () => { SelectTab(tab); };

            m_Tabs[tab].UnRegister();
        }

        /// <summary>
        /// Display the currently selected tab
        /// </summary>
        /// <param name="withAnim"></param>
        protected virtual void DisplayCurrentTab(bool withAnim = true) { }

        #endregion
    }
}