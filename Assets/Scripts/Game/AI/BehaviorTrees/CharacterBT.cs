using AI;
using Managers;
using UnityEngine;

namespace Game.AI
{
    public class CharacterBT : BehaviorTree
    {
        #region Members

        protected float m_BaseOffensiveMeter;

        Controller m_Target;

        /// <summary> Is the bot more encline to offensive or defensive plays ? </summary>
        public float OffensiveMeter => Mathf.Clamp01(m_BaseOffensiveMeter + (0.5f * m_Controller.Life.PercHp) - (0.5f * m_Target.Life.PercHp));

        /// <summary> XPos currently aimed by the bot </summary>
        public float AimingXPos { get; set; }

        #endregion


        #region Init & End

        public override void Initialize(SBotData botData)
        {
            base.Initialize(botData);
            
            m_Target = GameManager.Instance.GetFirstEnemy(m_Controller.Team);
            m_BaseOffensiveMeter = Random.Range(0.3f, 0.7f);
            AimingXPos = 0f;
        }

        #endregion

    }
}
