using Menu.MainMenu;
using Save;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Assets.Scripts.UI
{
    public class DebugTabContent : TabContent
    {
        #region Members

        List<EDebugOption> adminOptions = new List<EDebugOption>() { EDebugOption.ErrorHandler, EDebugOption.Console };

        DebugOptionUI m_TemplateDebugOptionUI;
        GameObject m_Content;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_TemplateDebugOptionUI = AssetLoader.Load<DebugOptionUI>("DebugOption", AssetLoader.c_SettingsPath);
            m_Content = gameObject;
        }


        protected override void SetUpUI()
        {
            base.SetUpUI();

            UIHelper.CleanContent(m_Content);
            foreach (EDebugOption option in Enum.GetValues(typeof(EDebugOption)))
            {
                if (adminOptions.Contains(option) && !ProfileCloudData.IsAdmin)
                    continue;

                (Action onActivate, Action onDestroy) = GetCallbacks(option);
                var template = Instantiate(m_TemplateDebugOptionUI, m_Content.transform);
                template.Initialize(option, onActivate, onDestroy);
            }
        }

        #endregion


        #region GUI Manipulators

        public override void Activate(bool activate)
        {
            gameObject.SetActive(activate);
        }

        #endregion


        #region Helpers

        (Action, Action) GetCallbacks(EDebugOption option)
        {
            switch (option)
            {
                case EDebugOption.Console:
                    return (() => ConsoleUI.Instance.Hide(), () => Destroy(ConsoleUI.Instance.gameObject));

                case EDebugOption.Monitor:
                    return (() => Debugger.PerformanceMonitor.Toggle(), () => Destroy(Debugger.PerformanceMonitor.gameObject));

                case EDebugOption.ErrorHandler:
                    return (() => ErrorHandler.Toggle(), () => ErrorHandler.Reset());

                default:
                    ErrorHandler.Error("Unhandled case : " +  option);  
                    return (() => Debug.Log("Not attributed"), () => Debug.Log("Not attributed"));
            }
        }

        #endregion


        #region Listeners


        #endregion
    }
}