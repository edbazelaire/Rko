using AI;
using Enums;
using Tools;

public class TaskDodge : TaskMove
{
    #region Members

    #endregion


    #region Init & End

    public TaskDodge(Controller controller) : base(controller) { }

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
