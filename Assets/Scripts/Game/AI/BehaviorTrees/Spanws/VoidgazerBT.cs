using AI;
using Enums;
using System.Collections.Generic;

namespace Game.AI.BehaviorTrees
{
    public class VoidgazerBT : DefaultBT
    {
        float m_TimeBetweenPatrols = 10f;

        public VoidgazerBT(Controller controller) : base(controller, EArenaDifficulty.Normal)
        {
            
        }

        public override Node LoadTree()
        {
            return new Selector(new List<Node>
            {
                // Ultimate
                new Sequence(new()
                {
                    new CheckInArea(m_Controller, ESpellTarget.None),
                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate, spellEvent: ESpellEvent.OnEnd),
                }),

                new Sequence(new()
                {
                    new CheckInArea(m_Controller, ESpellTarget.EnemyZone),
                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, spellEvent: ESpellEvent.OnEnd),
                }),
                
                new TaskPatrol(m_Controller, timeBetweenPatrols: m_TimeBetweenPatrols),
            });
        }
    }
}