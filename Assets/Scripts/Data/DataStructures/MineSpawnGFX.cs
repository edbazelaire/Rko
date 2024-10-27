using Assets.Scripts.Data.DataStructures.GFXLifetime;
using Assets.Scripts.Managers.Sound;
using Data;
using Enums;
using Game.SpellGFXs;
using Game.Spells;
using System;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures
{
    [Serializable]
    public class MineSpawnGFX
    {
        #region Members

        public GameObject       Prefab;
        public Material         MaterialEffect;
        public List<SSoundFX>   SoundFX;
        public MineGFXLifetime  GFXLifetime;
        public float            Size;
        public int              OrderInLayer;

        public ESpawnTarget     SpawnTarget;
        public ESpawnLocation   SpawnLocation;
        public EBodyPart        BodyPart;
        public bool             IsFollowing;
        public Vector2          Offset;

        public EAnimation Animation;
        public List<EStateEffect> StateEffects;

        /// <summary>
        /// Check if is only made with asynchron sounds
        /// </summary>
        public bool IsAsyncSoundOnly
        {
            get
            {
                // must have no prefab or material attached
                if (Prefab != null || MaterialEffect != null)
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

        public MineSpawnGFX(GameObject prefab, Material materialEffect, List<SSoundFX> soundFX, MineGFXLifetime gFXLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, Vector2 offset, EAnimation animation, List<EStateEffect> stateEffects = default, float size = 0f, int orderInLayer = 0)
        {
            Prefab = prefab;
            MaterialEffect = materialEffect;
            SoundFX = soundFX;
            GFXLifetime = gFXLifetime;
            Size = size;
            OrderInLayer = orderInLayer;

            SpawnTarget = spawnTarget;
            SpawnLocation = spawnLocation;
            BodyPart = bodyPart;
            IsFollowing = isFollowing;
            Offset = offset;

            Animation = animation;
            StateEffects = stateEffects;
        }

        #endregion


        #region Spawn & Instantiation

        /// <summary>
        /// Spawn a Prefab for a defined lifetime
        /// </summary>
        /// <param name="caster">       Controller at the origin of this action </param>
        /// <param name="spellData">    SpellData of the spell we are trying to cast or was casted </param>
        /// <param name="spell">        If the spell has already spawned, provide it (otherwise will be null) </param>
        /// <returns></returns>
        public SpellGFX Spawn(Controller caster, SpellData spellData, Spell spell = null, string stateEffectName = null, Controller targetController = null, Vector3 callFromPosition = default, Vector3 targetPos = default)
        {
            return null;

            // if is only asynchrone sounds, just play the sounds and leave
            if (IsAsyncSoundOnly)
            {
                PlaySoundOnly();
                return null;
            }

            GameObject go;
            if (Prefab == null)
                go = new GameObject();
            else
            {
                Transform parent = null;
                var position = Vector3.zero;

                //var parent = SpellGFX.CalculateParent(this, caster, spell, targetController);
                //var position = SpellGFX.CalculatePosition(parent, this, caster, callFromPosition, targetPos);

                if (position == Vector3.zero)
                {
                    ErrorHandler.Error("Spell GFX spawned in void : " + spellData.Name + " - " + Prefab.name);
                    return null;
                }

                go = GameObject.Instantiate(Prefab, position, Quaternion.identity, IsFollowing ? parent : null);
            }

            if (!go.TryGetComponent(out SpellGFX spellGfx))
            {
                // NO SPECIFIC COMPONENT : add default spell graphics component
                spellGfx = go.AddComponent<SpellGFX>();
            }

            //spellGfx.Initialize(this.SpawnTarget != ESpawnTarget.Target ? caster : targetController, spellData, spell, stateEffectName, this);
            return spellGfx;
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