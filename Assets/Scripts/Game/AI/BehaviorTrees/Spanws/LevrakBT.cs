using AI;
using Enums;
using System.Collections.Generic;
using System.Linq.Expressions;

namespace Game.AI.BehaviorTrees
{
    public class LevrakBT : DefaultBT
    {
        public LevrakBT(Controller controller) : base(controller, EArenaDifficulty.Normal)
        {
            
        }

        public override Node LoadTree()
        {
            return new Selector(new List<Node>
            {
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate, spellEvent: ESpellEvent.OnEnd),

                new Sequence(new List<Node>()
                {
                    new CheckInArea(m_Controller, ESpellTarget.EnemyZone),
                    new TaskUseSpell(m_Controller, ESpell.Netherglacier),
                }),

                //new Sequence(new List<Node>()
                //{
                //    new CheckObstacles(m_Controller, distance: 2.5f, checkLeft: true, checkRight: false, reversed: true),
                //    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),
                //}),
                
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, delay: 5),

                new TaskMove(m_Controller, checkZones: false, checkProjectiles: false, ignoreInvisibleWalls: true),
                new TaskWait(m_Controller),
            });
        }
    }
}