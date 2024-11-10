using AI;
using Enums;
using Tools;

namespace Game.AI.Tasks.Resets
{
    public class ResetTimer : BaseNode
    {
        #region Members

        string m_Id;
        float? m_Timer;
        bool? m_FreezeTimer;

        #endregion


        #region Init & End

        public ResetTimer(Controller controller, string id, float? timer = null, bool? freezeTimer = null) : base(controller)
        {
            m_Id = id;
            m_Timer = timer;
            m_FreezeTimer = freezeTimer;
        }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;
            if (m_Timer == null)
            {
                ErrorHandler.Log("ResetTimer - SUCCESS : Reseting " + m_Id + " with timer " + m_Timer, ELogTag.AITaskJump);
                m_Controller.BehaviorTree.DeleteTimer(m_Id);
            }
            else
            {
                ErrorHandler.Log("ResetTimer - SUCCESS : Reseting " + m_Id + " with timer " + m_Timer, ELogTag.AITaskJump);
                m_Controller.BehaviorTree.ResetTimer(m_Id, m_Timer.Value);
            }

            if (m_FreezeTimer != null)
            {
                m_Controller.BehaviorTree.FreezeTimer(m_Id, m_FreezeTimer.Value);
            }

            return m_State;
        }

        #endregion

    }
}

