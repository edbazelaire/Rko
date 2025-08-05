using Data;
using Enums;
using Game.Loaders;
using Save;
using System;
using Tools;

namespace Menu.Common.Buttons
{
    public class TemplateSpellButton : TemplateCollectableItemUI
    {
        #region Members

        // ========================================================================================
        // Button Data

        protected bool                  m_IsLinked;
        protected bool                  m_IsAutoTarget;

        // ========================================================================================
        // Public Accessors
        public ESpell Spell => (ESpell)m_CollectableCloudData.GetCollectable();

        #endregion


        #region GUI Manipulators

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            if (level > 0)
                return;

            if (m_IsLinked)
            {
                var character = CharacterLoader.GetCharacterWithSpell(Spell);
                if (character == null) 
                {
                    ErrorHandler.Error("Unable to find character with spell " + Spell);
                    return;
                }

                var cloudData = InventoryCloudData.Instance.GetCollectable(character);
                m_Level = cloudData.Level;
            }
        }

        protected override void SetUpUI(bool asIconOnly = false)
        {
            SpellData spellData = SpellLoader.GetSpellData((ESpell)m_Collectable, destroy: true);
            m_IsLinked = spellData.Linked;
            m_IsAutoTarget = spellData.IsAutoTarget;

            base.SetUpUI(asIconOnly);

            if (m_IsLinked && m_CollectionFillBar != null)
                m_CollectionFillBar.gameObject.SetActive(false);
        }

        /// <summary>
        /// Setup icon of the spell and color of its border
        /// </summary>
        protected virtual void SetUpSpellIconUI()
        {
            m_Icon.sprite = AssetLoader.LoadIcon(Spell);
            SetBottomOverlay(string.Format(LEVEL_FORMAT, m_Level));
            SetColor(SpellLoader.GetRaretyData(Spell).Color);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            if (m_IsLinked)
                InventoryCloudData.CollectableDataChangedEvent += OnCharacterDataChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            if (m_IsLinked)
                InventoryCloudData.CollectableDataChangedEvent -= OnCharacterDataChanged;
        }


        void OnCharacterDataChanged(SCollectableCloudData cloudData)
        {
            if (!m_IsLinked)
                return;

            var character = CharacterLoader.GetCharacterWithSpell(Spell);
            if (character == null)
            {
                ErrorHandler.Error("Unable to find character with spell " + Spell);
                return;
            }

            if (character.ToString() != cloudData.CollectableName)
                return;

            m_Level = cloudData.Level;
            RefreshUI();
        }

        #endregion
    }
}