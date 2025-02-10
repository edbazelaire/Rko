using System.Collections;
using UnityEngine;

namespace Assets.Scripts.Game
{
    using System.Collections.Generic;
    using Unity.Netcode;
    using UnityEngine;

    public class NetworkObjectPool : NetworkBehaviour
    {
        #region Members

        public static NetworkObjectPool Instance;
        private Dictionary<string, Queue<NetworkObject>> m_PoolDictionary = new Dictionary<string, Queue<NetworkObject>>();

        #endregion


        #region Init & End

        private void Awake()
        {
            Instance = this;
        }

        #endregion


        #region Pooling

        public NetworkObject GetObject(NetworkObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;

            if (!m_PoolDictionary.ContainsKey(key) || m_PoolDictionary[key].Count == 0)
            {
                // If the pool is empty, create a new object
                NetworkObject newObj = Instantiate(prefab, position, rotation);
                newObj.Spawn(true); // Important for network synchronization
                return newObj;
            }

            // Otherwise, retrieve an object from the pool
            NetworkObject obj = m_PoolDictionary[key].Dequeue();
            obj.transform.position = position;
            obj.transform.rotation = rotation;
            obj.gameObject.SetActive(true);
            obj.Spawn(true); // Resynchronize on the network
            return obj;
        }

        public void ReturnObject(NetworkObject obj)
        {
            string key = obj.name;

            if (!m_PoolDictionary.ContainsKey(key))
            {
                m_PoolDictionary[key] = new Queue<NetworkObject>();
            }

            obj.Despawn(false); // Do not destroy the object
            obj.gameObject.SetActive(false);
            m_PoolDictionary[key].Enqueue(obj);
        }

        public void PreloadObjects(NetworkObject prefab, int count)
        {
            string key = prefab.name;

            if (!m_PoolDictionary.ContainsKey(key))
            {
                m_PoolDictionary[key] = new Queue<NetworkObject>();
            }

            for (int i = 0; i < count; i++)
            {
                NetworkObject newObj = Instantiate(prefab);
                newObj.gameObject.SetActive(false);
                newObj.Spawn(false); // Do not sync immediately
                m_PoolDictionary[key].Enqueue(newObj);
            }
        }

        #endregion

    }

}