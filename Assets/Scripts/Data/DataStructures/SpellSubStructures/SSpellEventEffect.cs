using Enums;
using Game.Spells;
using System;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SSpellEventEffect
    {
        public string EffectName;
        public ESpellEvent SpellEvent;
        public ESpellTarget SpellTarget;
    }
}