using AI;
using Data;
using Enums;
using Game;
using Game.AI;
using Game.Character;
using Game.Spells;
using System;
using System.Collections;
using System.Collections.Generic;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

public class TaskMove : BaseTask
{
    #region Members

    // =============================================================================
    // CONSTANTS
    /// <summary> dodge time window for a projectile to be considerated as a threat </summary>
    public const float THREAT_TIME_WINDOW = 1f;
    public const float SIZE_OFFSET = 0.1f;

    // =============================================================================
    // Component & GameObjects
    protected Movement m_Movement => m_Controller.Movement;

    // =============================================================================
    // Data
    protected bool m_CheckZones;
    protected bool m_CheckProjectiles;
    protected bool m_IgnoreInvisibleWalls;
    protected bool m_IgnoreInvisibleStructures;
    protected bool m_RefreshMovement;           // when set to true, refresh the movement decision
    protected Coroutine m_CurrentCoroutine;     // current active coroutine

    // -- Serializable data (todo)
    protected float m_CheckObstaclesSize        = 1f;
    protected float m_CheckZoneSize             = 1f;

    // -- continue data
    protected List<int> m_AllowedMovements      = new List<int> { -1, 1 };
    protected int m_CurrentMoveX                = 1;
    protected float m_SizeOffset                = 0f;
    protected Vector2 m_Position => m_Controller.transform.position;

    #endregion


    #region Init & End

    public TaskMove(Controller controller, bool checkZones = true, bool checkProjectiles = true, bool ignoreInvisibleWalls = false, Func<float> weight = null) : base(controller, weight)
    { 
        m_CheckZones                    = checkZones;
        m_CheckProjectiles              = checkProjectiles;
        m_IgnoreInvisibleWalls          = ignoreInvisibleWalls;
        m_IgnoreInvisibleStructures     = controller.IsSpawn;       // invocations can ignore structures
        m_SizeOffset = GetSizeOffset();
    }

    #endregion


    #region Movement Evaluation

    public override NodeState Evaluate()
    {
        // select a movement direction
        SelectMovement();

        if (m_State != NodeState.FAILURE)
        {
            // apply movement (-1) because of team effect
            m_Movement.SetMovement((sbyte)((m_Controller.Team == 0 ? 1 : -1) * m_CurrentMoveX));
        }

        ErrorHandler.Log(() => "TaskMove - " + m_State, ELogTag.AITaskMove);
        return m_State;
    }

    protected virtual void SelectMovement()
    {
        if (m_State != NodeState.RUNNING)
            SetNodeState(NodeState.FAILURE);

        if (! m_Movement.CanMove || m_Movement.Speed <= 0)
        {
            ErrorHandler.Log(() => "TaskMove - " + m_State + " : m_Movement.CanMove = false", ELogTag.AITaskMove);
            SetNodeState(NodeState.FAILURE);
            return;
        }

        // reset allowed movements
        m_AllowedMovements = new List<int> { -1, 1 };

        // check if there is obstacle blocking a direction
        CheckObstacles();

        // check if a zone spell is blocking the direction
        if (m_CheckZones)
            CheckZones();

        // check if projectiles are in the way
        if (m_CheckProjectiles)
            CheckProjectiles();

        if (m_AllowedMovements.Count == 0)
        {
            m_CurrentMoveX = 0;
        }
        else if (! m_AllowedMovements.Contains(m_CurrentMoveX) || m_RefreshMovement)
        {
            // play coroutine on the side to call for movement refresh (= add randomness in movements)
            if (m_CurrentCoroutine != null)
                m_Controller.StopCoroutine(m_CurrentCoroutine);
            m_CurrentCoroutine = m_Controller.StartCoroutine(CheckRefreshMovement());

            // select movement (at random)
            var movements = m_AllowedMovements;
            movements.Shuffle();
            m_CurrentMoveX = movements[0];
        }

        SetNodeState(NodeState.RUNNING);
    }

