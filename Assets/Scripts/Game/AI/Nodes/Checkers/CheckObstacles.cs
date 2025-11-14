using AI;
using Enums;
using Game;
using Game.AI;
using Tools;
using Tools.Helpers;
using UnityEngine;

public class CheckObstacles : BaseChecker
{
    #region Members

    bool m_CheckLeft;
    bool m_CheckRight;
    float m_Distance;

    #endregion


    #region Init & End
    
    public CheckObstacles(Controller controller, float distance = 1f, bool checkLeft = true, bool checkRight = true, bool reversed = false) : base(controller, reversed) 
    {
        m_CheckLeft = checkLeft;
        m_CheckRight = checkRight;
        m_Distance = distance;
    }

    #endregion


    #region Evaluation

    public override NodeState Evaluate()
    {
        base.Evaluate();

        if (m_CheckLeft)
        {
            Collider2D[] colliders = CollisionChecker.GetCollidersInDistance(
                m_Controller.transform.position.x,
                -1 * m_Distance,
                CollisionChecker.GetObstacleLayers(ignoreInvisibleWalls: true, ignoreStructures: true)
            );

            if (colliders.Length > 0)
            {
                SetNodeState(NodeState.SUCCESS);
                return m_State;
            }
        }

        if (m_CheckRight)
        {
            Collider2D[] colliders = CollisionChecker.GetCollidersInDistance(
                m_Controller.transform.position.x,
                1 * m_Distance,
                CollisionChecker.GetObstacleLayers(ignoreInvisibleWalls: true, ignoreStructures: true)
            );

            if (colliders.Length > 0)
            {
                SetNodeState(NodeState.SUCCESS);
                return m_State;
            }
        }

        SetNodeState(NodeState.FAILURE);
        return m_State;
    }

    #endregion
}
