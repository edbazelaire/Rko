using Game.Spells;
using UnityEngine;
using System.Collections;
using Tools;

namespace Game.SpellGFXs
{
    [ExecuteAlways] // permet de voir aussi en mode éditeur (hors Play)
    public class BeamPulseGFX : BeamGFX
    {
        #region Members

        public bool Play = false;

        [Tooltip("Delay between each beam pulse (seconds)")]
        public float m_PulseTick = 0.5f;

        [Tooltip("Speed of each pulse traveling towards EndBeam")]
        public float m_PulseSpeed = 20f;

        [Tooltip("Time to wait before destroying the pulse once it reaches the Target")]
        public float m_PulsePersistance = 0f;

        /// <summary> contains all the created "Pulse" objects to delete them at once </summary>
        Transform m_PulsesContainer;
        float m_PulseTimer;
        bool m_IsPlaying = false;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_PulsesContainer = new GameObject("PulsesContainer").transform;
            m_PulsesContainer.transform.parent = transform;
            m_PulsesContainer.transform.localScale = Vector3.one;
        }

        protected override void ForceEnd()
        {
            Destroy(m_PulsesContainer.gameObject);

            base.ForceEnd();
        }

        #endregion


        #region Update

        protected override void Update()
        {
            if (Application.isPlaying)
            {
                if (m_BeamSpell == null || m_BeamSpell.IsOver)
                    return;
            } 
            else
            {
                CheckPlayButton();
                if (!m_IsPlaying)
                    return;
            }

            base.Update();

            // Pulse timer
            m_PulseTimer -= Time.deltaTime;
            if (m_PulseTimer <= 0f)
            {
                m_PulseTimer = m_PulseTick;
                CreatePulse();
            }
        }

        void CreatePulse()
        {
            GameObject pulse = Instantiate(m_BeamRay, transform.position, Quaternion.identity, m_PulsesContainer);
            StartCoroutine(MovePulse(pulse, m_EndBeam.transform));
        }

        IEnumerator MovePulse(GameObject pulse, Transform target)
        {
            while (pulse != null && target != null && Vector3.Distance(pulse.transform.position, target.position) > 0.05f)
            {
                pulse.transform.position = Vector3.MoveTowards(
                    pulse.transform.position,
                    target.position,
                    m_PulseSpeed * Time.deltaTime
                );
                yield return null;
            }

            yield return new WaitForSeconds(m_PulsePersistance);

            if (pulse != null)
                DestroyImmediate(pulse);
        }

        #endregion


        #region Playing (Editor)

        void CheckPlayButton()
        {
            if (Play == m_IsPlaying)
                return;

            if (Play)
                StartPlaying();
            else
                StopPlaying();
        }

        void StartPlaying()
        {
            m_PulsesContainer = new GameObject("PulsesContainer").transform;
            m_PulsesContainer.transform.parent = transform;

            m_IsPlaying = true;
        }

        void StopPlaying()
        {
            m_IsPlaying = false;
            StopAllCoroutines();
            DestroyImmediate(m_PulsesContainer.gameObject);
        }

        #endregion
    }
}
