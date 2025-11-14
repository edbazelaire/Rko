using Data.DataStructures.SpellSubStructures;
using Enums;
using Game;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Beam", menuName = "Game/Spells/Beam")]
    public class BeamData : ZoneData
    {
        #region Members

        public override ESpellType SpellType => ESpellType.Beam;

        [Header("Beam Data")]
        [SerializeField, Tooltip("Size of the ray (as percentage of spell global size)")]
        protected float m_RaySizePerc               = 1f;

        [SerializeField, Tooltip("Speed at which the beam extends (world units per second)")]
        protected float m_Speed                     = 0.25f;

        [SerializeField, Tooltip("Maximum length of the beam (if no wall is hit first)")]
        protected float m_MaxLength                 = -1f;

        [SerializeField, Tooltip("If true, beam rotates to follow target position each frame")]
        protected ESpellTarget m_TargetToFollow     = ESpellTarget.None;

        [SerializeField, Tooltip("If true, beam rotates to follow target position each frame")]
        protected float m_FollowingSpeed            = -1f;

        // ==================================================================================================
        // Public Accessors
        public float            RaySizePerc                 => m_RaySizePerc;
        public float            RaySize                     => Size * RaySizePerc;
        public float            Speed                       => GetScaledValue(ESpellProperty.Speed, m_Speed);
        public float            MaxLength                   => m_MaxLength;
        public bool             FollowTarget                => m_TargetToFollow != ESpellTarget.None; 
        public ESpellTarget     TargetToFollow              => m_TargetToFollow;
        public float            FollowingSpeed              => m_FollowingSpeed;

        #endregion


        #region Spawning

        protected override Transform FindParent(ulong clientId)
        {
            return m_SpawnPosition.CalculateParent(GameManager.Instance.GetPlayer(clientId), spell: null, targetController: null);
        }

        #endregion
    }
}
