using AI;
using Enums;
using Tools;

namespace Game.AI.Tasks.Variables
{
    public class SetState : BaseNode
    {
        #region Members

        string  m_BehaviorState;

        #endregion


        #region Init & End

        public SetState(Controller controller, string state) : base(controller)
        {
            m_BehaviorState = state;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            m_Controller.BehaviorTree.SetState(m_BehaviorState);
            return m_State;
        }

        #endregion

    }
}

