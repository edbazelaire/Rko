using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Menu.Common.Buttons
{
    public class HoldOnButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public Action<bool> PressedEvent;
        bool m_IsPressed = false;

        public void OnPointerDown(PointerEventData eventData)
        {
            m_IsPressed = true;
            PressedEvent?.Invoke(true);
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            m_IsPressed = false;
            PressedEvent?.Invoke(false);
        }

        /// <summary>
        /// Called when the finger is dragged across the screen
        /// </summary>
        /// <param name="eventData"></param>
        public void OnDrag(PointerEventData eventData)
        {
            // Check if the touch is no longer within the button's bounds
            if (!RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
            {
                if (m_IsPressed)
                {
                    m_IsPressed = false;
                    PressedEvent?.Invoke(false);
                }
            }
            else
            {
                if (!m_IsPressed)
                {
                    m_IsPressed = true;
                    PressedEvent?.Invoke(true);
                }
            }
        }

        // Helper method to check if the touch is over this button
        public bool IsTouchOverButton(PointerEventData eventData)
        {
            return RectTransformUtility.RectangleContainsScreenPoint(GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera);
        }
    }
}