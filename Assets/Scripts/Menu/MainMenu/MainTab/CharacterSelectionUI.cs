using Data.GameManagement;
using Data;
using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using System.Linq;


public class CharacterSelectionUI : MObject
{
    #region Members

    const string c_CharacterSelectionContainer = "CharacterSelectionContainer";

    GameObject m_TemplateCharacterButton;
    GameObject m_ButtonsContainer;

    Dictionary<ECharacter, TemplateCollectableItemUI> m_CharacterButtons;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        // check if a specific container for the buttons was provided, otherwise use this parent as container
        m_ButtonsContainer = Finder.Find(gameObject, c_CharacterSelectionContainer, throwError: false);
        if (m_ButtonsContainer == null)
            m_ButtonsContainer = gameObject;

        m_TemplateCharacterButton = AssetLoader.LoadTemplateItem("CharacterItem");
    }

    protected override void SetUpUI()
    {
        CreateCharacterButtons();
    }

    #endregion


    #region GUI Manipulators

    void CreateCharacterButtons()
    {
        // remove all children from the m_CharacterSelectionContainer
        UIHelper.CleanContent(m_ButtonsContainer);

        // reset dict
        m_CharacterButtons = new Dictionary<ECharacter, TemplateCollectableItemUI>();

        // create buttons for each characters
        List<CharacterData> charactersData = CollectablesManagementData.OrderCollectable(CharacterLoader.Instance.Characters.Values.ToList(), EOrderBy.Rarety);
        foreach (CharacterData characterData in charactersData)
        {
            var characterButton = Instantiate(m_TemplateCharacterButton, m_ButtonsContainer.transform).GetComponent<TemplateCollectableItemUI>();
            characterButton.Initialize(characterData.Character);
            characterButton.SetUpCollectionFillBar(false);
            m_CharacterButtons.Add(characterData.Character, characterButton);
        }
    }

    #endregion



}
