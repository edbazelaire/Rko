using Data;
using Enums;
using Game.Spells;
using System.Collections;
using System.Runtime.CompilerServices;
using Tools;
using UnityEngine;

namespace Game.SpellGFXs
{
    public class SpellGFX : MonoBehaviour
    {
        #region Members

        protected Controller        m_Controller;
        protected SpellData         m_SpellData;
        protected Spell             m_Spell;
        protected string       m_StateEffectName;
        protected SPrefabSpawn      m_PrefabSpawn;
        protected EBodyPart         m_BodyPart;

        protected Coroutine         m_EndCoroutine;
        protected float             m_PersistanceTimer = -1f;
        protected float             m_Duration;

        #endregion


        #region Init

        public virtual void Initialize(Controller controller, SpellData spellData, Spell spell, string stateEffectName, SPrefabSpawn prefabSpawn, EBodyPart bodyPart = EBodyPart.None)
        {
            m_Controller        = controller;
            m_SpellData         = spellData;
            m_Spell             = spell;
            m_StateEffectName   = stateEffectName;
            m_PrefabSpawn       = prefabSpawn;
            m_BodyPart          = bodyPart;


            // set Parent & Position based on provided data
            transform.localScale *= prefabSpawn.Size > 0 ? prefabSpawn.Size : (spellData != null ? spellData.Size : 1);

            // adjuste order and size of the elements
            AdjustOrderInLayer(prefabSpawn.OrderInLayer);

            // make controller play animation if any
            if (prefabSpawn.Animation != EAnimation.None)
            {
                controller.AnimationHandler.PlayAnimation(prefabSpawn.Animation);
            }

            // apply material of the effect on the target bodyparts
            ApplyMaterial();

            // add state effects happening during the lifetime
            AddStateEffects();

            if (prefabSpawn.GFXLifetime.EndSpellPart == ESpellEvent.None)
            {
                End();
                return;
            }

            RegisterListeners();
        }

        #endregion


        #region End

        public virtual void End()
        {
            ErrorHandler.Log("ENDED SPELL GFX : " + this.name, ELogTag.SpellGFX);

            // stop the animation
            if (m_PrefabSpawn.Animation != EAnimation.None)
                m_Controller.AnimationHandler.CancelCastAnimation(m_PrefabSpawn.Animation);

            // remove listeners
            UnRegisterListeners();

            StartCoroutine(EndCoroutine());
        }
        
        protected virtual IEnumerator EndCoroutine()
        {
            // delay destruction of spell graphismes for visual purpuses
            SetPersistantTimer();

            while (m_PersistanceTimer > 0)
            {
                m_PersistanceTimer -= Time.deltaTime;
                yield return null;
            }

            // remove the state effects of the animation
            RemoveStateEffects();

            // remove material applied
            RemoveMaterial();

            // destroy the spell
            Destroy(gameObject);
        }

        protected virtual void SetPersistantTimer()
        {
            m_PersistanceTimer = m_PrefabSpawn.GFXLifetime.Persistance;
        }

        void CheckEnd(ESpellEvent spellEvent)
        {
            if (m_PrefabSpawn.GFXLifetime.EndSpellPart == ESpellEvent.None || m_PrefabSpawn.GFXLifetime.EndSpellPart > spellEvent)
                return;

            End();
        }

        protected virtual void OnDestroy()
        {
            UnRegisterListeners();
        }

        #endregion


        #region Target & Position

        public static Transform CalculateParent(SPrefabSpawn prefabSpawn, Controller caster, Spell spell, Controller targetController)
        {
            switch (prefabSpawn.SpawnTarget)
            {
                case ESpawnTarget.TargetPos:
                    return null;

                case ESpawnTarget.Caster:
                    if (prefabSpawn.BodyPart != EBodyPart.None)
                    {
                        if (caster.GFXHandler.TryGetBodyPart(prefabSpawn.BodyPart, out GameObject bodyPartGO, trackError: true))
                            return bodyPartGO.transform;
                    }
                    return caster.transform;
                     
                case ESpawnTarget.Target:
                    if (targetController == null)
                    {
                        ErrorHandler.Error("Trying to spawn " + prefabSpawn.Prefab.name + " on TargetHit but targetController is None");
                        return null;
                    }
                    // check specific body part
                    if (prefabSpawn.BodyPart != EBodyPart.None)
                    {
                        if (targetController.GFXHandler.TryGetBodyPart(prefabSpawn.BodyPart, out GameObject bodyPartGO, trackError: true))
                            return bodyPartGO.transform;
                    }
                    return targetController.transform;

                case ESpawnTarget.OnSpell:
                    if (spell == null)
                    {
                        // It can happen that the spell is already destroyed when the client receive the data for "OnHit" or "OnEnd" effects.
                        // This is not an error then and his handled later
                        if (prefabSpawn.GFXLifetime.StartSpellPart <= ESpellEvent.OnHit)
                            ErrorHandler.Error("Trying to spawn " + prefabSpawn.Prefab.name + " OnSpell but spell is None");
                        return null;
                    }
                    return spell.transform;


                default:
                    ErrorHandler.Warning("SPrefabSpawn::Spawn() - Unhandled spawn location " + prefabSpawn.SpawnLocation);
                    return null;
            }
        }

