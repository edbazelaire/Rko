using Enums;
using Game;
using System.Collections.Generic;
using System.ComponentModel;
using Tools;
using UnityEngine;
using Data.GameManagement;
using static UnityEngine.RuleTile.TilingRuleOutput;
using MyBox;
using System;
using Data.DataStructures.SpellSubStructures;

namespace Data
{
    [CreateAssetMenu(fileName = "Projectile", menuName = "Game/Spells/Projectile")]
    public class ProjectileData : SpellData
    {
        #region Members
        public override ESpellType SpellType => ESpellType.Projectile;

        [Header("Movement Data")]
        [Tooltip("Type of path that the spell is taking")]
        public ESpellTrajectory     Trajectory;
        [SerializeField, Tooltip("Is the projectile triggered by the ground ?")]
        protected bool              m_TriggerGround = true;
        [SerializeField, Tooltip("Should the projectile end when reaching target position ?")]
        protected bool              m_StopOnTargetPos;
        [SerializeField, Tooltip("Speed of the spell")]
        protected float             m_Speed         = 0f;
        [SerializeField, Tooltip("Y spawn position of the spell (for straight cast only)"), ConditionalField("Trajectory", false, ESpellTrajectory.Straight)]
        protected float             m_YSpawnPos     = 0f;

        // ================================================================================================
        // Dependent Members
        /// <summary> Movement speed of the spell </summary>
        public float Speed                  => Settings.SpellSpeedFactor * m_Speed;
        public bool TriggerGround           => m_TriggerGround;
        public bool StopOnTargetPos         => m_StopOnTargetPos;
        public bool IsTrajectoryFromAbove   => Trajectory == ESpellTrajectory.High || Trajectory == ESpellTrajectory.Diagonal;

        #endregion


        #region Postion & Target

        public override void CalculateTarget(ref Vector3 target, ulong clientId, ulong? targetId = null)
        {
            switch (Trajectory)
            {
                case ESpellTrajectory.Curve:
                case ESpellTrajectory.High:
                case ESpellTrajectory.Diagonal:
                case ESpellTrajectory.DiagonalMiddle:
                    target.y = 0;
                    break;

                case ESpellTrajectory.Straight:
                    break;

                default:
                    ErrorHandler.Error("Unhandled type " + Trajectory);
                    break;

            }

            base.CalculateTarget(ref target, clientId);
        }

        public override void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId)
        {
            base.RecalculatePosition(ref position, target, clientId);

            // handle Y position
            switch (Trajectory)
            {
                case ESpellTrajectory.Diagonal:
                    position.y = Settings.SPELL_DIAGONAL_POS_Y;
                    break;

                case ESpellTrajectory.High:
                case ESpellTrajectory.DiagonalMiddle:
                    position.y = Settings.SPELL_HIGHT_POS_Y;
                    break;

                case ESpellTrajectory.Straight:
                    if (m_YSpawnPos > 0)
                        position.y = m_YSpawnPos;
                    break;
            }
            
            // handle X position
            switch (Trajectory)
            {
                case ESpellTrajectory.Straight:
                    break;

                case ESpellTrajectory.Diagonal:
                case ESpellTrajectory.Curve:
                    break;

                case ESpellTrajectory.DiagonalMiddle:
                    position.x = 0;
                    break;

                case ESpellTrajectory.High:
                    position.x = target.x;
                    break;

                default:
                    ErrorHandler.Error($"Trajectory {Trajectory} not implemented");
                    break;
            }

            (float Min, float Max)  bounds = ArenaManager.GetAreaBounds(position.x);
            if (bounds != (0f, 0f))
            {
                var offset = 0.1f + Size / 2;
                position.x = Mathf.Clamp(position.x, bounds.Min + offset, bounds.Max - offset);
            }
        }

        /// <summary>
        /// Recalculate the rotation of the spell on spawn
        /// </summary>
        /// <param name="rotation"></param>
        public override void RecalculateRotation(ref Quaternion rotation) { }

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
                case ESpellTrajectory.DiagonalMiddle:
                case ESpellTrajectory.Curve:
                    return new Vector3(0, 0, 0);

                case ESpellTrajectory.High:
                    return new Vector3(rotationFactor * controller.SpellHandler.SpellSpawn.transform.position.x, 1f, 0); ;

                default:
                    ErrorHandler.Error($"Trajectory {Trajectory} not implemented");
                    return new Vector3(0, 0, 0);
            }
        }

        #endregion


        #region Overriding

        public override bool CheckSpecialOverridingData(SOverridingData overridingData)
        {
            switch (overridingData.Property)
            {
                case ESpellProperty.Trajectory:
                    if (! Enum.TryParse(overridingData.Value, out ESpellTrajectory trajectory))
                    {
                        ErrorHandler.Error($"Spell ({Name}) - unable to parse {overridingData.Value} into a ESpellTrajectory");
                        return false;
                    }
                    Trajectory = trajectory;
                    return true;

                default:
                    return base.CheckSpecialOverridingData(overridingData);
            }
        }

        #endregion


        #region Info Display

        public override Dictionary<string, object> GetInfo()
        {
            var infoDict = base.GetInfo();

            if (m_Speed > 0)
                infoDict.Add("Speed", m_Speed);

            return infoDict;
        }

        #endregion
    }
}