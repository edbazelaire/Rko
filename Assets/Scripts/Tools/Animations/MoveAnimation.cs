using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class MoveAnimation : OvAnimation
    {
        #region Members

        [SerializeField] Vector3 m_StartPos;
        [SerializeField] Vector3 m_EndPos;

        RectTransform m_RectTransform = null;

        protected Vector3 m_Position 
        { 
            get { return m_RectTransform != null ? m_RectTransform.anchoredPosition : transform.position; } 
            set { if (m_RectTransform != null) m_RectTransform.anchoredPosition = value; else transform.position = value; } 
        }

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = 1f, Vector3? startPos = null, Vector3? endPos = null, bool checkRectTransform = false)
        {
            if (duration <= 0f)
            {
                ErrorHandler.Error("MoveAnimation can not be set as Inifinite, duration must be > 0 : " + duration);
                return;
            }

            base.Initialize(id, duration);

            if (checkRectTransform)
            {
                m_RectTransform = Finder.FindComponent<RectTransform>(gameObject);
                if (m_RectTransform == null)
                {
                    ErrorHandler.Warning("Unable to find requested RectTransform for MoveAnimation of game object  " + name);
                    return;
                }
            }

            m_StartPos      = startPos  ?? m_Position;
            m_EndPos        = endPos    ?? m_Position;

            // Set initial values immediately on initialization
            m_Position = m_StartPos;
        }

        public override void Deactivate()
        {
            base.Deactivate();
            m_Position = m_EndPos;
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            float progress = GetProgress();

            // interpolate position
            m_Position = Vector3.Lerp(m_StartPos, m_EndPos, progress);

            m_Timer += Time.deltaTime;
            yield return null;
        }


        #endregion

    }
}