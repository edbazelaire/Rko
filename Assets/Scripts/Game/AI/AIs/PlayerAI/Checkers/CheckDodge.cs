using AI;
using System.Collections.Generic;
using Tools;
using Unity.VisualScripting;

public class CheckDodge : BaseChecker
{
    #region Members

    #endregion


    #region Init & End
    
    public CheckDodge(Controller controller) : base(controller) { }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        base.Evaluate();

        if (m_State != NodeState.FAILURE)
            return m_State;

        // checks if is on a spell preview
        if (m_ImmediatThreatTrigger.CheckTriggerSpellSpawn())
        {
            m_State = NodeState.SUCCESS;
            return m_State;
        }

        // checks that is not in the trajectory of projectile
        if (m_ProjectileTrigger.IsTriggered)
        {
            foreach (var projectile in m_ProjectileTrigger.Projectiles)
            {
                (bool isThreat, bool isDodgeable, int _) = TaskMove.IsProjectileAtThreatDistance(projectile, m_Controller, new List<int>() { -1, 1 });
                if (isThreat && isDodgeable) 
                {
                    m_State = NodeState.SUCCESS;
                    return m_State;
                }
            }
        }

        return m_State;
    }

    #endregion


}
