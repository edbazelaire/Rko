using Assets.Scripts.Managers;
using Enums;
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
        ESkin m_Skin;
        int m_Level;

        // ============================================================
        // Components
        GameObject m_CharacterPreviewContainer;
        Button m_Button;
        public Button Button => m_Button;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_CharacterPreviewContainer = Finder.Find(gameObject, "CharacterPreviewContainer");
            m_Button                    = Finder.FindComponent<Button>(gameObject, "ButtonFront");
        }

        public virtual void Initialize(EBoss boss, ESkin skin, int level)
        {
            m_Boss = boss;
            m_Skin = skin;
            m_Level = level;

            base.Initialize();
        }

        protected override void SetUpUI()
        {
            base.SetUpUI();

            UIHelper.CleanContent(m_CharacterPreviewContainer);
            CoroutineManager.DelayMethod(() => UIHelper.SpawnCharacter(m_Boss.ToString(), m_Skin, m_CharacterPreviewContainer));
        }

        #endregion
    }

}

