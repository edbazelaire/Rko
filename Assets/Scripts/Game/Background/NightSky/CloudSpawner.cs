using UnityEngine;
using Game.Background.NightSky;
using System.Collections;

public class CloudSpawner : MonoBehaviour
{
    [SerializeField] private Cloud m_CloudPrefab;
    [SerializeField] private int m_InitialCount         = 15; // number of clouds to spawn at start
    [SerializeField] private Vector2 m_SpawnYRange      = new Vector2(-2f, 2f);
    [SerializeField] private Vector2 m_SpawnDelayRange  = new Vector2(0.5f, 5f);
    [SerializeField] private Vector2 m_SpeedRange       = new Vector2(0.5f, 1.5f);

    public void Initialize()
    {
        StartCoroutine(SpawnClouds());
    }

    IEnumerator SpawnClouds()
    {
        // Prewarm clouds at Arena start
        for (int i = 0; i < m_InitialCount; i++)
        {
            SpawnCloud();
            yield return new WaitForSeconds(Random.Range(m_SpawnDelayRange[0], m_SpawnDelayRange[1]));
        }
    }

    private void SpawnCloud()
    {
        Vector3 pos = transform.position;
        pos.y += Random.Range(m_SpawnYRange.x, m_SpawnYRange.y);

        Cloud cloud = Instantiate(m_CloudPrefab, pos, Quaternion.identity, transform);
        cloud.Initialize(this, Random.Range(m_SpeedRange.x, m_SpeedRange.y));
    }

    /// <summary>
    /// Called by a Cloud when it leaves the screen bounds.
    /// </summary>
    public void RecycleCloud(Cloud cloud)
    {
        Vector3 pos = transform.position;
        pos.y += Random.Range(m_SpawnYRange.x, m_SpawnYRange.y);

        cloud.ResetCloud(pos, Random.Range(m_SpeedRange.x, m_SpeedRange.y));
    }
}
