using UnityEngine;
using UnityEngine.UI;
using Tools;
using System;
using Data.ArenaEffects.ArenaMods;
using Enums;

namespace Menu.PopUps
{
    public class ArenaModButtonUI : MObject
    {
        #region Members

        EArenaMod m_ArenaMod;

        private Button m_Button;
        private Image m_Icon;
        private Image m_Selected;

        public Button Button => m_Button;
        public EArenaMod ArenaMod => m_ArenaMod;

        #endregion


        #region Init

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Button = Finder.FindComponent<Button>(gameObject);
            m_Icon = Finder.FindComponent<Image>(gameObject, "Icon");
            m_Selected = Finder.FindComponent<Image>(gameObject, "Selected");
        }

        public void Initialize(EArenaMod arenaMod)
        {
            m_ArenaMod = arenaMod;

            base.Initialize();

            m_Icon.sprite = AssetLoader.LoadIcon(arenaMod);
        }

        #endregion


        #region GUI Manipulators

        public void SetSelected(bool selected)
        {
            m_Selected.gameObject.SetActive(selected);
        }

        #endregion
    }
}
