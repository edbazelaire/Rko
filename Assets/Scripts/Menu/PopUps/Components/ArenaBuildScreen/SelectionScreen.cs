using Data;
using Data.GameManagement;
using Enums;
using Menu.Common.Buttons;
using Save;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Tools.Animations;
using Unity.VisualScripting;
using UnityEngine;

namespace Menu.PopUps.Components
{
    public class SelectionScreen : MObject
    {
        #region Members

        const int N_CHOICES = 3;

        public Action       OnEndEvent;

        GameObject          m_SelectionContainer;
        List<TemplateCollectableItemUI> m_TemplateItems = new();

        ECollectableType    m_CollectableType;
        int                 m_BuildIndex;
        List<Enum>          m_NotAllowedValues;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_SelectionContainer = Finder.Find(gameObject, "SelectionContainer");
        }

        public override void Initialize()
        {
            base.Initialize();

            Deactivate();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
        }

        #endregion


        #region GUI Manipulators

        void RefreshSelection()
        {
            // clean potential previous content
            UIHelper.CleanContent(m_SelectionContainer);
            m_TemplateItems.Clear();

            var baseTemplate = AssetLoader.LoadTemplateCollectableItem(m_CollectableType);     // load template of PowerUpItem
            for (int i = 0; i < N_CHOICES; i++)
            {
                // choose a random powerup filling criteria
                CollectableData data = CollectablesManagementData.GetRandomData(m_CollectableType, m_NotAllowedValues, ProfileCloudData.AccountLevel);
                if (data == null)
                {
                    ErrorHandler.Error("Unable to find data");
                    continue;
                }

                // add to list of already selected power ups
                m_NotAllowedValues.Add(data.Id);

                // instantiate the PowerUpItem with the provided data
                var template = Instantiate(baseTemplate, m_SelectionContainer.transform);
                template.Initialize(data.Id, level: ProfileCloudData.AccountLevel, asIconOnly: true);
                template.Button.interactable = true;

                int templateIndex = i;
                template.Button.onClick.AddListener(OnTemplateSelected(templateIndex));

                // add to list of current items
                m_TemplateItems.Add(template);
            }
        }


        #endregion


        #region Hide & Activate

        public void Activate(ECollectableType collectableType, int buildIndex, List<Enum> unAllowedValues)
        {
            m_CollectableType   = collectableType;
            m_BuildIndex        = buildIndex;
            m_NotAllowedValues   = unAllowedValues;

            RefreshSelection();

            gameObject.SetActive(true);
        }

        public void Deactivate()
        {
            gameObject.SetActive(false);
        }

        #endregion


        #region Animation

        IEnumerator SelectedAnimation(int index)
        {
            for (int i = 0; i < m_TemplateItems.Count; i++)
            {
                // deactivate buttons during animation
                m_TemplateItems[i].Button.interactable = false;

                // if not selected : Hide() animation
                if (i != index)
                {
                    StartCoroutine(Hide(m_TemplateItems[i]));
                    continue;
                }

                // if selected : Select() animation
                StartCoroutine(Select(m_TemplateItems[i]));
            }

            yield return new WaitForSeconds(1.5f);

            OnEndEvent?.Invoke();
            gameObject.SetActive(false);
        }

        IEnumerator Hide(TemplateCollectableItemUI item)
        {
            Fade fade = item.AddComponent<Fade>();
            fade.Initialize(duration: 1f, endOpacity: 0f);

            var move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 1f, startPos: item.transform.position, endPos: item.transform.position + new Vector3(0f, -0.5f, 0f));

            yield return new WaitUntil(() => fade.IsOver);
        }

        IEnumerator Select(TemplateCollectableItemUI item)
        {
            Fade fade = item.AddComponent<Fade>();
            fade.Initialize(duration: 1.5f, endOpacity: 0f);

            var rotate = item.AddComponent<RotateAnimation>();
            rotate.Initialize(duration: 1.5f, rotation: new Vector3(0f, 1040f, 0f));

            var move = item.AddComponent<MoveAnimation>();
            move.Initialize(duration: 1.5f, startPos: item.transform.position, endPos: item.transform.position + new Vector3(0f, 1f, 0f));

            yield return new WaitUntil(() => fade.IsOver);
        }

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

        UnityEngine.Events.UnityAction OnTemplateSelected(int index)
        {
            return () =>
            {
                ProgressionCloudData.SetCurrentArenaBuildValue(m_TemplateItems[index].Collectable, ProfileCloudData.AccountLevel, m_BuildIndex, true);
                StartCoroutine(SelectedAnimation(index));
            };

        }

        #endregion
    }
}

