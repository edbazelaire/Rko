using AI;
using Enums;
using Game;
using NUnit.Framework.Constraints;
using System;
using System.Collections;
using Tools;
using UnityEngine;

public class TaskPatrol : TaskMove
{
    #region Members

    float m_TimeBetweenPatrols;
    Vector3 m_StartPosition;
    Vector3 m_EndPosition;

    #endregion


    #region Init & End

    public TaskPatrol(Controller controller, float timeBetweenPatrols = 0f) : base(controller, false, false, false, null) 
    {
        m_TimeBetweenPatrols = timeBetweenPatrols;

        m_StartPosition = new Vector3((m_Controller.Team == 0 ? 1 : -1) * (-7 - m_SizeOffset), m_Controller.transform.position.y, 0);
        m_EndPosition = new Vector3(- m_StartPosition.x, m_StartPosition.y, m_StartPosition.z);
    }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        m_State = NodeState.SUCCESS;

        m_Movement.SetMovement(1);

        // check has reached the end of the map
        if (CheckReachedEnd())
            GameManager.Instance.StartCoroutine(DeactivateCharacter());

        ErrorHandler.Log(() => "TaskPatrol - " + m_State, ELogTag.AITaskMove);
        return m_State;
    }

    bool CheckReachedEnd()
    {
        return (m_Controller.Team == 0 ? m_Controller.transform.position.x >= m_EndPosition.x : m_Controller.transform.position.x <= m_EndPosition.x);
    }

    IEnumerator DeactivateCharacter()
    {
        m_Controller.transform.position = m_StartPosition;

        if (m_TimeBetweenPatrols > 0)
        {
            // reset character gfx (because of Animation RootMotion)
            m_Controller.GFXHandler.CharacterPreview.transform.localPosition = Vector3.zero;

            m_Controller.Activate(false);

            yield return new WaitForSeconds(m_TimeBetweenPatrols);

            m_Controller.Activate(true);
        }
    }

    #endregion

}
