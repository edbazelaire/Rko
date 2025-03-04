using System;
using System.Collections.Generic;
using Tools;
using Save.RSDs;

namespace Save
{
    public static class RSDManager
    {
        #region Members

        private static Dictionary<Type, object> m_RsdInstances = new();

        #endregion

        #region Init & End

        public static void Initialize()
        {
            LoadSave();
        }

        #endregion

        #region Load & Save

        public static void LoadSave()
        {
            m_RsdInstances.Clear();

            // Register known RSDs
            m_RsdInstances[typeof(GiftCodeRSD)] = new GiftCodeRSD();
        }

        #endregion

        #region Accessors

        public static T GetRSD<T>() where T : class, new()
        {
            if (m_RsdInstances.TryGetValue(typeof(T), out object rsd))
            {
                return rsd as T;
            }

            ErrorHandler.Warning($"RSD {typeof(T)} not found in RSDManager - creating new one");

            // Create and store a new instance if not found
            T instance = new T();
            m_RsdInstances[typeof(T)] = instance;
            return instance;
        }

        /// <summary> Check that all cloud data have been loaded </summary>
        public static bool LoadingCompleted
        {
            get
            {
                if (m_RsdInstances.Count == 0) return false;

                // Check if all registered RSDs have completed loading
                foreach (var rsd in m_RsdInstances.Values)
                {
                    if (rsd is ILoadable loadable && !loadable.LoadingCompleted)
                        return false;
                }

                return true;
            }
        }

        #endregion
    }

    public interface ILoadable
    {
        bool LoadingCompleted { get; }
    }
}
