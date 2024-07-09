using Assets;
using Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Tools;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Internal;
using Unity.Services.CloudSave.Models;
using Unity.Services.CloudSave.Models.Data.Player;
using SaveOptions = Unity.Services.CloudSave.Models.Data.Player.SaveOptions;

namespace Save
{
    public class CloudData
    {
        #region Members

        public static CloudData Instance => Main.CloudSaveManager.GetCloudData(typeof(CloudData));

        protected virtual Dictionary<string, object> m_Data { get; set; }
        protected virtual List<string> m_PublicKeys => new() { };

        /// <summary> list of remaining values to load</summary>
        List<string> m_KeysToLoad;

        protected virtual IPlayerDataService CloudDatabase => CloudSaveService.Instance.Data.Player;

        public Dictionary<string, object> Data => m_Data;
        public bool LoadingCompleted = false;

        #endregion


        #region Init & End 

        public CloudData()
        {
            LoadingCompleted = false;
            m_KeysToLoad = m_Data.Keys.ToList();
            Load();

            RegisterListeners();
        }

        protected virtual void RegisterListeners() { }
        protected virtual void UnRegisterListeners() { }

        #endregion


        #region Loading & Saving

        public virtual void Load()
        {
            foreach (var key in m_Data.Keys)
            {
                // if value exists in the cloud get it, other wise keep default
                LoadValueAsync(key);
            }
        }

        protected async virtual void LoadValueAsync(string key)
        {
            var cloudData = await CloudDatabase.LoadAsync(new HashSet<string> { key }, m_PublicKeys.Contains(key) ? new LoadOptions(new PublicReadAccessClassOptions()) : new LoadOptions(new DefaultReadAccessClassOptions()));

            if (!cloudData.TryGetValue(key, out var item))
            {
                OnCloudDataKeyLoaded(key);
                return;
            }

            var value = Convert(item);
            if (value == null)
                return;

            // set givent value (block saving)
            SetData(key, value, false);
            OnCloudDataKeyLoaded(key);
        }

        public virtual void Save()
        {
            foreach (var item in m_Data)
            {
                SaveValue(item.Key);
            }
        }

        public virtual async void SaveValue(string key)
        {
            if (!m_Data.ContainsKey(key))
            {
                ErrorHandler.Error("Unable to find key " + key + " in data of " + this.GetType().FullName);
                return;
            }

            var playerData = new Dictionary<string, object> { { key, m_Data[key] } };

            if (playerData == null)
            {
                ErrorHandler.Error($"Data of key ({key}) is null");
                return;
            }

            // to keep the trace (because async exception creates a "bad" trace)
            Error error = new Error($"Error saving key ({key})", display: false);

            try
            {
                await CloudDatabase.SaveAsync(playerData, m_PublicKeys.Contains(key) ? new SaveOptions(new PublicWriteAccessClassOptions()) : new SaveOptions(new DefaultWriteAccessClassOptions()));
            }
            catch (CloudSaveConflictException ex)
            {
                ErrorHandler.Error($"Conflict error saving key ({key}) : " + ex.Message + "\nData : " + TextHandler.ToString(m_Data[key]));
                ErrorHandler.Warning("Trace : \n" + error.GetTraceString());
                await ResolveConflictAsync(key, playerData);
            }
            catch (Exception ex)
            {
                ErrorHandler.Error($"Error saving key ({key}) : " + ex.Message + "\nData : " + TextHandler.ToString(m_Data[key]));
                ErrorHandler.Warning("Trace : \n" + error.GetTraceString());
            }
        }

        #endregion


        #region Loading / Saving Error Management

