using System;
using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class OvAnimation : MonoBehaviour
    {
        #region Members

        // ========================================================================================================
        // Scriptable Properties
        [SerializeField] protected bool             m_IsActivated = true;
        [SerializeField] protected float            m_Duration = -1f;
        [SerializeField] protected AnimationCurve   m_AnimationCurve;

        // ========================================================================================================
        // Actions
        public Action OnAnimationEnded;

        // ========================================================================================================
        // Internal Data
        protected bool      m_IsOver        = false;
        protected bool      m_EndWhenOver   = true;
        protected string    m_Id            = "";
        protected float     m_Timer         = 0f;
        protected Coroutine m_Coroutine     = null;

        // ========================================================================================================
        // Dependent Properties
        /// <summary> Get name cleaned </summary>
        public string Name
        {
            get
            {
                return GetType().ToString();
            }
        }

        public string Id => m_Id;

        /// <summary> is animation infinite </summary>
        protected bool m_IsInfinit => m_Duration <= 0f;

        public bool IsOver => m_IsOver;

        #endregion


        #region Constructor



        #endregion


        #region Init & End

        public void Start()
        {
            if (! m_IsActivated)
                return;

            Activate();
        }

        public virtual void Initialize(string id = "", float duration = -1f)
        {
            m_IsOver    = false;
            m_Id        = id == "" ? AnimationHandler.GenerateRandomId() : id;
            m_Duration  = duration;
            m_Timer     = 0f;
        }

        public virtual void Activate(bool activate = true)
        {
            m_IsActivated = activate;

            if (activate)
            {
                AnimationHandler.AddAnimation(this, m_Id);
                m_Coroutine = StartCoroutine(Play());
            }
            else
                Deactivate();
        }

        public virtual void Deactivate()
        {
            m_IsActivated = false;

            if (m_Coroutine != null)
            {
                StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }
        }

        public void End()
        {
            m_IsOver = true;
            OnAnimationEnded?.Invoke();
            Deactivate();

            if (m_EndWhenOver)
                Destroy(this);
        }

        protected virtual void OnDestroy()
        {
            // if was not called by the "End" method
            if (! m_IsOver) 
                AnimationHandler.EndAnimation(m_Id, Name);
        }

        #endregion


        #region Play Animation

        protected virtual IEnumerator Play()
        {
            m_IsOver = false;
            m_Timer = 0;

            while (m_IsInfinit || m_Timer <= m_Duration)
            {
                yield return AnimationFrame();
            }

            AnimationHandler.EndAnimation(m_Id, Name);
            End();
        }

        protected virtual IEnumerator AnimationFrame()
        {
            m_Timer += Time.deltaTime;
            yield return null;
        }

        #endregion


        #region Helpers

        protected float GetProgress()
        {
            if (m_Duration == 0)
                return 0f;

            if (m_IsInfinit || m_AnimationCurve == null)
            {
                float duration = m_IsInfinit ? 1f : m_Duration;
                return m_Timer / duration;
            }

            return m_AnimationCurve.Evaluate(m_Timer / m_Duration);
        }

        #endregion
    }
}