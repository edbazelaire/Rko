using AI;
using Enums;
using Game.AI;
using Game.Spells;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskExitZone : TaskMove
{
    #region Members

    #endregion


    #region Init & End
    
    public TaskExitZone(Controller controller, Func<float> weight = null) : base(controller, true, true, false, weight) { }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        m_State = NodeState.FAILURE;

        if (!m_Movement.CanMove)
            return m_State;

        (float? startX, float? endX) = GetZoneStartEnd(); 
        if (! startX.HasValue || !endX.HasValue)
        {
            m_State = NodeState.SUCCESS;
            return m_State;
        }

        m_State = NodeState.RUNNING;

        SelectExitMovement(startX.Value, endX.Value);

        m_Movement.SetMovement((sbyte)-m_CurrentMoveX);

        return m_State;
    }

    /// <summary>
    /// Check if there is a zone spell on the ground that prevents movement on the left or the right
    /// </summary>
    /// <param name="allowedMovements"></param>
    (float?, float?) GetZoneStartEnd()
    {
        Collider2D[] colliders = CollisionChecker.GetColliderCollisions(m_ImmediatThreatTrigger.Collider, new List<ELayer> { ELayer.Spell });

        float? startX = null;
        float? endX = null;

        // Now you can iterate through overlappedColliders to handle each collider
        foreach (Collider2D collider in colliders)
        {
            Spell spell = collider.GetComponent<Spell>();
            if (spell == null)
                continue;

            // ignore allies spells
            if (spell.Caster.Team == m_Controller.Team)
                continue;

            if (spell.SpellData.SpellType != ESpellType.Zone)
                continue;

            var newStartX = collider.transform.position.x - (collider.bounds.size.x / 2);
            if (startX == null || startX.Value > newStartX)
                startX = newStartX;

            float newEndX = collider.transform.position.x + (collider.bounds.size.x / 2);
            if (endX == null || endX < newEndX)
                endX = newEndX;
        }

        return (startX, endX);
    }

    /// <summary>
    /// Based on start and end of the Zone, select the most optimal exit movement
    /// </summary>
    void SelectExitMovement(float startX, float endX)
    {
        // CHECK OBSTACLES  --------------------------------------------------------------------------------
        // START : check if there is Obstacles on the way to the start -> move towards the end
        if (CollisionChecker.GetCollidersBetween(m_Position.x, startX - CollisionChecker.GetColliderWidth(m_ImmediatThreatTrigger.Collider), CollisionChecker.OBSTACLES_LAYERS).Length > 0)
        {
            m_CurrentMoveX = 1;
            return;
        }

        // END : check if there is Obstacles on the way to the end -> move towards the start
        if (CollisionChecker.GetCollidersBetween(m_Position.x, endX + CollisionChecker.GetColliderWidth(m_ImmediatThreatTrigger.Collider), CollisionChecker.OBSTACLES_LAYERS).Length > 0)
        {
            m_CurrentMoveX = -1;
            return;
        }

        // CHECK ZONES  --------------------------------------------------------------------------------
        Collider2D[] colliders;
        List<Spell> spells;
        // START : check if there is ZONES SPELLS on the way to the end (more than 1 because already in one) -> move towards the start
        colliders = CollisionChecker.GetCollidersBetween(m_Position.x, startX - CollisionChecker.GetColliderWidth(m_ImmediatThreatTrigger.Collider), ELayer.Spell);
        spells = CollisionChecker.FilterSpells(colliders, ESpellType.Zone, (m_Controller.Team + 1) % 2);
        if (spells.Count > 1)
        {
            m_CurrentMoveX = 1;
            return;
        }

        // END : check if there is ZONES SPELLS on the way to the end (more than 1 because already in one) -> move towards the start
        colliders = CollisionChecker.GetCollidersBetween(m_Position.x, endX + CollisionChecker.GetColliderWidth(m_ImmediatThreatTrigger.Collider), ELayer.Spell);
        spells = CollisionChecker.FilterSpells(colliders, ESpellType.Zone, (m_Controller.Team + 1) % 2);
        if (spells.Count > 1)
        {
            m_CurrentMoveX = -1;
            return;
        }

        // Otherwise  --------------------------------------------------------------------------------
        // set current X Move to closest distance between start and end
        m_CurrentMoveX = (Mathf.Abs(startX - m_Position.x) < Mathf.Abs(endX - m_Position.x)) ? -1 : 1;
               
    }

    #endregion

}
