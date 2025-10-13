using Game;
using Game.Pool;
using System.Collections.Generic;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Assets.Scripts.Game
{
    public class PoolManager : MonoBehaviour
    {
        #region Members

        public static PoolManager Instance;
        private Dictionary<string, Queue<NetworkObject>> m_NetworkObjectPool = new Dictionary<string, Queue<NetworkObject>>();
        private Dictionary<string, Queue<GameObject>> m_GameObjectPool = new Dictionary<string, Queue<GameObject>>();
        private static readonly List<IPoolLifecycle> s_LifecycleBuf = new(8);

        #endregion

        #region Init & End

        private void Awake()
        {
            m_NetworkObjectPool = new Dictionary<string, Queue<NetworkObject>>();
            m_GameObjectPool = new Dictionary<string, Queue<GameObject>>();

            Instance = this;
        }

        #endregion

        #region Pooling (GameObject)

        /// <summary>
        /// Pull a GameObject from pool (or instantiate if empty) and optionally activate it.
        /// In offline mode, IPoolLifecycle.OnSpawnedFromPool() is dispatched on activation.
        /// </summary>
        public static GameObject Pool(GameObject prefab, Transform parent, bool activate = true, bool checkSpawnLogic = false)
        {
            if (prefab == null)
                prefab = new GameObject("Default_GameObject");

            string key = prefab.name;

            GameObject obj;
            if (!Instance.m_GameObjectPool.ContainsKey(key) || Instance.m_GameObjectPool[key].Count == 0)
            {
                // If the pool is empty, create a new object
                obj = Instantiate(prefab, parent);
                obj.gameObject.SetActive(activate);
            }
            else
            {
                // Otherwise, retrieve an object from the pool
                obj = Instance.m_GameObjectPool[key].Dequeue();
                if (obj.IsDestroyed())
                {
                    // If the pool is empty, create a new object
                    obj = Instantiate(prefab, parent);
                    obj.gameObject.SetActive(activate);
                }
                else
                {
                    obj.transform.SetParent(parent);
                    obj.gameObject.SetActive(activate);
                }
            }

            if (checkSpawnLogic)
                DispatchSpawned(obj);

            return obj;
        }

        /// <summary>
        /// Overload with position/rotation.
        /// Will dispatch OnSpawnedFromPool in offline if 'activate' is true.
        /// </summary>
        public static GameObject Pool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent, bool activate = true, bool checkSpawnLogic = false)
        {
            GameObject obj = Pool(prefab, parent, activate: false, checkSpawnLogic: checkSpawnLogic);
            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(activate);

            return obj;
        }

        #endregion


        #region Pooling (NetworkObject)

        /// <summary>
        /// Pull a NetworkObject from pool. In offline mode, we do NOT spawn on the network:
        /// we simply activate and dispatch IPoolLifecycle.OnSpawnedFromPool().
        /// In online mode, we SpawnWithOwnership which will trigger OnNetworkSpawn (your code there).
        /// </summary>
        public static NetworkObject Pool(NetworkObject prefab, ulong clientId, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            string key = prefab.name;

            NetworkObject obj;
            if (!Instance.m_NetworkObjectPool.ContainsKey(key) || Instance.m_NetworkObjectPool[key].Count == 0)
            {
                // If the pool is empty, create a new object
                obj = Instantiate(prefab, position, rotation);
            }
            else
            {
                // Otherwise, retrieve an object from the pool
                obj = Instance.m_NetworkObjectPool[key].Dequeue();
                if (obj.transform.parent != null)
                    obj.transform.SetParent(null);

                obj.transform.SetPositionAndRotation(position, rotation);
                obj.gameObject.SetActive(true);
            }

            // OFFLINE: do not spawn on NGO, but invoke gameplay lifecycle
            if (GameManager.Instance.IsOfflineMode)
            {
                if (parent != null)
                {
                    obj.gameObject.transform.SetParent(parent, worldPositionStays);
                }

                obj.gameObject.SetActive(true);
                DispatchSpawned(obj.gameObject);
                return obj;
            }


            // ONLINE: resynchronize on the network (OnNetworkSpawn will handle gameplay lifecycle)
            if (!obj.IsSpawned)
                obj.SpawnWithOwnership(clientId);

            if (parent != null)
            {
                obj.TrySetParent(parent, worldPositionStays);
            }

            return obj;
        }

        #endregion

        #region Return

        /// <summary>
        /// Return a GameObject to its pool.
        /// In offline mode, dispatch IPoolLifecycle.OnReturnedFromPool() before disabling.
        /// </summary>
        public static void ReturnObject(GameObject obj, bool checkSpawnLogic = false)
        {
            if (!obj) return;

            string key = obj.name.Replace("(Clone)", "");

            if (!Instance.m_GameObjectPool.ContainsKey(key))
            {
                Instance.m_GameObjectPool[key] = new Queue<GameObject>();
            }

            if (checkSpawnLogic)
                DispatchReturned(obj);

            obj.SetActive(false);
            Instance.m_GameObjectPool[key].Enqueue(obj);
        }

        /// <summary>
        /// Return a NetworkObject to its pool.
        /// In offline mode, we DO NOT Despawn (no NGO cost); we dispatch lifecycle, then disable.
        /// In online mode, we Despawn (NGO will call OnNetworkDespawn where your gameplay cleanup should run).
        /// </summary>
        public static void ReturnObject(NetworkObject obj)
        {
            string key = obj.name.Replace("(Clone)", "");

            if (!Instance.m_GameObjectPool.ContainsKey(key))
            {
                Instance.m_NetworkObjectPool[key] = new Queue<NetworkObject>();
            }

            if (GameManager.Instance.IsOfflineMode)
            {
                // OFFLINE: run gameplay cleanup hooks before disabling
                DispatchReturned(obj.gameObject);
            }
            else if (obj.IsSpawned)
            {
                // ONLINE: let NGO drive lifecycle via OnNetworkDespawn
                obj.Despawn(false);
            }

            // ensure disabled after despawn (safe guard)
            obj.gameObject.SetActive(false);

            // add object at the end of the queue
            Instance.m_NetworkObjectPool[key].Enqueue(obj);
        }

        #endregion


        #region IPoolLifecycle Management

        /// <summary>
        /// Call IPoolLifecycle.OnSpawnedFromPool() on all components attached to 'go'.
        /// Only used by PoolManager in OFFLINE mode to mimic Netcode's spawn lifecycle without NGO cost.
        /// </summary>
        private static void DispatchSpawned(GameObject go)
        {
            go.GetComponents(s_LifecycleBuf);
            for (int i = 0; i < s_LifecycleBuf.Count; i++)
                s_LifecycleBuf[i].OnSpawned();
            s_LifecycleBuf.Clear();
        }

        /// <summary>
        /// Call IPoolLifecycle.OnReturnedToPool() on all components attached to 'go'.
        /// Only used by PoolManager in OFFLINE mode to mimic Netcode's despawn lifecycle without NGO cost.
        /// </summary>
        private static void DispatchReturned(GameObject go)
        {
            go.GetComponents(s_LifecycleBuf);
            for (int i = 0; i < s_LifecycleBuf.Count; i++)
                s_LifecycleBuf[i].OnDespawned();
            s_LifecycleBuf.Clear();
        }

        #endregion
    }
}
