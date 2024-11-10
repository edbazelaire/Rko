using Enums;
using Game.Loaders;
using Game.Spells;
using Save;
using Tools;
using UnityEngine;

namespace Menu.MainMenu
{

    public class SpellItemContainerUI : MonoBehaviour
    {
        #region Members

        bool m_Empty;
        int m_Index = -1;
        GameObject m_TemplateSpellItem;

        GameObject m_TemplateSpellItemContainer;
        GameObject m_EmptyBackground;

        #endregion


        #region Init & End

        public void Initialize(int index)
        {
            m_TemplateSpellItemContainer    = Finder.Find(gameObject, "TemplateSpellItemContainer");
            m_TemplateSpellItem             = AssetLoader.LoadTemplateItem("SpellItem");
            m_EmptyBackground               = Finder.Find(gameObject, "Background");

            m_Index = index;
            RefreshUI();

            // register listeners
            CharacterBuildsCloudData.SelectedCharacterChangedEvent += OnBuildChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent += OnBuildChanged;
        }

        public void OnDestroy()
        {
            // un-register listeners
            CharacterBuildsCloudData.SelectedCharacterChangedEvent -= OnBuildChanged;
            CharacterBuildsCloudData.CurrentBuildIndexChangedEvent -= OnBuildChanged;
        }

        #endregion


        #region GUI Manipulators

        void RefreshUI()
        {
            // clean current content
            UIHelper.CleanContent(m_TemplateSpellItemContainer);
            
            // no spell : empty content
            if (Spell == null)
            {
                m_EmptyBackground.SetActive(true);
                return;
            }

            m_EmptyBackground.SetActive(false);
            var spellItemUI = Instantiate(m_TemplateSpellItem, m_TemplateSpellItemContainer.transform).GetComponent<TemplateSpellItemUI>();
            spellItemUI.Initialize(Spell.Value);
        }

        #endregion


        #region Public Accessors

        /// <summary>
        /// When a spell is set, 
        /// </summary>
        /// <param name="spell"></param>
        public void SetSpell(ESpell spell)
        {
            CharacterBuildsCloudData.SetSpellInCurrentBuild(spell, m_Index);
            m_Empty = false;
            RefreshUI();
        }

        /// <summary>
        /// Display an "empty current spell" even if the value in cloud data is not changed (to avoid saving uncompleted builds)
        /// </summary>
        public void RemoveCurrentSpell()
        {
            m_Empty = true;
            CharacterBuildsCloudData.SetSpellInCurrentBuild(null, m_Index);
            RefreshUI();
        }

        #endregion


        #region Listeners
        
        /// <summary>
        /// When current build or selected character changes, refresh the UI 
        /// </summary>
        void OnBuildChanged()
        {
            // reset empty forcer
            m_Empty = false;
            // apply UI
            RefreshUI();
        }

        #endregion


        #region Dependent Properties

        public ESpell? Spell
        {
            get
            {
                // CHECK : index error 
                if (m_Index < 0 || m_Index >= CharacterBuildsCloudData.CurrentBuild.Length)
                {
                    ErrorHandler.Error("Bad index : " + m_Index);
                    return null;
                }

                // if "Empty" slot is active, return null
                if (m_Empty)
                    return null;

                var spellData = CharacterBuildsCloudData.CurrentBuild[m_Index];
                if (spellData == ESpell.None)
                {
                    m_Empty = true;
                    return null;
                }

                return spellData;
            }
        }

        #endregion
    }
}