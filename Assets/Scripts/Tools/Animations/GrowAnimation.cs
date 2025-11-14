using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace Tools.Animations
{
    public class GrowAnimation : OvAnimation
    {
        #region Members

        [SerializeField] float? m_StartWidth;
        [SerializeField] float? m_StartHeight;
        [SerializeField] float? m_EndWidth;
        [SerializeField] float? m_EndHeight;

        [SerializeField] bool m_CheckLayout = false;

        RectTransform m_RectTransform = null;
        LayoutElement m_LayoutElement = null;

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = 1f, float? startWidth = null, float? startHeight = null,
            float? endWidth = null, float? endHeight = null, bool checkLayout = false)
        {
            if (duration <= 0f)
            {
                ErrorHandler.Error("GrowAnimation cannot be infinite: duration must be > 0 : " + duration);
                return;
            }

            base.Initialize(id, duration);

            m_CheckLayout = checkLayout;

            m_RectTransform = Finder.FindComponent<RectTransform>(gameObject);

            if (m_CheckLayout)
                m_LayoutElement = gameObject.GetComponent<LayoutElement>();

            // Determine start values
            if (m_CheckLayout && m_LayoutElement != null)
            {
                m_StartWidth = startWidth ?? m_LayoutElement.preferredWidth;
                m_StartHeight = startHeight ?? m_LayoutElement.preferredHeight;
            }
            else
            {
                m_StartWidth = startWidth ?? m_RectTransform.sizeDelta.x;
                m_StartHeight = startHeight ?? m_RectTransform.sizeDelta.y;
            }

            m_EndWidth = endWidth;
            m_EndHeight = endHeight;
        }

        public override void Deactivate()
        {
            base.Deactivate();

            if (!m_EndWidth.HasValue && !m_EndHeight.HasValue)
                return;

            if (m_CheckLayout && m_LayoutElement != null)
            {
                if (m_EndWidth.HasValue)
                    m_LayoutElement.preferredWidth = m_EndWidth.Value;

                if (m_EndHeight.HasValue)
                    m_LayoutElement.preferredHeight = m_EndHeight.Value;
            }
            else
            {
                Vector2 size = m_RectTransform.sizeDelta;

                if (m_EndWidth.HasValue)
                    size.x = m_EndWidth.Value;

                if (m_EndHeight.HasValue)
                    size.y = m_EndHeight.Value;

                m_RectTransform.sizeDelta = size;
            }
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            float progress = GetProgress();

            float newWidth = m_StartWidth.Value;
            float newHeight = m_StartHeight.Value;

            if (m_EndWidth.HasValue)
                newWidth = Mathf.Lerp(m_StartWidth.Value, m_EndWidth.Value, progress);

            if (m_EndHeight.HasValue)
                newHeight = Mathf.Lerp(m_StartHeight.Value, m_EndHeight.Value, progress);

            if (m_CheckLayout && m_LayoutElement != null)
            {
                if (m_EndWidth.HasValue)
                    m_LayoutElement.preferredWidth = newWidth;

                if (m_EndHeight.HasValue)
                    m_LayoutElement.preferredHeight = newHeight;
            }
            else
            {
                Vector2 size = m_RectTransform.sizeDelta;
                size.x = newWidth;
                size.y = newHeight;
                m_RectTransform.sizeDelta = size;
            }

            m_Timer += Time.deltaTime;
            yield return null;
        }

        #endregion
    }
}
