using Enums;
using System;
using UnityEngine;

namespace Data.DataStructures.SpellSubStructures.SpellModules
{
    [Serializable]
    public class MChanneling
    {
        #region Members

        // =======================================================================
        // Serialized data
        [Tooltip("Can the spell be cancelled by the Owner ? (by moving or casting another spell)")]
        public bool IsCancellable   = true;
        [Tooltip("Can the spell be cancelled by the Enemy ? (by appling CC)")]
        public bool IsInterruptable = true;
        [SerializeField, Tooltip("Movement speed (percentage) of the character during the Channeling : O - immobilized, 1 - 100% of current speed")]
        protected float m_ForcedMovementSpeed = 0f;
        [Tooltip("Animation to use during channel")]
        public EAnimation Animation;

        // =======================================================================
        // Public accessors
        public float ForcedMovementSpeed => Mathf.Max(m_ForcedMovementSpeed, 0f);

        #endregion
    }
}