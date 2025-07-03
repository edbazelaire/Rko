using UnityEngine;
using Tools;
using Enums;
using System.Collections.Generic;

namespace Menu.PopUps
{
    public class CurrentArenaModsDisplayer : MObject
    {
        #region Members

        [SerializeField] private ArenaModButtonUI m_Template;

        GameObject m_Container;

        EArenaType          m_ArenaType         => PlayerPrefsHandler.GetArenaType();
        EArenaDifficulty    m_ArenaDifficulty   => PlayerPrefsHandler.GetArenaDifficulty(m_ArenaType);
        List<EArenaMod>     m_ArenaMods         => PlayerPrefsHandler.GetArenaMods(m_ArenaType, m_ArenaDifficulty);

        #endregion


        #region Init

        protected override void FindComponents()
        {
            base.FindComponents();
            m_Container = gameObject;
        }

        public override void Initialize()
        {
            base.Initialize();

            RefreshUI();
        }

        #endregion


        #region GUI Manipulators

        public void RefreshUI()
        {
            UIHelper.CleanContent(m_Container);
            foreach (EArenaMod arenaMod in m_ArenaMods)
            {
                ArenaModButtonUI template = Instantiate(m_Template, m_Container.transform);
                template.Initialize(arenaMod);
            }
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent += OnArenaTypeChanged;
            PlayerPrefsHandler.ArenaModsChangedEvent += OnArenaModsChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            PlayerPrefsHandler.ArenaTypeChangedEvent -= OnArenaTypeChanged;
            PlayerPrefsHandler.ArenaModsChangedEvent -= OnArenaModsChanged;
        }

        protected void OnArenaTypeChanged(EArenaType arenaType)
        {
            RefreshUI();
        }

        protected void OnArenaModsChanged()
        {
            RefreshUI();
        }

        #endregion
    }
}
