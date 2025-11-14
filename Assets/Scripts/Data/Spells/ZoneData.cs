using Data.DataStructures.SpellSubStructures;
using Enums;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

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
        [FormerlySerializedAs("m_TickDamage")]
        [SerializeField, Tooltip("Damage delt at each ticks")]
        protected int                   m_DotDamage   = 0;
        [FormerlySerializedAs("m_TickHeal")]
        [SerializeField, Tooltip("Heals provided at each ticks")]
        protected int                   m_DotHeal      = 0;
        [FormerlySerializedAs("m_TickHeal")]
        [SerializeField, Tooltip("Shield provided at each ticks")]
        protected int                   m_DotShield    = 0;
        [FormerlySerializedAs("m_TickHeal")]
        [SerializeField, Tooltip("Energy provided at each ticks")]
        protected int                   m_DotEnergy    = 0;
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
        public int DotDamage       => (int)GetScaledValue(ESpellProperty.DotDamage, m_DotDamage);
        public int DotHeal          => (int)GetScaledValue(ESpellProperty.DotHeal, m_DotHeal);
        public int DotShield        => (int)GetScaledValue(ESpellProperty.DotShield, m_DotShield);
        public int DotEnergy        => (int)GetScaledValue(ESpellProperty.DotEnergy, m_DotEnergy);
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
            infosDict.Add("Tick", DurationTick);

            if (DotDamage > 0)
                infosDict.Add("DotDamage", DotDamage);
            if (DotHeal > 0)
                infosDict.Add("DotHeal", DotHeal);
            if (DotShield > 0)
                infosDict.Add("DotShield", DotShield);
            if (DotEnergy > 0)
                infosDict.Add("DotEnergy", DotEnergy);

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