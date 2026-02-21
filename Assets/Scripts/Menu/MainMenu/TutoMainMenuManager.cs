using System.Collections;
using Enums;
using Game.UI;
using Menu;
using Menu.PopUps;
using Save;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using Assets.Scripts.Managers;
using Inventory;

namespace Menu.MainMenu
{
    /// <summary>
    /// Runs the post-game tutorial on MainMenu: open chest, go to Inventory tab, change one spell in build.
    /// Add this component to a GameObject in the MainMenu scene (e.g. on MainMenuManager or a dedicated "TutoMainMenu" object).
    /// </summary>
    public class TutoMainMenuManager : MonoBehaviour
    {
        #region Members

        static TutoMainMenuManager s_Instance;

        MainMenuManager m_MainMenuManager;
        HandUI m_Hand;
        FocusManager m_FocusManager;
        ESpell[] m_InitialBuildSpells;

        public static TutoMainMenuManager Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;
                s_Instance = FindAnyObjectByType<TutoMainMenuManager>();
                return s_Instance;
            }
        }

        #endregion


        #region Init & End

        void Start()
        {
            ErrorHandler.Log(() => $"[TutoMainMenu] Start() TutoFightDone={ProfileCloudData.TutoFightDone} TutoDone={ProfileCloudData.TutoDone}", ELogTag.GameSystem);
            if (ProfileCloudData.TutoDone)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] Skipping post-game tutorial (conditions not met)", ELogTag.GameSystem);
                return;
            }
            ErrorHandler.Log(() => "[TutoMainMenu] Starting PostGameTutorial coroutine", ELogTag.GameSystem);
            StartCoroutine(PostGameTutorial());
        }

        /// <summary>
        /// Main coroutine containing all the tutorial steps
        ///     - Open a chest
        ///     - Go to the Inventory and change a spell
        ///     - Start a new game
        /// </summary>
        /// <returns></returns>
        IEnumerator PostGameTutorial()
        {
            yield return new WaitForSeconds(0.5f);

            ErrorHandler.Log(() => "[TutoMainMenu] PostGameTutorial: getting MainMenuManager", ELogTag.GameSystem);
            m_MainMenuManager = ScreenManager.MainMenuManager;
            if (m_MainMenuManager == null)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] FAIL: MainMenuManager is null", ELogTag.GameSystem);
                yield break;
            }
            ErrorHandler.Log(() => "[TutoMainMenu] MainMenuManager OK, currentTab=" + m_MainMenuManager.CurrentTab, ELogTag.GameSystem);

            // Chest is already given at end of Tuto Fight (EndGameUI). Ensure we're on MainTab so chests are visible.
            if (m_MainMenuManager.CurrentTab != EMainMenuTabs.MainTab)
                m_MainMenuManager.SelectTab(EMainMenuTabs.MainTab, withAnim: false);

            yield return new WaitForSeconds(0.3f);

            // -- open chest tuto
            yield return WaitForPlayerToOpenChest();
            // -- inventory tutorial
            yield return WaitInventoryTuto();
            // -- play first game tuto
            yield return WaitClickFirstGame();

            ErrorHandler.Log(() => "[TutoMainMenu] All steps done, setting TutoDone=true", ELogTag.GameSystem);
            ProfileCloudData.Instance.SetData(ProfileCloudData.KEY_TUTO_DONE, true, true);
        }

        #endregion


        #region Open Chest Tuto

        IEnumerator WaitForPlayerToOpenChest()
        {
            ErrorHandler.Log(() => "[TutoMainMenu] Step 2: WaitForPlayerToOpenChest", ELogTag.GameSystem);

            ChestUnlock readyChest = GetFirstReadyChestUnlock();
            if (readyChest == null || readyChest.ButtonGameObject == null)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: no ready chest (readyChest=" + (readyChest != null) + " button=" + (readyChest != null && readyChest.ButtonGameObject != null) + ") HasAnyReady=" + HasAnyReadyChest(), ELogTag.GameSystem);
                yield break;
            }
            ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: found ready chest, loading Hand prefab", ELogTag.GameSystem);

            GameObject handPrefab = AssetLoader.Load<GameObject>("Hand", AssetLoader.c_TutoGameObjectsPath);
            if (handPrefab == null)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: Hand prefab is null, waiting for player to open chest manually", ELogTag.GameSystem);
                yield return new WaitUntil(() => !HasAnyReadyChest());
                yield break;
            }

            Canvas canvas = readyChest.ButtonGameObject.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = FindFirstCanvas();
            if (canvas == null)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: Canvas is null", ELogTag.GameSystem);
                yield break;
            }

            HandUI hand = Instantiate(handPrefab, canvas.transform).GetComponent<HandUI>();
            if (hand != null)
            {
                hand.Initialize();
                yield return hand.ClickOn(readyChest.ButtonGameObject, new Vector3(0, 1, 0));
                Destroy(hand.gameObject);
                ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: player clicked chest", ELogTag.GameSystem);
            }
            else
            {
                ErrorHandler.Log(() => "[TutoMainMenu] OpenChest: Hand prefab has no HandUI component, waiting for manual open", ELogTag.GameSystem);
                yield return new WaitUntil(() => !HasAnyReadyChest());
            }

            yield return new WaitUntil(() => ScreenManager.CurrentScreen == null || !(ScreenManager.CurrentScreen is RewardsScreen));
        }

        bool HasAnyReadyChest()
        {
            var chests = InventoryManager.Chests;
            if (chests == null) return false;
            for (int i = 0; i < chests.Length; i++)
            {
                if (chests[i] != null && chests[i].GetState() == EChestLockState.Ready)
                    return true;
            }
            return false;
        }

        ChestUnlock GetFirstReadyChestUnlock()
        {
            var all = FindObjectsByType<ChestUnlock>(FindObjectsSortMode.None);
            foreach (var c in all)
            {
                if (c.IsReady && c.ButtonGameObject != null)
                    return c;
            }
            return null;
        }

        #endregion


        #region Inventory Tuto

        /// <summary>
        /// - OPEN the Inventory Tab
        /// - Change a spell
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitInventoryTuto()
        {
            yield return null;

            //// Highlight InventoryTabButton and make player go to Inventory tab
            //ErrorHandler.Log(() => "[TutoMainMenu] Step 3: WaitForPlayerToOpenInventoryTab", ELogTag.GameSystem);
            //yield return WaitForPlayerToOpenInventoryTab();

            //yield return new WaitForSeconds(0.3f);

            //// Make player change at least one spell in current build
            //ErrorHandler.Log(() => "[TutoMainMenu] Step 4: WaitForPlayerToChangeSpell", ELogTag.GameSystem);
            //yield return WaitForPlayerToChangeSpell();
        }

        /// <summary>
        /// Make the player open the Inventory Tab
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitForPlayerToOpenInventoryTab()
        {
            var tabButton = m_MainMenuManager.GetTabButton(EMainMenuTabs.InventoryTab);
            if (tabButton == null || tabButton.gameObject == null)
            {
                ErrorHandler.Log(() => "[TutoMainMenu] OpenInventoryTab: InventoryTab button is null", ELogTag.GameSystem);
                yield break;
            }
            ErrorHandler.Log(() => "[TutoMainMenu] OpenInventoryTab: highlighting tab, waiting for player", ELogTag.GameSystem);

            // Highlight: try FocusManager (same as in-game tutorial)
            GameObject focusPrefab = AssetLoader.Load<GameObject>("FocusManager", AssetLoader.c_GameUIContentPath + "Tutorial/");
            if (focusPrefab != null)
            {
                Canvas canvas = FindFirstCanvas();
                if (canvas != null)
                {
                    var focusGo = Instantiate(focusPrefab, canvas.transform);
                    m_FocusManager = Finder.FindComponent<FocusManager>(focusGo);
                    if (m_FocusManager != null)
                        m_FocusManager.Focus(tabButton.gameObject);
                }
                else
                    ErrorHandler.Log(() => "[TutoMainMenu] OpenInventoryTab: Canvas null, no focus overlay", ELogTag.GameSystem);
            }
            else
                ErrorHandler.Log(() => "[TutoMainMenu] OpenInventoryTab: FocusManager prefab null", ELogTag.GameSystem);

            yield return new WaitUntil(() => m_MainMenuManager.CurrentTab == EMainMenuTabs.InventoryTab);
            ErrorHandler.Log(() => "[TutoMainMenu] OpenInventoryTab: player opened Inventory tab", ELogTag.GameSystem);

            if (m_FocusManager != null)
            {
                m_FocusManager.RemoveFocus();
                Destroy(m_FocusManager.gameObject);
                m_FocusManager = null;
            }
        }

        /// <summary>
        /// Shows the player how to change spells in the Inventory Tab
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitForPlayerToChangeSpell()
        {
            ErrorHandler.Log(() => "[TutoMainMenu] ChangeSpell: waiting for player to change at least one spell", ELogTag.GameSystem);
            m_InitialBuildSpells = (ESpell[])CharacterBuildsCloudData.CurrentSpells.Clone();
            bool changed = false;
            void OnBuildChanged()
            {
                var current = CharacterBuildsCloudData.CurrentSpells;
                if (current == null || m_InitialBuildSpells == null) return;
                for (int i = 0; i < current.Length && i < m_InitialBuildSpells.Length; i++)
                {
                    if (current[i] != m_InitialBuildSpells[i])
                    {
                        changed = true;
                        break;
                    }
                }
            }

            CharacterBuildsCloudData.CurrentBuildValueChangedEvent += OnBuildChanged;
            yield return new WaitUntil(() => changed);
            CharacterBuildsCloudData.CurrentBuildValueChangedEvent -= OnBuildChanged;
            ErrorHandler.Log(() => "[TutoMainMenu] ChangeSpell: player changed build", ELogTag.GameSystem);
        }

        #endregion


        #region Click First Game Tuto

        /// <summary>
        /// Make the player click its first game
        /// </summary>
        /// <returns></returns>
        IEnumerator WaitClickFirstGame()
        {
            yield return null;
        }

        #endregion


        #region Utility

        Canvas FindFirstCanvas()
        {
            var c = FindFirstObjectByType<Canvas>();
            return c;
        }


        #endregion
    }
}
