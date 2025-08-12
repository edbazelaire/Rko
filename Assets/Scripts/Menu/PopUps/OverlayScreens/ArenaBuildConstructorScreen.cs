using Assets.Scripts.Managers;
using Enums;
using Managers;
using Menu.PopUps.Components;
using Save;
using System.Collections.Generic;
using Tools;
using UnityEngine.UI;


namespace Menu.PopUps.OverlayScreens
{
    public class ArenaBuildConstructorScreen : OverlayScreen
    {
        #region Members

        RandomCollectableSelector  m_CharacterSelector;
        RandomCollectableSelector  m_SpellsSelector;
        RandomCollectableSelector  m_RunesSelector;

        Button m_VButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CharacterSelector = Finder.FindComponent<RandomCollectableSelector>(gameObject, "CharacterSelector");
            m_SpellsSelector = Finder.FindComponent<RandomCollectableSelector>(gameObject, "SpellsSelector");
            m_RunesSelector = Finder.FindComponent<RandomCollectableSelector>(gameObject, "RunesSelector");

            m_VButton = Finder.FindComponent<Button>(gameObject, "VButton");
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();

            m_CharacterSelector.Initialize(ECollectableType.Character, nSelection: 1, nPropositions: 3);
            m_SpellsSelector.Initialize(ECollectableType.Spell, nSelection: 4, nPropositions: 10);
            m_RunesSelector.Initialize(ECollectableType.Rune, nSelection: 3, nPropositions: 6);
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_VButton.onClick.AddListener(OnClickValidate);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_VButton.onClick.RemoveAllListeners();
        }

        private void OnClickValidate()
        {
            // CHARACTER
            ECharacter character = ECharacter.None;
            if (m_CharacterSelector.TryGetSelection(out List<ECharacter> characters))
                character = characters[0];
            else
            {
                ScreenManager.QuickMessage("Please select a character");
                return;
            }

            // SPELLS
            if (!m_SpellsSelector.TryGetSelection(out List<ESpell> spells))
            {
                ScreenManager.QuickMessage("Please select 4 spells");
                return;
            }

            // RUNES
            if (! m_RunesSelector.TryGetSelection(out List<ERune> runes))
            {
                ScreenManager.QuickMessage("Please select 3 runes");
                return;
            }

            SBuildData buildData = new SBuildData(
                characterLevel: ProfileCloudData.AccountLevel,
                character: character.ToString(),
                runes: runes.ToArray(),
                runeLevels: new int[] { ProfileCloudData.AccountLevel, ProfileCloudData.AccountLevel, ProfileCloudData.AccountLevel },
                spells: spells.ToArray(),
                spellLevels: new int[] { ProfileCloudData.AccountLevel, ProfileCloudData.AccountLevel, ProfileCloudData.AccountLevel, ProfileCloudData.AccountLevel }
            );

            ProgressionCloudData.SetCurrentArenaBuild(buildData);
            Exit();
        }

        #endregion
    }
}

