using Data.DataStructures.SpellSubStructures;
using Enums;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data
{
    public class CollectableData : ScriptableObject
    {
        #region Members
        [SerializeField, FormerlySerializedAs("Rarety")] protected ERarety m_Rarety;

        // ===================================================================================================
        // Protected Serialize Data
        [SerializeField, TextArea(minLines: 1, maxLines: 5)]
        protected string m_Description = "";

        [SerializeField, Tooltip("List of Element types of this collectable")]
        protected List<ESpellElement> m_SpellElements;

        // ===================================================================================================
        // Private Data
        protected int m_Level = 1;
        protected virtual Type m_EnumType => null;
        public string BaseDescription => m_Description;
       
        // ===================================================================================================
        // Dependent Data
        public int Level => m_Level;
        public List<ESpellElement>  SpellElements => m_SpellElements;
        public virtual ERarety Rarety => m_Rarety;
    
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

        }

        #endregion


        #region Levels

        public virtual CollectableData Clone(int level = 0, bool destroy = false)
        {
            CollectableData clone = Instantiate(this);
            clone.name = Name;
            
            if (destroy)
                CoroutineManager.DelayMethod(() => Destroy(clone));

            if (level != 0)
                clone.SetLevel(level);

            return clone;
        }

        public virtual void SetLevel(int level)
        {
            m_Level = level;
        }

        #endregion


        #region Infos

        public virtual Dictionary<string, object> GetInfo()
        {
            return new Dictionary<string, object>();
        }

        /// <summary>
        /// Get Description info of the StateEffect
        /// </summary>
        /// <returns></returns>
        public virtual string GetDescription()
        {
            string description = TextHandler.ReplaceProperties(m_Description, GetInfo(), this);
            description = TextHandler.ReplaceStateEffectTokens(description);
            description = TextHandler.ReplaceKeyWords(description);

            return description;
        }

        /// <summary>
        /// Check if provided property is scaling or not
        /// </summary>
        /// <returns></returns>
        public virtual bool IsScalingProperty(string property, out EScalingDirection scaling)
        {
            scaling = EScalingDirection.None;
            return false;
        }

        /// <summary>
        /// Convert a description variable into a string implemented into the description
        /// </summary>
        /// <returns></returns>
        public virtual string ConvertDescriptionVariable(SDescriptionVariable descriptionVariable, Dictionary<string, object> infos, bool throwError = true)
        {
            // PROPERTY of the CollectableData
            if (infos.ContainsKey(descriptionVariable.Name))
            {
                string value = infos[descriptionVariable.Name].ToString();
                if (float.TryParse(value, out float floatValue))
                    value = TextHandler.FormatPropertyValue(floatValue, descriptionVariable.Name);

                IsScalingProperty(descriptionVariable.Name, out EScalingDirection scaling);
                return TextHandler.FormatPropertyIcon(descriptionVariable.Name, value, descriptionVariable.WithIcon, withPropertyName: false, scaling: scaling);
            }

            // STATE EFFECT (Curse, Frozen, ...) : replace by the name + the icon of the state effect
            if (Enum.TryParse(descriptionVariable.Name, out EStateEffect _))
            {
                return TextHandler.FormatStateEffectIcon(descriptionVariable.Name, descriptionVariable.WithIcon);
            }
            
            if (throwError)
                ErrorHandler.Error("Unable to find property " + descriptionVariable.Name + " in info dict of spell " + Name);

            return TextHandler.UNDEFINED;
        }

        #endregion


        #region Dev Tools

        /// <summary>
        /// DevTool method to make changes in the description from Editor
        /// </summary>
        /// <param name="description"></param>
        public void OverrideDescription(string description) 
        { 
            m_Description = description;
        }

        #endregion
    }
}