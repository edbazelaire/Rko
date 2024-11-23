using Assets;
using Data;
using Data.DataStructures;
using Enums;
using Tools;


namespace Menu.Common.Buttons.TemplateItemButtons
{
    public class TemplateRunePowerUI : TemplateItemButton
    {
        #region Members

        // Init Data
        SRunePower      m_RunePower;

        #endregion


        #region Init & End

        public virtual void Initialize(SRunePower runePower)
        {
            if (runePower == null)
            {
                ErrorHandler.Error("Trying to initialize TemplateRunePowerUI with no runePowerData");
                Destroy(gameObject);
                return;
            }

            m_RunePower = runePower;

            base.Initialize();

            SetUpUI();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            m_Icon.sprite = AssetLoader.LoadIcon(m_RunePower.RuneName);

            SetBottomOverlay("Level " + m_RunePower.Level);
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

            Main.SetPopUp(EPopUpState.RunePowerPopUp, m_RunePower);
        }

        #endregion
    }
}
