using Enums;
using Game.Spells;
using System;

namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public struct SDescriptionVariable
    {
        public string Name;
        public bool WithIcon;

        public SDescriptionVariable(string name, bool withIcon = true)
        {
            Name = name;
            WithIcon = withIcon;
        }
    }
}