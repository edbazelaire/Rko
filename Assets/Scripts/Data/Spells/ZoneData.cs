using Assets.Scripts.Data.DataStructures.SpellSubStructures;
using Data.DataStructures.SpellSubStructures;
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
        #region Members

        public override ESpellType SpellType => ESpellType.Zone;

        [Header("Zone Data")]
        [Tooltip("Tick over time duration of re-appliance of the spell")]
        public float                    DurationTick    = 0f;
        [SerializeField, Tooltip("Damage delt at each ticks")]
        protected int                   m_TickDamage   = 0;
        [SerializeField, Tooltip("Heals provided at each ticks")]
        protected int                   m_TickHeal      = 0;
        [SerializeField, Tooltip("Shield provided at each ticks")]
        protected int                   m_TickShield    = 0;
        [SerializeField, Tooltip("Continuous force in the zone")]
        protected SForce                m_ZoneForce     = default;
        [Tooltip("Growing factor of the spell (as bonus percentage)")]
        public float                    GrowSizeFactor  = 0f;
        [Tooltip("Percentage of the duration where the Zone will reach max size")]
        public float                    MaxSizeAt       = 1f;
        [Tooltip("List of effects that proc while the player is in the zone")]
        public List<SStateEffectData>   PersistentStateEffects;

        // ==================================================================================================
        // PUBLIC ACCESSORS
        public int TickDamage       => (int)GetScaledValue(ESpellProperty.TickDamage, m_TickDamage);
        public int TickHeal         => (int)GetScaledValue(ESpellProperty.TickHeal, m_TickHeal);
        public int TickShield       => (int)GetScaledValue(ESpellProperty.TickShield, m_TickShield);
        public SForce ZoneForce     => m_ZoneForce;

        #endregion


        #region Clone & Level

        public override void SetLevel(int level)
        {
            base.SetLevel(level);

            m_ZoneForce.SetLevel(level);
        }

        #endregion


        #region Infos & Description

        public override Dictionary<string, object> GetInfo()
        {
            var infosDict = base.GetInfo();

            if (TickDamage > 0)
                infosDict.Add("TickDamage", TickDamage);
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