using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


public class MovementButtonsContainer : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IDragHandler
{
    #region Members

    public Action<int> MovementInputEvent;

    [SerializeField] private Color  m_ActivationColor;
    [SerializeField] GameObject     m_LeftMovementButton;
    [SerializeField] GameObject     m_RightMovementButton;
    [SerializeField] Image          m_LeftMovementButtonImage;
    [SerializeField] Image          m_RightMovementButtonImage;

    public GameObject LeftMovementButton => m_LeftMovementButton;
    public GameObject RightMovementButton => m_RightMovementButton;

    bool m_IsTouching = false;

    #endregion


    #region Listeners
    public void OnPointerDown(PointerEventData eventData)
    {
        m_IsTouching = true;
        HandleTouch(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        m_IsTouching = false;

        MovementInputEvent?.Invoke(0);
        m_LeftMovementButtonImage.color = Color.white;
        m_RightMovementButtonImage.color = Color.white;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (m_IsTouching)
        {
            HandleTouch(eventData);
        }
    }

    private void HandleTouch(PointerEventData eventData)
    {
        // Check if the touch is over the left button
        if (RectTransformUtility.RectangleContainsScreenPoint(m_LeftMovementButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
        {
            MovementInputEvent?.Invoke(-1);
            m_LeftMovementButtonImage.color = m_ActivationColor;
            m_RightMovementButtonImage.color = Color.white;
        }

        // Check if the touch is over the right button
        else if (RectTransformUtility.RectangleContainsScreenPoint(m_RightMovementButton.GetComponent<RectTransform>(), eventData.position, eventData.pressEventCamera))
        {
            MovementInputEvent?.Invoke(1);
            m_LeftMovementButtonImage.color = Color.white;
            m_RightMovementButtonImage.color = m_ActivationColor;
        }

        else
        {
            MovementInputEvent?.Invoke(0);
            m_LeftMovementButtonImage.color = Color.white;
            m_RightMovementButtonImage.color = Color.white;
        }
    }

    #endregion

}