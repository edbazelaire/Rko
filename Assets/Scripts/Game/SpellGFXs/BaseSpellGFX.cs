using Assets.Scripts.Managers.Sound;
using Data;
using Data.GameManagement;
using Enums;
using Game.Spells;
using MyBox;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Game.SpellGFXs
{
    public class BaseSpellGFX<TEnum> : MonoBehaviour where TEnum : Enum
    {
        #region Members

        // ======================================================================================
        // Compontents & GameObjects
        protected AudioSource       m_AudioSource;
        
        // ======================================================================================
        // Data
        protected Controller            m_Controller;
        protected SpellData             m_SpellData;
        protected Spell                 m_Spell;
        protected string                m_StateEffectName;
        protected SPrefabSpawn<TEnum>   m_PrefabSpawn;
        protected EBodyPart             m_BodyPart;

        protected Coroutine             m_EndCoroutine;
        protected bool                  m_EndStarted            = false;
        protected float                 m_PersistanceTimer      = -1f;
        protected float                 m_Duration;

        public string m_Name => m_SpellData != null ? m_SpellData.Name : (string.IsNullOrEmpty(m_StateEffectName) ? "" : m_StateEffectName);
        protected float m_Delay => m_PrefabSpawn.GFXLifetime.StartAt > 0 ? m_PrefabSpawn.GFXLifetime.StartAt * m_Duration : 0f;

        #endregion


        #region Init

        protected virtual void FindComponents() 
        {
            m_AudioSource = Finder.FindComponent<AudioSource>(gameObject);
        }

        public virtual void Initialize(Controller controller, SpellData spellData, Spell spell, string stateEffectName, SPrefabSpawn<TEnum> prefabSpawn)
        {
            if (controller == null)
            {
                ErrorHandler.Error("Controller is null");
                return;
            }

            m_Controller        = controller;
            m_SpellData         = spellData;
            m_Spell             = spell;
            m_StateEffectName   = stateEffectName;
            m_PrefabSpawn       = prefabSpawn;
            m_BodyPart          = prefabSpawn.BodyPart;

            if (prefabSpawn.Prefab != null && ArenaManager.IsInVoid(transform.position.x) && prefabSpawn.SpawnTarget != ESpawnTarget.MapCenter)
                ErrorHandler.Warning("Spell GFX spawned in void : " + m_Name + " - " + prefabSpawn.Prefab.name);

            // find components if any
            FindComponents();

            // set Parent & Position based on provided data
            transform.localScale *= prefabSpawn.Size > 0 ? prefabSpawn.Size : (spellData != null ? spellData.Size : 1);

            // adjust rotation depending on team
            transform.rotation = Quaternion.Euler(0f, controller.Team == 0 ? 0f : 180f, 0f); 

            // adjuste order and size of the elements
            AdjustOrderInLayer(prefabSpawn.OrderInLayer);

            // make controller play animation if any
            if (prefabSpawn.Animation != EAnimation.None)
            {
                controller.AnimationHandler.PlayAnimation(prefabSpawn.Animation != EAnimation.Self ? prefabSpawn.Animation.ToString() : spellData.Name);
            }

            // make spell play animation if any
            PlaySpellAnimation(prefabSpawn.SpellAnimation);

            // make controller play animation if any
            AdjustSoundFX();

            // make controller play animation if any
            PlayExtraSoundFX();

            // apply material of the effect on the target bodyparts
            ApplyMaterial();

            // allow children to apply post processing
            ApplyPostProcessing();

            // add state effects happening during the lifetime
            AddStateEffects();

            // if not specific end time : call immediat end
            if (prefabSpawn.GFXLifetime.EndSpellPart.ToString() == "None")
            {
                StartAnimation();
                End();
                return;
            }

            // register the listeners at the end of configuration
            RegisterListeners();

            // start animation
            StartAnimation();
        }

        protected virtual void StartAnimation() { }

        #endregion


        #region End

        /// <summary>
        /// Start the end of the spell, enabling Persistance
        /// </summary>
        public virtual void End()
        {
            if (m_EndStarted)
                return;

            ErrorHandler.Log("ENDED SPELL GFX : " + this.name, ELogTag.SpellGFX);

            // call that end has already started
            m_EndStarted = true;

            // stop the animation
            if (m_PrefabSpawn.Animation != EAnimation.None)
                m_Controller.AnimationHandler.CancelCastAnimation(m_PrefabSpawn.Animation);

            // remove the state effects of the animation
            RemoveStateEffects();

            // remove listeners
            UnRegisterListeners();

            // start the delayed end (if has persistant timer)
            StartCoroutine(EndCoroutine());
        }

        /// <summary>
        /// End the graphix immediatly without using the Graphics or Persistance
        /// </summary>
        protected virtual void ForceEnd()
        {
            // remove listeners
            UnRegisterListeners();

            Destroy(this);
        }

        /// <summary>
        /// If the graphics have persistance, delay the end of graphics
        /// </summary>
        /// <returns></returns>
        protected virtual IEnumerator EndCoroutine()
        {
            // delay destruction of spell graphismes for visual purpuses
            SetPersistantTimer();

            while (m_PersistanceTimer > 0)
            {
                m_PersistanceTimer -= Time.deltaTime;
                yield return null;
            }

            // remove material applied
            RemoveMaterial();

            // destroy the spell
            Destroy(gameObject);
        }

        protected virtual void SetPersistantTimer()
        {
            m_PersistanceTimer = m_PrefabSpawn.GFXLifetime.Persistance;
        }

        protected virtual void OnDestroy()
        {
            UnRegisterListeners();
        }

        #endregion


        #region Target & Position

        /// <summary>
        /// Based on configuration, find out the spawn parent
        /// </summary>
        /// <param name="prefabSpawn"></param>
        /// <param name="caster"></param>
        /// <param name="spell"></param>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public static Transform CalculateParent(SPrefabSpawn<TEnum> prefabSpawn, Controller caster, Spell spell, Controller targetController)
        {
            switch (prefabSpawn.SpawnTarget)
            {
                case ESpawnTarget.MapCenter:
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
                        return null;
                    }
                    return spell.transform;

                default:
                    ErrorHandler.Warning("SPrefabSpawn::Spawn() - Unhandled spawn Target " + prefabSpawn.SpawnTarget + " for prefab " + prefabSpawn.Prefab.name);
                    return null;
            }
        }

        /// <summary>
        /// Based on the configuration, find out the position relative to the parent
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="prefabSpawn"></param>
        /// <param name="controller"></param>
        /// <param name="callFromPosition"></param>
        /// <returns></returns>
        public static Vector3 CalculatePosition(Transform parent, SPrefabSpawn<TEnum> prefabSpawn, Controller controller, Vector3 callFromPosition, Vector3 targetPosition)
        {
            Vector3 basePos = callFromPosition;
            if (parent != null)
                basePos = parent.transform.position;

            else if (prefabSpawn.SpawnTarget == ESpawnTarget.TargetPos)
                basePos = targetPosition;

            else if (prefabSpawn.SpawnTarget == ESpawnTarget.MapCenter)
                basePos = Vector3.zero;

            switch (prefabSpawn.SpawnLocation)
            {
                case ESpawnLocation.None:
                case ESpawnLocation.Center:
                    break;

                case ESpawnLocation.Ground:
                    basePos.y = 0;
                    break;

                case ESpawnLocation.Hight:
                    basePos.y = Settings.SPELL_DIAGONAL_POS_Y;
                    break;

                case ESpawnLocation.Sky:
                    basePos.y = Settings.SPELL_HIGHT_POS_Y;
                    break;

                default:
                    Debug.LogError("SPrefabSpawn::Spawn() - Unknown spawn location " + prefabSpawn.SpawnLocation + " for prefab " + prefabSpawn.Prefab.name);
                    break;
            }

            int direction = 1;
            if (controller != null && controller.Team == 1)
                direction = -1; 

            return basePos + new Vector3(direction * prefabSpawn.Offset.x, prefabSpawn.Offset.y, 0);
        }

        void AdjustOrderInLayer(int orderInLayer)
        {
            if (orderInLayer == 0)
                return;

            var particles = Finder.FindComponents<ParticleSystemRenderer>(gameObject);
            foreach (var particle in particles)
            {
                particle.sortingOrder = orderInLayer;
            }
        }

        #endregion


        #region Duration

        protected virtual float GetDuration()
        {
            if (m_Duration == 0)
                CalculateDuration();

            return m_Duration;
        }

        protected virtual void CalculateDuration()
        {
            if (m_Duration != 0)
                return;

            m_Duration = 0;

            m_Duration += Mathf.Max(0, m_PrefabSpawn.GFXLifetime.Persistance);
        }

        /// <summary>
        /// Anticipate how much time the spell will take to be casted
        /// </summary>
        /// <returns></returns>
        protected virtual float CalculateCastTime()
        {
            return m_SpellData.AnimationTimer / m_Controller.SpellHandler.GetCastSpeed(m_SpellData.Spell.ToString());
        }

        #endregion


        #region Audio
        
        /// <summary>
        /// Adjust the sound effect of the Spell AudioSource
        /// </summary>
        protected virtual void AdjustSoundFX()
        {
            if (m_AudioSource == null)
                return;

            m_AudioSource.volume *= SoundFXManager.GetVolume(EVolumeOption.SoundEffectsVolume);

            if (! m_AudioSource.loop)
            {
                if (GetDuration() <= 0)
                    return;

                SoundFXManager.AdjustDuration(ref m_AudioSource, m_Duration);
            }
        }

        /// <summary>
        /// Play each extra sound FX
        /// </summary>
        protected virtual void PlayExtraSoundFX()
        {
            foreach (SSoundFX soundFX in m_PrefabSpawn.SoundFX)
            {
                switch (soundFX.SoundDuration)
                {
                    case ESoundDuration.PlayOnce:
                        SoundFXManager.PlayOnce(soundFX.AudioClip);
                        break;

                    case ESoundDuration.Loop:
                        SoundFXManager.PlaySoundFXClip(soundFX.AudioClip);
                        break;

                    case ESoundDuration.Fit:
                        SoundFXManager.PlayOnce(soundFX.AudioClip, GetDuration());
                        break;

                    default:
                        ErrorHandler.Warning("Unhandled case : " + soundFX.SoundDuration);
                        break;
                }
            }
        }

        #endregion


        #region Post Processing

        protected virtual void ApplyPostProcessing() { }

        protected virtual void ActivateObjects(List<GameObject> gameObjects, bool activate = true) 
        {
            if (gameObjects == default)
                return;

            foreach (var go in gameObjects)
            {
                go.SetActive(activate);
            }
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


        #region Animation

        void PlaySpellAnimation(string spellAnimation)
        {
            if (spellAnimation.IsNullOrEmpty())
                return;

            // Get SPELL
            if (m_Spell == null)
            {
                ErrorHandler.Warning("Unable to find spell to play animation " + spellAnimation);
                return;
            }

            // Get GRAPHICS
            if (m_Spell.Graphics == null)
            {
                ErrorHandler.Warning("Unable to find graphics in spell " + m_SpellData.Name + " to play animation " + spellAnimation);
                return;
            }

            // Get Animator
            var animator = Finder.FindComponent<Animator>(m_Spell.Graphics);
            if (animator == null)
            {
                ErrorHandler.Warning("Unable to find Animator to play animation " + spellAnimation + " in spell " + m_SpellData.Name);
                return;
            }

            // PLAY Animation
            animator.Play(spellAnimation);
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
            GameManager.Instance.State.OnValueChanged += OnGameStateChanged;
        }

        protected virtual void UnRegisterListeners()
        {
            if (GameManager.Exists)
                GameManager.Instance.State.OnValueChanged -= OnGameStateChanged;
        }

        protected virtual void OnGameStateChanged(EGameState oldState, EGameState newState)
        {
            if (newState != EGameState.GameOver) 
                return;

            ForceEnd();
        }

        #endregion
    }
}