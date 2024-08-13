using Data.DataStructures;
using Game.Loaders;
using Menu.Common.Buttons.TemplateItemButtons;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.PopUps
{
    public class TriggerEffectPopUp : PopUp
    {
        #region Members

        // =========================================================================================
        // Data
        STriggerEffect                      m_TriggerEffect;

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

        public void Initialize(STriggerEffect triggerEffect)
        {
            m_TriggerEffect = triggerEffect;

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
            var template = AssetLoader.LoadTemplateItem<TemplateTriggerEffectUI>();

            // Instantiate & Init UI
            Instantiate(template, m_PreviewContainer.transform).Initialize(m_TriggerEffect);
        }

        void SetUpTitle()
        {
            string effectName = m_TriggerEffect.SpellDataName;
            if (effectName.StartsWith("_Trigger"))
                effectName = effectName[8..];
            if (effectName.StartsWith("_"))
                effectName = effectName[1..];

            m_TitleText.text = TextHandler.Split(effectName);
        }

        void SetUpDescription()
        {
            string description = "";

            if (SpellLoader.SpellExists(m_TriggerEffect.SpellDataName))
            {
                description = SpellLoader.GetSpellDescription(m_TriggerEffect.SpellDataName, m_TriggerEffect.Level);
            } 
            else if (SpellLoader.StateEffectExists(m_TriggerEffect.SpellDataName))
            {
                description = SpellLoader.GetStateEffectDescription(m_TriggerEffect.SpellDataName, m_TriggerEffect.Level);
            }
            else
            {
                ErrorHandler.Error("Unable to find any spell or state effect named : " +  m_TriggerEffect.SpellDataName);
            }

            m_Description.text = description;
        }

        #endregion
    }
}