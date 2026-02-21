using Assets.Scripts.Managers;
using Data;
using Enums;
using Game.Loaders;
using System.Collections.Generic;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;
using static UnityEngine.UI.AspectRatioFitter;

namespace Menu.Common.Infos
{
    public class AbilityInfoRowUI : InfoRowUI
    {
        #region Members

        [SerializeField] GameObject m_TemplateIconDisplayer;

        GameObject m_StateEffectsContainer;

        SpellData m_SpellData;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_StateEffectsContainer = Finder.Find(gameObject, "StateEffectsContainer");
            m_Button = Finder.FindComponent<Button>(gameObject);
        }

        public void Initialize(string name, int level)
        {
            m_SpellData = SpellLoader.GetSpellData(name, level);

            base.Initialize(name, null);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            SetupStateEffects();
        }

        #endregion


        #region GUI Manipulators

        protected override void SetUpIcon()
        {
            m_Icon.sprite = AssetLoader.LoadSpellIcon(m_SpellData.Name);
        }

        void SetupStateEffects()
        {
            var spellInfos = m_SpellData.GetInfo();
            if (!spellInfos.ContainsKey("Effects"))
            {
                m_StateEffectsContainer.SetActive(false);
                return;
            }

            var stateEffects = (List<SStateEffectData>)spellInfos["Effects"];
            if (stateEffects.Count == 0)
            {
                m_StateEffectsContainer.SetActive(false);
                return;
            }

            m_StateEffectsContainer.SetActive(true);
            UIHelper.CleanContent(m_StateEffectsContainer);
            foreach (var stateEffectData in stateEffects)
            {
                var template = Instantiate(m_TemplateIconDisplayer, m_StateEffectsContainer.transform);
                var arf = Finder.FindComponent<AspectRatioFitter>(template);
                arf.enabled = true;
                arf.aspectMode = AspectMode.HeightControlsWidth;
                Finder.FindComponent<Image>(template, "Icon").sprite = AssetLoader.LoadStateEffectIcon(stateEffectData.StateEffect.ToString());
            }

            LayoutRebuilder.ForceRebuildLayoutImmediate(m_StateEffectsContainer.GetComponent<RectTransform>());
        }


        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_Button.onClick.AddListener(OpenInfoPopUp);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_Button.onClick.RemoveAllListeners();
        }

        void OpenInfoPopUp()
        {
            ScreenManager.SetPopUp(EPopUpState.SpellInfoPopUp, m_SpellData.Spell, m_SpellData.Level, true);
        }

        #endregion
    }
}