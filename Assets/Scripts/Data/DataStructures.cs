using Assets.Scripts.Game;
using Assets.Scripts.Managers.Sound;
using Enums;
using Game.Loaders;
using Game.SpellGFXs;
using Game.Spells;
using MyBox;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;


namespace Data
{
    [Serializable]
    public struct SMinMax
    {
        public float Min;
        public float Max;
    }

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
        public float                        BonusStacksPerLevel;
        public List<SStateEffectProperty>   OverridingProperties;

        int m_Level;

        public SStateEffectData(EStateEffect stateEffect, int stacks = 1, float bonusStacks = 0, int level = 1, List<SStateEffectProperty> overridingProperties = default)
        {
            StateEffect             = stateEffect;
            Stacks                  = stacks;
            BonusStacksPerLevel     = bonusStacks;
            OverridingProperties    = overridingProperties;

            m_Level = level;
        }

        public void SetLevel(int level)
        {
            m_Level = level;
        }

        public int GetStacks()
        {
            return Stacks + (int)Math.Floor(m_Level * BonusStacksPerLevel);
        }

        public string Description => TextHandler.ReplaceStateEffectTokens($"apply {GetStacks()} stacks of [{StateEffect}]");

        public string EffectDescription => SpellLoader.GetStateEffect(StateEffect, m_Level).GetDescription();
    }

    [Serializable]
    public class SGFXLifetime<TEnum> where TEnum : Enum 
    {
        [SerializeField] protected TEnum m_StartSpellPart;
        [SerializeField] protected TEnum m_EndSpellPart;

        public float StartAt;
        public float EndAt;
        public float Persistance;

        public TEnum StartSpellPart => m_StartSpellPart;
        public TEnum EndSpellPart   => m_EndSpellPart;

        public SGFXLifetime(TEnum startSpellPart, TEnum endSpellPart, float startAt = 0f, float endAt = 1f, float persistance = 0f)
        {
            m_StartSpellPart = startSpellPart;
            m_EndSpellPart = endSpellPart;

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
    public struct SSoundFX
    {
        public AudioClip        AudioClip;
        public ESoundDuration   SoundDuration;
    }


    [Serializable]
    public class SPrefabSpawn<TEnum> where TEnum : Enum
    {
        #region Members

        public GameObject           Prefab;
        public Material             MaterialEffect;
        public List<SSoundFX>       SoundFX;
        public SGFXLifetime<TEnum>  GFXLifetime;
        public float                Size;
        public int                  OrderInLayer;

        public ESpawnTarget         SpawnTarget;
        public ESpawnLocation       SpawnLocation;
        public EBodyPart            BodyPart;
        public bool                 IsFollowing;
        public Vector2              Offset;

        public EAnimation           Animation;
        public string               SpellAnimation;
        public List<EStateEffect>   StateEffects;

        /// <summary>
        /// Check if is only made with asynchron sounds
        /// </summary>
        public bool IsAsyncSoundOnly
        {
            get
            {
                // must have no prefab or material attached
                if (Prefab != null 
                    || MaterialEffect != null 
                    || StateEffects.Count != 0 
                    || Animation != EAnimation.None 
                    || ! SpellAnimation.IsNullOrEmpty())
                    return false;

                // must contains sounds
                if (SoundFX.Count == 0)
                {
                    ErrorHandler.Error("SPrefabSpawn has no Prefab, Material or Sound attached");
                    return false;
                }

                foreach (var soundFX in SoundFX)
                {
                    if (soundFX.SoundDuration != ESoundDuration.PlayOnce)
                        return false;
                }

                return true;
            }
        }

        #endregion


        #region Contructor

        public SPrefabSpawn(GameObject prefab, Material materialEffect, List<SSoundFX> soundFX, SGFXLifetime<TEnum> gfxLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, Vector2 offset, EAnimation animation, List<EStateEffect> stateEffects = default, float size = 0f, int orderInLayer = 0)
        {
            Prefab          = prefab;
            MaterialEffect  = materialEffect;
            SoundFX         = soundFX;
            GFXLifetime     = gfxLifetime;
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
        /// Spawns a prefab for a defined lifetime with specific enum events
        /// </summary>
        public BaseSpellGFX<TEnum> Spawn(
            Controller caster,
            SpellData spellData,
            Spell spell = null,
            string stateEffectName = null,
            Controller targetController = null,
            Vector3 callFromPosition = default,
            Vector3 targetPos = default,
            float? forcedDuration = null
            )
        {
            if (IsAsyncSoundOnly)
            {
                PlaySoundOnly();
                return null;
            }

            GameObject go = InstantiatePrefab(caster, spell, targetController, callFromPosition, targetPos);
            return InitializeGFXComponent(go, caster, spellData, spell, stateEffectName, targetController, forcedDuration);
        }

        private GameObject InstantiatePrefab(Controller caster, Spell spell, Controller targetController, Vector3 callFromPosition, Vector3 targetPos)
        {
            var parent = BaseSpellGFX<TEnum>.CalculateParent(this, caster, spell, targetController);
            var position = BaseSpellGFX<TEnum>.CalculatePosition(parent, this, caster, callFromPosition, targetPos);

            // SAFETY : do not display GFX spawning in void
            if (position == Vector3.zero && SpawnTarget != ESpawnTarget.MapCenter)
            {
                ErrorHandler.Warning($"Spell GFX spawned in void - spell : { (spell != null ? spell.name : "null")} | position : {position} | callFromPosition : {callFromPosition} | targetPos : {targetPos} ");
                return null;
            }

            if (Prefab.name.Contains("FireTrail"))
                Debug.Log("InstantiatePrefab() FireTrail GFX");

            return PoolManager.Pool(Prefab, position, Quaternion.identity, IsFollowing ? parent : null);
        }

        protected virtual BaseSpellGFX<TEnum> InitializeGFXComponent(
            GameObject go,
            Controller caster,
            SpellData spellData,
            Spell spell,
            string stateEffectName,
            Controller targetController,
            float? forcedDuration
            )
        {
            return null;
        }
 
        public void PlaySoundOnly()
        {
            foreach (SSoundFX soundFX in SoundFX)
            {
                switch (soundFX.SoundDuration)
                {
                    case ESoundDuration.PlayOnce:
                        SoundFXManager.PlayOnce(soundFX.AudioClip);
                        break;

                    default:
                        ErrorHandler.Warning("Unhandled case : " + soundFX.SoundDuration);
                        break;
                }
            }
        }

        #endregion

    }


}