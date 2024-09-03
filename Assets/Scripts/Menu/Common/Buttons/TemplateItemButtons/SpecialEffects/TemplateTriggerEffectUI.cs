using Assets;
using Data.DataStructures;
using Enums;
using Tools;


namespace Menu.Common.Buttons.TemplateItemButtons
{
    public class TemplateTriggerEffectUI : TemplateItemButton
    {
        #region Members

        // Init Data
        STriggerEffect m_TriggerEffect;

        #endregion


        #region Init & End

        public virtual void Initialize(STriggerEffect triggerEffect)
        {
            m_TriggerEffect = triggerEffect;

            base.Initialize();

            SetUpUI();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.LoadSpellIcon(m_TriggerEffect.SpellDataName);

            SetBottomOverlay("Level " + m_TriggerEffect.Level);
        }

        #endregion


        #region GUI Manipulators



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

        protected override void OnClick()
        {
            base.OnClick();

            Main.SetPopUp(EPopUpState.TriggerEffectPopUp, m_TriggerEffect);
        }

        #endregion
    }
}
