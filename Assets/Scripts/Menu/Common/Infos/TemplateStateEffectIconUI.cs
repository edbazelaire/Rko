using Assets;
using Data;
using Enums;
using Game.Loaders;
using System.Linq;
using TMPro;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.Common.Infos
{
    public class TemplateStateEffectIconUI : MonoBehaviour
    {
        #region Members

        Image m_Icon;
        TMP_Text m_DurationValue;
        Button m_Button;

        SStateEffectData m_StateEffectData;
        int m_Level;

        #endregion


        #region Init & End

        private void FindComponents()
        {
            m_Icon = Finder.FindComponent<Image>(gameObject, "StateEffectIcon");
            m_DurationValue = Finder.FindComponent<TMP_Text>(gameObject, "StateEffectDuration");
            m_Button = Finder.FindComponent<Button>(gameObject);

            m_Button.onClick.AddListener(OnClick);
        }

        public void Initialize(SStateEffectData stateEffectData, int level)
        {
            FindComponents();

            m_StateEffectData = stateEffectData;
            m_Level = level;

            m_Icon.sprite = AssetLoader.LoadStateEffectIcon(stateEffectData.StateEffect.ToString());

            float duration = stateEffectData.OverridingProperties.Any(value => value.StateEffectProperty.Equals(EStateEffectProperty.Duration)) ? stateEffectData.OverridingProperties.First(value => value.StateEffectProperty.Equals(EStateEffectProperty.Duration)).Value : SpellLoader.GetStateEffect(stateEffectData.StateEffect.ToString(), level).GetFloat(EStateEffectProperty.Duration);
            if (duration > 0)
            {
                m_DurationValue.text = duration.ToString("F2");
            } else
            {
                m_DurationValue.gameObject.SetActive(false);
            }
        }

        #endregion


        #region Listeners

        private void OnClick()
        {
            Main.StateEffectPopUp(m_StateEffectData, m_Level);
        }

        #endregion

    }
}