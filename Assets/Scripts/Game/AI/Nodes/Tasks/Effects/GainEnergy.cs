using AI;
using System.Collections;
using UnityEngine;

namespace Game.AI
{
    public class GainEnergy : BaseNode
    {
        #region Members

        protected int m_EnergyGain;

        #endregion

        public GainEnergy(Controller controller, int energy) : base(controller)
        {
            m_EnergyGain = energy;
        }

        public override NodeState Evaluate()
        {
            m_State = NodeState.SUCCESS;

            m_Controller.EnergyHandler.AddEnergy(m_EnergyGain);

            return m_State;
        }

    }
}
