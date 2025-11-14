using UnityEngine;
using UnityEngine.EventSystems;
using System;

namespace Game.Arena
{
    [RequireComponent(typeof(BoxCollider2D))] // Required for World Space Canvas
    public class TargettableArea : MObject
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

        #endregion
    }
}
