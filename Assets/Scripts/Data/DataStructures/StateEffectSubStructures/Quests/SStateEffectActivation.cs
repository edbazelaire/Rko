using Enums;
using System;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public struct SStateEffectActivation
    {
        public EStateEffectEvent    StateEffectEvent;
        public string               StateEffectName;
        public int                  Stacks;
    }

    [Serializable]
    public struct SSpellActivation
    {
        public ESpellEvent          SpellEvent;
        public string               SpellName;
        public int                  Stacks;
    }
}