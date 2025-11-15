using AI;
using Enums;
using Game;
using Tools.Helpers;

public class CheckInArea : BaseChecker
{
    #region Members

    ESpellTarget m_TargetArea;

    #endregion


    #region Init & End
    
    public CheckInArea(Controller controller, ESpellTarget targetArea) : base(controller) 
    {
        m_TargetArea = targetArea;
    }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        base.Evaluate();

        float min = -6f;
        float max = 6f;
        if (m_TargetArea != ESpellTarget.None)
            (min, max) = ArenaManager.GetAreaBounds(m_Controller.Team, TargetHelper.IsEnemyTarget(m_TargetArea));
        return m_Controller.transform.position.x > min && m_Controller.transform.position.x < max ? NodeState.SUCCESS : NodeState.FAILURE;
    }

    #endregion
}
