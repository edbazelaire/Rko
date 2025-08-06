using Menu.Common.Buttons;
using Menu.MainMenu;
using System;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Menu.MainMenu.InventoryTab
{
    public class SelectedSpellWindow : MonoBehaviour
    {
        #region Members

        GameObject m_SelectedCardContainer;
        bool m_IsInitialized;

        #endregion


        #region Init & End

        public void Initialize()
        {
            m_SelectedCardContainer = Finder.Find(gameObject, "SelectedCardContainer");
            m_IsInitialized = true;
            gameObject.SetActive(false);
        }

        #endregion


        #region Activation & Deactivation

        public void Activate(Enum collectable)
        {
            // activate game object and clean potentiel previous content
            gameObject.SetActive(true);
            UIHelper.CleanContent(m_SelectedCardContainer);

            // create spell item
            var spellItem = Instantiate(AssetLoader.LoadTemplateItem(collectable), m_SelectedCardContainer.transform).GetComponent<TemplateCollectableItemUI>();
            spellItem.Initialize(collectable, asIconOnly: true);

            // set game object anchors to match parent size
            UIHelper.SetFullSize(spellItem.gameObject);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        #endregion


        #region Updates

        protected virtual void Update()
        {
            if (!m_IsInitialized)
                return;

            CheckTouch();
        }

        void CheckTouch()
        {
            if (Input.GetMouseButtonUp(0)) // Check for left mouse button click
            {
                // delay method by one frame (in case of clicking a button that needs the value
                CoroutineManager.DelayMethod(() => { CurrentBuildDisplayUI.SetCurrentSelectedItem(null); } );
            }
        }

        #endregion


    }
}