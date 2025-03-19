using Enums;
using Inventory;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools.Debugs;
using Unity.Netcode;
using UnityEngine;

namespace Tools
{
    public class Debugger : MObject
    {
        #region Members

        static Debugger s_Instance;

        /// <summary> list of commands registered in the code </summary>
        List<SCommand>                                  m_Commands      = new();
        /// <summary> collection of classes currently alive and accessible threw the console </summary>
        List<object>                                    m_ClassesAlive  = new();
        /// <summary> dictionary of variables saved from the console </summary>
        Dictionary<string, object>                      m_Variables     = new();

        /// <summary> displayer of game perfs monitors </summary>
        PerformanceMonitor m_PerformanceMonitor;

        bool m_IsActivated => ProfileCloudData.Instance != null && ProfileCloudData.Instance.LoadingCompleted && ProfileCloudData.IsAdmin && PlayerPrefsHandler.GetDebug(EDebugOption.DebugMode);

        public static Debugger                          Instance            => s_Instance;
        public static PerformanceMonitor                PerformanceMonitor  => Instance.m_PerformanceMonitor;
        public static bool                              IsActivated         => Instance.m_IsActivated;
        public static List<SCommand>                    Commands            => Instance.m_Commands;
        public static List<object>                      ClassesAlive        => Instance.m_ClassesAlive;
        public static Dictionary<string, object>        Variables           => Instance.m_Variables;

        #endregion


        #region Init & End

        void Awake() 
        {
            if (s_Instance != null)
            {
                Destroy(gameObject);
                return;
            }

            // register instance
            s_Instance = this;

            // setup data
            m_Commands = new();

            // keep this debugger alive
            DontDestroyOnLoad(this); 

            // get and hide perf monitor
            m_PerformanceMonitor = Finder.FindComponent<PerformanceMonitor>(gameObject);
            m_PerformanceMonitor.gameObject.SetActive(PlayerPrefsHandler.GetDebug(EDebugOption.Monitor));
        }

        #endregion


        #region Updates

        private void Update()
        {
            // deactivate all key commands when the input field is currently selected
            if (ConsoleUI.Instance != null && ConsoleUI.InputFieldSelected)
                return;

            // check callback inputs
            foreach (SCommand sCommand in m_Commands)
            {
                // Only display console works when not activated
                if (! m_IsActivated && sCommand.keyCode != KeyCode.KeypadMinus)
                    continue;

                if (Input.GetKeyDown(sCommand.keyCode))
                    sCommand.callback.Invoke();
            }
        }

        #endregion


        #region GUI Manipulators


        #endregion


        #region Registering / Unregistering

        /// <summary>
        /// Check if a class is already registered
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public bool IsRegistered(object obj)
        {
            return m_ClassesAlive.Contains(obj);
        }

        /// <summary>
        /// Register a class and its public methods
        /// </summary>
        /// <param name="classType"></param>
        public void RegisterClass(object obj)
        {
            ErrorHandler.Log("RegisterClass " + obj.GetType().ToString(), ELogTag.Debugger);

            if (IsRegistered(obj))
            {
                ErrorHandler.Warning("Class " + obj.GetType().ToString() + " already registered");
                return;
            }

            // register class as alive
            m_ClassesAlive.Add(obj);

            // register specific commands
            RegisterClassCommands(obj);
        }

        /// <summary>
        /// Register a class and its public methods
        /// </summary>
        /// <param name="classType"></param>
        public void UnregisterClass(object obj, List<string> skips = default)
        {
            ErrorHandler.Log("UnregisterClass " + obj.GetType().ToString(), ELogTag.Debugger);
            if (! IsRegistered(obj))
                return;

            // register class as alive
            m_ClassesAlive.Remove(obj);

            // register specific commands
            UnregisterClassCommands(obj, skips);
        }

