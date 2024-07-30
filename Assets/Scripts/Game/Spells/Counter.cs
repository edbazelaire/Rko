using Data;
using Enums;
using NUnit.Framework.Internal;
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

        float m_CounterTimer;

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

            transform.position = new Vector3(transform.position.x + m_SpellData.SpawnOffset.x, m_SpellData.SpawnOffset.y, 0);
            if (m_SpellData.ColorSwap != default)
                m_Controller.GFXHandler.AddColorClientRPC(m_SpellData.ColorSwap);

            if (!IsServer)
                return;

            m_CounterTimer = m_SpellData.Duration;

            // TODO : BETTER - if spell is not impacting player by blocking movement or cast, and is not Trigger by player, do not add to list of Counters
            if (!m_SpellData.IsBlockingCast && !m_SpellData.IsBlockingMovement && m_SpellData.CounterActivation == ECounterActivation.SelfTrigger)
                return;

            m_Controller.CounterHandler.AddCounter(this);
        }

        protected override void End()
        {
            if (m_SpellData.ColorSwap != default)
                m_Controller.GFXHandler.RemoveColorClientRPC(m_SpellData.ColorSwap);

            m_Controller.CounterHandler.RemoveCounter(this);

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
            CallSpellEvent(ESpellEvent.OnHit);
            
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


    }
}