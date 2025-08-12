using Data.GameManagement;
using Enums;
using Game;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using Tools;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Serialization;

namespace Data
{
    [Serializable]
    public struct SDamageConversionEffects
    {
        #region Members

        int m_Level;

        [SerializeField] float          m_Ratio;
        [SerializeField] float          m_RatioScalingLevel;
        [SerializeField] ESpellTarget   m_Target;
        [SerializeField, FormerlySerializedAs("StateEffect"), Tooltip("Effect converting the damages : Spell, StateEffect, Property, ...")] 
        string                          m_Effect;

        // min/max value to safeguard the conversion
        [SerializeField] float          m_MinValue;
        [SerializeField] float          m_MaxValue;

        // =========================================================================================
        // Public Accessors
        public readonly float Ratio     => m_Ratio * Mathf.Pow(1 + m_RatioScalingLevel, m_Level);
        public readonly float MinValue  => m_MinValue > 0 ? m_MinValue * Mathf.Pow(1.05f, m_Level) : 0;
        public readonly float MaxValue  => m_MaxValue > 0 ? m_MaxValue * Mathf.Pow(1.05f, m_Level) : 0;

        #endregion


        #region Apply Effect

        public void Apply(Spell spell, Controller caster, string parent)
        {
            Controller target = GetTarget(caster);
            if (target == null)
                return;

            if (string.IsNullOrEmpty(m_Effect))
            {
                ErrorHandler.Warning("Trying to convert damages, but no effect was provided");
                return;
            }

            int stacks = ConvertStacks(spell);

            if (Enum.TryParse(m_Effect, out EStateEffectProperty property))
            {
                ApplyProperty(target, caster, stacks, property, parent);
            } else if (SpellLoader.IsSpell(m_Effect))
            {
                ErrorHandler.Warning("Unhandled case : " + m_Effect + " - Spell");
            }
            else if (SpellLoader.IsStateEffect(m_Effect))
            {
                ApplyStateEffect(target, caster, stacks);
            } else
            {
                ErrorHandler.Warning("Unhandled case : " +  m_Effect + " - neither recognized as Property, Spell or StateEffect");
            }
        }

        public int ConvertStacks(Spell spell)
        {
            // Calculate Stacks
            int stacks = (int)Mathf.Floor(Ratio * spell.SpellData.Damage);
            if (m_MaxValue > 0f && stacks > Mathf.Round(m_MaxValue))
                stacks = (int)Mathf.Round(m_MaxValue);
            if (m_MinValue > 0f && stacks < Mathf.Round(m_MinValue))
                stacks = (int)Mathf.Round(m_MinValue);

            return stacks;
        }

        public void ApplyProperty(Controller target, Controller caster, int stacks, EStateEffectProperty property, string parent)
        {
            switch (property)
            {
                case EStateEffectProperty.Heal:
                    target.Life.Heal(stacks, caster.PlayerId, parent, ESpellCategory.Direct);
                    break;
                case EStateEffectProperty.Damage:
                    target.Life.Heal(stacks, caster.PlayerId, parent, ESpellCategory.Direct);
                    break;
                default:
                    target.CharacterData.AddBonusStat(property, stacks, default);
                    break;
            }
        }

        public void ApplyStateEffect(Controller target, Controller caster, int stacks)
        {
            // Apply state Effect on Target
            target.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(m_Effect, m_Level), caster, new SStateEffectData(EStateEffect.None, stacks));
        }

        Controller GetTarget(Controller self) 
        { 
            switch (m_Target)
            {
                case ESpellTarget.Self:
                    return self;
                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(self.Team, self.PlayerId);
                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(self.Team);

                default:
                    ErrorHandler.Error("Unhandled case : " + m_Target);
                    return null;
            }
        }

        #endregion


        #region Level

        public void SetLevel(int level)
        {
            m_Level = level;
        } 

        #endregion


        #region Description

        public string GetDescription()
        {
            if (string.IsNullOrEmpty(m_Effect))
                return "";
            if (Enum.TryParse(m_Effect, out EStateEffectProperty property))
            { 
                switch (property)
                {
                    case EStateEffectProperty.Heal:
                        return TextHandler.FormatStateEffectIcon(m_Effect, true) + " " + (m_Target == ESpellTarget.Self ? "your character" : "your enemy") + " for " + Mathf.Floor(Ratio * 100) + "% of damage blocked" + (MaxValue > 0 ? " (maxed at " + (int)Mathf.Round(MaxValue) + ")" : "");
                    case EStateEffectProperty.Damage:
                        return TextHandler.FormatStateEffectIcon(m_Effect, true) + " " + (m_Target == ESpellTarget.Self ? "your character" : "your enemy") + " for " + Mathf.Floor(Ratio) + "% of damage blocked" + (MaxValue > 0 ? " (maxed at " + (int)Mathf.Round(MaxValue) + ")" : "");
                    default:
                        return "Each " + Mathf.Floor(Ratio) + " damage blocked, grants " + TextHandler.FormatStateEffectIcon(m_Effect, true) + " to " + (m_Target == ESpellTarget.Self ? "your character" : "your enemy") + (MaxValue > 0 ? " (maxed at " + (int)Mathf.Round(MaxValue) + ")" : "");
                }
            } 
            
            if (SpellLoader.IsStateEffect(m_Effect))
                return "Apply " + Ratio * 100 + "% of damage blocked as stack of " + TextHandler.FormatStateEffectIcon(m_Effect, true) + " on " + (m_Target == ESpellTarget.Self ? "your character" : "your enemy") + (MaxValue > 0 ? " (maxed at " + (int)Mathf.Round(MaxValue) + " stacks)" : "");
            
            if (SpellLoader.IsSpell(m_Effect))
            {
                ErrorHandler.Warning("Unhandled case - get description for spell : " + m_Effect);
                return "";
            }

            ErrorHandler.Warning("Unhandled case - get description for effect : " + m_Effect + " - neither recognized as property, spell or effect");
            return "";
        }

