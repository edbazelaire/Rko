using System.Collections;
using UnityEngine;

namespace AI
{
    public class BaseChecker : BaseNode
    {
        #region Members

        protected bool m_IsReversed;

        NodeState? m_ExpectedState = null;
        Coroutine m_ReactionTimeCoroutine = null;

        #endregion


        #region Init & End

        public BaseChecker(Controller controller, bool reversed = false) : base(controller) 
        { 
            m_IsReversed = reversed;
        }

        public override void Reset()
        {
            base.Reset();

            if (m_ReactionTimeCoroutine != null)
                m_Controller.StopCoroutine(m_ReactionTimeCoroutine);

            m_ExpectedState = null;
        }

        #endregion


        #region State Management

        public override void SetNodeState(NodeState state)
        {
            if (m_IsReversed)
            {
                if (state == NodeState.FAILURE)
                    state = NodeState.SUCCESS;

                else if (state == NodeState.SUCCESS)
                    state = NodeState.FAILURE;
            }

            StartSetStateCoroutine(state);
        }

        void StartSetStateCoroutine(NodeState state)
        {
            // if state is already current state or is expected state - exit
            if (state == m_ExpectedState || state == m_State)
                return;

            m_ExpectedState = state;

            if (m_ReactionTimeCoroutine != null)
                m_Controller.StopCoroutine(m_ReactionTimeCoroutine);

            m_ReactionTimeCoroutine = m_Controller.StartCoroutine(SetStateCoroutine(state));
        }

        IEnumerator SetStateCoroutine(NodeState state)
        {
            var reactionTime = Random.Range(m_Controller.BehaviorTree.BotData.MinReactionTime, m_Controller.BehaviorTree.BotData.MaxReactionTime);
            while (reactionTime > 0)
            {
                // state already set - exit
                if (state == m_State)
                    yield break;

                // no longer the expected state - exit
                if (state != m_ExpectedState)
                    yield break;

                reactionTime -= Time.deltaTime;
                yield return null;
            }

            // when the reaction time is done - set the state
            base.SetNodeState(state);
        }

        #endregion
    }
}