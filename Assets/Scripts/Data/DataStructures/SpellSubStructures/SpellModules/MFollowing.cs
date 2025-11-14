using Data.DataStructures.CharacterSubStructures;
using Enums;
using Game.Character.Controllers;
using Game.Spells;
using System;
using Tools;
using Tools.Helpers;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

namespace Data.DataStructures.SpellSubStructures.SpellModules
{
    [Serializable]
    public class MFollowing
    {
        #region Members

        // =======================================================================
        // Serialized data
        [SerializeField, Tooltip("If true, beam rotates to follow target position each frame")]
        protected ESpellTarget m_TargetToFollow = ESpellTarget.None;
        [SerializeField, Tooltip("If true, beam rotates to follow target position each frame")]
        protected Vector2 m_TargetOffset = Vector2.zero;
        [SerializeField, Tooltip("If true, beam rotates to follow target position each frame")]
        protected float m_FollowingSpeed = -1f;

        // =======================================================================
        // Public accessors
        public ESpellTarget TargetToFollow => m_TargetToFollow;
        public float        FollowingSpeed => m_FollowingSpeed;

        #endregion


        #region 

        public Vector3 UpdatePosition(Vector3 position, ulong casterId, ulong? targetId = null)
        {
            var targetPosition = position;

            // recalculate current target to follow
            TargetHelper.GetTargetPosition(
                target:         ref targetPosition,
                spellTarget:    m_TargetToFollow,
                offset:         m_TargetOffset,                   
                clampTargetPos: true,
                casterId:       casterId,
                targetId:       targetId
            );

            // inifinit speed - instantly snap
            if (FollowingSpeed < 0f)
            {
                return targetPosition;
            }

            // direction to the final target
            Vector3 dir = targetPosition - position;

            // move to the target
            float step = FollowingSpeed * Time.deltaTime;
            if (dir.magnitude <= step)
            {
                position = targetPosition; // snap if close
            }
            else
            {
                position += dir.normalized * step;
            }

            return position;
        }

        #endregion


        #region Override Data

        public void Override(ESpellProperty spellProperty, string value)
        {
            switch (spellProperty)
            {
                case ESpellProperty.MFollowing_TargetToFollow:
                    if (!Enum.TryParse(value, out ESpellTarget targetToFollow))
                    {
                        ErrorHandler.Warning("Trying to override "+ spellProperty + " with value " + value + " - but this is not recognized as ESpellTarget");
                        return;
                    }
                    m_TargetToFollow = targetToFollow;
                    return;

                case ESpellProperty.MFollowing_TargetOffset:
                    if (!TextHandler.TryParseVector2(value, out Vector2 targetOffset))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as Vector2");
                        return;
                    }
                    m_TargetOffset = targetOffset;
                    return;

                case ESpellProperty.MFollowing_FollowingSpeed:
                    if (!float.TryParse(value, out float followingSpeed))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as float");
                        return;
                    }
                    m_FollowingSpeed = followingSpeed;
                    return;

                default:
                    ErrorHandler.Error("Unhandled case : " + spellProperty);
                    return;
            }
        }

        #endregion
    }
}