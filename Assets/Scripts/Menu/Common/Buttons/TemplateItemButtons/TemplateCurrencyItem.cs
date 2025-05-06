using Enums;
using Menu.Common;
using Menu.Common.Buttons;
using Tools;

namespace Assets.Scripts.Menu.Common.Buttons.TemplateItemButtons
{
    public class TemplateCurrencyItem : TemplateItemButton
    {
        #region Members

        ECurrency m_Currency;
        int m_Qty;

        #endregion


        #region Init & End

        public void Initialize(ECurrency currency, int qty)
        {
            base.Initialize();

            m_Currency  = currency == ECurrency.TotalXp ? ECurrency.Xp : currency;
            m_Qty       = qty;

            // set icon and level
            SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        protected override void SetUpUI() 
        {
            m_Icon.sprite = AssetLoader.LoadCurrencyIcon(m_Currency, m_Qty);
            m_BottomText.text = "+ " + m_Qty;
        }

        #endregion
    }
}