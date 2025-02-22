using Save;
using TMPro;
using Tools;
using UnityEngine.UI;


namespace DevTools
{
    public class ArenaDevTool : MObject
    {
        #region Members

        TMP_InputField m_LevelInputField;
        TMP_InputField m_StageInputField;
        Button m_ValidateButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_LevelInputField = Finder.FindComponent<TMP_InputField>(gameObject, "IF_Level");
            m_StageInputField = Finder.FindComponent<TMP_InputField>(gameObject, "IF_Stage");
            m_ValidateButton = Finder.FindComponent<Button>(gameObject, "ValidateButton");
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();
            
            // make sure that is is inactive at start
            gameObject.SetActive(false);

            // ADMIN ONLY
            if (ProfileCloudData.IsAdmin)
            {
                // only display with current arena in progress
                if (!ProgressionCloudData.HasArenaInProgress)
                    return;

                gameObject.SetActive(true);
                RefreshInputFields();
            }
        }

        void RefreshInputFields()
        {
            m_LevelInputField.text = ProgressionCloudData.CurrentArena.Level.ToString();
            m_StageInputField.text = ProgressionCloudData.CurrentArena.Stage.ToString();
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_ValidateButton.onClick.AddListener(OnValidateButtonClicked);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_ValidateButton.onClick.RemoveAllListeners();
        }

        void OnValidateButtonClicked()
        {
            if (! int.TryParse(m_LevelInputField.text, out int level))
            {
                ErrorHandler.Error("Unable to parse " + m_LevelInputField.text + " as int");
                return;
            }

            if (! int.TryParse(m_StageInputField.text, out int stage))
            {
                ErrorHandler.Error("Unable to parse " + m_LevelInputField.text + " as int");
                return;
            }

            ProgressionCloudData.UpdateCurrentArena(level: level, stage: stage);

            // refresh values (if wrong data provided, it will set value back to allowed values)
            RefreshInputFields();
        }

        #endregion
    }
}
