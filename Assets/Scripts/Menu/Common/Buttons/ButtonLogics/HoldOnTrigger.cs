using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections;
using System;

namespace Menu.Common.Buttons
{
    public class HoldOnTrigger : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        #region Members

        /// <summary> event called when the "hold" is trigerred </summary>
        public Action<bool> HoldTriggeredEvent;

        public float HoldDuration = 1f;
        public Image ProgressCircle;

        private bool m_IsHolding = false;
        private float m_HoldTimer = 0f;
        private Coroutine m_HoldCoroutine;

        #endregion


        #region Init & End

        private void Awake()
        {
            UpdateProgressUI(0f);
        }

        #endregion


        #region GUI Manipulators

        void SetIsHolding(bool isHolding)
        {
            if (isHolding == m_IsHolding)
                return;

            m_HoldTimer = 0f;
            UpdateProgressUI(0f);
            m_IsHolding = isHolding;

            if (m_HoldCoroutine != null)
            {
                StopCoroutine(m_HoldCoroutine);
                m_HoldCoroutine = null;
            }
        }

        private void UpdateProgressUI(float progress)
        {
            if (ProgressCircle != null)
            {
                ProgressCircle.fillAmount = Mathf.Clamp01(progress);
            }
        }

        private void CancelHold()
        {
            if (m_HoldCoroutine != null)
            {
                StopCoroutine(m_HoldCoroutine);
                m_HoldCoroutine = null;
                HoldTriggeredEvent?.Invoke(false);
            }

            SetIsHolding(false);
        }

        #endregion


        #region Pointer Events

        public void OnPointerDown(PointerEventData eventData)
        {
            if (m_HoldCoroutine == null)
                m_HoldCoroutine = StartCoroutine(HoldRoutine());
        }

        public void OnPointerUp(PointerEventData eventData)
        {
            if (m_HoldCoroutine != null)
                CancelHold();
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            if (m_HoldCoroutine != null)
                CancelHold();
        }

        private IEnumerator HoldRoutine()
        {
            // start holding
            SetIsHolding(true);

            while (m_HoldTimer < HoldDuration)
            {
                m_HoldTimer += Time.deltaTime;
                UpdateProgressUI(m_HoldTimer / HoldDuration);
                yield return null;
            }

            // send success on holding until the end
            HoldTriggeredEvent?.Invoke(true);

            // end holding
            SetIsHolding(false);
        }

        #endregion
    }

}
