using Data;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using MyBox;
using Save;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.StateEffects.Quests
{
    [CreateAssetMenu(fileName = "ArenaQuest", menuName = "Game/StateEffects/Quests/ArenaQuest")]
    public class ArenaQuestEffect : QuestEffect
    {
        #region Members

        #endregion


        #region Init & End

        protected override void ApplyPreProcessing()
        {
            base.ApplyPreProcessing();
            Load();
        }

        #endregion


        #region Save & Load

        public virtual void Load()
        {
            int stacks = ProgressionCloudData.CurrentArena.GetMetaData<int>(StateEffectName);
            if (stacks <= 0)
                return;

            SetStacks(stacks);
        }

        public virtual void Save()
        {
            ProgressionCloudData.SetArenaMetaData(StateEffectName, m_Stacks.ToString());
        }

        #endregion


        #region Info & Description

        public override string GetDescription()
        {
            string description;
            if (ProgressionCloudData.HasArenaInProgress && ProgressionCloudData.CurrentArena.HasMetaData(StateEffectName))
            {
                description = $"Current Stacks : {ProgressionCloudData.CurrentArena.GetMetaData<int>(StateEffectName):0}";
            } else
            {
                description = TextHandler.ReplaceKeyWords("{ArenaQuest}");
            }

            description += "\n\n" + base.GetDescription();
            return description;
        }

        #endregion
    }
}