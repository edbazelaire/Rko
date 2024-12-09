using Enums;
using Game;
using Tools;
using UnityEditor;
using UnityEngine;

namespace Data
{
    [CreateAssetMenu(fileName = "Jump", menuName = "Game/Spells/Jump")]
    public class JumpData : ProjectileData
    {
        public override ESpellType SpellType => ESpellType.Jump;

        [Header("Jump Data")]
        public EJumpType JumpType           = EJumpType.None;
        public EAnimation JumpAnimation     = EAnimation.Jump;

        #region Target & Position

        public override void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId)
        {
            //base.RecalculatePosition(ref position, target, clientId);

            // ===========================================================================================
            // TODO : change this by "RecalculatePosition" : 
            // Re-do position calcul before using same method as SpellGFX
            position = GameManager.Instance.GetPlayer(clientId).transform.position;
            // ===========================================================================================
        }

        #endregion
    }
}