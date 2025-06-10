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
}