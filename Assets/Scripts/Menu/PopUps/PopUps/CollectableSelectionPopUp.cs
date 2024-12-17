using Enums;
using Game.Loaders;
using Menu.Common.Buttons;
using Menu.PopUps;
using Save;
using System;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;


public class CollectableSelectionPopUp : PopUp
{
    #region Members

    Type m_CollectableType;
    bool m_UnlockedOnly;
    Action<Enum> m_OnCollectableClicked;

    GameObject m_ScrollerContent;

    #endregion


    #region Init & End

    protected override void FindComponents()
    {
        base.FindComponents();

        m_ScrollerContent = Finder.Find(gameObject, "ScrollerContent");
    }

    public void Initialize(Type collectableType, Action<Enum> onCollectableClicked, bool unlockedOnly = true)
    {
        m_UnlockedOnly = unlockedOnly;
        m_CollectableType = collectableType;
        m_OnCollectableClicked = onCollectableClicked;

        base.Initialize();
    }

    protected override void OnPrefabLoaded()
    {
        base.OnPrefabLoaded();
    
        UIHelper.CleanContent(m_ScrollerContent);
        foreach(Enum value in Enum.GetValues(m_CollectableType))
        {
            if (value.ToString() == "None")
                continue;

            if (m_CollectableType == typeof(ESpell) && (SpellLoader.IsLinked(value.ToString()) || SpellLoader.IsBossSpell((ESpell)value)))
                continue;

            if (InventoryCloudData.Instance.GetCollectable(value).Level <= 0 && m_UnlockedOnly)
                continue;

            var templateItem = Instantiate(AssetLoader.LoadTemplateItem(value), m_ScrollerContent.transform).GetComponent<TemplateCollectableItemUI>();
            templateItem.Initialize(value, asIconOnly: true);
            templateItem.SetBottomOverlay(value.ToString());

            // set button
            templateItem.Button.interactable = true;
            templateItem.Button.onClick.RemoveAllListeners();
            templateItem.Button.onClick.AddListener(() => {
                m_OnCollectableClicked(value);
                Exit();
            });
        }
    }

    #endregion


    #region GUI Manipulators

    #endregion


    #region Listeners

    protected override void RegisterListeners()
    {
        base.RegisterListeners();
    }

    protected override void UnRegisterListeners()
    {
        base.UnRegisterListeners();
    }

    #endregion
}
