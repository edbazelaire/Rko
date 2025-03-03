using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class Pulse : OvAnimation
    {
        #region Members

        protected float        m_MinSize         = 0.95f;
        protected float        m_MaxSize         = 1.05f;
        protected float        m_PulseDuration   = 1f;
        protected int          m_NPulsePerLoop   = 2;
        protected float        m_PauseDuration   = 0.5f;

        #endregion


        #region Init & End

        public void Initialize(string id = "", float duration = -1f, float minSize = 0.95f, float maxSize = 1.05f, float pulseDuration = 1f, int nPulsePerLoop = 2, float pauseDuration = 0.5f)
        {
            m_MinSize = minSize;
            m_MaxSize = maxSize;
            m_PulseDuration = pulseDuration;
            m_NPulsePerLoop = nPulsePerLoop;
            m_PauseDuration = pauseDuration;

            base.Initialize(id, duration);
        }

        public override void Deactivate()
        {
            base.Deactivate();

            // reset gameObject size on destroy
            gameObject.transform.localScale = Vector3.one;
        }

        #endregion


        #region Animation

        protected override IEnumerator AnimationFrame()
        {
            yield return base.AnimationFrame();

            float[] stages = new float[] { m_MaxSize, 1f, m_MinSize, 1f };
            // divide pulse stage duration by number of stages
            float pulseDuration = m_PulseDuration / stages.Length;

            for (int i = 0; i < m_NPulsePerLoop; i++)
            {
                float currentSize = 1f;

                foreach (var nextSize in stages)
                {
                    // reset timer
                    float pulseTimer = 0;

                    while (pulseTimer <= pulseDuration)
                    {
                        // calculate scale
                        float t = pulseTimer / pulseDuration;
                        float scale = Mathf.Lerp(currentSize, nextSize, t);

                        // update scale
                        gameObject.transform.localScale = new Vector3(scale, scale, 1f);

                        // update timer
                        pulseTimer += Time.deltaTime;
                        yield return null;
                    }

                    // set new current size 
                    currentSize = nextSize;
                }
            }

            // PAUSE BETWEEN TIMERS
            float pauseTimer = m_PauseDuration;
            while (pauseTimer > 0f)
            {
                pauseTimer -= Time.deltaTime;
                yield return null;
            }
        }

        #endregion
    }
}