    #endregion


    #region State Management

    protected override void OnRunning()
    {
        base.OnRunning();

        m_Controller.StartCoroutine(CheckMovementDuration());
    }

    #endregion


    #region Checkers

    /// <summary>
    /// Check if there is an obstacle on the ground that prevents movement on the left or the right
    /// </summary>
    protected virtual void CheckObstacles()
    {
        if (m_AllowedMovements.Count == 0)
            return;

        // duplicate array to be able to remove while going through
        var allowedMovement = m_AllowedMovements.ToArray();

        foreach (int moveX in allowedMovement)
        {
            Collider2D[] colliders = CollisionChecker.GetCollidersInDistance(
                m_Controller.transform.position.x,
                moveX * (m_CheckObstaclesSize * m_SizeOffset + SIZE_OFFSET),
                CollisionChecker.GetObstacleLayers(m_IgnoreInvisibleWalls, m_IgnoreInvisibleStructures)
            );

            if (colliders != null && colliders.Length > 0)
            {
                ErrorHandler.Log(() => $"      -- TaskMove CheckObstacles() : removing movement {moveX}", ELogTag.AITaskMove);
                m_AllowedMovements.Remove(moveX);
            }
        }
    }


    /// <summary>
    /// Check if there is a zone spell on the ground that prevents movement on the left or the right
    /// </summary>
    /// <param name="m_AllowedMovements"></param>
    protected virtual void CheckZones()
    {
        if (m_AllowedMovements.Count == 0)
            return;

        // duplicate array to be able to remove while going threw
        var allowedMovement = m_AllowedMovements.ToArray();

        // for each remaining allowed movements, check if there is an Enemy ZoneSpell in that direction
        foreach (int moveX in allowedMovement)
        {
            // get all colliders on Layer "Spell"
            Collider2D[] colliders = CollisionChecker.GetCollidersInDistance(m_Controller.transform.position.x, moveX * m_CheckZoneSize, ELayer.Spell);
            
            // filter spells of type Zone of Enemies
            var zones = CollisionChecker.FilterSpells(colliders, ESpellType.Zone, (m_Controller.Team + 1) % 2);
            if (zones.Count == 0)
                continue;

            // if any : un-allow movement
            ErrorHandler.Log(() => "      -- TaskMove CheckZones() : removing movement " + moveX, ELogTag.AITaskMove);
            m_AllowedMovements.Remove(moveX);
        }
    }

    /// <summary>
    /// Check allowed movements to dodge projectiles
    /// </summary>
    protected virtual void CheckProjectiles()
    {
        if (m_AllowedMovements.Count == 0)
            return;

        // CHECK : straight line projectile
        if (m_AllowedMovements.Contains(-1) && m_ProjectileTrigger.CheckStraightProjectiles(out float xPos))
        {
            // no straight projectile can reach us (add offset for safety)
            if (xPos + 0.5f <= m_Controller.transform.position.x)
                return;

            ErrorHandler.Log(() => "      -- TaskMove CheckProjectiles() : removing movement -1 because of STRAIGHT PROJECTILE", ELogTag.AITaskMove);

            // otherwise : run the other direction
            m_AllowedMovements.Remove(-1);
        }

        foreach (Projectile projectile in m_ProjectileTrigger.Projectiles)
        {
            if (projectile.IsDestroyed())
                continue;

            (bool isThreat, bool isDodgeable, int move) = IsProjectileAtThreatDistance(projectile, m_Controller, m_AllowedMovements);
            
            // force dodge first encoutered dodgeable projectile
            if (isThreat && isDodgeable && move != 0)
            {
                ErrorHandler.Log(() => "      -- TaskMove CheckProjectiles() : forcing movement "+move+" because of PROJECTILE", ELogTag.AITaskMove);
                m_AllowedMovements = new List<int>() { move };
                break;
            }
        }
    }

