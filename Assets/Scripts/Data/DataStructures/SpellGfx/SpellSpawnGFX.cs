using Data;
using Enums;
using Game.SpellGFXs;
using Game.Spells;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Assets.Scripts.Data.DataStructures
{
    [Serializable]
    public class SpellPrefabSpawn : SPrefabSpawn<ESpellEvent>
    {
        public SpellPrefabSpawn(GameObject prefab, Material materialEffect, List<SSoundFX> soundFX, SGFXLifetime<ESpellEvent> gfxLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, bool isReplacingGFX, Vector2 offset, EAnimation animation, List<EStateEffect> stateEffects = null, float size = 0, int orderInLayer = 0) : base(prefab, materialEffect, soundFX, gfxLifetime, spawnTarget, spawnLocation, bodyPart, isFollowing, isReplacingGFX, offset, animation, stateEffects, size, orderInLayer)
        {
        }

        protected override BaseSpellGFX<ESpellEvent> InitializeGFXComponent(GameObject go, Controller caster, SpellData spellData, Spell spell, string stateEffectName, Controller targetController, float? forcedDuration = null)
        {
            if (go == null)
                return null;

            if (! go.TryGetComponent(out SpellGFX spellGfx))
            {
                spellGfx = go.AddComponent<SpellGFX>();
            }

            spellGfx.Initialize(
                controller:         this.SpawnTarget != ESpawnTarget.Target ? caster : targetController, 
                spellData:          spellData, 
                spell:              spell, 
                stateEffectName:    stateEffectName, 
                prefabSpawn:        this,
                forcedDuration:     forcedDuration
            );

            return spellGfx;
        }
    }
}