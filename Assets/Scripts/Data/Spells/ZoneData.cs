using Enums;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEngine;
using UnityEngine.Video;

namespace Data
{
    [CreateAssetMenu(fileName = "Zone", menuName = "Game/Spells/Zone")]
    public class ZoneData : AoeData
    {
        public override ESpellType SpellType => ESpellType.Zone;

        [Header("Zone Data")]
        [Description("Tick over time duration of re-appliance of the spell")]
        public float DurationTick = 0f;
        [Description("Damages delt at each ticks")]
        [SerializeField] protected int m_TickDamages = 0;
        [Description("Heals provided at each ticks")]
        [SerializeField] protected int m_TickHeal = 0;
        [Description("Shield provided at each ticks")]
        [SerializeField] protected int m_TickShield = 0;
        [Description("Growing factor of the spell (as bonus percentage)")]
        public float GrowSizeFactor = 0f;
        [Description("Percentage of the duration where the Zone will reach max size")]
        public float MaxSizeAt = 1f;
        [Description("List of effects that proc while the player is in the zone")]
        public List<SStateEffectData> PersistentStateEffects;

        // ==================================================================================================
        // PUBLIC ACCESSORS
        public int TickDamages => (int)Mathf.Round(m_TickDamages * GetSpellLevelFactor(ESpellProperty.TickDamages));
        public int TickHeal => (int)Mathf.Round(m_TickHeal * GetSpellLevelFactor(ESpellProperty.TickHeal));
        public int TickShield => (int)Mathf.Round(m_TickShield * GetSpellLevelFactor(ESpellProperty.TickShield));


        #region Infos & Description

        public override Dictionary<string, object> GetInfos()
        {
            var infosDict = base.GetInfos();

            if (TickDamages > 0)
                infosDict.Add("TickDamages", TickDamages);
            if (TickHeal > 0)
                infosDict.Add("TickHeal", TickHeal);
            if (TickShield > 0)
                infosDict.Add("TickShield", TickShield);

            if (PersistentStateEffects.Count > 0)
            {
                if (!infosDict.ContainsKey("Effects"))
                    infosDict.Add("Effects", new List<SStateEffectData>());
                ((List<SStateEffectData>)infosDict["Effects"]).AddRange(PersistentStateEffects);
            }

            return infosDict;
        }

        #endregion
    }
}