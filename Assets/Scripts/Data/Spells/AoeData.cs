using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Aoe", menuName = "Game/Spells/Aoe")]
    public class AoeData : SpellData
    {
        public override ESpellType SpellType        => ESpellType.Aoe;
        //public override EHitCategory SpellCategory  => EHitCategory.Aoe;

        [Header("AoeData")]
        [SerializeField] protected ESpellSpawn m_SpellSpawn = ESpellSpawn.Ground;

        public ESpellSpawn SpellSpawn => m_SpellSpawn;

        public override void OverrideSpellSpawn(ESpellSpawn spellSpawn)
        {
            m_SpellSpawn = spellSpawn;
        }
    }
}