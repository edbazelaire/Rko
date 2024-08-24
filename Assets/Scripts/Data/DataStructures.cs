using Enums;
using Game.SpellGFXs;
using Game.Spells;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.UIElements;


namespace Data
{
    [Serializable] 
    public struct SStateEffectProperty
    {
        public EStateEffectProperty StateEffectProperty;
        public float Value;

        public SStateEffectProperty(EStateEffectProperty stateEffectProperty, float value)
        {
            StateEffectProperty = stateEffectProperty;
            Value = value;
        }
    }

    /// <summary>
    /// Structure allowing to define a state effect in the SpellData inspector
    /// </summary>
    [Serializable]
    public struct SStateEffectData
    {
        public EStateEffect                 StateEffect;
        public int                          Stacks;
        public List<SStateEffectProperty>   OverridingProperties;

        public SStateEffectData(EStateEffect stateEffect, int stacks = 1, List<SStateEffectProperty> overridingProperties = default)
        {
            StateEffect             = stateEffect;
            Stacks                  = stacks;
            OverridingProperties    = overridingProperties;
        }
    }

    [Serializable]
    public struct SGFXLifetime
    {
        public ESpellEvent StartSpellPart;
        public ESpellEvent EndSpellPart;
        public float StartAt;
        public float EndAt;
        public float Persistance;

        public SGFXLifetime(ESpellEvent startSpellSpart, ESpellEvent endSpellSpart = ESpellEvent.None, float startAt = 0f, float endAt = 1f, float persistance = 0f)
        {
            StartSpellPart = startSpellSpart;
            EndSpellPart = endSpellSpart;

            if (startAt < 0)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (endAt < 0)
            {
                ErrorHandler.Error($"EndAt ({endAt}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (persistance < 0)
            {
                ErrorHandler.Error($"PersistanceAfterEnd ({persistance}) set with value < 0 - setting by default with value 0 ");
                startAt = 0;
            }

            if (startAt > 1)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value > 1 - setting by default with value 0 ");
                startAt = 0;
            }

            if (startAt > endAt)
            {
                ErrorHandler.Error($"StartAt ({startAt}) set with value > ({endAt}) - setting with default value 0 and 1");
                startAt = 0;
                endAt = 1f;
            }

            StartAt = startAt;
            EndAt = endAt;
            Persistance = persistance;
        }
    }

    [Serializable]
    public struct SPrefabSpawn
    {
        #region Members

        public GameObject           Prefab;
        public Material             MaterialEffect;
        public SGFXLifetime         GFXLifetime;
        public float                Size;
        public int                  OrderInLayer;

        public ESpawnTarget         SpawnTarget;
        public ESpawnLocation       SpawnLocation;
        public EBodyPart            BodyPart;
        public bool                 IsFollowing;
        public Vector2              Offset;

        public EAnimation           Animation;
        public List<EStateEffect>   StateEffects;

        #endregion


        #region Contructor

        public SPrefabSpawn(GameObject prefab, Material materialEffect, SGFXLifetime gFXLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, Vector2 offset, EAnimation animation, List<EStateEffect> stateEffects = default, float size = 0f, int orderInLayer = 0)
        {
            Prefab          = prefab;
            MaterialEffect  = materialEffect;
            GFXLifetime     = gFXLifetime;
            Size            = size;
            OrderInLayer    = orderInLayer;

            SpawnTarget     = spawnTarget;
            SpawnLocation   = spawnLocation;
            BodyPart        = bodyPart;
            IsFollowing     = isFollowing;
            Offset          = offset;

            Animation       = animation;
            StateEffects    = stateEffects;
        }

        #endregion


        #region Spawn & Instantiation

        /// <summary>
        /// Spawn a Prefab for a defined lifetime
        /// </summary>
        /// <param name="caster">   Controller at the origin of this action </param>
        /// <param name="spellData">    SpellData of the spell we are trying to cast or was casted </param>
        /// <param name="spell">        If the spell has already spawned, provide it (otherwise will be null) </param>
        /// <returns></returns>
        public readonly SpellGFX Spawn(Controller caster, SpellData spellData, Spell spell = null, string stateEffectName = null, Controller targetController = null, Vector3 callFromPosition = default)
        {
            GameObject go;
            if (Prefab == null)
                go = new GameObject();
            else
            {
                var parent = SpellGFX.CalculateParent(this, caster, spell, targetController);
                var position = SpellGFX.CalculatePosition(parent, this, caster, callFromPosition);
                go = GameObject.Instantiate(Prefab, position, Quaternion.identity, IsFollowing ? parent : null);
            }
            
            if (! go.TryGetComponent(out SpellGFX spellGfx))
            {
                // NO SPECIFIC COMPONENT : add default spell graphics component
                spellGfx = go.AddComponent<SpellGFX>();
            }

            spellGfx.Initialize(this.SpawnTarget != ESpawnTarget.Target ? caster : targetController, spellData, spell, stateEffectName, this);
            return spellGfx;
        }

        #endregion

    }
}