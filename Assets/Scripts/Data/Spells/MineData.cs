using Assets.Scripts.Data.DataStructures;
using Enums;
using Game.Spells;
using System.Collections.Generic;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Mine", menuName = "Game/Spells/Mine")]
    public class MineData : SpellData
    { 
        public override ESpellType SpellType    => ESpellType.Mine;
        public override Enums.ESpellCategory SpellCategory  => Enums.ESpellCategory.Zone;


        [Header("MineData")]
        [SerializeField] protected SpellData    m_ActivationData;
        [SerializeField] protected int          m_NumActivations;
        [SerializeField] protected float        m_InactiveTimer;
        [SerializeField] protected float        m_ArmedTimer;
        [SerializeField] protected float        m_TrigerredTimer;
        [SerializeField] protected float        m_ActivateTimer;

        [SerializeField] protected List<MinePrefabSpawn> m_MineSpawnGFX;

        public SpellData    ActivationData          => m_ActivationData;
        public int          NumActivations          => m_NumActivations;
        public float        InactiveTimer           => m_InactiveTimer;
        public float        ArmedTimer              => m_ArmedTimer;
        public float        TrigerredTimer          => m_TrigerredTimer;
        public float        ActivateTimer           => m_ActivateTimer;
        public List<MinePrefabSpawn> MineSpawnGFX   => m_MineSpawnGFX;
    }
}