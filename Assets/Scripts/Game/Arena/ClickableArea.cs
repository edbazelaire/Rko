using UnityEngine;
using UnityEngine.EventSystems;
using System;
using Tools;
using Enums;

namespace Game.Arena
{
    [RequireComponent(typeof(BoxCollider2D))] // Required for World Space Canvas
    public class ClickableArea : MObject, IPointerDownHandler, IDragHandler
    {
        #region Members

        [Tooltip("Send X position of click normalized between -1 (left) and 1 (right)")]
        public Action<float> ClickedEvent;

        RectTransform m_RectTransform;
        public RectTransform RectTransform => m_RectTransform;

        #endregion


        #region Init

        private void Awake()
        {
            Initialize();
        }

        protected override void FindComponents()
        {
            base.FindComponents();
            m_RectTransform = GetComponent<RectTransform>();
        }

        #endregion


        #region Input Handling

        public void OnPointerDown(PointerEventData eventData)
        {
            ErrorHandler.Log("OnPointerDown() : " + eventData.position, ELogTag.SpellRelocation);
            SendClickPosition(eventData);
        }

        public void OnDrag(PointerEventData eventData)
        {
            SendClickPosition(eventData);
        }

        private void SendClickPosition(PointerEventData eventData)
        {
            Vector2 localPoint;
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(m_RectTransform, eventData.position, eventData.pressEventCamera, out localPoint))
            {
                float normalizedX = localPoint.x - transform.position.x;
                ErrorHandler.Log("SendClickPosition() : " + normalizedX, ELogTag.SpellRelocation);
                ClickedEvent?.Invoke(normalizedX);
            }
        }

        #endregion
    }
}
