using Enums;
using System;
using Tools;
using UnityEngine.UIElements;


namespace Menu.PopUps
{
    public enum ERuneInfosTabs
    {
        MinorRune,
        MajorRune,
        PrimalRune
    }

    public class RuneInfosTabManager : TabsManager
    {
        #region Members

        protected override Type m_TabEnumType { get; set; } = typeof(ERuneInfosTabs);
        /// <summary> default tab opened </summary>
        protected override Enum m_DefaultTab { get; set; } = ERuneInfosTabs.MinorRune;

        #endregion


        #region Init & End

        public void Initialize(ERuneActivation runeActivation)
        {
            base.Initialize();

            ERuneInfosTabs tab;
            switch (runeActivation)
            {
                case ERuneActivation.Minor:
                    tab = ERuneInfosTabs.MinorRune;
                    break;

                case ERuneActivation.Major:
                    tab = ERuneInfosTabs.MajorRune;
                    break;

                case ERuneActivation.Primal:
                    tab = ERuneInfosTabs.PrimalRune;
                    break;

                case ERuneActivation.None:
                default:
                    tab = (ERuneInfosTabs)m_DefaultTab;
                    break;
            }

            SelectTab(tab);
        }

        #endregion


        #region GUI Manipulators

        public void DeactivateTab(ERuneActivation runeActivation)
        {
            if (!m_TabButtons.ContainsKey(GetTabName(runeActivation)))
                return;
            
            m_TabButtons[GetTabName(runeActivation)].Button.interactable = false;
        }

        #endregion


        #region Helpers

        public ERuneInfosTabs GetTabName(ERuneActivation runeActivation)
        {
            switch (runeActivation)
            {
                case ERuneActivation.Minor:
                    return ERuneInfosTabs.MinorRune;
                case ERuneActivation.Major:
                    return ERuneInfosTabs.MajorRune;
                case ERuneActivation.Primal:
                    return ERuneInfosTabs.PrimalRune;
                default:
                    ErrorHandler.Warning("Unhandled case : " + runeActivation);
                    return ERuneInfosTabs.MinorRune;
            }
        }

        #endregion
    }
}
