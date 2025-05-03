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

namespace Data
{
    [Serializable]
    public struct SDamageConversionEffects
    {
        #region Members

        int m_Level;

        [SerializeField] float          m_Ratio;
        [SerializeField] ESpellTarget   m_Target;
        [SerializeField] string         m_StateEffect;

        // min/max value to safeguard the conversion
        [SerializeField] float          m_MinValue;
        [SerializeField] float          m_MaxValue;

        // =========================================================================================
        // Public Accessors
        public readonly float Ratio     => m_Ratio * Mathf.Pow(0.9f, m_Level);
        public readonly float MinValue  => m_MinValue > 0 ? m_MinValue * Mathf.Pow(1.05f, m_Level) : 0;
        public readonly float MaxValue  => m_MaxValue > 0 ? m_MaxValue * Mathf.Pow(1.05f, m_Level) : 0;

        #endregion


        #region Apply Effect

        public void Apply(Spell spell, Controller self)
        {
            Controller target = GetTarget(self);
            if (target == null)
                return;

            ApplyStateEffect(target, self, spell);
        }

        public void ApplyStateEffect(Controller target, Controller caster, Spell spell)
        {
            if (string.IsNullOrEmpty(m_StateEffect))
                return;
            
            // Calculate Stacks
            int stacks = (int)Mathf.Floor(Ratio * spell.SpellData.Damage);
            if (m_MaxValue > 0f && stacks > Mathf.Round(m_MaxValue))
                stacks = (int)Mathf.Round(m_MaxValue);
            if (m_MinValue > 0f && stacks < Mathf.Round(m_MinValue))
                stacks = (int)Mathf.Round(m_MinValue);

            // Apply state Effect on Target
            target.StateHandler.AddStateEffect(SpellLoader.GetStateEffect(m_StateEffect, m_Level), caster, new SStateEffectData(EStateEffect.None, stacks));
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
            if (string.IsNullOrEmpty(m_StateEffect))
                return "";
        
            return "Each " + Mathf.Floor(1 / Ratio) + " damages blocked, apply a stack of " + TextHandler.FormatStateEffectIcon(m_StateEffect, true) + " on " + (m_Target == ESpellTarget.Self ? "your character" : "your enemy") + (MaxValue > 0 ? " (maxed at " + (int)Mathf.Round(MaxValue) + " stacks)" : "");
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