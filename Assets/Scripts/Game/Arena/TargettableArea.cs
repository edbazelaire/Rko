using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Game.Arena
{
    [RequireComponent(typeof(BoxCollider2D))] // Required for World Space Canvas
    public class TargettableArea : MObject, IPointerDownHandler, IDragHandler
    {
        #region Members

        [Tooltip("Send X position of click normalized between -1 (left) and 1 (right)")]
        public Action<float> ClickedEvent;

        RectTransform m_RectTransform;

        public RectTransform RectTransform => m_RectTransform;
        public float Size => m_RectTransform.rect.width;

        #endregion

        #region Init

        protected override void FindComponents()
        {
            base.FindComponents();
            m_RectTransform = GetComponent<RectTransform>();
        }

        protected override void SetUpUI()
        {
            EnsureColliderMatchesRect();
        }

        private void EnsureColliderMatchesRect()
        {
            var collider = GetComponent<BoxCollider2D>();
            if (collider != null)
            {
                collider.size = m_RectTransform.rect.size;
                collider.offset = Vector2.zero;
            }
        }

        #endregion

        #region Input Handling

        public void OnPointerDown(PointerEventData eventData)
        {
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
                float normalizedX = transform.position.x - localPoint.x;
                ClickedEvent?.Invoke(normalizedX);
            }
        }

        #endregion
    }
}
