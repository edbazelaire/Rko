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

        public new RuneData Clone(int level = 0, bool destroy = false)
        {
            return (RuneData)base.Clone(level, destroy);
        }
    }
}