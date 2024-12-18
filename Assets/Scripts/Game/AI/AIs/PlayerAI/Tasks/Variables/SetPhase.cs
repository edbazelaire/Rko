using AI;
using Enums;
using Tools;

namespace Game.AI.Tasks.Variables
{
    public class SetPhase : BaseNode
    {
        #region Members

        int  m_Phase;

        #endregion


        #region Init & End

        public SetPhase(Controller controller, int phase) : base(controller)
        {
            m_Phase = phase;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            m_Controller.BehaviorTree.SetPhase(m_Phase);
            return m_State;
        }

        #endregion

    }
}

