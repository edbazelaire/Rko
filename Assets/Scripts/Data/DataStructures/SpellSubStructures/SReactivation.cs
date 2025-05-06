using Data;
using Enums;
using Game.Loaders;
using Game.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SReactivation
    {
        #region Members

        // =======================================================================
        // Serialized data
        [SerializeField, Tooltip("Number of reactivations that this effect allows")]
        protected int m_NReactivations;
        [SerializeField, Tooltip("Time allowed to re-activate the spell")]
        protected float m_Duration;
        [SerializeField, Tooltip("List of effects applying at each re-activation")]
        protected List<string> m_ReactivationEffects = new List<string>();

        // =======================================================================
        // Private
        protected Controller    m_Controller;
        protected Spell         m_Spell;
        protected int           m_Level;
        protected int           m_CurrentReactivationIndex  = 0;
        protected float         m_Timer                     = 0f;
        protected Coroutine     m_Coroutine;

        // =======================================================================
        // Public accessors
        public Controller       Controller              => m_Controller;
        public bool             IsOver                  => m_Coroutine == null;
        public bool             IsReactivable           => m_NReactivations > m_CurrentReactivationIndex;
        public int              Level                   => m_Level;
        public int              NReactivations          => m_NReactivations;
        public float            Duration                => m_Duration;
        protected List<string>  ReactivationEffects     => m_ReactivationEffects;

        #endregion


        #region Start & Update

        public void Start(Controller controller, Spell spell)
        {
            m_Controller = controller;
            m_Spell = spell;
            m_CurrentReactivationIndex = 0;

            m_Controller.StartCoroutine(UpdateCoroutine());
        }

        public void End()
        {
            if (m_Coroutine != null)
            {
                m_Controller.StopCoroutine(m_Coroutine);
                m_Coroutine = null;
            }
        }

        IEnumerator UpdateCoroutine()
        {
            m_Timer = Duration;
            while (m_Timer > 0f && m_CurrentReactivationIndex < m_NReactivations)
            {
                m_Timer -= Time.deltaTime;
                yield return null;
            }

            End();
        }

        #endregion


        #region Reactivation

        public void Reactivate(Vector3 targetPos, bool recalculateTarget)
        {
            var effectName = m_ReactivationEffects[Math.Min(m_ReactivationEffects.Count, m_CurrentReactivationIndex)];
            if (SpellLoader.IsSpell(effectName))
            {
                var spellData = SpellLoader.GetSpellData(effectName, m_Level);
                if (spellData.AnimationTimer > 0)
                {
                    if (! m_Controller.SpellHandler.TryStartCastSpell(spellData))
                        return;
                }
                else
                {
                    m_Controller.StartCoroutine(spellData.CastDelay(
                        clientId:           m_Controller.PlayerId,
                        target:             targetPos,
                        position:           m_Spell.transform.position,
                        recalculateTarget:  recalculateTarget
                    ));
                }

            } else if (SpellLoader.IsStateEffect(effectName)) 
            {
                ErrorHandler.Warning("Unhandled case - reactivating spell with a StateEffect");
                End();
            }
            m_CurrentReactivationIndex++;
        }


        #endregion


        #region Clone & Level

        public void SetLevel(int level) 
        { 
            m_Level = level;
        }

        #endregion
    }
}