using Data;
using Enums;
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
        public override void Initialize(ulong clientId, Vector3 target, string spellName, int level)
        {
            base.Initialize(clientId, target, spellName, level);

            if (!IsServer)
                return;

            m_CounterTimer = m_SpellData.Duration;
            m_Shield = m_SpellData.Shield;

            // apply self state effects
            ApplyAllyStateEffects(m_Controller);

            // TODO : BETTER - if spell is not impacting player by blocking movement or cast, and is not Trigger by player, do not add to list of Counters
            if (! m_SpellData.IsLinkedCounter)
                return;

            m_Controller.CounterHandler.AddCounter(this);
        }

        protected override void End()
        {
            if (m_SpellData.IsLinkedCounter)
                m_Controller.CounterHandler.RemoveCounter(this);

            if (m_SpellData.AllyStateEffects != null)
            {
                foreach (var stateEffect in m_SpellData.AllyStateEffects)
                {
                    if (!m_Controller.StateHandler.HasState(stateEffect.StateEffect))
                        continue;

                    m_Controller.StateHandler.RemoveStateEffect(stateEffect.StateEffect, true, stateEffect.GetStacks());
                }
            }

            base.End();
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

        public bool CanBeProc(Spell spell)
        {
            return COUNTER_PROCABLE_SPELLTYPE.Contains(spell.SpellData.SpellType);
        }

        public bool ProcCounter(Spell enemySpell)
        {
            if (!IsServer)
                return false;

            // check if type of spell can proc counter
            if (! CanBeProc(enemySpell))
                return false;

            var targetPosition = enemySpell.Controller.transform.position;
            switch (m_SpellData.CounterType)
            {
                // cast the counter spell on the enemy
                case ECounterType.Proc:
                    m_SpellData.OnCounterProc.Cast(OwnerClientId, targetPosition, transform.position, recalculateTarget: false);
                    break;

                // block the spell : do nothing
                case ECounterType.Block:
                    if (m_SpellData.Shield > 0)
                    {
                        HitShield(enemySpell.GetBoostedDamages(m_Controller));
                    }

                    enemySpell.CallSpellEventClientRPC(ESpellEvent.OnHit, m_Controller.PlayerId);
                    break;

                // Recast the spell to the enemy
                case ECounterType.Reflect:
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

            // Converts spell incoming damages into someting else
            ProcDamageConversionEffects(enemySpell);

            // Destroy the spell
            Destroy(enemySpell.gameObject);

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

    }
}