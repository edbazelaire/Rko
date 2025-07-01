using Data.GameManagement;
using Inventory;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Displayers
{
    public class RewardsDisplayer : MonoBehaviour
    {
        #region Members

        [SerializeField] HorizontalLayoutGroup m_RowTemplate;

        HorizontalLayoutGroup m_RewardsDisplayRow;
        GameObject m_RewardsDisplayContainer;
        GameObject m_TemplateReward;

        #endregion

        public void Initialize(SRewardsData rewardsData, int maxElemPerRow = 4)
        {
            Initialize(rewardsData.Rewards, maxElemPerRow);
        }

        public void Initialize(List<SReward> rewardsData, int maxElemPerRow = 4)
        {
            m_RewardsDisplayContainer = Finder.Find(gameObject, "Content");
            m_RewardsDisplayRow = m_RowTemplate != null ? m_RowTemplate : Finder.FindComponent<HorizontalLayoutGroup>(m_RewardsDisplayContainer);
            m_TemplateReward = AssetLoader.LoadTemplateItem("Reward");

            // clean items
            UIHelper.CleanContent(m_RewardsDisplayRow.gameObject);

            SetUpRewards(rewardsData, maxElemPerRow);
        }

        public void SetUpRewards(List<SReward> rewards, int maxElemPerRow = 4)
        {
            UIHelper.CleanContent(m_RewardsDisplayContainer, startAt: m_RowTemplate == null ? 1 : 0);

            // Total number of rewards
            int rewardCount = rewards.Count;

            // Calculate the number of rows needed
            int rowCount = Mathf.CeilToInt((float)rewardCount / maxElemPerRow);
            if (rowCount == 0)
                return;

            // Calculate base number of items per row
            int baseItemsPerRow = rewardCount / rowCount;
            // Calculate how many rows will have one extra item
            int rowsWithExtraItem = rewardCount % rowCount;

            Transform row = m_RewardsDisplayRow.transform;
            int rewardIndex = 0;

            for (int i = 0; i < rowCount; i++)
            {
                // Determine how many elements to put in this row
                int itemsInRow = baseItemsPerRow + (i < rowsWithExtraItem ? 1 : 0);

                // Instantiate a new row
                if (i > 0)
                    row = Instantiate(m_RewardsDisplayRow, m_RewardsDisplayContainer.transform).GetComponent<Transform>();
              
                UIHelper.CleanContent(row);
                for (int j = 0; j < itemsInRow; j++)
                {
                    // Instantiate the reward template and initialize it with the reward
                    var template = Instantiate(m_TemplateReward, row).GetComponent<TemplateReward>();
                    template.Initialize(rewards[rewardIndex]);
                    rewardIndex++;
                }
            }
        }

    }
}