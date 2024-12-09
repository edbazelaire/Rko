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

                // ASTRAL ICEFALL
                new TaskUseSpell(m_Controller, ESpell.AstralIcefall),

                // USE SPECIAL ABILITY
                new TaskUseSpell(m_Controller, ESpell.Scythefall, delay: 8f),
            
                // GREAT VORTEX
                new TaskUseSpell(m_Controller, ESpell.GreatVortex, delay: 15f),

                // AUTO ATTACK
                new TaskUseSpell(m_Controller, ESpell.ChaosOrb),

                // MOVE
                new TaskMove(m_Controller, checkZones: false, checkProjectiles: false),

                // Stand Still if cant move
                new TaskWait(m_Controller),
            });
        }

        #endregion
    }
}

