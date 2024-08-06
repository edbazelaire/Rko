using Inventory;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;

namespace Menu
{
    public class ChestsSectionUI : MonoBehaviour
    {
        #region Members

        const string c_ChestsContainer = "ChestsContainer";

        GameObject m_ChestsContainer;
        GameObject m_ChestUnlockPrefab;
        List<ChestUnlock> m_ChestItems;

        #endregion


        #region Init & End

        private void Awake()
        {
            m_ChestsContainer = Finder.Find(gameObject, c_ChestsContainer);
            m_ChestUnlockPrefab = AssetLoader.Load<GameObject>("ChestUnlock", AssetLoader.c_MainTabPath);

            SetupChestItems();
        }

        #endregion


        #region GUI Manipulators

        void SetupChestItems()
        {
            // check ratio of the game object to decide wich display format to use
            string format = UIHelper.GetSizeRatio(m_ChestsContainer) < 0.66f ? "line" : "square";

            // init List of items and parent container
            m_ChestItems = new();
            GameObject parent;
            if (format == "line")
            {
                parent = m_ChestsContainer;
            } else
            {
                parent = Finder.Find(m_ChestsContainer, "Row1");
            }

            // clean container from potential TEST values
            UIHelper.CleanContent(parent);

            for (int i = 0; i < InventoryManager.Chests.Length; i++)
            {
                // change row (if square format)
                if (i == 2 && format == "square")
                {
                    parent = Finder.Find(m_ChestsContainer, "Row2");
                    UIHelper.CleanContent(parent);
                }

                ChestUnlock chestItem = Instantiate(m_ChestUnlockPrefab, parent.transform).GetComponent<ChestUnlock>();
                chestItem.Initialize(i);
                m_ChestItems.Add(chestItem);
            }
        }

        #endregion

    }
}