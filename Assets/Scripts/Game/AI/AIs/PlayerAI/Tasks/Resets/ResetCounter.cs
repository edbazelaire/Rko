using AI;
using Enums;
using Tools;

namespace Game.AI.Tasks.Resets
{
    public class ResetCounter : BaseNode
    {
        #region Members

        string  m_Id;
        int?    m_Counter;

        #endregion


        #region Init & End

        public ResetCounter(Controller controller, string id, int? counter = null) : base(controller)
        {
            m_Id = id;
            m_Counter = counter;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            if (m_Counter == null)
            {
                ErrorHandler.Log("ResetTimer - SUCCESS : Reseting " + m_Id + " with timer " + m_Counter, ELogTag.AITaskJump);
                m_Controller.BehaviorTree.DeleteCounter(m_Id);
            }
            else
            {
                ErrorHandler.Log("ResetTimer - SUCCESS : Reseting " + m_Id + " with timer " + m_Counter, ELogTag.AITaskJump);
                m_Controller.BehaviorTree.ResetCounter(m_Id, m_Counter.Value);
            }

            return m_State;
        }

        #endregion

    }
}

