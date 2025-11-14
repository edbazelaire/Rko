using Assets.Scripts.Managers;
using Data.GameManagement;
using Enums;
using Tools;
using UnityEngine;
using UnityEngine.UI;


namespace Menu.MainMenu.MainTab
{
    public class StageSection_EM : ArenaStageSectionUI
    {
        #region Members

        [SerializeField] GameObject m_SpawnIconPrefab;

        EternalMenagerieData m_EternalMenagerieData => m_ArenaData as EternalMenagerieData;

        #endregion


        #region Path Display

        protected override void ResetPathDisplay()
        {
            UIHelper.CleanContent(m_PathDisplayContainer);

            var spawnList = m_EternalMenagerieData.GetWavesAt(m_Level).GetListOfSpawns();
            foreach (ESpawn spawn in spawnList)
            {
                // create icon and load the icon
                var spawnIcon = Instantiate(m_SpawnIconPrefab, m_PathDisplayContainer.transform);
                var icon = Finder.FindComponent<Image>(spawnIcon, "Icon");
                icon.sprite = AssetLoader.LoadIcon(spawn);

                // setup info button
                var button = spawnIcon.AddComponent<Button>();
                button.onClick.AddListener(() => ScreenManager.SetPopUp(
                    EPopUpState.BossInfoPopUp, 
                    spawn.ToString(), 
                    ESkin.None,
                    m_ArenaData.GetBaseCharacterLevel(m_ArenaData.ArenaDifficulty, m_Level)
                ));

            }
        }

        #endregion


        #region Helpers

        protected override string GetLevelString()
        {
            return "Phase " + TextHandler.ToRoman(m_Level + 1);
        }

        #endregion


        #region Listeners

        protected override void RegisterListeners()
        {
            base.RegisterListeners();
        }

        protected override void UnRegisterListeners()
        {
            base.UnRegisterListeners();
        }

        #endregion
    }
}