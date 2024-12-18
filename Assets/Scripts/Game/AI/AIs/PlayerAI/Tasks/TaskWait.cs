using AI;
using Enums;
using Game.Character;
using System.Collections;
using Tools;
using UnityEngine;

namespace Game.AI
{
    public class TaskWait : BaseNode
    {
        #region Members

        protected Movement m_Movement => m_Controller.Movement;

        #endregion


        #region Init & End

        public TaskWait(Controller controller) : base(controller)
        {
            
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            m_Movement.SetMovement(0);
            return m_State;
        }

        #endregion
    }
}