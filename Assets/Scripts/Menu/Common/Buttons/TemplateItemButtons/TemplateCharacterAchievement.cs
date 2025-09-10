using Data;
using Menu.Common.Buttons;
using Save;
using TMPro;
using Tools;
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

        public override void Initialize()
        {
            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            RefreshLock();
        }

        #endregion


        #region GUI Manipulators

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


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}
