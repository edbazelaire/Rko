using Game.Spells;
using System;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SSpellPassiveEffect
    {
        public StateEffect StateEffect;
        public bool DeactivateOnCooldown;
    }
}