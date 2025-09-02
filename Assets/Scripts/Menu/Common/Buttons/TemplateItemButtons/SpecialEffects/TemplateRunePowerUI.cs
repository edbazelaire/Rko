using Assets;
using Data;
using Data.DataStructures;
using Data.DataStructures.PowerEffects;
using Enums;
using Tools;


namespace Menu.Common.Buttons.TemplateItemButtons
{
    public class TemplateRunePowerUI : TemplateItemButton
    {
        #region Members

        // Init Data
        SPowerEffect m_PowerEffect;

        #endregion


        #region Init & End

        public virtual void Initialize(SPowerEffect runePower)
        {
            if (runePower == null)
            {
                ErrorHandler.Error("Trying to initialize TemplateRunePowerUI with no runePowerData");
                Destroy(gameObject);
                return;
            }

            m_PowerEffect = runePower;

            base.Initialize();

            SetUpUI();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.LoadIcon(m_PowerEffect.BaseName);

            SetBottomOverlay("Level " + m_PowerEffect.Level);
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

            Main.SetPopUp(EPopUpState.RunePowerPopUp, m_PowerEffect);
        }

        #endregion
    }
}
