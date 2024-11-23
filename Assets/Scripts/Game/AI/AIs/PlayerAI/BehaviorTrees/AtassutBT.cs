using System.Collections.Generic;
using AI;
using Enums;

namespace Game.AI.BehaviorTrees
{
    public class AtassutBT : DefaultBT
    {
        public AtassutBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }


        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // COUNTER STATE
                new Sequence(new List<Node> {
                    new CheckHasCounter(m_Controller),
                }),

                // ULTIMATE
                new Sequence(new List<Node> {
                    new CheckCanBeCasted(m_Controller, m_Controller.SpellHandler.Ultimate),
                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),
                }),

                // USE SPECIAL ABILITY
                new TaskUseSpell(m_Controller, ESpell.Scythefall),
            
                // USE VORTEX
                new Sequence(new List<Node> {
                    new CheckCanBeCasted(m_Controller, ESpell.Vortex),
                    new TaskUseSpell(m_Controller, ESpell.Vortex, delay: 15),
                }),

                // MOVE
                new TaskMove(m_Controller, checkZones: true, checkProjectiles: false),

                // Stand Still if cant move
                new TaskWait(m_Controller),
            });
        }

        #endregion
    }
}

