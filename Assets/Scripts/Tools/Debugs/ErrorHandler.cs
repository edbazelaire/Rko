using Enums;
using System.Collections.Generic;

namespace Tools
{
    public static class ErrorHandler
    {
        #region Members

        /// <summary> Is Error Handler activated ? </summary>
        public static bool IsActivated => PlayerPrefsHandler.GetDebug(EDebugOption.ErrorHandler);
        /// <summary> check if application is currently closing </summary>
        public static bool IsExiting = false;
        /// <summary> list of errors </summary>
        public static List<Error> Errors = new List<Error>();

        #endregion


        #region Init & End

        public static void Toggle()
        {
            PlayerPrefsHandler.SetDebug(EDebugOption.ErrorHandler, !IsActivated);

            if (!IsActivated)
                Reset();
        }

        #endregion


        #region Basic Errors

        public static void Log(string message, ELogTag logTag = ELogTag.None, int frame = 0)
        {
            if (!IsActivated)
                return;
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
    }
}