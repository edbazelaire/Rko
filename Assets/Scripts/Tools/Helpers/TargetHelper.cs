using Data.GameManagement;
using Enums;
using Game;
using MyBox;
using System.Collections.Generic;
using UnityEngine;

namespace Tools.Helpers
{
    public static class TargetHelper
    {
        #region Members

        public static int ALL_LAYER_MASK    = LayerMask.GetMask(ETargetLayer.Player.ToString(), ETargetLayer.Structure.ToString());
        public static int PLAYER_LAYER_MASK = LayerMask.GetMask(ETargetLayer.Player.ToString());

        #endregion


        #region Target Position

        public static void GetTargetPosition(ref Vector3 target, ESpellTarget spellTarget, Vector2 offset, bool clampTargetPos, ulong casterId, ulong? targetId = null)
        {
            Controller controller = GameManager.Instance.GetPlayer(casterId);
            int direction = ArenaManager.GetAreaMovementDirection(controller.Team, IsEnemyTarget(spellTarget));

            switch (spellTarget)
            {
                case ESpellTarget.Self:
                    target.x = controller.transform.position.x;
                    break;

                case ESpellTarget.FirstAlly:
                    target.x = GameManager.Instance.GetFirstAlly(controller.Team, casterId).transform.position.x;
                    break;

                case ESpellTarget.FirstEnemy:
                    // Special case : trying to target first enemy but there are only "Spawns" on the map : set targeting to "Fix"
                    if (GameManager.Instance.IsOnlySpawnEnemies(controller.Team))
                    {
                        GetTargetPosition(ref target, ESpellTarget.Fixed, offset, clampTargetPos, casterId, targetId);
                        return;
                    }
                    target.x = GameManager.Instance.GetFirstEnemy(controller.Team).transform.position.x;
                    break;

                case ESpellTarget.CurrentTarget:
                    if (!targetId.HasValue)
                    {
                        ErrorHandler.Error("SpellTarget is CurrentTarget but no target id was provided");
                        break;
                    }
                    target.x = GameManager.Instance.GetPlayer(targetId.Value).transform.position.x;
                    break;

                case ESpellTarget.AllyZoneCenter:
                case ESpellTarget.EnemyZoneCenter:
                case ESpellTarget.EnemyZone:
                case ESpellTarget.AllyZone:
                    target.x = GetTargettableArea(controller.Team, spellTarget).position.x;
                    break;

                case ESpellTarget.AllyZoneEnd:
                case ESpellTarget.EnemyZoneStart:
                    var centerPos = GetTargettableArea(controller.Team, spellTarget).position.x;
                    target.x = centerPos - direction * ArenaManager.Instance.TargettableAreaSize / 2;
                    break;

                case ESpellTarget.AllyZoneStart:
                case ESpellTarget.EnemyZoneEnd:
                    target.x = GetTargettableArea(controller.Team, spellTarget).position.x + direction * ArenaManager.Instance.TargettableAreaSize / 2;
                    break;

                case ESpellTarget.Mirror:
                    target.x = -controller.transform.position.x;
                    break;

                case ESpellTarget.Fixed:
                    direction = ArenaManager.GetAreaMovementDirection(controller.Team, true);
                    target.x = controller.transform.position.x + direction * Settings.SpellFixedDistance;
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + spellTarget);
                    break;
            }

            // APPLY OFFSET
            target.x += direction * offset.x;
            target.y += offset.y;

            // CLAMP target in between available positions
            if (clampTargetPos && spellTarget != ESpellTarget.Self)
                ClampTargetX(ref target, spellTarget, casterId);
        }

        public static Transform GetTargettableArea(int team, ESpellTarget spellTarget)
        {
            if (IsEnemyTarget(spellTarget))
                return ArenaManager.GetTargettableAreaTransform(team, true);

            else if (IsAllyTarget(spellTarget))
                return ArenaManager.GetTargettableAreaTransform(team, false);

            else
                return ArenaManager.Instance.Arena.transform;
        }

        public static void ClampTargetX(ref Vector3 target, ESpellTarget spellTarget, ulong clientId)
        {
            // clamp target between min/max xPos of the target zone
            var zoneCenter = GetTargettableArea(GameManager.Instance.GetPlayer(clientId).Team, spellTarget).position.x;
            target.x = Mathf.Clamp(target.x, zoneCenter - ArenaManager.Instance.TargettableAreaSize / 2, zoneCenter + ArenaManager.Instance.TargettableAreaSize / 2);
        }

