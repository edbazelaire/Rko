using Enums;
using Game.SpellGFX;
using Game.Spells;
using System;
using Tools;
using UnityEngine;


namespace Data
{
    /// <summary>
    /// Structure allowing to define a state effect in the SpellData inspector
    /// </summary>
    [Serializable]
    public struct SStateEffectData
    {
        public EStateEffect     StateEffect;
        public int              Stacks;
        public float            Duration;
        public float            SpeedBonus;

        public readonly bool OverrideDuration    => Duration > 0;
        public readonly bool OverrideSpeedBonus  => SpeedBonus != 0;

        public SStateEffectData(EStateEffect stateEffect, int stacks = 1, float duration = -1f, float speedBonus = 0f)
        {
            StateEffect = stateEffect;
            Stacks      = stacks;
            Duration    = duration;
            SpeedBonus  = speedBonus;
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

        public GameObject       Prefab;
        public SGFXLifetime     GFXLifetime;
        public float            Size;
        public int              OrderInLayer;

        public ESpawnTarget     SpawnTarget;
        public ESpawnLocation   SpawnLocation;
        public EBodyPart        BodyPart;
        public bool             IsFollowing;
        public Vector2          Offset;

        public EAnimation       Animation;

        #endregion


        #region Contructor

        public SPrefabSpawn(GameObject prefab, SGFXLifetime gFXLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, Vector2 offset, EAnimation animation, float size = 0f, int orderInLayer = 0)
        {
            Prefab          = prefab;
            GFXLifetime     = gFXLifetime;
            Size            = size;
            OrderInLayer    = orderInLayer;

            SpawnTarget     = spawnTarget;
            SpawnLocation   = spawnLocation;
            BodyPart        = bodyPart;
            IsFollowing     = isFollowing;
            Offset          = offset;

            Animation       = animation;
        }

        #endregion


        #region Spawn & Instantiation

        public readonly GameObject Spawn(Controller controller, SpellData spellData, Spell spell)
        {
            if (Animation != EAnimation.None)
                controller.AnimationHandler.PlayAnimation(Animation);

            if (Prefab == null)
                return null;

            var parent = SpellGFX.CalculateParent(this, controller, spell);
            var position = SpellGFX.CalculatePosition(parent, this, controller);

            GameObject go = GameObject.Instantiate(Prefab, position, Quaternion.identity, IsFollowing ? parent : null);
            if (go.TryGetComponent(out SpellGFX spellGfx))
            {
                spellGfx.Initialize(controller, spellData, spell, this);
            }

            //else if (go.TryGetComponent(out Spell spawnSpell))
            //{
            //    spawnSpell.Cast();
            //}

            // NO SPECIFIC COMPONENT : add default spell graphix component
            else
            {
                go.AddComponent<SpellGFX>().Initialize(controller, spellData, spell, this);
            }

            return go;
        }

        #endregion

    }
}