using AI;
using Enums;
using System;
using Tools;

public class TaskDodge : TaskMove
{
    #region Members

    #endregion


    #region Init & End

    public TaskDodge(Controller controller, Func<float> weight = null) : base(controller, true, true, weight) { }

    #endregion


    #region Movement Selection

    protected override void SelectMovement()
    {
        base.SelectMovement();

        if (m_AllowedMovements.Count == 0)
        {
            m_State = NodeState.FAILURE;
            return;
        }
    }

    #endregion

}
