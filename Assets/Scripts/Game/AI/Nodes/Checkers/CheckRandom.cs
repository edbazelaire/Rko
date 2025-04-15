using UnityEngine;

namespace AI
{
    public class CheckRandom : BaseChecker
    {
        #region Init & End

        public CheckRandom(Controller controller) : base(controller) { }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_State = NodeState.FAILURE;

            if (RandomActivation())
            {
                m_State = NodeState.SUCCESS;
                return m_State;
            }

            return m_State;
        }

        protected virtual bool RandomActivation()
        {
            return m_Controller.BehaviorTree.BotData.Randomness > Random.Range(0f, 1f);
        }

        #endregion
    }
}