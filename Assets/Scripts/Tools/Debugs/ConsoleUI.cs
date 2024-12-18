using System.Collections.Generic;
using TMPro;
using Tools.Debugs;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Tools
{
    public class ConsoleUI : MObject
    {
        #region Members

        static ConsoleUI s_Instance;

        const string    c_ScrollView        = "ScrollView";
        const string    c_ConsoleText       = "ConsoleText";
        const string    c_InputField        = "InputField";

        Button          m_ScrollViewButton;
        TMP_Text        m_ConsoleText;
        TMP_InputField  m_InputField;

        bool            m_IsInitialized;
        List<SLog>      m_Logs              = new();
        List<string>    m_InputsStack       = new();
        int             m_InputStacksIndex  = -1;    
        int             m_MaxCharsInLine    = 25;

        #endregion


        #region Init & End

        /// <summary>
        /// Initialize components & listeners
        /// </summary>
        public override void Initialize()
        {
            // Init Components
            m_ScrollViewButton = Finder.FindComponent<Button>(c_ScrollView);
            m_ConsoleText = Finder.FindComponent<TMP_Text>(c_ConsoleText);
            m_InputField = Finder.FindComponent<TMP_InputField>(c_InputField);

            // Setup Listeners
            Application.logMessageReceived += OnLog;
            m_ScrollViewButton.onClick.AddListener(SelectInputField);
            m_InputField.onSubmit.AddListener(SendInput);

            // deactivate game object 
            gameObject.SetActive(false);
            m_IsInitialized = true;
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            if (!m_IsInitialized)
                return;

            Application.logMessageReceived -= OnLog;
            m_ScrollViewButton.onClick.RemoveAllListeners();
            m_InputField.onSubmit.RemoveAllListeners();
        }

        #endregion


        #region Updates

        void Update()
        {
            if (!gameObject.activeInHierarchy || !m_IsInitialized)
                return;

            // check Escape
            if (Input.GetKeyDown(KeyCode.Escape))
                DeselectInputField();

            // check press Space
            if (Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                SelectInputField();

            // these only work if input field is selected
            if (!InputFieldSelected)
                return;

            // check last / next input
            if (Input.GetKeyDown(KeyCode.UpArrow))
                LastInput();

            if (Input.GetKeyDown(KeyCode.DownArrow))
                NextInput();
        }

        #endregion


        #region Console Management

        /// <summary>
        /// Log a value
        /// </summary>
        public static void Log(string logString, LogType logType = LogType.Log)
        {
            Instance.m_ConsoleText.text += "\n" + TextHandler.Clean(TextWrapper(logString, logType));
        }

        /// <summary>
        /// Display last input
        /// </summary>
        void LastInput()
        {
            if (m_InputsStack.Count == 0)
                return;

            if (m_InputStacksIndex < m_InputsStack.Count - 1)
                m_InputStacksIndex++;

            SetInputField(m_InputsStack[m_InputStacksIndex]);
        }

        /// <summary>
        /// Display next input
        /// </summary>
        void NextInput()
        {
            if (m_InputStacksIndex >= 0)
                m_InputStacksIndex--;

            SetInputField(m_InputStacksIndex >= 0 ? m_InputsStack[m_InputStacksIndex] : "");
        }

        /// <summary>
        /// Set value of the inputfield and set cursor at the end
        /// </summary>
        /// <param name="value"></param>
        void SetInputField(string value)
        {
            m_InputField.text = value;
            m_InputField.caretPosition = m_InputField.text.Length;

        }

        /// <summary>
        /// Display all commands
        /// </summary>
        [Command]
        public void Clear()
        {
            m_ConsoleText.text = "";
        }

        /// <summary>
        /// Display all commands
        /// </summary>
        [Command]
        public void PrintAllLogs()
        {
            foreach (SLog slog in m_Logs)
            {
                Log(slog.Log, slog.LogType);
            }
        }

        [Command(KeyCode.KeypadMinus)]
        public void Hide()
        {
            gameObject.SetActive(!gameObject.activeInHierarchy);
        }

        #endregion


        #region Input Field

        /// <summary>
        /// 
        /// </summary>
        void DeselectInputField()
        {
            if (!InputFieldSelected)
                return;

            EventSystem.current.SetSelectedGameObject(null);
        }

        /// <summary>
        /// Select input field if not done
        /// </summary>
        void SelectInputField()
        {
            if (InputFieldSelected)
                return;

            m_InputField.Select();
            m_InputField.ActivateInputField();
        }

        /// <summary>
        /// Send console input to console text and 
        /// </summary>
        void SendInput(string text)
        {
            if (!InputFieldSelected && m_InputField.text == "")
                return;

            text = text.Trim();

            // display input
            Log(" > " + text);

            if (text != "")
            {
                // save input
                m_InputsStack.Insert(0, text);

                // call for command execution
                Debugger.Instance.Execute(text);
            }

            // reset content & index of the stack input
            m_InputField.text = "";
            m_InputStacksIndex = -1;
        }

        #endregion


        #region GUI Manipulators

        void RefreshConsoleData()
        {
            // Get the preferred values for the TMP_Text component
            TMP_TextInfo textInfo = m_ConsoleText.textInfo;
            float lineWidth = m_ConsoleText.rectTransform.rect.width;

            // Get the index of the first character of the last line
            int lastLineIndex = textInfo.lineCount - 1;

            // Get the width of the last line
            float lastLineWidth = textInfo.lineInfo[lastLineIndex].lineExtents.max.x - textInfo.lineInfo[lastLineIndex].lineExtents.min.x;

            // Calculate the maximum number of characters that can fit in one line
            m_MaxCharsInLine = Mathf.FloorToInt((lastLineWidth / lineWidth) * textInfo.characterCount);
        }

        #endregion


        #region Messages

        public static string TextWrapper(string log, LogType logLevel)
        {
            string color = "";

            switch (logLevel)
            {
                case LogType.Log:
                    return log;

                case LogType.Warning:
                    color = "FB9700";
                    break;

                default:
                    color = "FA1B00";
                    break;
            }

            return $"<color=#{color}>{log}</color>";
        }

        public static void PrintSeparator(string symbol = "=", int n = 0)
        {
            if (n <= 0)
                n = Instance.m_MaxCharsInLine;

            ConsoleUI.Log( string.Concat(System.Linq.Enumerable.Repeat(symbol, n)) );
        }

        #endregion


        #region Dependent Members

        public static ConsoleUI Instance
        {
            get
            {
                if (s_Instance == null)
                {
                    s_Instance = FindFirstObjectByType<ConsoleUI>();
                    if (s_Instance == null)
                        return null;
                    
                    s_Instance.Initialize();
                }

                return s_Instance;
            }
        }

        public static bool InputFieldSelected
        {
            get
            {
                return Instance.m_IsInitialized
                    && Instance.gameObject.activeInHierarchy
                    && Instance.m_InputField.isFocused;
            }
        }

        #endregion


        #region Listeners

        /// <summary>
        /// When a log is received, display it (or store it)
        /// </summary>
        /// <param name="logString"></param>
        /// <param name="stackTrace"></param>
        /// <param name="type"></param>
        void OnLog(string logString, string stackTrace, LogType logType)
        {
            m_Logs.Add(new SLog(logString, stackTrace, logType));
            Log(logString, logType);
        }

        #endregion  
    }
}