        /// <summary>
        /// Read all commands attributes of the class to register the command
        /// </summary>
        /// <param name="classType"> type of the class to register </param>
        public void RegisterClassCommands(object obj)
        {
            // Search for methods with CommandAttribute and register them as callbacks
            foreach (var method in obj.GetType().GetMethods())
            {
                var commandAttribute = (Command)Attribute.GetCustomAttribute(method, typeof(Command));
                if (commandAttribute == null)
                    continue;

                RegisterCommand((Action)Delegate.CreateDelegate(typeof(Action), obj, method), command: method.Name, key: commandAttribute.KeyCode);
            }
        }

        /// <summary>
        /// Remove all commands attributes of the class to register the command
        /// </summary>
        public void UnregisterClassCommands(object obj, List<string> skips = default)
        {
            // Search for methods with CommandAttribute and register them as callbacks
            foreach (var method in obj.GetType().GetMethods())
            {
                var commandAttribute = (Command)Attribute.GetCustomAttribute(method, typeof(Command));
                if (commandAttribute == null)
                    continue;

                // skip unregister if requested
                if (skips != null && skips.Contains(method.Name))
                    continue;

                UnregisterCommand(method.Name);
            }
        }

        /// <summary>
        /// Link a callback to a key or a prompt command
        /// </summary>
        /// <param name="callback"></param>
        /// <param name="command"></param>
        /// <param name="key"></param>
        public void RegisterCommand(Action callback, string command = "", KeyCode key = KeyCode.None)
        {
            ErrorHandler.Log("Registering command : " + command, ELogTag.Debugger);

            bool hasError = false;
            foreach (SCommand sCommand in m_Commands)
            {
                if (key != KeyCode.None && key == sCommand.keyCode)
                {
                    ErrorHandler.Error("Trying to register callback for key " + key + " but it is already taken (" + sCommand.name + ")");
                    hasError = true;
                }

                if (command != "" && command == sCommand.name)
                {
                    ErrorHandler.Error("Trying to register callback for command " + command + " but it is already taken");
                    hasError = true;
                }
            }

            // dont register on errror spotted
            if (hasError)
                return;

            m_Commands.Add(new SCommand(callback, command, key));
        }

        /// <summary>
        /// Remove a callback by name
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool UnregisterCommand(string command)
        {
            ErrorHandler.Log("Unregistering Command : " + command, ELogTag.Debugger);

            foreach (SCommand sCommand in m_Commands)
            {
                if (sCommand.name != command)
                    continue;

                m_Commands.Remove(sCommand);
                return true;
            }

            ErrorHandler.Warning("Callback " + command + " not found");
            return false;
        }

        /// <summary>
        /// Remove a callback by keycode
        /// </summary>
        /// <param name="command"></param>
        /// <returns></returns>
        public bool UnRegisterCommand(KeyCode key)
        {
            Debug.Log("Unregistering Key : " + key);

            foreach (SCommand sCommand in m_Commands)
            {
                if (sCommand.keyCode != key)
                    continue;

                m_Commands.Remove(sCommand);
                return true;
            }

            ErrorHandler.Warning("Callback for key " + key + " not found");
            return false;
        }

        #endregion


        #region Exectutions

        public void Execute(string execution)
        {
            // check registered commands
            if (ExecuteCommand(execution))
                return;

            // check if compiler can execute that code
            if (Compiler.ExecuteCode(execution, out object output))
            {
                if (output != null)
                    ConsoleUI.Log(output.ToString());
                return;
            }

            ErrorHandler.Warning("Bad input " + execution);
        }

