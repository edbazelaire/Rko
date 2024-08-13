using Enums;
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
            // init List of items and parent container
            m_ChestItems = new();
            GameObject parent;
            if (UIHelper.ScreenAspect == EScreenAspect.Square)
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
                if (i == 2 && UIHelper.ScreenAspect != EScreenAspect.Square)
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