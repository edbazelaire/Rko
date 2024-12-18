using Data;
using Data.DataStructures;
using Game.Loaders;
using Menu.Common.Buttons.TemplateItemButtons;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class RunePowerPopUp : PopUp
    {
        #region Members

        // =========================================================================================
        // Data
        SRunePower                          m_RunePower;

        // =========================================================================================
        // GameObjects & Components
        GameObject                          m_PreviewContainer;
        TMP_Text                            m_TitleText;
        TMP_Text                            m_Description;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // -- left section
            var leftSection = Finder.Find(m_WindowContent, "LeftSection");
            m_PreviewContainer = Finder.Find(leftSection, "PreviewContainer");
            m_TitleText = Finder.FindComponent<TMP_Text>(leftSection, "TitleText");

            // -- right section
            var rightSection = Finder.Find(m_WindowContent, "RightSection");
            m_Description = Finder.FindComponent<TMP_Text>(rightSection, "Description");
        }

        public void Initialize(SRunePower runePower)
        {
            m_RunePower = runePower;

            base.Initialize();
        }

        /// <summary>
        /// Called when the prefab is loaded : register all components & game objects, then initilaize UI
        /// </summary>
        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            CoroutineManager.DelayMethod(SetUpIcon);
            SetUpTitle();
            SetUpDescription();
        }

        #endregion


        #region GUI Manipulators

        void SetUpIcon()
        {
            // clean before instantiating
            UIHelper.CleanContent(m_PreviewContainer);

            // load template of TriggerEffect UI
            var template = AssetLoader.LoadTemplateItem<TemplateRunePowerUI>();

            // Instantiate & Init UI
            Instantiate(template, m_PreviewContainer.transform).Initialize(m_RunePower);
        }

        void SetUpTitle()
        {
            string effectName = m_RunePower.Name;
            if (effectName.StartsWith("_Trigger"))
                effectName = effectName[8..];
            if (effectName.StartsWith("_"))
                effectName = effectName[1..];

            m_TitleText.text = TextHandler.SplitCamelCase(effectName);
        }

        void SetUpDescription()
        {
            m_Description.text = m_RunePower.GetDescription();
        }

        #endregion
    }
}