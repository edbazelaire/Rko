using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;


namespace Assets.Scripts.Game
{
    public class PoolManager : MonoBehaviour
    {
        #region Members

        public static PoolManager Instance;
        private Dictionary<string, Queue<NetworkObject>> m_NetworkObjectPool = new Dictionary<string, Queue<NetworkObject>>();
        private Dictionary<string, Queue<GameObject>> m_GameObjectPool = new Dictionary<string, Queue<GameObject>>();

        #endregion


        #region Init & End

        private void Awake()
        {
            m_NetworkObjectPool = new Dictionary<string, Queue<NetworkObject>>();
            m_GameObjectPool = new Dictionary<string, Queue<GameObject>>();

            Instance = this;
        }

        #endregion


        #region Pooling

        public static GameObject Pool(GameObject prefab, Transform parent)
        {
            if (prefab == null)
                prefab = new GameObject("Default_GameObject");

            string key = prefab.name;

            GameObject obj;
            if (! Instance.m_GameObjectPool.ContainsKey(key) || Instance.m_GameObjectPool[key].Count == 0)
            {
                // If the pool is empty, create a new object
                obj = Instantiate(prefab, parent);
            }
            else
            {
                // Otherwise, retrieve an object from the pool
                obj = Instance.m_GameObjectPool[key].Dequeue();
                obj.transform.parent = parent;
                obj.gameObject.SetActive(true);
            }

            return obj;
        }

        public static GameObject Pool(GameObject prefab, Vector3 position, Quaternion rotation, Transform parent)
        {
            GameObject obj = Pool(prefab, parent);
            obj.transform.SetPositionAndRotation(position, rotation);

            return obj;
        }

        public static NetworkObject Pool(NetworkObject prefab, ulong clientId, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;

            NetworkObject obj;
            if (! Instance.m_NetworkObjectPool.ContainsKey(key) || Instance.m_NetworkObjectPool[key].Count == 0)
            {
                // If the pool is empty, create a new object
                obj = Instantiate(prefab, position, rotation);
            } else
            {
                // Otherwise, retrieve an object from the pool
                obj = Instance.m_NetworkObjectPool[key].Dequeue();
                obj.transform.SetPositionAndRotation(position, rotation);
                obj.gameObject.SetActive(true);
            }
            
            obj.SpawnWithOwnership(clientId); // Resynchronize on the network
            return obj;
        }

        public static void ReturnObject(GameObject obj)
        {
            string key = obj.name.Replace("(Clone)", "");

            if (! Instance.m_GameObjectPool.ContainsKey(key))
            {
                Instance.m_GameObjectPool[key] = new Queue<GameObject>();
            }

            obj.SetActive(false);
            Instance.m_GameObjectPool[key].Enqueue(obj);
        }

        public static void ReturnObject(NetworkObject obj)
        {
            string key = obj.name.Replace("(Clone)", "");

            if (! Instance.m_NetworkObjectPool.ContainsKey(key))
            {
                Instance.m_NetworkObjectPool[key] = new Queue<NetworkObject>();
            }

            // despawn and deactivate object
            obj.Despawn(false);                 
            obj.gameObject.SetActive(false);

            // add object at the end of the queue
            Instance.m_NetworkObjectPool[key].Enqueue(obj);
        }

        #endregion

    }

}