        private async Task ResolveConflictAsync(string key, Dictionary<string, object> newPlayerData)
        {
            // Fetch existing value
            try
            {
                var existingData = await CloudDatabase.LoadAsync(new HashSet<string> { key });

                if (existingData.TryGetValue(key, out var existingValue))
                {
                    // Implement your conflict resolution logic here
                    ErrorHandler.Warning($"Existing value for {key}: {existingValue}");

                    // Example: decide to overwrite or merge
                    var mergedValue = MergeValues(existingValue, newPlayerData[key]);

                    // Save the resolved value
                    await SaveResolvedValueAsync(key, mergedValue);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Error($"Error resolving conflict for key ({key}) : " + ex.Message + "\nData : " + TextHandler.ToString(newPlayerData[key]));
            }
        }

        private async Task SaveResolvedValueAsync(string key, object resolvedValue)
        {
            var resolvedData = new Dictionary<string, object> { { key, resolvedValue } };

            try
            {
                await CloudDatabase.SaveAsync(resolvedData);
                ErrorHandler.Log($"Successfully saved resolved value for {key}");
            }
            catch (Exception ex)
            {
                ErrorHandler.Error($"Error saving resolved value for key ({key}) : " + ex.Message + "\nData : " + TextHandler.ToString(resolvedValue));
            }
        }

        private object MergeValues(object existingValue, object newValue)
        {
            // Implement your merging logic
            // This example simply returns the new value, but you can merge dictionaries, lists, etc.
            return newValue;
        }

        #endregion


        #region Reset & Unlock

        public virtual void Reset(string key) { }

        public virtual void ResetAll()
        {
            var keys = m_Data.Keys.ToArray();
            foreach (string dataKey in keys)
            {
                Reset(dataKey);
            }

            Save();
        }

        public virtual bool IsUnlockable(string key)
        {
            return false;
        }

        public virtual void Unlock(string key, bool save = true)
        {
            if (!m_Data.ContainsKey(key))
                ErrorHandler.Error("Key does not exist in the data");
        }

        #endregion


        #region Checkers

        protected virtual void CheckData() { }

        #endregion


        #region Helpers

        public virtual void SetData(string key, object value, bool save = true)
        {
            m_Data[key] = value;
            if (save)
                SaveValue(key);
        }

        protected virtual object Convert(Item item)
        {
            var expectedType = m_Data[item.Key].GetType().Name;

            if (m_Data[item.Key].GetType() == typeof(List<string>))
                return item.Value.GetAs<List<string>>();

            if (m_Data[item.Key].GetType() == typeof(List<int>))
                return item.Value.GetAs<List<string>>();

            if (m_Data[item.Key].GetType() == typeof(List<float>))
                return item.Value.GetAs<List<string>>();

            if (m_Data[item.Key].GetType() == typeof(List<bool>))
                return item.Value.GetAs<List<string>>();

            switch (expectedType)
            {
                case "String":
                    return item.Value.GetAs<string>();

                case "Int16":
                case "Int32":
                case "Int64":
                    return item.Value.GetAs<int>();

                case "Double":
                    return item.Value.GetAs<double>();

                case "Float":
                    return item.Value.GetAs<float>();

                case "Boolean":
                    return item.Value.GetAs<bool>();

                case "ECharacter":
                    return item.Value.GetAs<ECharacter>();
                case "ESpell":
                    return item.Value.GetAs<ESpell>();
                case "EBadge":
                    return item.Value.GetAs<EBadge>();
                case "EBadge[]":
                    return item.Value.GetAs<EBadge[]>();

                default:
                    ErrorHandler.Error("Unhandled type : " + expectedType);
                    return null;
            }
        }

        public async Task<bool> KeyExists(string key)
        {
            var keys = await CloudDatabase.ListAllKeysAsync();
            for (int i = 0; i < keys.Count; i++)
            {
                if (key == keys[i].Key)
                    return true;
            }

            return false;
        }

        #endregion


        #region Listeners

        protected virtual void OnCloudDataKeyLoaded(string key)
        {
            if (m_KeysToLoad.Contains(key))
                m_KeysToLoad.Remove(key);

            if (m_KeysToLoad.Count == 0)
                OnCloudDataLoadingCompleted();

            ErrorHandler.Log("Key Loaded : " + key, ELogTag.CloudData);
            //ErrorHandler.Log(TextHandler.ToString(m_Data[key]), ELogTag.CloudData);
        }

        protected virtual void OnCloudDataLoadingCompleted()
        {
            CheckData();
            LoadingCompleted = true;
        }

        #endregion


        #region Debug

        public override string ToString()
        {
            return TextHandler.ToString(m_Data);
        }

        #endregion
    }

}