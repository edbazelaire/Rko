using Enums;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "RuneData", menuName = "Game/Runes/Default")]
    public class RuneData : CollectableData
    {
        [Description("Description informations of the Rune")]
        public string Description;

        public virtual string GetDescription()
        {
            return Description;
        }
    }
}