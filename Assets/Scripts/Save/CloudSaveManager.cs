using Assets;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Save
{
    public class CloudSaveManager : MonoBehaviour
    {
        #region Members

        GameCloudData                       m_GameCloudData;
        List<CloudData>                     m_CloudData;

        public GameCloudData GameCloudData => m_GameCloudData;

        public List<CloudData> CloudData => m_CloudData;
        
        #endregion


        #region Init & End

        private void OnDestroy()
        {
            SaveAll();
        }

        public void OnApplicationQuit()
        {
            SaveAll();
        }

        #endregion


        #region Load & Save

        public void LoadSave()
        {
            //m_GameCloudData = new GameCloudData();

            m_CloudData = new()
            {
                new CharacterBuildsCloudData(),
                new InventoryCloudData(),
                new ChestsCloudData(),
                new ProfileCloudData(),
                new StatCloudData(),
                new ProgressionCloudData(),
                new TimeCloudData(),
                new NotificationCloudData(),
            };
        }

        public void SaveAll()
        {
            if ( ! LoadingCompleted )
                return;

            if ( ! Main.ActivateSaveOnClose)
            {
                ErrorHandler.Log("SaveAll() called but deactivated");
                return;
            }
            
            foreach (CloudData data in m_CloudData)
            {
                data.Save();
            }
        }

        #endregion


        #region Accessors

        public CloudData GetCloudData(Type cloudDataType)
        {
            if (m_CloudData == null)
                return null;

            foreach (CloudData cloudData in m_CloudData)
            {
                if (cloudData.GetType() == cloudDataType)
                    return cloudData;
            }

            ErrorHandler.Warning($"Cloud data {cloudDataType} not found in CloudSaveManager - creating new one");

            // Create an instance of the class represented by cloudDataType
            CloudData instance = (CloudData)Activator.CreateInstance(cloudDataType);
            m_CloudData.Add(instance);

            return instance;
        }


        /// <summary> check that all cloud data have been loaded </summary>
        public bool LoadingCompleted
        {
            get
            {
                // check initialized
                if (m_CloudData == null || m_CloudData.Count == 0)
                    return false;

                // check all cloud data have beed loaded
                foreach (CloudData cloudData in m_CloudData)
                {
                    if (! cloudData.LoadingCompleted)
                        return false;
                }

                return true;
            }
        }

        public void ResetAll()
        {
            foreach (CloudData cloudData in m_CloudData)
            {
                cloudData.ResetAll();
            }
        }

        #endregion

    }
}