        #endregion


        #region Target Controller

        public static Controller GetTargetController(ulong casterId, ESpellTarget spellTarget, ulong? targetId = null, bool throwError = true)
        {
            if (!GameManager.Exists)
                return null;

            Controller controller = GameManager.Instance.GetPlayer(casterId);

            switch (spellTarget)
            {
                case ESpellTarget.Self:
                    return controller;

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(controller.Team, casterId);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(controller.Team);

                case ESpellTarget.CurrentTarget:
                    if (targetId == null)
                    {
                        ErrorHandler.Error("SpellTarget is CurrentTarget but no target id was provided");
                        return null;
                    }
                    return GameManager.Instance.GetPlayer(targetId.Value);

                default:
                    if (throwError)
                        ErrorHandler.Warning("Unhandled case " + spellTarget);
                    return null;
            }
        }

        public static Controller GetTargetController(ulong casterId, EStateEffectTarget stateEffectTarget, ulong? targetId = null, bool throwError = true)
        {
            if (!GameManager.Exists)
                return null;

            Controller controller = GameManager.Instance.GetPlayer(casterId);

            switch (stateEffectTarget)
            {
                case EStateEffectTarget.Self:
                    return controller;

                case EStateEffectTarget.Ally:
                    return GameManager.Instance.GetFirstAlly(controller.Team, casterId);

                case EStateEffectTarget.Enemy:
                    return GameManager.Instance.GetFirstEnemy(controller.Team);

                case EStateEffectTarget.Target:
                    if (targetId == null)
                    {
                        ErrorHandler.Error("SpellTarget is CurrentTarget but no target id was provided");
                        return null;
                    }
                    return GameManager.Instance.GetPlayer(targetId.Value);

                default:
                    if (throwError)
                        ErrorHandler.Warning("Unhandled case " + stateEffectTarget);
                    return null;
            }
        }

        #endregion


        #region Is Allowed Target

        public static bool IsAllowedTarget(Controller targetController, Controller caster, EStateEffectTarget allowedTarget, ulong? targetId = null)
        {
            switch (allowedTarget)
            {
                case EStateEffectTarget.AllAllies:
                case EStateEffectTarget.Ally:
                    return targetController.Team == caster.Team;

                case EStateEffectTarget.Self:
                    return targetController == caster;

                case EStateEffectTarget.AllEnemies:
                case EStateEffectTarget.Enemy:
                    return targetController.Team != caster.Team;

                case EStateEffectTarget.Target:
                    if (!targetId.HasValue)
                    {
                        ErrorHandler.Warning("Trying to target CURRENT TARGET but no target id was provided");
                        return false;
                    }
                    return targetController.PlayerId == targetId.Value;

                default:
                    ErrorHandler.Warning("Unhandled case : " + allowedTarget);
                    return false;
            }
        }

        public static bool IsAllowedTarget(Controller targetController, Controller caster, List<EStateEffectTarget> allowedTargets, ulong? targetId = null)
        {
            // no specific targets provided - can be applied on anyone
            if (allowedTargets.IsNullOrEmpty())
                return true;

            foreach (EStateEffectTarget allowedTarget in allowedTargets)
            {
                if (IsAllowedTarget(targetController, caster, allowedTarget, targetId))
                    return true;
            }

            return false;
        }

        #endregion


        #region Type of Target

        public static bool IsEnemyTarget(ESpellTarget spellTarget) => spellTarget == ESpellTarget.FirstEnemy
            || spellTarget == ESpellTarget.EnemyZone
            || spellTarget == ESpellTarget.EnemyZoneStart
            || spellTarget == ESpellTarget.EnemyZoneCenter
            || spellTarget == ESpellTarget.EnemyZoneEnd
            || spellTarget == ESpellTarget.Fixed
            || spellTarget == ESpellTarget.Mirror;


        public static bool IsAllyTarget(ESpellTarget spellTarget) => spellTarget == ESpellTarget.FirstAlly
            || spellTarget == ESpellTarget.Self
            || spellTarget == ESpellTarget.AllyZone
            || spellTarget == ESpellTarget.AllyZoneStart
            || spellTarget == ESpellTarget.AllyZoneCenter
            || spellTarget == ESpellTarget.AllyZoneEnd;

        #endregion

    }
}