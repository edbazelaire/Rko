using AI;
using Game.Character;

namespace Game.AI
{
    public class TaskRandom : BaseTask
    {
        #region Members

        protected Movement m_Movement => m_Controller.Movement;

        #endregion


        #region Init & End

        public TaskRandom(Controller controller) : base (controller) { }

        #endregion


        #region Evaluate

        public override NodeState Evaluate()
        {
            m_Movement.SetMovement(0);
            return NodeState.SUCCESS;
        }

        #endregion

    }
}