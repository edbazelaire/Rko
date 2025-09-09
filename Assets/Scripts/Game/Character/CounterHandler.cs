using Enums;
using Game.Spells;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

namespace Game.Character
{
    public class CounterHandler : NetworkBehaviour
    {
        #region Members

        // TODO : Never used ???
        private NetworkVariable<bool> m_HasCounter              = new(false);
        // TODO : Never used ???

        private NetworkVariable<bool> m_IsBlockingMovement      = new(false);
        private NetworkVariable<bool> m_IsBlockingCast          = new(false);

        Controller m_Controller;
        List<Counter> m_Counters = new List<Counter>();

        public NetworkVariable<bool> IsBlockingMovement => m_IsBlockingMovement;
        public NetworkVariable<bool> IsBlockingCast => m_IsBlockingCast;
        public bool HasCounter => m_Counters.Count > 0;

        public int RemainingShield
        {
            get
            {
                var shield = 0;
                foreach (var counter in m_Counters)
                {
                    shield += Math.Max(0, counter.Shield);
                }

                return shield;
            }
        }

        #endregion


        #region Init & End

        public override void OnNetworkSpawn()
        {
            base.OnNetworkSpawn();

            m_Controller = Finder.FindComponent<Controller>(gameObject);
            m_Counters = new List<Counter>();
        }

        #endregion


        #region Public Manipulators

        /// <summary>
        /// When a spell hits a player, check for counters
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CheckCounters(Spell spell)
        {
            // check has counters
            if (m_Counters.Count == 0)
                return false;

            // check is same team
            if (GameManager.Instance.GetPlayer(spell.Caster.PlayerId).Team == m_Controller.Team)
                return false;

            // duplicate to avoid inference during loop
            var counters = m_Counters.ToArray();

            // find first counter that has an "OnHit" proc effect
            foreach (Counter counter in counters)
            {
                // check still exists
                if (counter == null || counter.IsDestroyed())
                    continue;

                // check has right type
                if (counter.SpellData.CounterActivation != ECounterActivation.OnHitPlayer)
                    continue;

                // check that spell can proc counters
                if (! counter.SpellData.DamageTypeActivation.Contains(spell.SpellData.SpellCategory))
                    continue;

                // try to proc it, return true if successfull
                if (counter.ProcCounter(spell) && counter.SpellData.IsDestroyingSpell)
                    return true;
            }

            // no spell has proc any counter : return false
            return false;
        }

        /// <summary>
        /// When a spell hits a player, check for counters
        /// </summary>
        /// <param name="spell"></param>
        /// <returns></returns>
        public bool CheckCounters(int damages, Controller caster, EHitCategory spellCategory)
        {
            // check has counters
            if (m_Counters.Count == 0)
                return false;

            // check is same team
            if (caster.Team == m_Controller.Team)
                return false;

            // find first counter that has an "OnHit" proc effect
            foreach (Counter counter in m_Counters)
            {
                // check has right type
                if (counter.SpellData.CounterActivation != ECounterActivation.OnHitPlayer)
                    continue;

                // check that spell can proc counters
                if (! counter.SpellData.DamageTypeActivation.Contains(spellCategory))
                    continue;

                // try to proc it, return true if successfull
                if (counter.ProcCounter(damages, caster, spellCategory) && counter.SpellData.IsDestroyingSpell)
                    return true;
            }

            // no spell has proc any counter : return false
            return false;
        }

        public void AddCounter(Counter counterSpell)
        {
            if (!IsServer)
                return;

            // add counter to list of counters
            m_Counters.Add(counterSpell);

            if (counterSpell.SpellData.CounterAnimation != EAnimation.None)
                m_Controller.AnimationHandler.PlayAnimationClientRPC(counterSpell.SpellData.CounterAnimation);

            // now that this spell has been added, check if there is still blocking actions
            CheckBlockingActions();
        }

        public void RemoveCounter(Counter counter)
        {
            if (!IsServer)
                return;

            if (counter == null)
                return;

            // find index
            int index = -1;
            for (int i = 0; i < m_Counters.Count; i++)
            {
                if (m_Counters[i] == counter)
                {
                    index = i;
                    break;
                }
            }

            // remove at index if found
            if (index >= 0)
                m_Counters.RemoveAt(index);
            else
                ErrorHandler.Error("Unable to find counter " + counter.name + " in list of counters");

            // now that this spell has been removed, check if there is still blocking actions
            CheckBlockingActions();

            // check if still has counter
            for(int i = m_Counters.Count - 1; i >= 0; i--)
            {
                if (m_Counters[i].SpellData.CounterAnimation != EAnimation.None)
                {
                    m_Controller.AnimationHandler.PlayAnimationClientRPC(m_Counters[i].SpellData.CounterAnimation);
                    break;
                }
            }
        }

        #endregion


        #region Private Manipulators

        void CheckBlockingActions()
        {
            bool blockingMovement   = false;
            bool blockingCast       = false;

            foreach(var counterSpell in m_Counters)
            {
                if (counterSpell.SpellData.IsBlockingMovement)
                {
                    blockingMovement = true;
                }

                if (counterSpell.SpellData.IsBlockingCast)
                {
                    blockingCast = true;
                }
            }

            SetIsBlockingMovement(blockingMovement);
            SetIsBlockingCast(blockingCast);
        }

        private void SetIsBlockingMovement(bool blockingMovement)
        {
            if (!IsServer)
                return;

            if (blockingMovement == m_IsBlockingMovement.Value)
                return;

            if (blockingMovement)
                m_Controller.Movement.CancelMovement(true);

            m_IsBlockingMovement.Value = blockingMovement;
        }

        private void SetIsBlockingCast(bool blockingCast)
        {
            Debug.Log("SetIsBlockingCast() : " + blockingCast);

            if (!IsServer)
                return;

            if (blockingCast == m_IsBlockingCast.Value)
                return;

            m_IsBlockingCast.Value = blockingCast;
        }

        #endregion
    }
}