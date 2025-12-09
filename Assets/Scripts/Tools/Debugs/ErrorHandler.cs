using Assets;
using Enums;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Tools
{
    public static class ErrorHandler
    {
        #region Members

        /// <summary> Is Error Handler activated ? </summary>
        public static bool IsActivated = false;
        /// <summary> check if application is currently closing </summary>
        public static bool IsExiting = false;
        /// <summary> list of errors </summary>
        public static List<Error> Errors = new List<Error>();

        public static Dictionary<string, int> RegisteredObjects = new ();

        #endregion


        #region Init & End

        public static void Toggle()
        {
            PlayerPrefsHandler.SetDebug(EDebugOption.ErrorHandler, !IsActivated);
            IsActivated = !IsActivated;
        }

        #endregion


        #region Basic Errors

        public static void Log(Func<string> messageFactory, ELogTag logTag = ELogTag.None, int frame = 0)
        {
            if (! IsActivated)
                return;

            if (! ShouldDisplayLogTag(logTag))
                return;

            Log(messageFactory(), logTag, frame + 1);
        }

        public static void Log(string message, ELogTag logTag = ELogTag.None, int frame = 0)
        {
            AddError(message, EError.Log, frame + 1, logTag);
        }

        public static void Warning(string message, int frame = 0)
        {
            if (!IsActivated)
                return;
            AddError(message, EError.Warning, frame + 1);
        }

        public static void Error(string message, int frame = 0)
        {
            if (!IsActivated)
                return;
            AddError(message, EError.Error, frame + 1);
        }

        public static void FatalError(string message, int frame=0)
        {
            if (!IsActivated)
                return;
            AddError(message, EError.FatalError, frame + 1);
        }

        #endregion


        #region Specific Errors

        /// <summary>
        /// Add an error of type "Null Object" to the stack
        /// </summary>
        /// <param name="message"></param>
        public static void NullObject(string message = "")
        {
            AddError("Null object" + (message != "" ? " : " + message : ""), EError.Error);
        }

        /// <summary>
        /// Add an error of type "Array Size" to the stack
        /// </summary>
        /// <param name="message"></param>
        public static void ArraySize(string message)
        {
            AddError("Array size error" + (message != "" ? " : " + message : ""), EError.Error);
        }

        #endregion


        #region Error Stack Management

        /// <summary>
        /// Add an error to the stack
        /// </summary>
        /// <param name="error"></param>
        static void AddError(string message, EError type = EError.Error, int frame = 0, ELogTag logTag = ELogTag.None)
        {
            Errors.Add(new Error(message, type, frame+1, logTag));
        }

        /// <summary>
        /// Reset all errors
        /// </summary>
        public static void Reset()
        {
            Errors = new List<Error>();
        }

        #endregion


        #region Error LogTag

        public static bool ShouldDisplayLogTag(ELogTag logTag)
        {
            if (logTag == ELogTag.None)
                return true;

            if (Main.LogTags.Contains(ELogTag.All))
                return true;

            if (Main.LogTags.Contains(logTag))
                return true;

            // CHECK that contains GLOBAL GROUPS log tag
            int groupValue = 100 * (int)Math.Floor((int)logTag / 100f);
            if (groupValue == 0)
                return false;

            // CHECK that this log tag has a valid LogTag for the group AND that this log is allowed
            if (Enum.IsDefined(typeof(ELogTag), groupValue))
            {
                return Main.LogTags.Contains((ELogTag)groupValue);
            }

            return false;
        }

        #endregion


        #region Object & Method & Events registration

        public static void Register(string context)
        {
            if (! RegisteredObjects.ContainsKey(context))
            {
                RegisteredObjects[context] = 0;
            }

            RegisteredObjects[context]++;

            Debug.Log(" ++ Register : " + context);
        }

        public static void Unregister(string context)
        {
            if (! RegisteredObjects.ContainsKey(context))
            {
                Warning("Trying to unregister context that is not registered : " + context);
                return;
            }

            RegisteredObjects[context]--;

            if (RegisteredObjects[context] == 0)
                RegisteredObjects.Remove(context);

            Debug.Log(" -- Unregister : " + context);
        }

        #endregion
    }
}