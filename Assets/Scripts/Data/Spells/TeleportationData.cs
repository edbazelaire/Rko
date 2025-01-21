using Enums;
using UnityEngine;

namespace Data.Spells
{
    [CreateAssetMenu(fileName = "TeleportationSpell", menuName = "Game/Spells/TeleportationSpellData")]
    public class TeleportationData : SpellData
    {
        public override ESpellType SpellType => ESpellType.Teleportation;

        [Header("TeleportationSpell")]
        [SerializeField, Tooltip("Is the spell graphics attached to the target")]
        protected bool m_Display = false;


        #region Targetting


        #endregion
    }
}