    /// <summary>
    /// Check if there are obstacles between controller and provided X position
    /// </summary>
    /// <param name="xPos"></param>
    /// <returns></returns>
    public static bool HasObstacles(float originalXPos, float targetXPos)
    {
        Collider2D[] colliders = CollisionChecker.GetCollidersInDistance(originalXPos, targetXPos - originalXPos, CollisionChecker.OBSTACLES_LAYERS);
        return colliders.Length > 0;
    }

    /// <summary>
    /// Check if the projectile is close enought to be a threat that needs to be dodged.
    /// Also check if the projectile is actually dodgeable by movement
    /// </summary>
    /// <param name="projectile">           Projectile that is coming towards the Controller                        </param>
    /// <param name="controller">           Controller of the Player trying to dodge the Projectile                 </param>
    /// <param name="startIntersection">    Starts of the Intersection between the projectile and the Controller    </param>
    /// <param name="endIntersection">      Starts of the Intersection between the projectile and the Controller    </param>
    /// <returns></returns>
    public static (bool isThreat, bool isDodgeable, int move) IsProjectileAtThreatDistance(Projectile projectile, Controller controller, List<int> allowedMovements)
    {
        if (projectile.IsDestroyed())
            return (false, false, 0);

        // get data of the projectiles
        ProjectileData projectileData = projectile.SpellData as ProjectileData;

        // ====================================================================================================
        // CALCULATE DISTANCE
        // get points to reach left and right to be sage
        (float startX, float endX) = ProjectileTrigger.CalculateProjectileSafeBounds(projectile, controller);

        // check if both positions left and right can be accessed
        if (allowedMovements.Contains(-1))
        {
            if (! ArenaManager.IsInAreaBounds(startX, controller.Team, false) || HasObstacles(controller.transform.position.x, startX) )
                allowedMovements.Remove(-1);
        }

        if (allowedMovements.Contains(1))
        {
            if (!ArenaManager.IsInAreaBounds(endX, controller.Team, false) || HasObstacles(controller.transform.position.x, endX))
                allowedMovements.Remove(1);
        }

        // no movement left : THREAT : yes | DOGEABLE : false 
        if (allowedMovements.Count == 0)
            return (true, false, 0);

        float distanceToMoveLeft = Mathf.Min(Mathf.Abs(startX - controller.transform.position.x), Mathf.Abs(endX - controller.transform.position.x));
        float distanceToMoveRight = Mathf.Min(Mathf.Abs(startX - controller.transform.position.x), Mathf.Abs(endX - controller.transform.position.x));

        // at which point in space the projectile would intersect with the player
        Vector3 intersectionPointLeft           = new Vector3(startX, controller.CharacterHeight + projectileData.Size / 2, 0f);
        float projectileRemainingDistanceLeft   = Vector3.Distance(projectile.transform.position, intersectionPointLeft);
        float projectileRemainingDistanceRight  = Vector3.Distance(projectile.transform.position, projectile.Target - new Vector3(projectileData.Size / 2, 0f, 0f));

        switch (projectileData.Trajectory)
        {
            case ESpellTrajectory.Curve:
                projectileRemainingDistanceLeft = Vector3.Distance(ArenaManager.Instance.TargetHight.position, projectile.Target);

                if (projectile.transform.position.x < ArenaManager.Instance.TargetHight.position.x)
                {
                    projectileRemainingDistanceLeft     = Vector3.Distance(projectile.transform.position, ArenaManager.Instance.TargetHight.position) + Vector3.Distance(ArenaManager.Instance.TargetHight.position, intersectionPointLeft);
                    projectileRemainingDistanceRight    = Vector3.Distance(projectile.transform.position, ArenaManager.Instance.TargetHight.position) + Vector3.Distance(ArenaManager.Instance.TargetHight.position, projectile.Target);
                }
                break;

            case ESpellTrajectory.Straight:
                distanceToMoveLeft = Mathf.Infinity;
                projectileRemainingDistanceRight = Vector3.Distance(projectile.transform.position, projectile.Target);
                break;

            default:
                break;
        }

        // ====================================================================================================
        // CALCULATE TIME 
        int move;
        float timeToMove;           // time that the character will take to move the required distance to safety
        float timeProjectile;       // time that the projectile will take to reach its target
        if (allowedMovements.Count == 1)
        {
            if (allowedMovements.Contains(-1))
            {
                move = -1;
                timeToMove = distanceToMoveLeft / controller.Movement.Speed;
                timeProjectile = projectileRemainingDistanceLeft / projectileData.Speed;
            }
            else
            {
                move = 1;
                timeToMove = distanceToMoveRight / controller.Movement.Speed;
                timeProjectile = projectileRemainingDistanceRight / projectileData.Speed;
            }
        }
        else
        {
            move = distanceToMoveRight > distanceToMoveLeft ? -1 : 1;
            timeProjectile = (distanceToMoveRight > distanceToMoveLeft ? projectileRemainingDistanceLeft : projectileRemainingDistanceRight) / projectileData.Speed;
            timeToMove = Mathf.Min(distanceToMoveLeft, distanceToMoveRight) / controller.Movement.Speed;
        }

        // projectile is a threat if the required time to move (+ a safety) is superior to the time that the
        return (timeToMove + THREAT_TIME_WINDOW > timeProjectile, timeToMove <= timeProjectile, move);
    }

