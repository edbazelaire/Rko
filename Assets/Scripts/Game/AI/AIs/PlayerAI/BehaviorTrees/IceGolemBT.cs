using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using Tools;

namespace Game.AI.BehaviorTrees
{
    public class IceGolemBT: DefaultBT
    {
        public static float CarapiceDelay = 10f;

        public IceGolemBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }


        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // CHECK (MELTING) : Can cast Carapice
                new Sequence(new List<Node> {
                    new CheckHasState(m_Controller, "Melting"),
                    new TaskUseSpell(m_Controller, ESpell.Carapice, delay: CarapiceDelay),
                }),

                // CHECK : Ultimate
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),

                // CHECK : 
                new TaskUseSpell(m_Controller, ESpell.CryoPunch, delay: 0f),

                // Stalacmite
                new Sequence(new List<Node> {
                    new CheckTimer(m_Controller, "Stalacmite", 8f),
                    new TaskUseSpell(m_Controller, ESpell.Stalacmite),
                }),

                // IceField
                new TaskUseSpell(m_Controller, ESpell.IceField),

                // AutoAttack
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),

                // Move
                new TaskMove(m_Controller, checkZones: false, checkProjectiles: false),

                // Wait
                new TaskWait(m_Controller),
            });
        }

        #endregion



        #region State

        public override void OnStateChanged(string state)
        {
            base.OnStateChanged(state);
        }

        #endregion
    }
}

