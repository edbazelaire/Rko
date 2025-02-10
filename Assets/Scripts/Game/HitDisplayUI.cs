using Enums;
using Game;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public struct HitDisplayData
    {
        public int Damage;
        public EHitType HitType;
        public Enums.ESpellCategory DamageType;

        public HitDisplayData(int damage, EHitType hitType, Enums.ESpellCategory damageType)
        {
            Damage = damage;
            HitType = hitType;
            DamageType = damageType;
        }
    }

    public class HitDisplayUI : MonoBehaviour
    {
        public static HitDisplayUI Instance;

        [SerializeField] private GameObject m_FloatingTextPrefab; // Prefab for floating text
        [SerializeField] private float m_QueueDelay = 0.35f;       // Delay for queued texts

        private Dictionary<ulong, Queue<HitDisplayData>> m_HitQueues = new();
        private HashSet<ulong> m_ActiveDisplays = new();

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void DisplayHit(ulong clientId, int damage, EHitType hitType, ESpellCategory damageType)
        {
            if (!m_HitQueues.ContainsKey(clientId))
            {
                m_HitQueues[clientId] = new Queue<HitDisplayData>();
            }

            // check if should be displayed
            if (damageType == ESpellCategory.None || (hitType == EHitType.Damage && damageType == ESpellCategory.Tick))
                return;

            // check if is a spawn 
            if (GameManager.Instance.IsSpawnId(clientId))
                return;

            // Enqueue the hit data for the player
            HitDisplayData data = new(damage, hitType, damageType);
            m_HitQueues[clientId].Enqueue(data);

            // If not currently displaying for this player, start processing their queue
            if (!m_ActiveDisplays.Contains(clientId))
            {
                m_ActiveDisplays.Add(clientId);
                StartCoroutine(DisplayQueue(clientId));
            }
        }

        private IEnumerator DisplayQueue(ulong clientId)
        {
            while (m_HitQueues.ContainsKey(clientId) && m_HitQueues[clientId].Count > 0)
            {
                HitDisplayData data = m_HitQueues[clientId].Dequeue();
                ShowDamage(clientId, data);

                yield return new WaitForSeconds(m_QueueDelay);
            }

            // Mark this player's queue as no longer active
            m_ActiveDisplays.Remove(clientId);
        }

        private void ShowDamage(ulong clientId, HitDisplayData data)
        {
            var player = GameManager.Instance.GetPlayer(clientId);
            if (player == null)
                return;

            var pos = GameManager.Instance.GetPlayer(clientId).transform.position;
            pos.y += 0.7f;
            var damageText = Instantiate(m_FloatingTextPrefab, pos, Quaternion.identity);
            damageText.GetComponent<FloatingTextUI>().SetText(data.Damage, data.HitType);

            if (GameManager.Instance.Owner.Team == 1)
            {
                damageText.transform.rotation = Quaternion.Euler(transform.rotation.x, -180f, transform.rotation.z);
            }
        }
    }
}