    #endregion


    #region Helpers

    /// <summary>
    /// Calculate half the length of the character (required to check accuracte collision with walls)
    /// </summary>
    /// <returns></returns>
    float GetSizeOffset()
    {
        // Récupère le collider depuis le Controller -> GFXHandler
        var col = m_Controller?.GFXHandler?.Collider;

        // Largeur monde du collider (avec scale), fallback sur CharacterSize si besoin
        float colliderWidthWorld = (col != null)
            ? col.bounds.size.x                      // largeur complète (gauche->droite)
            : m_Controller.GFXHandler.CharacterSize; // fallback si pas de collider

        // Option : travailler en "demi-largeur"
        return colliderWidthWorld * 0.5f + SIZE_OFFSET;
    }

    #endregion


    #region Coroutines

    IEnumerator CheckMovementDuration()
    {
        if (m_Controller.BehaviorTree.BotData.MaxMovementTime <= 0)
        {
            yield return null;
        }

        var timer = UnityEngine.Random.Range(m_Controller.BehaviorTree.BotData.MinMovementTime, m_Controller.BehaviorTree.BotData.MaxMovementTime);

        while (timer > 0f)
        {
            if (m_State != NodeState.RUNNING)
                yield break;

            timer -= Time.deltaTime;
            yield return null;
        }

        SetNodeState(NodeState.SUCCESS);
    }

    IEnumerator CheckRefreshMovement()
    {
        m_RefreshMovement = false;
        if (m_Controller.BehaviorTree.BotData.MinMovementRefresh <= 0 && m_Controller.BehaviorTree.BotData.MaxMovementRefresh <= 0)
            yield break;

        var timer = UnityEngine.Random.Range(m_Controller.BehaviorTree.BotData.MinMovementRefresh, m_Controller.BehaviorTree.BotData.MaxMovementRefresh);

        while (timer > 0f)
        {
            timer -= Time.deltaTime;
            yield return null;
        }

        ErrorHandler.Log(() => "      -- TaskMove CheckRefreshMovement() : REFRESHING", ELogTag.AITaskMove);

        m_RefreshMovement = true;
    }

    #endregion


    #region Reset

    public override void Reset()
    {
        base.Reset();

        m_RefreshMovement = false;

        if (m_CurrentCoroutine != null)
            m_Controller.StopCoroutine(m_CurrentCoroutine);
    }

    #endregion
}
