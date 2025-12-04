using Tools;

namespace AI
{
    public class CheckImmediatThreat : BaseChecker
    {
        #region Members

        #endregion


        #region Init & End

        public CheckImmediatThreat(Controller controller) : base(controller) { }


        #endregion


        #region Evaluation

        public override NodeState Evaluate()
        {
            base.Evaluate();

            // checks if is on a spell preview
            if (m_ImmediatThreatTrigger.IsTriggered)
            {
                ErrorHandler.Log(() => "ImmediatThreat Trigger detected", Enums.ELogTag.AI);
                SetNodeState(NodeState.SUCCESS);
            }

            return m_State;
        }

        #endregion
    }
}