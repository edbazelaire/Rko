using Data.GameManagement;
using Enums;
using Inventory;
using Menu.Common.Buttons;
using Save;
using System;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common
{
    public class TemplateReward : MonoBehaviour
    {
        #region Members

        Image m_Icon;
        TMP_Text m_Qty;

        SReward m_Reward;
        bool m_IsAborted;

        public bool IsAborted => m_IsAborted;

        #endregion


        #region Init & End

        public void Initialize(SReward reward)
        {
            m_IsAborted = false;
            m_Reward = reward;
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Qty = Finder.FindComponent<TMP_Text>(gameObject, "Qty");
           
            // POWER ORBS
            if (reward.RewardType == typeof(EPowerOrb))
            {
                SPowerOrb powerOrb = new SPowerOrb(reward.RewardName);

                var baseTemplate = powerOrb.LoadTemplate();
                if (baseTemplate == null)
                    return;

                var template = Instantiate(baseTemplate, m_Icon.transform.parent);
            }

            // CURRENCY
            else if (reward.RewardType == typeof(ECurrency) && Enum.TryParse(reward.RewardName, out ECurrency currency))
            {
                m_Icon.sprite = AssetLoader.LoadCurrencyIcon(currency, reward.Qty);
            }

            // ACHIEVEMENT REWARDS
            else if (ProfileCloudData.TryGetType(reward.RewardType, out EAchievementReward arType, false))
            {
                var baseTemplate = AssetLoader.LoadAchievementRewardTemplate(arType);
                if (baseTemplate == null)
                    return;

                m_Qty.gameObject.SetActive(false);

                // instantiate Title Template
                var template = Instantiate(baseTemplate, m_Icon.transform.parent);
                template.Initialize(reward.RewardName, arType);

                // remove button from template
                Destroy(template.Button);

                // deactivate icon
                m_Icon.gameObject.SetActive(false);
            }

            // COLLECTABLES
            else if (CollectablesManagementData.IsCollectableType(reward.RewardType))
            {
                if (! CollectablesManagementData.TryCast(reward.RewardName, reward.RewardType, out Enum collectable))
                {
                    Abort("Unable to parse " + reward.RewardName + " as Collectable");
                    return;
                }

                var baseTemplate = AssetLoader.LoadTemplateItem(collectable).GetComponent<TemplateCollectableItemUI>();
                if (baseTemplate == null)
                { 
                    Abort("Unable to load template for reward " + reward.RewardName + " of collectable type : " + collectable);
                    return;
                }

                m_Qty.gameObject.SetActive(false);

                // instantiate Title Template
                var template = Instantiate(baseTemplate, m_Icon.transform.parent);
                template.Initialize(collectable, asIconOnly: true);
                template.SetBottomOverlay(reward.Qty > 1 ? "x" + reward.Qty.ToString() : "");

                var ratioFitter = template.AddComponent<AspectRatioFitter>();
                ratioFitter.aspectMode = AspectRatioFitter.AspectMode.HeightControlsWidth;
                ratioFitter.aspectRatio = 0.66f;

                // remove button from template
                Destroy(template.Button);

                // disable / enable VerticalLayoutGroup to make it refresh size
                var layout = template.GetComponent<VerticalLayoutGroup>();
                if (layout != null)
                {
                    layout.enabled = false;
                    CoroutineManager.DelayMethod(() => { if (layout.IsDestroyed()) return; layout.enabled = true; });
                }

                // deactivate icon
                m_Icon.gameObject.SetActive(false);
            }

            // BOOSTS
            else if (reward.RewardType == typeof(EBoost))
            {
                if (!Enum.TryParse(reward.RewardName, out EBoost boost))
                {
                    Abort("Unable to parse " + reward.RewardName + " as Boost");
                    return;
                }

                // load boost template
                var template = Instantiate(AssetLoader.LoadBoostTemplate(boost), m_Icon.transform);

                // SPECIAL BOOSTS
                if (boost == EBoost.ChestSpeedBoost)
                {
                    // deactivate qty
                    m_Qty.gameObject.SetActive(false);
                    Finder.FindComponent<TMP_Text>(template, "Timer").text = reward.Qty + "H";
                }

                // deactivate icon
                m_Icon.enabled = false;
            }

            // OTHERS
            else
                m_Icon.sprite = AssetLoader.LoadIcon(reward.RewardName, reward.RewardType);

            // set Qty (or deactivate if only one)
            if (m_Qty != null && m_Qty.gameObject.activeSelf)
            {
                if (reward.Qty <= 1)
                    m_Qty.gameObject.SetActive(false);
                else
                    m_Qty.text = "x " + reward.Qty.ToString();
            }
           
        }

        #endregion


        #region GUI Manipulators

        void Abort(string errorMsg = "")
        {
            m_IsAborted = true;
            if (m_Icon == null)
                return;

            m_Icon.sprite = null;
            m_Qty.text = "Not found : " + m_Reward.RewardName;

            if (errorMsg != "")
                ErrorHandler.Error(errorMsg);
        }
     
        #endregion
    }
}