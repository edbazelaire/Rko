using Enums;
using System;
using System.Collections.Generic;
using Tools;

namespace Menu.PopUps
{
    public class BossInfoPopUp : CharacterInfoPopUp
    {
        #region Members

        // =========================================================================================
        // Data
        List<string> m_ExtraAbilities = new List<string>();

        // =========================================================================================
        // Dependent Members
        protected override bool m_IsUnlocked        => false;
        protected override bool m_IsMaxedLevel      => false;
        protected override bool m_CanUpgrade        => false;
        protected override bool m_CanBuy            => false;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();
        }

        public void Initialize(string charName, int level, List<string> extraAbilities = default)
        {
            if (extraAbilities != null)
                m_ExtraAbilities = extraAbilities;

            Enum enumValue = null;
            if (Enum.TryParse(charName, out EBoss boss))
                enumValue = boss;
            else if (Enum.TryParse(charName, out ESpawn spawn))
                enumValue = spawn;
            else if (Enum.TryParse(charName, out ECharacter character))
                enumValue = character;
            else
            {
                ErrorHandler.Error("Unable to parse " + charName + " as character");
                Exit();
                return;
            }

            base.Initialize(enumValue, level, true);
        }

        protected override void OnPrefabLoaded()
        {
            base.OnPrefabLoaded();
        }

        #endregion


        #region UIManipulators

        protected override void SetUpTitle()
        {
            base.SetUpTitle();

            m_Title.text += $" (Level {m_Level})";
        }

        protected override void SetUpPreview()
        {
            UIHelper.CleanContent(m_PreviewContainer);
            CoroutineManager.DelayMethod(() => UIHelper.SpawnCharacter(m_CharacterData.Name, m_PreviewContainer));
        }


        #endregion


        #region Helpers

        protected override void LoadTemplateItem() { }

        protected override List<string> GetAbilities()
        {
            var abilities = base.GetAbilities();
            abilities.AddRange(m_ExtraAbilities);
            return abilities;
        }

        #endregion
    }
}