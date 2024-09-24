using Assets.Scripts.Menu.Common.Buttons.SubButtons;
using Data.GameManagement;
using Enums;
using Save;
using System;
using Tools;
using UnityEngine;

namespace Menu.Common.Buttons
{
    public class TemplateCharacterItemUI : TemplateCollectableItemUI
    {
        #region Members

        ECharacter m_Character => m_CollectableCloudData.GetCollectable<ECharacter>();
        
        PriceDisplay m_PriceDisplay;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            if (m_LockState == null)
                return;

            m_PriceDisplay = Finder.FindComponent<PriceDisplay>(m_LockState, "PriceDisplay");
        }

        public override void Initialize(Enum collectable, bool asIconOncly = false)
        {
            base.Initialize(collectable, asIconOncly);

            m_PriceDisplay.Initialize(ShopManagementData.GetPrice(m_Character));
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  += OnSelectedCharacterChanged;
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
            CharacterBuildsCloudData.SelectedCharacterChangedEvent  -= OnSelectedCharacterChanged;
        }

        protected void OnSelectedCharacterChanged()
        {
            SetSelected(m_Character == CharacterBuildsCloudData.SelectedCharacter);
        }

        #endregion
    }
}