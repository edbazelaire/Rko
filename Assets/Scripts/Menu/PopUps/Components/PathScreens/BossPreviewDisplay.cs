using Enums;
using Menu.Common.Buttons;
using System.Collections;
using Tools;
using UnityEngine;
using UnityEngine.UI;

namespace Menu.PopUps
{
    public class BossPreviewDisplay : MObject
    {
        #region Members

        // ============================================================
        // Data
        EBoss m_Boss;
        int m_Level;

        // ============================================================
        // Components
        GameObject m_CharacterPreviewContainer;
        Button m_CharacterInfoButton;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CharacterPreviewContainer = Finder.Find(gameObject, "CharacterPreviewContainer");
            m_CharacterInfoButton       = Finder.FindComponent<Button>(gameObject, "CharacterInfoButton");
        }

        public virtual void Initialize(EBoss boss, int level)
        {
            m_Boss = boss;
            m_Level = level;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            UIHelper.CleanContent(m_CharacterPreviewContainer);
            CoroutineManager.DelayMethod(() => UIHelper.SpawnCharacter(m_Boss.ToString(), m_CharacterPreviewContainer));
        }

        #endregion


        #region GUI Manipulators

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();

            m_CharacterInfoButton.onClick.AddListener(OnClickCharacterInfoButton);
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();

            m_CharacterInfoButton.onClick.RemoveListener(OnClickCharacterInfoButton);
        }

        void OnClickCharacterInfoButton()
        {
            Debug.Log("TODO : OnClickCharacterInfoButton()");
        }

        #endregion
    }

}

