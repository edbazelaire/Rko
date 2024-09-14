using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Tools
{
    public class TimeErrorWrapper : MonoBehaviour
    {
        #region Members

        static TimeErrorWrapper s_Instance;

        Dictionary<string, Coroutine> m_Coroutines = new();

        #endregion


        #region Init & End

        private void Start()
        {
            DontDestroyOnLoad(s_Instance);
        }

        #endregion


        #region Coroutine Management

        public IEnumerator WrapCoroutine(string id, float timer, Action onTimerEnd)
        {
            while (timer > 0)
            {
                timer -= Time.deltaTime;
                yield return null;
            }

            onTimerEnd?.Invoke();

            // remove from list of coroutines
            if (!m_Coroutines.ContainsKey(id))
            {
                ErrorHandler.Error("Unable to find coroutine with id : " + id);
                yield break;
            }

            m_Coroutines.Remove(id);
        }

        /// <summary>
        /// Create a new TimeWrapper that will throw an Error on time limit reached
        /// </summary>
        /// <param name="id"></param>
        /// <param name="timer"></param>
        /// <param name="onTimerEnd"></param>
        public void New(string id, float timer, Action onTimerEnd)
        {
            if (timer < 0)
            {
                ErrorHandler.Error("Time wrapper provided with negative timer ("+timer+"): cancelling");
                return;
            }

            if (id == "")
            {
                ErrorHandler.Error("Time wrapper provided with empty id : cancelling");
                return;
            }
            
            // if coroutine already exisiting : cancel before creating new one
            if (m_Coroutines.ContainsKey(id) && m_Coroutines[id] != null)
            {
                Cancel(id);
            }

            m_Coroutines[id] = StartCoroutine(WrapCoroutine(id, timer, onTimerEnd));
        }

        /// <summary>
        /// Stop a time wrapper with a specific ID
        /// </summary>
        /// <param name="id"></param>
        public void Cancel(string id)
        {
            if (! m_Coroutines.ContainsKey(id))
            {
                return;
            }

            StopCoroutine(m_Coroutines[id]);
            m_Coroutines.Remove(id);
        }

        #endregion


        #region Dependent Members

        public static TimeErrorWrapper Instance
        {
            get
            {
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = FindFirstObjectByType<TimeErrorWrapper>();
                if (s_Instance != null)
                    return s_Instance;

                s_Instance = GameObject.Instantiate(AssetLoader.LoadManager<TimeErrorWrapper>());
                return s_Instance;
            }
        }  

        #endregion
    }
}