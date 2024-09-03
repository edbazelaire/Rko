using Enums;
using Game.Loaders;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data
{
    public class CollectableData : ScriptableObject
    {
        #region Members

        public ERarety Rarety;

        // ===================================================================================================
        // Private Data
        protected int m_Level = 1;
        protected virtual Type m_EnumType => null;
       
        // ===================================================================================================
        // Dependent Data
        public int Level => m_Level;

        public string Name
        {
            get
            {
                string myName = name;
                if (myName.EndsWith("(Clone)"))
                    myName = myName[..^"(Clone)".Length];

                return myName;
            }
        }

        public Enum Id
        {
            get
            {
                if (m_EnumType == null)
                {
                    ErrorHandler.Error("EnumType not defined in " + name);
                    return null;
                }

                string[] enumNames = Enum.GetNames(m_EnumType);
                int i = 0;
                foreach (var val in Enum.GetValues(m_EnumType))
                {
                    if (Name == enumNames[i])
                        return (Enum)val;
                    i++;
                }

                ErrorHandler.Error("Unable to parse enum for : " + Name);
                return null;
            }
        }

        #endregion


        #region End & Destroy

        protected virtual void OnDestroy()
        {
            //if (ErrorHandler.IsExiting)
            //    ErrorHandler.Error("Unhandled Destroy() Data : " + Name);
        }

        #endregion


        #region Levels

        public virtual CollectableData Clone(int level = 0, bool destroy = false)
        {
            CollectableData clone = Instantiate(this);
            if (level == 0)
                return clone;

            clone.SetLevel(level);
            clone.name = Name;

            if (destroy)
                CoroutineManager.DelayMethod(() => Destroy(clone));

            return clone;
        }

        protected virtual void SetLevel(int level)
        {
            m_Level = level;
        }

        #endregion


        #region Infos

        public virtual Dictionary<string, object> GetInfos()
        {
            return new Dictionary<string, object>();
        }

        #endregion
    }
}