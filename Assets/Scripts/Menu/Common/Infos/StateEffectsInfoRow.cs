using Data;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Menu.Common.Infos
{
    public class StateEffectsInfoRow : InfoRowUI
    {
        #region Members

        GameObject      m_InfosContainer;
        GameObject      m_ValueContainer;

        List<TemplateStateEffectIconUI> m_StateEffectIcons;

        #endregion


        #region Init & End

        void Awake()
        {
            m_InfosContainer    = Finder.Find(gameObject, "InfosContainer");
            m_ValueContainer    = Finder.Find(m_InfosContainer, "ValueContainer");
        }


        public void Initialize(List<SStateEffectData> stateEffectDatas, int spellLevel)
        {
            m_StateEffectIcons = new();
            UIHelper.CleanContent(m_ValueContainer);
            int i = 0;
            foreach (var stateEffectData in stateEffectDatas)
            {
                // add new line of state effects
                if (i%5 == 0 && i > 0)
                {
                    var newValueContainer = Instantiate(m_ValueContainer, m_InfosContainer.transform);
                    UIHelper.CleanContent(newValueContainer);
                    m_ValueContainer = newValueContainer;
                }

                var icon = Instantiate(AssetLoader.LoadTemplateItem("StateEffectIcon"), m_ValueContainer.transform).GetComponent<TemplateStateEffectIconUI>();
                icon.Initialize(stateEffectData, spellLevel);

                m_StateEffectIcons.Add(icon);
                i++;
            }
        }

        #endregion
    }
}