using AI;
using Enums;
using Game.AI;
using Game.Spells;
using System.Collections.Generic;
using Tools;
using UnityEngine;

public class CheckInZone : BaseChecker
{
    #region Members

    #endregion


    #region Init & End
    
    public CheckInZone(Controller controller) : base(controller) { }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        base.Evaluate();
        if (m_State != NodeState.FAILURE)
            return m_State;

        if (CheckIsInZone())
        {
            ErrorHandler.Log("CheckIsInZone() : SUCCESS", ELogTag.AICheckers);
            m_State = NodeState.SUCCESS;
            return m_State;
        }

        return m_State;
    }

    /// <summary>
    /// Check if character is standing on a Spell Preview
    /// </summary>
    /// <returns></returns>
    bool CheckIsInZone()
    {
        // Check for overlapped colliders with the capsule collider
        Collider2D[] colliders = CollisionChecker.GetColliderCollisions(m_ImmediatThreatTrigger.Collider, new List<ELayer>() { ELayer.Spell });

        // Now you can iterate through overlappedColliders to handle each collider
        foreach (Collider2D collider in colliders)
        {
            Spell spell = collider.gameObject.GetComponent<Spell>();
            if (spell == null) 
            {
                ErrorHandler.Error("Found collider on layer Spell but unable to find a Spell component");
                continue;
            }

            // ignore allies spells
            if (spell.Controller.Team == m_Controller.Team)
                continue;

            // only check zones
            if (spell.SpellData.SpellType != ESpellType.Zone)
                continue;

            return true;
        }

        return false;
    }

    #endregion
}
