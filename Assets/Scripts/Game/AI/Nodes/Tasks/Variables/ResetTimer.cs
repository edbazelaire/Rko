using AI;
using Enums;
using System;
using Tools;

namespace Game.AI.Tasks.Variables
{
    public class ResetTimer : BaseNode
    {
        #region Members

        string m_Id;
        Func<float> m_TimerMethod;
        bool? m_FreezeTimer;

        float? m_Timer => m_TimerMethod.Invoke();

        #endregion


        #region Init & End

        public ResetTimer(Controller controller, string id, float? timer = null, bool? freezeTimer = null) : base(controller)
        {
            m_Id = id;
            m_TimerMethod = timer == null ? null : () => timer.Value;
            m_FreezeTimer = freezeTimer;
        }

        public ResetTimer(Controller controller, string id, Func<float> timerMethod = null, bool? freezeTimer = null) : base(controller)
        {
            m_Id = id;
            m_TimerMethod = timerMethod;
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

