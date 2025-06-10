using Enums;
using Game.Loaders;
using Game.Spells;
using Game.StateEffects.Quests;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public struct SQuestThreshold
    {
        [Min(1)]
        public int RequiredStacks;
        public Sprite ReplacementIcon;
        public List<SActivableEffect> ActivableEffects;
        public List<SBonusStats> BonusStats;

        int m_Level;
        public void SetLevel(int level)
        {
            m_Level = level;
            for (int i = 0; i < ActivableEffects.Count(); i++)
            {
                var effect = ActivableEffects[i];
                effect.SetLevel(level);
                ActivableEffects[i] = effect;
            }
        }

        public string GetDescription()
        {
            string description = $"<b><u>{RequiredStacks} Stacks</u></b>";

            foreach (var activableEffect in ActivableEffects)
            {
                description += "\n<i>" + SpellLoader.GetDescription(activableEffect.Effect, activableEffect.Level) + "</i>";
            }

            if (BonusStats.Count() > 0)
            {
                description += "\nFor each new stacks, apply :";
                foreach (var effect in BonusStats)
                {
                    string effectValue = TextHandler.FormatPropertyIcon(
                        effect.StateEffectProperty.ToString(),
                        effect.Get(m_Level, 1),
                        withIcon: ! TextHandler.IGNORED_ICONS.Contains(effect.StateEffectProperty.ToString()),
                        withPropertyName: false,
                        scaling: effect.ScalingDirection
                    );
                    description += $"\n     • {effect.StateEffectProperty} : {effectValue}";
                }
            }

            return description;
        }
    }
}