using System.Collections;
using UnityEngine;

namespace Tools.Animations
{
    public class Pulse : OvAnimation
    {
        private enum PulseStage
        {
            None,
            Growing,
            Pausing
        }

        #region Members

        protected float m_MinSize = 0.95f;
        protected float m_MaxSize = 1.05f;
        protected float m_PulseDuration = 1f;
        protected int m_NPulsePerLoop = 2;
        protected float m_PauseDuration = 0.5f;

        private PulseStage m_CurrentStage = PulseStage.None;
        private float[] m_Stages = null;
        private int m_CurrentPulse = 0;
        private int m_StageIndex = 0;
        private float m_CurrentSize = 1f;
        private float m_NextSize = 1f;
        private float m_StageTimer = 0f;
        private float m_PauseTimer = 0f;
        private float m_StageDuration = 0f;

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

            if (m_Stages == null)
            {
                // First-time initialization
                m_Stages            = new float[] { m_MaxSize, 1f, m_MinSize, 1f };
                m_StageDuration     = m_PulseDuration / m_Stages.Length;
                m_CurrentPulse      = 0;
                m_StageIndex        = 0;
                m_CurrentSize       = 1f;
                m_NextSize          = m_Stages[m_StageIndex];
                m_StageTimer        = 0f;
                m_CurrentStage      = PulseStage.Growing;
            }

            switch (m_CurrentStage)
            {
                case PulseStage.Growing:
                    m_StageTimer += Time.deltaTime;

                    float t = Mathf.Clamp01(m_StageTimer / m_StageDuration);
                    float scale = Mathf.Lerp(m_CurrentSize, m_NextSize, t);
                    transform.localScale = new Vector3(scale, scale, 1f);

                    if (m_StageTimer >= m_StageDuration)
                    {
                        m_CurrentSize = m_NextSize;
                        m_StageIndex++;
                        m_StageTimer = 0f;

                        if (m_StageIndex >= m_Stages.Length)
                        {
                            m_CurrentPulse++;
                            m_StageIndex = 0;

                            if (m_CurrentPulse >= m_NPulsePerLoop)
                            {
                                m_CurrentStage = PulseStage.Pausing;
                                m_PauseTimer = m_PauseDuration;
                                break;
                            }
                        }

                        m_NextSize = m_Stages[m_StageIndex];
                    }
                    break;

                case PulseStage.Pausing:
                    m_PauseTimer -= Time.deltaTime;
                    if (m_PauseTimer <= 0f)
                    {
                        // Reset for next pulse loop
                        m_CurrentPulse = 0;
                        m_StageIndex = 0;
                        m_CurrentSize = 1f;
                        m_NextSize = m_Stages[m_StageIndex];
                        m_StageTimer = 0f;
                        m_CurrentStage = PulseStage.Growing;
                    }
                    break;
            }
        }

        #endregion
    }
}
