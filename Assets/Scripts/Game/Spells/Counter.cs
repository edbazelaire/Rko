using Data;
using Enums;
using Game.Loaders;
using Game.UI;
using System;
using System.Linq;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class Counter : Spell
    {
        #region Members

        public static ESpellType[] COUNTER_PROCABLE_SPELLTYPE = { ESpellType.Projectile, ESpellType.MultiProjectiles };

        CounterData m_SpellData => m_BaseSpellData as CounterData;
        public new CounterData SpellData => m_SpellData;

        int m_Shield;
        float m_CounterTimer;

        public int Shield => m_Shield;

        #endregion


        #region Init & End

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellName"></param>
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level, string parent)
        {
            base.Initialize(clientId, target, spellName, level, parent);

            if (!IsServer)
                return;

            m_CounterTimer  = m_SpellData.Duration;
            m_Shield        = m_SpellData.Shield;

            if (m_SpellData.OnCounterProc != null)
                m_SpellData.OnCounterProc.SetParent(m_SpellData.Parent);

            // apply self state effects
            ApplyAllyStateEffects(m_Controller);

            // TODO : BETTER - if spell is not impacting player by blocking movement or cast, and is not Trigger by player, do not add to list of Counters
            if (! m_SpellData.IsLinkedCounter)
                return;

            if (m_SpellData.IsCanceledOnCast)
                m_Controller.SpellHandler.OnPreSpellEvent += OnPreSpellEvent;

            m_Controller.CounterHandler.AddCounter(this);
        }

        protected override void End()
        {
            if (! IsServer) 
                return;

            if (m_IsOver)
                return;

            m_Controller.SpellHandler.OnPreSpellEvent -= OnPreSpellEvent;

            if (m_SpellData.IsLinkedCounter)
                m_Controller.CounterHandler.RemoveCounter(this);

            if (m_SpellData.AllyStateEffects != null)
            {
                foreach (var effect in m_SpellData.AllyStateEffects)
                {
                    if (!m_Controller.StateHandler.HasState(effect.StateEffect))
                        continue;

                    m_Controller.StateHandler.RemoveStateEffect(effect.StateEffect, true, effect.GetStacks());
                }
            }

            base.End();
        }

        public override void OnDestroy()
        {
            if (! m_IsOver) 
                m_Controller.SpellHandler.OnPreSpellEvent -= OnPreSpellEvent;

            base.OnDestroy();
        }

        #endregion


        #region Update 

        /// <summary>
        /// 
        /// </summary>
        protected override void Update()
        {
            base.Update();

            if (!IsServer)
                return;

            if (m_SpellData.Duration <= 0)
                return;

            m_CounterTimer -= Time.deltaTime;
            if (m_CounterTimer <= 0)
                End();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="collision"></param>
        protected virtual void OnTriggerEnter2D(Collider2D collision)
        {
            if (!IsServer)
                return;

            // has to be type of SelfTrigger to be able to trigger itself
            if (m_SpellData.CounterActivation != ECounterActivation.SelfTrigger) 
                return;

            // check if is spell
            if (collision.gameObject.layer != LayerMask.NameToLayer("Spell"))
                return;

            // check if spell can proc counter
            Spell spell = Finder.FindComponent<Spell>(collision.gameObject);

            // check that this is not a spell from the same team
            if (spell.Controller.Team == m_Controller.Team)
                return;

            ProcCounter(spell);
        }

        #endregion


        #region Counter Proc 

        public bool CanBeProc(Enums.ESpellCategory damageType)
        {
            return m_SpellData.DamageTypeActivation.Contains(damageType);
        }

        public bool ProcCounter(Spell enemySpell)
        {
            if (!IsServer)
                return false;

            // check if type of spell can proc counter
            if (! CanBeProc(enemySpell.SpellData.SpellCategory))
                return false;

            var targetPosition = enemySpell.Controller.transform.position;
            targetPosition.y = 0;
            switch (m_SpellData.CounterType)
            {
                // cast the counter spell on the enemy
                case ECounterType.Proc:
                    m_SpellData.OnCounterProc.Cast(OwnerClientId, targetPosition, transform.position, recalculateTarget: true);
                    break;

                // block the spell : do nothing
                case ECounterType.Block:
                    if (m_SpellData.Shield > 0)
                    {
                        HitShield(enemySpell.GetBoostedDamages(m_Controller) + enemySpell.GetBoostedExecutionDamages(m_Controller));
                    }

                    enemySpell.CallSpellEventClientRPC(ESpellEvent.OnHit, m_Controller.PlayerId);
                    break;

                // Recast the spell to the enemy
                case ECounterType.Reflect:
                    // set reflection parent
                    enemySpell.SpellData.SetParent(m_SpellData.Parent);

                    // if enemy spell is sub-spell of a multiprojectile spell : only cast one instance of the spell
                    if (enemySpell.SpellData.SpellType == ESpellType.MultiProjectiles)
                    {
                        ((MultiProjectilesData)enemySpell.SpellData).CastOneProjectile(OwnerClientId, targetPosition, transform.position);
                        break;
                    }

                    enemySpell.SpellData.Cast(OwnerClientId, targetPosition, transform.position, recalculateTarget: false);
                    break;

                default:
                    Debug.LogError("Unhandled counter type : " + m_SpellData.CounterType);
                    break;
            }

            // Add energy from counter proc
            m_Controller.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // Converts spell incoming damages into someting else
            ProcDamageConversionEffects(enemySpell);

            // Destroy the spell
            if (m_SpellData.IsDestroyingSpell)
                Destroy(enemySpell.gameObject);

            // Call "OnHit" event for the Counter
            CallSpellEventClientRPC(ESpellEvent.OnHit);
            
            // Check MaxHit
            m_HittedPlayerId.Add(0);
            if (m_SpellData.MaxHit > 0 && m_HittedPlayerId.Count >= m_SpellData.MaxHit)
                End();

            return true;
        }

        public bool ProcCounter(int damages, Controller caster, Enums.ESpellCategory damageType)
        {
            if (!IsServer)
                return false;

            if (damages <= 0)
                return false;

            // check if type of spell can proc counter
            if (! CanBeProc(damageType))
                return false;

            var targetPosition = caster.transform.position;
            targetPosition.y = 0;
            switch (m_SpellData.CounterType)
            {
                // cast the counter spell on the enemy
                case ECounterType.Proc:
                    m_SpellData.OnCounterProc.Cast(OwnerClientId, targetPosition, transform.position, recalculateTarget: true);
                    break;

                // block the spell : do nothing
                case ECounterType.Block:
                    if (m_SpellData.Shield > 0)
                    {
                        HitShield(damages);
                    }
                    break;

                // Recast the spell to the enemy
                case ECounterType.Reflect:
                    ErrorHandler.Error("Unhandled counter type Reflect in the situation");
                    break;

                default:
                    Debug.LogError("Unhandled counter type : " + m_SpellData.CounterType);
                    break;
            }

            // Add energy from counter proc
            m_Controller.EnergyHandler.AddEnergy(m_SpellData.EnergyGain);

            // Call "OnHit" event for the Counter
            CallSpellEventClientRPC(ESpellEvent.OnHit);
            
            // Check MaxHit
            m_HittedPlayerId.Add(0);
            if (m_SpellData.MaxHit > 0 && m_HittedPlayerId.Count >= m_SpellData.MaxHit)
                End();

            return true;
        }

        /// <summary>
        /// Converts spell incoming damages into someting else
        /// </summary>
        void ProcDamageConversionEffects(Spell enemySpell)
        {
            if (m_SpellData.DamageConversionEffects == null || m_SpellData.DamageConversionEffects.Count == 0)
                return;

            foreach(var effect in m_SpellData.DamageConversionEffects)
            {
                effect.Apply(enemySpell, Controller);
            }
        }

        #endregion


        #region Shield

        public void HitShield(int damages)
        {
            if (damages < 0)
            {
                ErrorHandler.Warning("Trying to hit shield with negative damages : " + damages);
                return;
            }

            if (damages == 0)
                return;

            m_Shield = Math.Max(0, m_Shield - damages);
            if (m_Shield <= 0)
                End();

            m_Controller.Life.RecalculateShield();
        }

        public void AddShield(int shield)
        {
            if (shield < 0)
            {
                ErrorHandler.Warning("Trying to add negative shield : " + shield);
                return;
            } 
            
            if (shield == 0)
                return;

            m_Shield += shield;
            m_Controller.Life.RecalculateShield();
        }

        #endregion


        #region Listeners

        void OnPreSpellEvent(string spellNamen, ESpellEvent spellEvent)
        {
            if (spellEvent == ESpellEvent.OnStartCast && m_SpellData.IsCanceledOnCast)
            {
                End();
            }
        }

        #endregion

    }
}