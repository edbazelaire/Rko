using Data;
using Data.DataStructures.StateEffectSubStructures;
using Enums;
using Game.Loaders;
using Game.Spells;
using MyBox;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Tools.Helpers;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.StateEffects.Quests
{
    [CreateAssetMenu(fileName = "QuestEffect", menuName = "Game/StateEffects/Quests/Quest")]
    public class QuestEffect : StateEffect
    {
        #region Members

        // =====================================================================================================
        // Serialize Fields
        [Header("Quest")]
        [SerializeField, Tooltip("Are the effect of the list cumulative ?")]
        protected bool m_IsCumulativeEffects = new();

        [SerializeField, Tooltip("List of effects for each thresholds")]
        protected List<SQuestThreshold> m_QuestThresholds = new();

        // =====================================================================================================
        // Private
        /// <summary>maximum threshold allowed by this effect </summary>
        int m_MaxThresholdIndex         = -1;
        /// <summary> current activated effect </summary>
        int m_CurrentThresholdIndex     = -1;
        /// <summary> list of spells procced by this effect - using threshold index as reference </summary>
        List<(int index, Spell)>         m_Spells                    = new();
        /// <summary> list of state effects applied by this effect - using threshold index as reference </summary>
        List<(int index, StateEffect)>   m_StateEffects              = new();

        // =====================================================================================================
        // Dependent
        int m_MaxIndex => m_MaxThresholdIndex < 0 ? m_QuestThresholds.Count() : m_MaxThresholdIndex;
        public List<SQuestThreshold> QuestThresholds => m_QuestThresholds;

        #endregion



        #region Stacks

        protected override void SetStacks(int stacks)
        {
            base.SetStacks(stacks);

            // go through all allowed thresholds
            for (int i = 0; i < m_MaxIndex; i++)
            {
                // CHECK : number of stacks above stacks treshold ?
                if (stacks >= m_QuestThresholds[i].RequiredStacks)
                {
                    // CHECK : if is last of the thresholds - activate this effect
                    if (i == m_MaxIndex - 1)
                    {
                        ActivateThresholdIndex(i);
                        return;
                    }

                    // CHECK : if is NOT last - check next effect
                    else
                        continue;
                }

                // Current threshold not reached - activate last threshold
                ActivateThresholdIndex(i - 1);
                return;
            }
        }
        
        public void SetMaxThresholdIndex(int index)
        {
            m_MaxThresholdIndex = index;
        }

        public void SetMaxStacks(int maxStacks)
        {
            m_MaxStacks = maxStacks;
        }

        #endregion


        #region Treshold Effects Activation / Deactivation

        void ActivateThresholdIndex(int index)
        {
            if (m_CurrentThresholdIndex == index)
                return;

            // NOT CUMULATIVE : deactivate current effect - activate new effect
            if (!m_IsCumulativeEffects && m_CurrentThresholdIndex >= 0)
            {
                DeactivateEffectAtIndex(m_CurrentThresholdIndex);
                if (index >= 0)
                    ActivateEffect(m_QuestThresholds[index]);
            }

            // CUMULATIVE : Activate/Deactivate effects between each thresholds
            else
            {
                if (m_CurrentThresholdIndex < index)
                {
                    for (int i = m_CurrentThresholdIndex + 1; i <= index; i++)
                    {
                        ActivateEffect(m_QuestThresholds[i]);
                    }
                }
                else
                {
                    for (int i = m_CurrentThresholdIndex - 1; i >= index; i--)
                    {
                        DeactivateEffectAtIndex(i);
                    }
                }
            }

            // set new index
            m_CurrentThresholdIndex = index;

            // call CLIENT event that a new index has been activated
            m_Controller.StateHandler.CallQuestThresholdEventClientRPC(StateEffectName, index);
        }

        void ActivateEffect(SQuestThreshold questThreshold)
        {
            int tresholdIndex = m_QuestThresholds.IndexOf(questThreshold);

            ErrorHandler.Log("ActivateEffect at treshold : " + questThreshold.RequiredStacks, ELogTag.StateEffects);

            foreach (SActivableEffect activableEffect in questThreshold.ActivableEffects)
            {
                if (SpellLoader.IsSpell(activableEffect.Effect))
                {
                    ErrorHandler.Log("     + ActivateEffect Spell : " + activableEffect.Effect, ELogTag.StateEffects);

                    SpellData spellData = SpellLoader.GetSpellData(activableEffect.Effect, activableEffect.Level);
                    spellData.SetParent(m_Parent);
                    if (activableEffect.Target != ESpellTarget.None)
                        spellData.SpellTarget = activableEffect.Target;

                    spellData.CastDelay(m_Controller.PlayerId, Vector3.zero, recalculateTarget: true, recalculatePosition: true);

                    // add to list of state effects - to allow deactivation if necessary
                    if (activableEffect.IsDeactivable)
                        spellData.OnSpellSpawn += (Spell spell) => { m_Spells.Add((tresholdIndex, spell)); };
                }

                else if (SpellLoader.IsStateEffect(activableEffect.Effect))
                {
                    ErrorHandler.Log("     + ActivateEffect StateEffect : " + activableEffect.Effect, ELogTag.StateEffects);

                    Controller targetController = TargetHelper.GetTargetController(m_Caster.PlayerId, activableEffect.Target);
                    if (targetController == null)
                        return;

                    StateEffect stateEffect = SpellLoader.GetStateEffect(activableEffect.Effect, activableEffect.Level, parent: m_Parent);
                    stateEffect.SetParent(m_Parent);
                    targetController.StateHandler.AddStateEffect(stateEffect, m_Caster);

                    // add to list of state effects - to allow deactivation if necessary
                    if (activableEffect.IsDeactivable)
                        m_StateEffects.Add((tresholdIndex, stateEffect));
                }

                else
                {
                    ErrorHandler.Error("Unhandled effect - " + activableEffect.Effect);
                }
            }

            // Add bonus stats
            if (! questThreshold.BonusStats.IsNullOrEmpty())
            {
                ErrorHandler.Log("     + Adding BonusStats : " + questThreshold.BonusStats.Count(), ELogTag.StateEffects);
                m_BonusStats.AddRange(questThreshold.BonusStats);
            }
        }

        void DeactivateEffectAtIndex(int thresholdIndex)
        {
            ErrorHandler.Log("DeactivateEffectAtIndex : " + thresholdIndex, ELogTag.StateEffects);
            
            var spells = m_Spells;

            // Remove spells
            for (int i = spells.Count() - 1; i >= 0; i--)
            {
                if (m_Spells[i].index != thresholdIndex)
                    continue;

                Spell spell = m_Spells[i].Item2;
                if (spell != null && ! spell.IsDestroyed())
                    spell.Terminate();

                m_Spells.RemoveAt(i);
            }

            // Remove State Effects
            for (int i = m_StateEffects.Count() - 1; i >= 0; i--)
            {
                if (m_StateEffects[i].index != thresholdIndex)
                    continue;
                
                StateEffect stateEffect = m_StateEffects[i].Item2;
                if (stateEffect != null && ! stateEffect.IsDestroyed())
                {
                    stateEffect.End();
                }

                m_StateEffects.RemoveAt(i);
            }

            // Remove BonusStats
            foreach (var bonusStats in m_QuestThresholds[thresholdIndex].BonusStats)
            {
                m_BonusStats.Remove(bonusStats);
            } 
        }

        #endregion


        #region Setter

        protected override void SetProperty(EStateEffectProperty property, object value)
        {
            if (property == EStateEffectProperty.MaxThresholdIndex)
            {
                if (! int.TryParse(value.ToString(), out int iValue))
                {
                    ErrorHandler.Error($"Trying to override {property} in {StateEffectName} with non int value {value}");
                    return;
                }

                SetMaxThresholdIndex(iValue);
                return;
            }

            base.SetProperty(property, value);
        }

        #endregion


        #region Level & Clone

        protected override void SetLevel(int level)
        {
            base.SetLevel(level);

            for (int i = 0; i < m_QuestThresholds.Count(); i++)
            {
                var questThreshold = m_QuestThresholds[i];
                questThreshold.SetLevel(level);
                m_QuestThresholds[i] = questThreshold;
            }
        }

        #endregion


       #region Info & Description

        public override string GetDescription()
        {
            var description = base.GetDescription();

            int index = -1;
            foreach (var questThreshold in m_QuestThresholds)
            {
                index++;

                // Required Stacks is above max allowed stacks, meaning that the effect is never getting triggered
                if ((m_MaxStacks > 0 && questThreshold.RequiredStacks > m_MaxStacks) || index >= m_MaxIndex)
                    break;

                if (!description.IsNullOrEmpty())
                    description += "\n\n";

                description += questThreshold.GetDescription();
            }

            return description;
        }

        #endregion
    }
}