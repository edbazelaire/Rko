using AI;
using Enums;
using Game;
using Tools.Helpers;

public class TaskMoveToArea : TaskMove
{
    #region Members

    ESpellTarget m_TargetArea;

    #endregion


    #region Init & End

    public TaskMoveToArea(Controller controller, ESpellTarget targetArea) : base(controller, false, false, true, null)
    {
        m_TargetArea = targetArea;
    }

    #endregion


    #region Movement Selection

    protected override void SelectMovement()
    {
        (float min, float max) = ArenaManager.GetAreaBounds(m_Controller.Team, TargetHelper.IsEnemyTarget(m_TargetArea));

        m_AllowedMovements.Clear();

        m_State = NodeState.RUNNING;
        if (m_Controller.transform.position.x >= max - m_SizeOffset)
        {
            //m_CurrentMoveX = m_Controller.Team == 0 ? -1 : 1;
            m_CurrentMoveX = -1;
            return;
        }

        if (m_Controller.transform.position.x <= min + m_SizeOffset)
        {
            //m_CurrentMoveX = m_Controller.Team == 0 ? 1 : -1;
            m_CurrentMoveX = 1;
            return;
        }

        m_State = NodeState.SUCCESS;
        return;
    }

    #endregion

}
