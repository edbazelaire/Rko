using Data;
using Menu.Common.Buttons;
using Save;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;


namespace Menu.Common.Buttons
{
    public class TemplateCharacterAchievement : TemplateAchievementButton
    {
        #region Members

        GameObject m_Lock;
        TMP_Text m_MasteryText;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_Lock = Finder.Find(gameObject, "Lock");
            m_MasteryText = Finder.FindComponent<TMP_Text>(m_Lock, "MasteryText");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            RefreshLock();
        }

        #endregion


        #region GUI Manipulators

        protected override void RefreshUI()
        {
            if (this.IsDestroyed())
            {
                ErrorHandler.Error("Trying to RefreshUI() of TemplateAchievementButton but button is destroyed");
                return;
            }

            base.RefreshUI();
            RefreshLock();
        }

        void RefreshLock()
        {
            if (m_AchievementData.IsMasteryUnlocked())
            {
                m_Lock.SetActive(false);
                return;
            }

            m_Lock.SetActive(true);
            m_MasteryText.text = "Mastery " + TextHandler.ToRoman(InventoryCloudData.Instance.GetCollectable(m_AchievementData.Character).Mastery + 1);
        }

        #endregion
    }
}
