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
        // Protected Serialize Data
        [SerializeField] protected string m_Description = "";
        [SerializeField] protected List<SDescriptionVariable> m_DescriptionVariables = new List<SDescriptionVariable>();

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

        /// <summary>
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            List<string> values = new List<string>();
            var infos = GetInfos();

            foreach (SDescriptionVariable descriptionVariable in m_DescriptionVariables)
            {
                values.Add(ConvertDescriptionVariable(descriptionVariable, infos));
            }

            return string.Format(m_Description, values.ToArray());
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public virtual string ConvertDescriptionVariable(SDescriptionVariable descriptionVariable, Dictionary<string, object> infos = default)
        {
            if (Enum.TryParse(descriptionVariable.Name, out EStateEffect _))
            {
                return TextHandler.FormatStateEffectIcon(descriptionVariable.Name, descriptionVariable.WithIcon);
            }

            if (infos.ContainsKey(descriptionVariable.Name))
            {
                string value = infos[descriptionVariable.Name].ToString();
                if (float.TryParse(value, out float floatValue))
                    value = TextHandler.FormatPropertyValue(floatValue, descriptionVariable.Name);

                string iconTag = descriptionVariable.WithIcon ? $" <sprite name=\"{"Ic_" + descriptionVariable.Name}\">" : "";
                return $"<b>{value}</b>{iconTag}";
            }

            ErrorHandler.Error("Unable to find property " + descriptionVariable.Name + " in info dict of spell " + Name);
            return "<b>UNDEFINED</b>";
        }

        #endregion
    }
}