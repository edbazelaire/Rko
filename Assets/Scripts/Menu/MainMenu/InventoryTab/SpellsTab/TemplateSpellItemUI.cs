using Assets;
using Assets.Scripts.Managers;
using Enums;
using Game.Loaders;
using Inventory;
using Menu.Common.Buttons;
using Save;
using System;
using System.Linq;
using Tools;
using UnityEngine;

namespace Menu.MainMenu
{
    public class TemplateSpellItemUI : TemplateSpellButton
    {
        #region Members
        
        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            // -- hide sub buttons by default
            if (m_CSubButtons != null)
                m_CSubButtons.gameObject.SetActive(false);
        }

        #endregion


        #region GUI Manipulators

        public override void SetAsIconOnly(bool activate = true)
        {
            base.SetAsIconOnly(activate);
        }

        public override void SetUpCollectionFillBar(bool activate = true)
        {
            base.SetUpCollectionFillBar(activate && ! SpellLoader.GetSpellData(Spell, destroy: true).Linked);    
        }

        #endregion


        #region State Management

        /// <summary>
        /// Check context to define which state the button is in - set state accordingly
        /// </summary>
        protected override void UpdateState()
        {
            // linked spell are always in "Normal" state
            if (SpellLoader.GetSpellData(Spell, destroy: true).Linked)
            {
                SetState(EButtonState.Normal);
                return;
            }

            base.UpdateState();
        }

        /// <summary>
        /// Set UI according to the provided state
        /// </summary>
        /// <param name="state"></param>
        public override void SetState(EButtonState state)
        {
            base.SetState(state);

            switch (state)
            {
                case (EButtonState.Locked):
                    SetBottomOverlay("");
                    break;

                case (EButtonState.Updatable):
                case (EButtonState.Normal):
                    SetBottomOverlay(string.Format(LEVEL_FORMAT, m_Level));
                    break;

                default:
                    ErrorHandler.Error("UnHandled state " + state);
                    break;
            }
        }

        #endregion


        #region Listeners

        /// <summary>
        /// Action happening when the button is clicked on - depending on button context
        /// </summary>
        protected override void OnClick()
        {
            if (m_AsIconOnly)
                return;

            if (m_IsLinked)
            {
                Main.SetPopUp(EPopUpState.SpellInfoPopUp, (ESpell)Collectable, m_Level);
                return;
            }

            // behavior when the USE button was clicked and this is one of the current build spells
            if (CurrentBuildDisplayUI.CurrentSelectedItem != null && CharacterBuildsCloudData.CurrentSpells.Contains(Spell))
            {
                CurrentBuildDisplayUI.ReplaceSpell(Spell);
                return;
            }

            base.OnClick();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="character"></param>
        protected override void OnCollectableUpgraded(Enum collectable, int level)
        {
            base.OnCollectableUpgraded(collectable, level);

            // for link spell, when a character is upgraded, the spell is too
            if (!m_IsLinked || collectable.GetType() != typeof(ECharacter))
                return;

            // this applies only for linked spell of the current selected character
            if (! CharacterBuildsCloudData.SelectedCharacter.Equals((ECharacter)collectable))
                return;

            // refresh spell cloud data
            m_CollectableCloudData = InventoryManager.GetSpellData(Spell);
            RefreshUI();
        }

        #endregion
    }
}