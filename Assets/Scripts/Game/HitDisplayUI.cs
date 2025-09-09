using Data.GameManagement;
using Enums;
using Game;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Game
{
    /// <summary>
    /// Data container for a hit display request.
    /// Holds the hit value, type (damage/heal/etc.) and category (direct/tick/etc.).
    /// </summary>
    public struct HitDisplayData
    {
        public int Damage;
        public EHitType HitType;
        public EHitCategory DamageType;

        public HitDisplayData(int damage, EHitType hitType, Enums.EHitCategory damageType)
        {
            Damage = damage;
            HitType = hitType;
            DamageType = damageType;
        }
    }

    /// <summary>
    /// UI manager for displaying floating hit texts above characters.
    /// Handles separate queues for direct hits and tick hits.
    /// </summary>
    public class HitDisplayUI : MonoBehaviour
    {
        public static HitDisplayUI Instance;

        [SerializeField] private GameObject m_FloatingTextPrefab;   // Prefab used for floating text
        [SerializeField] private float m_QueueDelay = 0.35f;        // Delay between direct/zone texts
        [SerializeField] private float m_TickBatchDelay = 0.5f;     // Delay before batching tick damage together

        // Queues for different hit categories
        private Dictionary<ulong, Queue<HitDisplayData>> m_HitQueues = new();   // Direct/Zone
        private Dictionary<ulong, Queue<HitDisplayData>> m_TickQueues = new();  // Tick-only

        // Active trackers to prevent duplicate coroutines
        private HashSet<ulong> m_ActiveHitDisplays = new();
        private HashSet<ulong> m_ActiveTickDisplays = new();

        private void Awake()
        {
            if (Instance == null)
                Instance = this;
            else
                Destroy(gameObject);
        }

        /// <summary>
        /// Entry point to request the display of a hit.
        /// Chooses the correct queue based on spell category.
        /// </summary>
        public void DisplayHit(ulong clientId, int value, EHitType hitType, EHitCategory damageType)
        {
            if (value <= 0)
            {
                ErrorHandler.Warning("Value (" + value + ") <= 0");
                return;
            }

            // ✅ Check player settings
            if (! PlayerSettings.IsDisplayed(hitType, damageType))
                return;

            // Do not display hits on spawn objects
            if (GameManager.Instance.IsSpawnId(clientId))
                return;

            HitDisplayData data = new(value, hitType, damageType);

            if (damageType == EHitCategory.Tick)
            {
                // Queue for ticks
                if (!m_TickQueues.ContainsKey(clientId))
                    m_TickQueues[clientId] = new Queue<HitDisplayData>();

                m_TickQueues[clientId].Enqueue(data);

                if (!m_ActiveTickDisplays.Contains(clientId))
                {
                    m_ActiveTickDisplays.Add(clientId);
                    StartCoroutine(DisplayTickQueue(clientId));
                }
            }
            else
            {
                // Queue for direct/zone hits
                if (!m_HitQueues.ContainsKey(clientId))
                    m_HitQueues[clientId] = new Queue<HitDisplayData>();

                m_HitQueues[clientId].Enqueue(data);

                if (!m_ActiveHitDisplays.Contains(clientId))
                {
                    m_ActiveHitDisplays.Add(clientId);
                    StartCoroutine(DisplayHitQueue(clientId));
                }
            }
        }

        /// <summary>
        /// Process queue for direct/zone hits.
        /// </summary>
        private IEnumerator DisplayHitQueue(ulong clientId)
        {
            while (m_HitQueues.ContainsKey(clientId) && m_HitQueues[clientId].Count > 0)
            {
                if (GameManager.IsGameOver)
                {
                    Destroy(gameObject);
                    yield break;
                }

                HitDisplayData data = m_HitQueues[clientId].Dequeue();
                ShowDamage(clientId, data);

                yield return new WaitForSeconds(m_QueueDelay);
            }

            m_ActiveHitDisplays.Remove(clientId);
        }

        /// <summary>
        /// Process queue for tick hits (batched).
        /// Accumulates ticks for a short window before displaying.
        /// </summary>
        private IEnumerator DisplayTickQueue(ulong clientId)
        {
            float tickTimer = 0f;
            int tickAccumulator = 0;

            while (m_TickQueues.ContainsKey(clientId) && m_TickQueues[clientId].Count > 0)
            {
                if (GameManager.IsGameOver)
                {
                    Destroy(gameObject);
                    yield break;
                }

                HitDisplayData data = m_TickQueues[clientId].Dequeue();

                // Accumulate tick damage
                tickAccumulator += data.Damage;
                tickTimer += m_QueueDelay;

                if (tickTimer >= m_TickBatchDelay)
                {
                    ShowDamage(clientId, new HitDisplayData(tickAccumulator, data.HitType, EHitCategory.Tick));
                    tickAccumulator = 0;
                    tickTimer = 0f;
                }

                yield return new WaitForSeconds(m_QueueDelay);
            }

            // Flush remaining ticks
            if (tickAccumulator > 0)
            {
                ShowDamage(clientId, new HitDisplayData(tickAccumulator, EHitType.Damage, EHitCategory.Tick));
            }

            m_ActiveTickDisplays.Remove(clientId);
        }

        /// <summary>
        /// Spawns a floating text prefab and sets its content.
        /// </summary>
        private void ShowDamage(ulong clientId, HitDisplayData data)
        {
            var player = GameManager.Instance.GetPlayer(clientId);
            if (player == null) return;

            var pos = player.transform.position;
            pos.y += 0.7f; // Offset above character head

            var damageText = PoolManager.Pool(m_FloatingTextPrefab, pos, Quaternion.identity, null, true);
            damageText.GetComponent<FloatingTextUI>().SetText(data.Damage, data.HitType, data.DamageType);

            // Flip text for enemy team if needed
            if (GameManager.Instance.Owner.Team == 1)
            {
                damageText.transform.rotation = Quaternion.Euler(transform.rotation.x, -180f, transform.rotation.z);
            }
        }
    }
}
