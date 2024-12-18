using AI;
using Enums;
using Tools;

namespace Game.AI.Tasks.Variables
{
    public class IncreaseCounter : BaseNode
    {
        #region Members

        string  m_Id;
        int     m_Increase;

        #endregion


        #region Init & End

        public IncreaseCounter(Controller controller, string id, int increase = 1) : base(controller)
        {
            m_Id = id;
            m_Increase = increase;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            m_Controller.BehaviorTree.IncreaseCounter(m_Id, m_Increase);
            return m_State;
        }

        #endregion

    }
}

