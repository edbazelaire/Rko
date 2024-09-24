using Enums;
using Game;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;
using Data.GameManagement;

namespace Data
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Game/Spells/Projectile")]
    public class ProjectileData : SpellData
    {
        #region Members
        public override ESpellType SpellType => ESpellType.Projectile;

        [Header("Movement Data")]
        [Description("Type of path that the spell is taking")]
        public ESpellTrajectory Trajectory;
        [Description("Should the projectile end when reaching target position ?")]
        [SerializeField] bool m_StopOnTargetPos;
        [Description("Speed of the spell")]
        [SerializeField] float            m_Speed       = 0f;

        // ================================================================================================
        // Dependent Members
        /// <summary> Movement speed of the spell </summary>
        public float Speed => Settings.SpellSpeedFactor * m_Speed;
        public bool StopOnTargetPos => m_StopOnTargetPos;
        public bool IsTrajectoryFromAbove => Trajectory == ESpellTrajectory.Hight || Trajectory == ESpellTrajectory.Diagonal;
        #endregion


        #region Inherited Spawning Members

        public override void SpellPreview(Controller controller, Transform parent = default, Vector3 offset = default)
        {
            offset = GetSpawnOffset(controller);
            switch (Trajectory)
            {
                case ESpellTrajectory.Curve:
                case ESpellTrajectory.Hight:
                    parent = ArenaManager.Instance.Arena.transform;
                    break;

                case ESpellTrajectory.Diagonal:
                case ESpellTrajectory.DiagonalMiddle:
                case ESpellTrajectory.Straight:
                    break;

                default:
                    Debug.LogError($"Trajectory {Trajectory} not implemented");
                    break;
            }

            base.SpellPreview(controller, parent, offset);
        }

        #endregion


        #region Postion & Target

        public override void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId)
        {
            Controller controller = GameManager.Instance.GetPlayer(clientId);

            // handle Y position
            switch (Trajectory)
            {
                case ESpellTrajectory.Diagonal:
                    position.y = Settings.SPELL_DIAGONAL_POS_Y;
                    break;

                case ESpellTrajectory.Hight:
                case ESpellTrajectory.DiagonalMiddle:
                    position.y = Settings.SPELL_HIGHT_POS_Y;
                    break;
            }
                
            // bounds of the proc area
            (float Min, float Max) bounds = (0f, 0f);
            
            // handle X position
            switch (Trajectory)
            {
                case ESpellTrajectory.Straight:
                case ESpellTrajectory.Diagonal:
                case ESpellTrajectory.Curve:
                    position += GetSpawnOffset(controller);
                    bounds = ArenaManager.GetAreaBounds(controller.Team, false);
                    break;

                case ESpellTrajectory.DiagonalMiddle:
                    position.x = 0;
                    break;

                case ESpellTrajectory.Hight:
                    position.x = target.x;
                    break;

                default:
                    ErrorHandler.Error($"Trajectory {Trajectory} not implemented");
                    break;
            }

            if (bounds != (0f, 0f))
            {
                var offset = 0.1f + Size / 2;
                position.x = Mathf.Clamp(position.x, bounds.Min + offset, bounds.Max - offset);
            }
        }

        /// <summary>
        /// Calculate offset of the spawn position depending on the trajectory
        /// </summary>
        /// <param name="trajectory"></param>
        /// <returns></returns>
        protected virtual Vector3 GetSpawnOffset(Controller controller)
        {
            int rotationFactor = controller.transform.rotation.y > 0 ? 1 : -1;

            switch (Trajectory)
            {
                case ESpellTrajectory.Straight:
                case ESpellTrajectory.Diagonal:
                    return new Vector3(0, 0, 0);

                case ESpellTrajectory.Curve:
                case ESpellTrajectory.DiagonalMiddle:
                    return new Vector3(rotationFactor * 0.1f, 0.25f, 0);

                case ESpellTrajectory.Hight:
                    return new Vector3(rotationFactor * controller.SpellHandler.SpellSpawn.transform.position.x, 1f, 0); ;

                default:
                    ErrorHandler.Error($"Trajectory {Trajectory} not implemented");
                    return new Vector3(0, 0, 0);
            }
        }

        #endregion


        #region Info Display

        public override Dictionary<string, object> GetInfos()
        {
            var infoDict = base.GetInfos();

            if (m_Speed > 0)
                infoDict.Add("Speed", m_Speed);

            return infoDict;
        }

        #endregion
    }
}