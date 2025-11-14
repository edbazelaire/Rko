using Assets.Scripts.Managers.Sound;
using Data.GameManagement;
using Enums;
using Game.Loaders;
using Menu.Common.Displayers;
using System;
using System.Collections;
using TMPro;
using Tools;
using Tools.Animations;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class PropertyInfoPopUp : QuickMessagePopUp
    {
        #region Members

        // ==========================================================================================
        // GameObjects & Components
        protected TMP_Text          m_IconTitle;
        protected Image             m_Icon;

        string m_Key;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_IconTitle     = Finder.FindComponent<TMP_Text>(gameObject, "IconTitle");
            m_Icon          = Finder.FindComponent<Image>(gameObject, "Icon");
        }

        public virtual void Initialize(string key, string value, float duration = -1f)
        {
            m_Key = key;
            base.Initialize(PropertyHandler.GetDescription(key, value), duration);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            (string iconName, string prettyName) = PropertyHandler.GetIconAndPrettyName(m_Key);
            m_IconTitle.text = prettyName;

            if (SpellLoader.IsStateEffect(iconName))
                m_Icon.sprite = AssetLoader.LoadStateEffectIcon(iconName);
            else if (SpellLoader.IsSpell(iconName))
                m_Icon.sprite = AssetLoader.LoadSpellIcon(iconName);
            else
                m_Icon.sprite = AssetLoader.LoadUIElementIcon(iconName);
        }

        #endregion
    }
}