using Enums;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "DefaultSpellData", menuName = "Game/Spells/DefaultSpellData")]
    public class DefaultSpellData : SpellData
    {
        [SerializeField] protected ESpellType m_SpellType;

        public override ESpellType SpellType => m_SpellType;

    }
}