using System.Collections.Generic;
using UnityEngine;

public class InGameDebugger : MObject
{
    #region Members

    [SerializeField, Tooltip("Temps entre deux mesures (en secondes)")]
    protected float m_RefreshTimer = 1f;

    private float m_Timer;
    private int m_MaxCount;

    private Queue<int> m_ParticleSystemCount = new Queue<int>(120);

    #endregion


    #region Update

    private void Update()
    {
        m_Timer += Time.deltaTime;

        if (m_Timer >= m_RefreshTimer)
        {
            m_Timer = 0f;

            int count = CountActiveParticleSystems();
            if (count > m_MaxCount)
                m_MaxCount = count;

            m_ParticleSystemCount.Enqueue(count);

            // Limite à 60 échantillons max
            if (m_ParticleSystemCount.Count > 60)
                m_ParticleSystemCount.Dequeue();

            float avg = GetAverageCount();
            Debug.Log($"🌀 Particle Systems — Now: {count} | Avg: {avg:F1} | Max: {m_MaxCount}");
        }
    }

    #endregion


    #region Utils

    private int CountActiveParticleSystems()
    {
        ParticleSystem[] systems = FindObjectsOfType<ParticleSystem>();
        int activeCount = 0;

        foreach (var ps in systems)
        {
            if (ps.isPlaying || ps.IsAlive())
                activeCount++;
        }

        return activeCount;
    }

    private float GetAverageCount()
    {
        if (m_ParticleSystemCount.Count == 0)
            return 0f;

        int sum = 0;
        foreach (var c in m_ParticleSystemCount)
            sum += c;

        return (float)sum / m_ParticleSystemCount.Count;
    }

    #endregion
}
