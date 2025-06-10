using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;

namespace Game.AI.BehaviorTrees
{
    public class IceGolemBT: DefaultBT
    {
        public static float CarapiceDelay = 30f;

        public IceGolemBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }


        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // CHECK : Ultimate
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),

                // CHECK (MELTING) : Can cast Carapice
                new Sequence(new List<Node> {
                    new CheckHasState(m_Controller, "Melting"),

                    new Selector(new List<Node>
                    {
                        // Carapice
                        new TaskUseSpell(m_Controller, ESpell.Carapice, delay: CarapiceDelay),

                        // CHECK - spells specifics to "Melting" state
                        new TaskUseSpell(m_Controller, ESpell.CryoPunch, delay: 2f),
                        new TaskUseSpell(m_Controller, ESpell.Stalacmite, delay: 8f),
                    })
                }),

                // CHECK - other spells
                new TaskUseSpell(m_Controller, ESpell.FrostfistRain),

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