        /// <summary>
        /// Execute a command
        /// </summary>
        /// <param name="command"></param>
        public bool ExecuteCommand(string command)
        {
            foreach (SCommand sCommand in m_Commands)
            {
                if (command.ToLower() == sCommand.name.ToLower())
                {
                    sCommand.callback?.Invoke();
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region Classes & Variables

        /// <summary>
        /// Get a class in list of alived classes
        /// </summary>
        /// <param name="className"></param>
        /// <returns></returns>
        public object GetClass(string className, ulong? clientId = null)
        {
            foreach(var obj in m_ClassesAlive)
            {
                if (className != GetClassName(obj))
                    continue;

                // if no client id or object is not of type NetworkBehavior : no reason to check matching client id
                if (clientId == null || obj is not NetworkBehaviour)
                    return obj;

                // check that owner is the requested client id
                if (clientId.Value == ((NetworkBehaviour)obj).OwnerClientId)
                    return obj;
            }

            return null;
        }

        /// <summary>
        /// Convert object type to string and extract class name
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public static string GetClassName(object myClass)
        {
            return GetClassName(myClass.GetType().ToString());
        }

        /// <summary>
        /// Name if last element of Type
        /// </summary>
        /// <param name="classType"></param>
        /// <returns></returns>
        public static string GetClassName(string classType)
        {
            return classType.Split('.')[^1];
        }

        /// <summary>
        /// Add a variable to dict of current variables
        /// </summary>
        /// <param name="varName"></param>
        /// <param name="value"></param>
        public static void AddVariable(string varName, object value)
        {
            if (HasVariable(varName))
                RemoveVariable(varName);

            Instance.m_Variables[varName] = value;
        }

        /// <summary>
        /// Remove a variable from dict of current variables
        /// </summary>
        /// <param name="varname"></param>
        public static void RemoveVariable(string varName)
        {
            if (! HasVariable(varName))
            {
                ErrorHandler.Warning("trying to remove variable " + varName + " but it was not found in dict of variables");
                return;
            }

            Instance.m_Variables.Remove(varName);
        }

        /// <summary>
        /// Check if has variable
        /// </summary>
        /// <param name="varName"></param>
        /// <returns></returns>
        public static bool HasVariable(string varName)
        {
            return Instance.m_Variables.ContainsKey(varName);
        }

        #endregion


        #region Default Callbacks

        /// <summary>
        /// Display all commands
        /// </summary>
        [Command]
        public void Help()
        {
            ConsoleUI.Log(" ");
            ConsoleUI.PrintSeparator();
            ConsoleUI.Log("Debugger Help : ");
            foreach (SCommand sCommand in m_Commands)
            {
                sCommand.Display();
            }
        }

        [Command(KeyCode.V)]
        public void ToggleCursor()
        {
            Cursor.visible = !Cursor.visible;
        }

        [Command]
        public void Monitors()
        {
            m_PerformanceMonitor.Toggle();
        }

        /// <summary>
        /// List all alive classes
        /// </summary>
        [Command]
        public void ListAlive()
        {
            ConsoleUI.PrintSeparator();
            ConsoleUI.Log("Listing all classes alive : ");
            int i = 0;
            foreach (var classAlive in m_ClassesAlive)
            {
                ConsoleUI.Log("     + " + GetClassName(classAlive));
                i++;
            }
        }

        /// <summary>
        /// List all alive classes
        /// </summary>
        [Command]
        public void ListVars()
        {
            ConsoleUI.Log("Listing all variables : ");
            string text = "";
            foreach (var item in m_Variables)
            {
                text += "\n     + " + item.Key + " " + TextHandler.TAG_ALIGNMENT + " : " +  item.Value;
            }

            ConsoleUI.Log(text);
        }


        #region Inventory

        [Command]
        public void Babylon()
        {
            InventoryManager.UpdateCurrency(ECurrency.Golds, 9999999, "DebugTool");
            InventoryManager.UpdateCurrency(ECurrency.Gems, 9999999, "DebugTool");
            InventoryManager.UpdateCurrency(ECurrency.TotalXp, 9999999, "DebugTool");
        }

        [Command]
        public void Xp100()
        {
            InventoryManager.UpdateCurrency(ECurrency.TotalXp, 100, "DebugTool");
        }

        [Command]
        public void Xp1000()
        {
            InventoryManager.UpdateCurrency(ECurrency.Xp, 1000, "DebugTool");
        }

        [Command]
        public void Xp10000()
        {
            InventoryManager.UpdateCurrency(ECurrency.Xp, 10000, "DebugTool");
        }

        [Command]
        public void Xp100000()
        {
            InventoryManager.UpdateCurrency(ECurrency.Xp, 100000, "DebugTool");
        }

        #endregion


        #region Boosts

        [Command(KeyCode.W)]
        public void ToggleChestSpeedBoost()
        {
            if (! TimeCloudData.HasBoost(EBoost.ChestSpeedBoost))
                TimeCloudData.AddBoost(EBoost.ChestSpeedBoost, 3600 * 48);
            else
                TimeCloudData.RemoveBoost(EBoost.ChestSpeedBoost);
        }

        #endregion


        #region Debug Achievements

        [Command]
        public void ResetAchievements()
        {
            ProfileCloudData.Instance.Reset(ProfileCloudData.KEY_ACHIEVEMENTS);
        }

        #endregion


        #region Debug Achivement Rewards

        [Command]
        public void UnlockAllAR()
        {
            // reset all before unlocking all (to avoid Error throws)
            ResetAllAR();

            UnlockAllAvatars();
            UnlockAllBorders();
            UnlockAllTitles();
            UnlockAllBadges();

            ProfileCloudData.Instance.Save();
        }

        [Command]
        public void ResetAllAR()
        {
            ProfileCloudData.Instance.Reset(ProfileCloudData.KEY_ACHIEVEMENT_REWARDS);
        
            // -- also reset current profile data to avoid settings not unlocked
            ProfileCloudData.Instance.Reset(ProfileCloudData.KEY_CURRENT_PROFILE_DATA);
        }

        [Command]
        public void UnlockAllAvatars()
        {
            foreach (EAvatar value in Enum.GetValues(typeof(EAvatar)))
            {
                if (value == EAvatar.None)
                    continue;

                ProfileCloudData.AddAchievementReward(EAchievementReward.Avatar, value.ToString(), false);
            }
        }

        [Command]
        public void UnlockAllBorders()
        {
            foreach (EBorder value in Enum.GetValues(typeof(EBorder)))
            {
                if (value == EBorder.None)
                    continue;

                ProfileCloudData.AddAchievementReward(EAchievementReward.Border, value.ToString(), false);
            }
        }

        [Command]
        public void UnlockAllTitles()
        {
            foreach (ETitle value in Enum.GetValues(typeof(ETitle)))
            {
                if (value == ETitle.None)
                    continue;

                ProfileCloudData.AddAchievementReward(EAchievementReward.Title, value.ToString(), false);
            }
        }

        [Command]
        public void UnlockAllBadges()
        {
            foreach (EBadge value in Enum.GetValues(typeof(EBadge)))
            {
                if (value == EBadge.None)
                    continue;

                foreach (ELeague league in Enum.GetValues(typeof(ELeague)))
                {
                    string badgeName = ProfileCloudData.BadgeToString(value, league);

                    // check icon exists
                    if (AssetLoader.LoadBadgeIcon(badgeName) == null)
                        continue;

                    ProfileCloudData.AddAchievementReward(EAchievementReward.Badge, badgeName, false);
                }

            }
        }

        #endregion


        #region Errors

        [Command]
        public void DisplayErrors()
        {
            if (ErrorHandler.Errors.Count == 0)
            {
                ConsoleUI.Log("No error registered");
                return;
            }

            foreach (Error error in ErrorHandler.Errors)
            {
                ConsoleUI.Log(error.Message);
            }
        }

        [Command]
        public void LastErrorTrace()
        {
            if (ErrorHandler.Errors.Count == 0)
            {
                ConsoleUI.Log("No error registered");
                return;
            }

            ConsoleUI.Log(ErrorHandler.Errors.Last().GetTraceString());
        }

        [Command]
        public void PrintBuilds()
        {
            ConsoleUI.Log(CharacterBuildsCloudData.Instance.ToString());
        }

        [Command]
        public void PrintInventory()
        {
            ConsoleUI.Log(InventoryCloudData.Instance.ToString());
        }


        #endregion


        #endregion
    }
}