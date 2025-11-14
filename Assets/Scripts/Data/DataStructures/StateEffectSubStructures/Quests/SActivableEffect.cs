using Enums;
using System;


namespace Data.DataStructures.StateEffectSubStructures
{
    [Serializable]
    public struct SActivableEffect
    {
        public ESpellTarget         Target;
        public string               Effect;
        public int                  BonusLevel;
        public bool                 IsDeactivable;
        public int                  ReactivatedEveryStacks;

        int m_BaseLevel;
        public void SetLevel(int level) => m_BaseLevel = level;
        public int Level => m_BaseLevel + BonusLevel;
    }
}