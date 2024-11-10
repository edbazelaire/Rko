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
    public class MinePrefabSpawn : SPrefabSpawn<EMineState>
    {
        public MinePrefabSpawn(GameObject prefab, Material materialEffect, List<SSoundFX> soundFX, SGFXLifetime<EMineState> gfxLifetime, ESpawnTarget spawnTarget, ESpawnLocation spawnLocation, EBodyPart bodyPart, bool isFollowing, Vector2 offset, EAnimation animation, List<EStateEffect> stateEffects = null, float size = 0, int orderInLayer = 0) : base(prefab, materialEffect, soundFX, gfxLifetime, spawnTarget, spawnLocation, bodyPart, isFollowing, offset, animation, stateEffects, size, orderInLayer)
        {
        }

        protected override BaseSpellGFX<EMineState> InitializeGFXComponent(GameObject go, Controller caster, SpellData spellData, Spell spell, string stateEffectName, Controller targetController)
        {
            if (! go.TryGetComponent(out MineSpellGFX mineSpellGFX))
            {
                mineSpellGFX = go.AddComponent<MineSpellGFX>();
            }

            mineSpellGFX.Initialize(this.SpawnTarget != ESpawnTarget.Target ? caster : targetController, spellData, spell, stateEffectName, this);
            return mineSpellGFX;
        }
    }
}