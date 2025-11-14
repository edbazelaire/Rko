using Enums;
using Game.Loaders;
using TMPro;
using Tools;
using UnityEngine;

namespace Menu.Common.Infos
{
    public class SpellInfoRowUI : InfoRowUI
    {
        #region Members

        TMP_Text    m_BonusValue;
        EScalingDirection m_ScalingDirection = EScalingDirection.None;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
            m_BonusValue        = Finder.FindComponent<TMP_Text>(gameObject, "BonusValue");

            // by default, deactivate bonus value
            m_BonusValue.gameObject.SetActive(false);
        }

        public virtual void Initialize(string name, object value, object newValue = null, EScalingDirection scalingDirection = EScalingDirection.None, string title = "")
        {
            base.Initialize(name, value, title);

            // change color depending on scaling direction
            SetupScalingDirection(scalingDirection);

            // refresh value
            RefreshValue(value, newValue);
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        /// <summary>
        /// Handle the display of special cases
        /// </summary>
        /// <returns></returns>
        protected override bool CheckSpecialCases()
        {
            if (base.CheckSpecialCases())
                return true;

            switch (m_Name)
            {
                case "Type":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon(m_Value.ToString());
                    m_NameText.text = TextLocalizer.LocalizeText("Type");
                    m_ValueText.text = TextLocalizer.SplitCamelCase(TextLocalizer.LocalizeText(m_Value.ToString()));
                    return true;

                case "Target":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon("Target");
                    m_NameText.text = TextLocalizer.LocalizeText("Target");
                    m_ValueText.text = TextLocalizer.LocalizeText(m_Value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return true;

                case "CounterActivation":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon("Counter");
                    m_NameText.text = TextLocalizer.LocalizeText("Counter");
                    m_ValueText.text = TextLocalizer.LocalizeText(m_Value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return true;

                case "Movement":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon("Movement");
                    m_NameText.text = TextLocalizer.LocalizeText("Movement");
                    m_ValueText.text = TextLocalizer.LocalizeText(m_Value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return true;

                case "IsTrueDamage":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon("TrueDamage");
                    m_NameText.text = TextLocalizer.LocalizeText("Ignore Resistances");
                    m_ValueText.text = "";
                    m_ValueContainer?.SetActive(false);
                    return true;

                case "IgnoreCC":
                    m_Icon.sprite = AssetLoader.LoadUIElementIcon("IgnoreCC");
                    m_NameText.text = TextLocalizer.LocalizeText("Removes Control Effects");
                    m_ValueText.text = "";
                    m_ValueContainer?.SetActive(false);
                    return true;
            }

            return false;
        }

        protected override void SetUpIcon()
        {
            base.SetUpIcon();

            if (SpellLoader.IsStateEffect(m_IconName))
            {
                m_Icon.sprite = AssetLoader.LoadStateEffectIcon(m_IconName);
                if (SpellLoader.IsStateEffect(m_NameText.text))
                    m_NameText.text += " Cost";
            }
            else
            {
                m_Icon.sprite = AssetLoader.LoadUIElementIcon(m_IconName);
            }
        }

        public override void SetUpName(string name)
        {
            base.SetUpName(name);
        }

        void SetupScalingDirection(EScalingDirection scalingDirection)
        {
            m_ScalingDirection = scalingDirection;

            // Update name color based on scaling
            switch (m_ScalingDirection)
            {
                case EScalingDirection.None:
                    break;

                case EScalingDirection.Up:
                    m_NameText.color = Color.green;
                    break;

                case EScalingDirection.Down:
                    m_NameText.color = Color.red;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled case : " + scalingDirection);
                    break;
            }
        }

        public override void RefreshValue(object value, object newValue = null)
        {
            // not a float : skip
            if (!float.TryParse(value.ToString(), out float floatValue))
                return;

            // try and parse newValue into a float if provided
            float? floatNewValue = null;
            if (newValue != null)
            {
                if (!float.TryParse(newValue.ToString(), out float temp))
                    ErrorHandler.Error("Unable to parse new value (" + newValue.ToString() + ") of property " + name + " into a float");
                else
                    floatNewValue = temp;
            }

            RefreshValue(floatValue, floatNewValue);
        }

        public void RefreshValue(float value, float? newValue = null)
        {
            m_ValueText.text = TextHandler.FormatPropertyValue(value, m_Name);

            if (newValue == null || newValue.Value - value == 0)
            {
                m_BonusValue.gameObject.SetActive(false);
                return;
            }

            float bonus = newValue.Value - value;
            Color color = bonus > 0 ? Color.green : Color.red;
            m_BonusValue.gameObject.SetActive(true);
            m_BonusValue.text = (bonus > 0 ? "+" : "") + TextHandler.FormatPropertyValue(bonus, m_Name);
            m_BonusValue.color = color;
        }

        #endregion


        #region Format

        

        #endregion
    }
}