        #endregion
    }


    [CreateAssetMenu(fileName = "Counter", menuName = "Game/Spells/Counter")]
    public class CounterData : SpellData
    {
        public override ESpellType SpellType => ESpellType.Counter;

        [Header("Counter")]
        [Tooltip("Type of counter")]
        public ECounterType         CounterType;
        [Tooltip("How is the counter triggerred ? ")]
        public ECounterActivation   CounterActivation;
        [SerializeField, Tooltip("Type of spells that can proc the counter")] 
        protected List<Enums.ESpellCategory>  m_DamageTypeActivation                   = new List<Enums.ESpellCategory>() { Enums.ESpellCategory.Direct };
        [SerializeField, Tooltip("Offset spawning of the counter proc spell")] 
        protected Vector2           m_SpawnOffset                           = new Vector2(0, 0);
        [SerializeField, Tooltip("")]
        protected bool              m_IsFollowing                           = true;
        [SerializeField, Tooltip("")]
        protected EAnimation        m_CounterAnimation                      = EAnimation.Counter;
        [SerializeField, Tooltip("Location where the counter is spawning")] 
        protected ESpawnLocation    m_SpawnLocation                         = ESpawnLocation.Center;
        public bool                 IsDestroyingSpell                       = true;
        public bool                 IsBlockingMovement                      = true;
        public bool                 IsBlockingCast                          = true;
        public bool                 IsCanceledOnCast                        = false;

        [SerializeField, Tooltip("List of effects converting enemy damages into something else (stacks, runes, ...)")]
        protected List<SDamageConversionEffects> m_DamageConversionEffects;

        [Tooltip("Spell Casted when the counter procs"), MyBox.ConditionalField("CounterType", false, ECounterType.Proc)]
        public SpellData OnCounterProc;

        // ===================================================================================
        // Public Accessors
        public List<Enums.ESpellCategory>       DamageTypeActivation    => m_DamageTypeActivation;
        public bool                             IsLinkedCounter         => IsBlockingCast || IsBlockingMovement || CounterActivation == ECounterActivation.OnHitPlayer;
        public List<SDamageConversionEffects>   DamageConversionEffects => m_DamageConversionEffects;
        public Vector2                          SpawnOffset             => m_SpawnOffset;
        public EAnimation                       CounterAnimation        => m_CounterAnimation;


        #region Target & Position 

        protected override Transform FindParent(ulong clientId)
        {
            if (!m_IsFollowing)
                return null;

            return GameManager.Instance.GetPlayer(clientId).transform;
        }

        public override void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId)
        {
            // init position to target position
            switch (m_SpawnLocation)
            {
                case ESpawnLocation.None:
                case ESpawnLocation.Center:
                    position = GameManager.Instance.GetPlayer(clientId).transform.position;
                    break;

                case ESpawnLocation.Ground:
                    position = GameManager.Instance.GetPlayer(clientId).transform.position;
                    position.y = 0;
                    break;

                case ESpawnLocation.Hight:
                    position = GameManager.Instance.GetPlayer(clientId).transform.position;
                    position.y = Settings.SPELL_DIAGONAL_POS_Y;
                    break;

                case ESpawnLocation.Sky:
                    position = GameManager.Instance.GetPlayer(clientId).transform.position;
                    position.y = 5;
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + m_SpawnLocation);
                    position = GameManager.Instance.GetPlayer(clientId).transform.position;
                    break;
            }
            
            // add offset
            position.x += SpawnOffset.x;
            position.y += SpawnOffset.y;
        }


        #endregion


        #region Level Management

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            for (int i = 0; i < m_DamageConversionEffects.Count; i++)
            {
                var effect = m_DamageConversionEffects[i];
                effect.SetLevel(level);
                m_DamageConversionEffects[i] = effect;
            }
        }

        #endregion


        #region Infos & Description

        public override Dictionary<string, object> GetInfo()
        {
            var infos = base.GetInfo();

            var description = "";
            switch (CounterType)
            {
                case ECounterType.Proc:
                    description = "Trigger effect";
                    break;

                case ECounterType.Reflect:
                    description = "Reflect enemy spells";
                    break;

                case ECounterType.Block:
                    description = "Block incoming enemy spells";
                    break;
            }

            infos["CounterActivation"] = description;
            return infos;
        }

        public override string GetDescription()
        {
            string description = base.GetDescription();
            if (m_DamageConversionEffects == null || m_DamageConversionEffects.Count == 0)
                return description;

            foreach (var effect in m_DamageConversionEffects)
            {
                description += "\n" + effect.GetDescription();
            }

            return description;
        }

        #endregion
    }
}