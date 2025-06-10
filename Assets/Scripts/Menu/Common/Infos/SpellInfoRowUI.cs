using Enums;
using Game.Loaders;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Infos
{
    public class SpellInfoRowUI : InfoRowUI
    {
        #region Members

        Image       m_Icon;
        TMP_Text    m_Name;
        TMP_Text    m_Value;
        TMP_Text    m_BonusValue;

        string m_PropertyName;

        #endregion


        #region Init & End

        void FindComponents()
        {
            var iconContainer   = Finder.Find(gameObject, "IconContainer");
            m_Icon              = Finder.FindComponent<Image>(iconContainer, "Icon");

            //var infosContainer  = Finder.Find(gameObject, "InfosContainer");
            m_Name              = Finder.FindComponent<TMP_Text>(gameObject, "Name");
            m_Value             = Finder.FindComponent<TMP_Text>(gameObject, "Value");
            m_BonusValue        = Finder.FindComponent<TMP_Text>(gameObject, "BonusValue");
        }

        public void Initialize(string name, object value, object newValue = null, EScalingDirection scalingDirection = EScalingDirection.None)
        {
            // find components before instantiation
            FindComponents();

            // by default, deactivate bonus value
            m_BonusValue.gameObject.SetActive(false);

            // setup name of the property
            if (!TextHandler.IsSpecialPropertyName(name, out string propertyName, out string specialCondition))
                propertyName = name;
            m_PropertyName = propertyName;

            // setup name and color of the info row title
            SetUpName(name, scalingDirection);

            // handles special cases
            switch (propertyName)
            {
                case "Type":
                    m_Icon.sprite   = AssetLoader.LoadUIElementIcon(value.ToString());
                    m_Value.text    = TextLocalizer.SplitCamelCase(TextLocalizer.LocalizeText(value.ToString()));
                    return; 

                case "Target":
                    m_Icon.sprite   = AssetLoader.LoadUIElementIcon("Target");
                    m_Value.text    = TextLocalizer.LocalizeText(value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return;

                case "CounterActivation":
                    m_Icon.sprite   = AssetLoader.LoadUIElementIcon("Counter");
                    m_Value.text    = TextLocalizer.LocalizeText(value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return;

                case "Movement":
                    m_Icon.sprite   = AssetLoader.LoadUIElementIcon("Movement");
                    m_Value.text    = TextLocalizer.LocalizeText(value as string);
                    m_BonusValue.gameObject.SetActive(false);
                    return;
            }

            if (SpellLoader.IsStateEffect(propertyName))
            {
                m_Icon.sprite = AssetLoader.LoadStateEffectIcon(propertyName);
                m_Name.text += " Cost";
            }
            else
            {
                m_Icon.sprite = AssetLoader.LoadUIElementIcon(propertyName);
            }

            RefreshValue(value, newValue);
        }

        #endregion


        #region GUI Manipulators

        public void SetUpName(string name, EScalingDirection scalingDirection)
        {
            // set name of the row
            m_Name.text = TextLocalizer.SplitCamelCase(TextLocalizer.LocalizeText(name));

            // Update name color based on scaling
            switch (scalingDirection)
            {
                case EScalingDirection.None:
                    break;

                case EScalingDirection.Up:
                    m_Name.color = Color.green;
                    break;

                case EScalingDirection.Down:
                    m_Name.color = Color.red;
                    break;

                default:
                    ErrorHandler.Warning("Unhandled case : " + scalingDirection);
                    break;
            }
        }

        public void RefreshValue(object value, object newValue = null)
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
            m_Value.text = TextHandler.FormatPropertyValue(value, m_PropertyName);

            if (newValue == null || newValue.Value - value == 0)
            {
                m_BonusValue.gameObject.SetActive(false);
                return;
            }

            float bonus = newValue.Value - value;
            Color color = bonus > 0 ? Color.green : Color.red;
            m_BonusValue.gameObject.SetActive(true);
            m_BonusValue.text = (bonus > 0 ? "+" : "") + TextHandler.FormatPropertyValue(bonus, m_PropertyName);
            m_BonusValue.color = color;
        }

        #endregion


        #region Format

        

        #endregion
    }
}