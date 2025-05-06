using AI;
using Enums;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

public class CheckDodge : BaseChecker
{
    #region Members

    float m_ActivationDuration;
    float m_ActivationTimer;

    #endregion


    #region Init & End
    
    public CheckDodge(Controller controller, float activeDuration = 0f) : base(controller) 
    { 
        m_ActivationDuration = activeDuration;
    }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        if (m_ActivationTimer > 0)
            return m_State;

        // checks if is on a spell preview
        if (m_ImmediatThreatTrigger.CheckTriggerSpellSpawn())
        {
            ErrorHandler.Log("CheckDodge : CheckTriggerSpellSpawn() SUCCESS", ELogTag.AICheckers);
            SetNodeState(NodeState.SUCCESS);
            return m_State;
        }

        // checks that is not in the trajectory of projectile
        if (m_ProjectileTrigger.IsTriggered)
        {
            foreach (var projectile in m_ProjectileTrigger.Projectiles)
            {
                (bool isThreat, bool isDodgeable, int _) = TaskMove.IsProjectileAtThreatDistance(projectile, m_Controller, new List<int>() { -1, 1 });
                if (isThreat) 
                {
                    ErrorHandler.Log("CheckDodge : IsProjectileAtThreatDistance() SUCCESS - isThreat = true | isDodgeable = " + isDodgeable, ELogTag.AICheckers);
                    SetNodeState(NodeState.SUCCESS);
                    return m_State;
                }
            }
        }

        SetNodeState(NodeState.FAILURE);
        return m_State;
    }

    #endregion


    #region Coroutines

    protected override void OnSuccess()
    {
        base.OnSuccess();
        //m_Controller.StartCoroutine(StartActivationTimer());
    }


    // =================================================================
    // TODO : Remove ? (keep node active for X seconds)
    IEnumerator StartActivationTimer()
    {
        Debug.Log("StartActivationTimer() - START");
        m_ActivationTimer = m_ActivationDuration;

        while (m_ActivationTimer > 0)
        {
            if (m_State != NodeState.SUCCESS)
                yield break;

            m_ActivationTimer -= Time.deltaTime;
            yield return null;
        }

        Debug.Log("StartActivationTimer() - END");
    }
    // =================================================================

    #endregion


    #region Reset

    public override void Reset()
    {
        base.Reset();

        m_ActivationTimer = 0;
    }

    #endregion


}
