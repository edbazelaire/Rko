using Game;
using Game.Pool;
using System.Collections.Generic;
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
            GameObject SpawnObject()
            {
                GameObject obj;

                // If the pool is empty, create a new object
                obj = Instantiate(prefab, parent);
                obj.SetActive(activate);

                // offline mode : destroy NetworkObject component
                if (obj.TryGetComponent<NetworkObject>(out var netObj))
                    Destroy(netObj);

                return obj;
            }     

            if (prefab == null)
                prefab = new GameObject("Default_GameObject");

            string key = prefab.name;

            GameObject obj;
            if (!Instance.m_GameObjectPool.ContainsKey(key) || Instance.m_GameObjectPool[key].Count == 0)
            {
                obj = SpawnObject();
            }
            else
            {
                // Otherwise, retrieve an object from the pool
                obj = Instance.m_GameObjectPool[key].Dequeue();
                if (obj.IsDestroyed())
                {
                    obj = SpawnObject();
                }
                else
                {
                    obj.transform.SetParent(parent);
                    obj.SetActive(activate);
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
        public static GameObject Pool(NetworkObject prefab, ulong clientId, Vector3 position, Quaternion rotation, Transform parent = null, bool worldPositionStays = true)
        {
            string key = prefab.name;

            // OFFLINE MODE : pool GameObject (and destroy the NetworkObject component)
            if (GameManager.Instance.IsOfflineMode)
            {
                return Pool(prefab.gameObject, position, rotation, parent, activate: true, checkSpawnLogic: true);
            }

            NetworkObject netObj;
            if (!Instance.m_NetworkObjectPool.ContainsKey(key) || Instance.m_NetworkObjectPool[key].Count == 0)
            {
                // If the pool is empty, create a new object
                netObj = Instantiate(prefab);
            }
            else
            {
                // Otherwise, retrieve an object from the pool
                netObj = Instance.m_NetworkObjectPool[key].Dequeue();
            }

            GameObject obj = netObj.gameObject;
            

            // spawn the NetworkObject
            netObj.SpawnWithOwnership(clientId);

            // set global position and rotation
            obj.transform.SetParent(null, false);
            obj.transform.SetPositionAndRotation(position, rotation);

            // reparent object
            if (parent != null)
                obj.transform.SetParent(parent, worldPositionStays);

            obj.SetActive(true);
            return obj;
        }

        #endregion


        #region Return

        /// <summary>
        /// Return a GameObject to its pool.
        /// Optionally checks spawn logic and detaches the object from its parent before pooling.
        /// </summary>
        public static void ReturnObject(GameObject obj, bool checkSpawnLogic = false)
        {
            if (!obj) return;

            string key = obj.name.Replace("(Clone)", "");

            if (!Instance.m_GameObjectPool.ContainsKey(key))
                Instance.m_GameObjectPool[key] = new Queue<GameObject>();

            if (checkSpawnLogic)
                DispatchReturned(obj);

            // --- Ensure clean state
            obj.SetActive(false);

            // --- Requeue
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

            if (!Instance.m_NetworkObjectPool.ContainsKey(key))
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