        public static Vector3 CalculatePosition(Transform parent, SPrefabSpawn prefabSpawn, Controller controller, Vector3 callFromPosition)
        {
            Vector3 basePos = callFromPosition;
            if (parent != null)
                basePos = parent.transform.position;

            else if (prefabSpawn.SpawnTarget == ESpawnTarget.TargetPos)
                basePos = controller.SpellHandler.TargetPos;

            switch (prefabSpawn.SpawnLocation)
            {
                case ESpawnLocation.None:
                case ESpawnLocation.Center:
                    break;

                case ESpawnLocation.Ground:
                    basePos.y = 0;
                    break;

                default:
                    Debug.LogError("SPrefabSpawn::Spawn() - Unknown spawn location " + prefabSpawn.SpawnLocation);
                    break;
            }

            return basePos + new Vector3(prefabSpawn.Offset.x, prefabSpawn.Offset.y, 0);

        }

        void AdjustOrderInLayer(int orderInLayer)
        {
            if (orderInLayer == 0)
                return;
            
            // TODO
        }

        #endregion


        #region Duration

        protected virtual void CalculateDuration()
        {
            m_Duration = Mathf.Max(0, m_PrefabSpawn.GFXLifetime.Persistance);
            if (m_PrefabSpawn.GFXLifetime.EndSpellPart <= m_PrefabSpawn.GFXLifetime.StartSpellPart)
            {
                if (m_PrefabSpawn.GFXLifetime.Persistance <= 0)
                {
                    ErrorHandler.Warning("GFXLifetime of " + name + " is set with incoherent values");
                    ErrorHandler.Warning("      + StartSpellPart : " + m_PrefabSpawn.GFXLifetime.StartSpellPart);
                    ErrorHandler.Warning("      + EndSpellPart : " + m_PrefabSpawn.GFXLifetime.EndSpellPart);
                    ErrorHandler.Warning("      + Persistance : " + 0);
                    return;
                }
            }

            if (IsGFXAlive(ESpellEvent.OnStartCast))
                m_Duration += CalculateCastTime();

            if (IsGFXAlive(ESpellEvent.OnCast))
                m_Duration += m_SpellData.Delay;

            if (m_PrefabSpawn.GFXLifetime.EndAt > 0)
                m_Duration *= m_PrefabSpawn.GFXLifetime.EndAt;
        }

        /// <summary>
        /// Check if SpellGFX is alive at the provided time
        /// </summary>
        /// <param name="spellEvent"></param>
        /// <returns></returns>
        bool IsGFXAlive(ESpellEvent spellEvent)
        {
            return m_PrefabSpawn.GFXLifetime.StartSpellPart >= spellEvent && spellEvent < m_PrefabSpawn.GFXLifetime.EndSpellPart;
        }

        /// <summary>
        /// Anticipate how much time the spell will take to be casted
        /// </summary>
        /// <returns></returns>
        float CalculateCastTime()
        {
            return m_SpellData.AnimationTimer / m_Controller.SpellHandler.GetCastSpeed(m_SpellData.Spell);
        }

        #endregion


        #region Materials & Colors

        protected void ApplyMaterial()
        {
            if (m_PrefabSpawn.MaterialEffect == null)
                return;

            m_Controller.GFXHandler.ApplyMaterial(m_PrefabSpawn.MaterialEffect, m_BodyPart);
        }

        protected void RemoveMaterial()
        {
            if (m_PrefabSpawn.MaterialEffect == null)
                return;

            m_Controller.GFXHandler.RemoveMaterial(m_PrefabSpawn.MaterialEffect, m_BodyPart);
        }

        #endregion


        #region StateEffects

        protected void AddStateEffects()
        {
            foreach (var effect in m_PrefabSpawn.StateEffects)
                m_Controller.StateHandler.AddStateEffect(effect, m_Controller, duration: -1);
        }

        protected void RemoveStateEffects()
        {
            foreach (var effect in m_PrefabSpawn.StateEffects)
                m_Controller.StateHandler.RemoveStateEffect(effect);
        }

        #endregion


        #region Listeners

        protected virtual void RegisterListeners()
        {
            // Only register to SpellHandler events if this was spawn BEFORE casting
            if (m_PrefabSpawn.GFXLifetime.StartSpellPart < ESpellEvent.OnSpawn)
                m_Controller.SpellHandler.OnPreSpellEvent += OnPreSpellEvent;

            if (m_Spell != null)
                m_Spell.OnSpellEvent += OnSpellEvent;

            if (m_StateEffectName != null)
                m_Controller.StateHandler.OnStateEffectEvent += OnStateEffectEvent;
        }

        protected virtual void UnRegisterListeners()
        {
            if (m_Controller != null)
                m_Controller.SpellHandler.OnPreSpellEvent -= OnPreSpellEvent;

            if (m_Spell != null)
                m_Spell.OnSpellEvent -= OnSpellEvent;
            
            if (m_StateEffectName != null)
                m_Controller.StateHandler.OnStateEffectEvent -= OnStateEffectEvent;
        }

        protected virtual void OnPreSpellEvent(string spell, ESpellEvent spellEvent)
        {
            // --------
            // ISSUE with CancelCast()
            // --------
            //if (spell != m_SpellData.ToString())
            //    return;

            CheckEnd(spellEvent);
        }

        void OnSpellEvent(ESpellEvent spellEvent)
        {
            CheckEnd(spellEvent);
        }

        void OnStateEffectEvent(ESpellEvent spellEvent, string stateEffectName)
        {
            if (m_StateEffectName != stateEffectName)
                return;

            CheckEnd(spellEvent);
        }

        #endregion
    }
}