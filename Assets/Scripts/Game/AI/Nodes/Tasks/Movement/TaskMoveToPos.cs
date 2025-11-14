using AI;
using Game.AI;
using System;
using System.Collections.Generic;
using UnityEngine;

public class TaskMoveToPos : TaskMove
{
    #region Members

    CharacterBT m_CharacterBT;

    #endregion


    #region Init & End

    public TaskMoveToPos(Controller controller, Func<float> weight = null) : base(controller, true, true, true, weight)
    {
        m_CharacterBT = (CharacterBT)m_Controller.BehaviorTree;
    }

    #endregion


    #region Movement Selection

    protected override void SelectMovement()
    {
        // CHECK : has aiming pos
        if (m_CharacterBT.AimingXPos == 0f)
        {
            m_State = NodeState.FAILURE;
            return;
        }

        // check destination reached
        if (Mathf.Abs(m_Controller.transform.position.x - m_CharacterBT.AimingXPos) <= 0.1f)
        {
            m_State = NodeState.SUCCESS;
            m_CharacterBT.AimingXPos = 0f;
            return;
        }

        // check allowed movments
        base.SelectMovement();

        // no movement or something went wrong - exit
        if (m_State == NodeState.FAILURE)
            return;

        // check that expected direction is in allowed movements
        int direction = m_CharacterBT.AimingXPos > m_Controller.transform.position.x ? 1 : -1;
        if (m_AllowedMovements.Contains(direction))
        {
            m_State = NodeState.RUNNING;
            m_AllowedMovements = new List<int>() { direction };
            return;     
        }

        m_State = NodeState.FAILURE;
    }

    #endregion

}
