using Assets;
using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Tools;
using UnityEngine.Networking;

namespace Save.RSDs
{
    public struct SSheetsData
    {
        public string range;
        public string majorDimension;
        public List<List<string>> values;
    }

    public class RSDData
    {
        #region Members

        protected bool m_IsAborted = false;

        #endregion


        #region Init & End

        public RSDData() { }

        public void Initialize(List<string> keys, List<string> values)
        {
            for (int i = 0; i < keys.Count; i++)
            {
                SetProperty(keys[i], i < values.Count ? values[i] : "");
            }
        }

        #endregion


        #region Error Management

        public bool IsAborted()
        {
            return m_IsAborted;
        }

        #endregion


        #region Reflection Methods

        /// <summary>
        /// Get Reflection PropertyInfo of desire StateEffect property
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected bool TryGetPropertyInfo(string property, out FieldInfo propertyInfo, bool throwError = true)
        {
            // Get the PropertyInfo object for the provided property
            propertyInfo = this.GetType().GetField(property);

            // check if the property exists
            if (propertyInfo == null)
            {
                if (throwError)
                    ErrorHandler.Error("Unknown property " + property + " for RSDData " + this.GetType());
                return false;
            }

            return true;
        }

        /// <summary>
        /// Set the value of a property by Reflection
        /// </summary>
        /// <param name="property"></param>
        /// <param name="value"></param>
        protected virtual void SetProperty(string property, string value, bool throwError = true)
        {
            if (!TryGetPropertyInfo(property, out FieldInfo propertyInfo, throwError))
            {
                if (throwError)
                    m_IsAborted = true;
                return;
            }

            if (propertyInfo.FieldType == typeof(int))
            {
                if (!int.TryParse(value, out int result))
                {
                    ErrorHandler.Error("Unable to parse " + value + " as int");
                    m_IsAborted = true;
                    return;
                }

                propertyInfo.SetValue(this, result);
            }

            else if(propertyInfo.FieldType == typeof(float))
            {
                if (!float.TryParse(value, out float result))
                {
                    ErrorHandler.Error("Unable to parse " + value + " as float");
                    m_IsAborted = true;
                    return;
                }

                propertyInfo.SetValue(this, result);
            }

            else if(propertyInfo.FieldType == typeof(bool))
            {
                if (!bool.TryParse(value, out bool result))
                {
                    ErrorHandler.Error("Unable to parse " + value + " as bool");
                    m_IsAborted = true;
                    return;
                }

                propertyInfo.SetValue(this, result);
            }

            else if (propertyInfo.FieldType == typeof(string))
            {
                propertyInfo.SetValue(this, value);
            }

            else
            {
                ErrorHandler.Error("Unhandled case : " + propertyInfo.FieldType);
                m_IsAborted = true;
                return;
            }
        }

        #endregion
        
    }

    public class RSD
    {
        #region Members

        public static RSD Instance => RSDManager.GetRSD<RSD>();

        bool m_LoadingCompleted = false;

        const string                API_KEY        = "AIzaSyDaxXaNw8fIOdB0hU2JTizoSiDnmx6ZZO8";
        protected virtual string    m_SheetId      => "";
        protected virtual string    m_SheetName    => "";

        public virtual List<TokenData> Data { get; set; }
        string m_SheetUrl => "https://sheets.googleapis.com/v4/spreadsheets/" + m_SheetId + "/values/" + m_SheetName + "?alt=json&key=" + API_KEY;

        public bool LoadingCompleted => m_LoadingCompleted;

        #endregion


        #region Init & End

        public RSD()
        {
            m_LoadingCompleted = false;
            Main.Instance.StartCoroutine(LoadData(m_SheetUrl));
        }

        #endregion


        #region Loading & Saving

        /// <summary>
        /// Load data from the sheets URL, read, format and store them in Data
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public IEnumerator LoadData(string url)
        {
            UnityWebRequest request;
            int retryAttempt = 0;
            do
            {
                retryAttempt++;
                request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success)
                {
                    // error 
                    ErrorHandler.Warning(request.result + " - Attempt (" + retryAttempt + ") : Unable to load RSD " + this.GetType() + " at " + url);

                    yield return null;
                }
            } while (request.result != UnityWebRequest.Result.Success && retryAttempt <= 3);

            if (request.result != UnityWebRequest.Result.Success)
            {
                // error 
                ErrorHandler.Error(request.result + " : Unable to load RSD " + this.GetType() + " at " + url);
                yield break;
            }

            SSheetsData data = JsonConvert.DeserializeObject<SSheetsData>(request.downloadHandler.text);

            ReadSheetsData(data);

            m_LoadingCompleted = true;
        }

        /// <summary>
        /// Must be override by the child to provide appropriated RSDData
        /// </summary>
        /// <param name="data"></param>
        protected virtual void ReadSheetsData(SSheetsData data) { Data = FormatSheetData<TokenData>(data); }

        /// <summary>
        /// Format sheet data to the format of provided Type
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="sheetData"></param>
        /// <returns></returns>
        protected List<T> FormatSheetData<T>(SSheetsData sheetData) where T : RSDData, new ()
        {
            List<T> formattedData = new List<T>();
            for (int i = 1; i < sheetData.values.Count; i++)
            {
                T data = new T();
                data.Initialize(sheetData.values[0], sheetData.values[i]);

                if (data.IsAborted())
                    continue;

                formattedData.Add(data);
            }

            return formattedData;
        }

        #endregion
    }
}