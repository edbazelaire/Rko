using Data;
using Enums;
using Game;
using Tools;
using UnityEngine;
using System;
using System.Collections.Generic;
using Data.DataStructures;

namespace Assets.Scripts.Data.PowerUp
{
    [CreateAssetMenu(fileName = "PowerEffect", menuName = "Game/PowerUp/Default")]
    public class PowerEffectData : CollectableData
    {
        #region Members

        [SerializeField] 
        protected ESpellTarget                  m_Target;
        [SerializeField] 
        protected ESpellActivation              m_SpellActivationEvent;
        [SerializeField]
        protected List<SCharacterStatScaling>   m_BonusStats;
        [SerializeField]
        protected List<SRunePower>              m_SubPowerUps;

        public ESpellTarget                     Target => m_Target;
        public List<SCharacterStatScaling>      BonusStats => m_BonusStats;

        // ==================================================================================
        // DATA
        protected Controller                    m_Controller;
        protected bool                          m_IsActivated;

        public bool IsActivated => m_IsActivated;

        public string BaseName
        {
            get
            {
                string name = Name;
                string[] raretyNames = Enum.GetNames(typeof(ERarety));  // Get all enum values as strings

                // Loop through each rarety name
                foreach (string raretyName in raretyNames)
                {
                    string suffix = "_" + raretyName;  // Create the suffix like "_Epic", "_Common", etc.

                    if (name.EndsWith(suffix))
                    {
                        // If name ends with the suffix, return the base name without the rarity part
                        return name.Substring(0, name.Length - suffix.Length);  // Remove the suffix to get the base name
                    }
                }

                // If no rarety found, return the original name
                return name;
            }
        }

        #endregion


        #region Constructor

        public virtual PowerEffectData FromTriggerEffect(STriggerEffect triggerEffect, ERarety rarety)
        {
            Rarety                  = rarety;
            m_Target                = triggerEffect.Target;
            m_SpellActivationEvent  = triggerEffect.SpellActivationEvent;
            m_BonusStats            = new List<SCharacterStatScaling>();

            return this;
        }

        #endregion


        #region Activation

        public virtual void Initialize(Controller controller)
        {
            m_Controller = controller;

            RegisterActivation();
        }

        public virtual void Activate()
        {
            m_IsActivated = true;
        }

        #endregion  


        #region Deactivation / End

        public virtual void Deactivate()
        {
            m_IsActivated = false;
        }

        public virtual void End()
        {
            UnRegisterListeners();
        }

        #endregion


        #region Helpers
        
        public List<SCharacterStatScaling> GetBonusStats()
        {
            List<SCharacterStatScaling> stats = new();

            foreach (var stat in BonusStats)
            {
                stat.AsBonus(m_Level);

                // check if already in list
                int index = stats.FindIndex(value => value.StateEffectProperty.Equals(stat.StateEffectProperty));

                // if not in list : add as new bonus value
                if (index < 0)
                {
                    stats.Add(stat);
                    continue;
                }

                // add bonus value to existing bonus value
                var newStat = stats[index];
                newStat.BonusValue += stat.BonusValue;
                stats[index] = newStat;
            }

            return stats;
        }

        protected virtual Controller CalculateTarget(ulong? targetId = null)
        {
            switch (Target)
            {
                case ESpellTarget.None:
                case ESpellTarget.Self:
                    return m_Controller;

                case ESpellTarget.CurrentTarget:
                    if (targetId.HasValue)
                        return GameManager.Instance.GetPlayer(targetId.Value);
                    return GameManager.Instance.GetFirstEnemy(m_Controller.Team);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(m_Controller.Team);

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(m_Controller.Team, m_Controller.PlayerId);

                default:
                    ErrorHandler.Warning("Unhandled case : " + Target);
                    return m_Controller;
            }
        }

        protected virtual void UnRegisterListeners()
        {
            GameManager.GameStartedEvent -= Activate;
        }


        #endregion


        #region Info & Description

        public override Dictionary<string, object> GetInfos()
        {
            var infos = base.GetInfos();

            if (m_BonusStats == null)
                return infos;

            foreach (var stat in m_BonusStats)
            {
                infos.Add(stat.StateEffectProperty.ToString(), stat.GetDefaultValue(m_Level));
            }

            return infos;
        }

        #endregion


        #region Listeners

        protected virtual void RegisterActivation()
        {
            switch (m_SpellActivationEvent)
            {
                case ESpellActivation.GameStart:
                    if (GameManager.Instance.IsGameStarted)
                        Activate();
                    else
                        GameManager.GameStartedEvent += Activate;
                    break;

                default:
                    ErrorHandler.Error("Unahanlded case : " + m_SpellActivationEvent);
                    break;
            } 
        }

        #endregion
    }
}