using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;

namespace Game.AI.BehaviorTrees
{
    enum EAtassutState
    {
        None,

        // -- P1
        Counter,
        Awake,

        // -- P2
    }

    public class AtassutBT : DefaultBT
    {
        public AtassutBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }


        #region Trees

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // NONE STATE               ===================================================
                new Sequence(new List<Node> {
                    new CheckBTState(m_Controller, EAtassutState.None.ToString()),
                    new SetPhase(m_Controller, 1),

                    // ULTIMATE
                    new Sequence(new List<Node> {
                        new CheckCanBeCasted(m_Controller, m_Controller.SpellHandler.Ultimate),
                        new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),
                        new SetState(m_Controller, EAtassutState.Counter.ToString())
                    }),
                }),

                // COUNTER STATE      ===================================================
                new Sequence(new List<Node> {
                    new CheckBTState(m_Controller, EAtassutState.Counter.ToString()),

                    // check spell to use
                    new Selector(new List<Node>
                    {
                        // check next state
                        new Sequence(new List<Node> {
                            new CheckHasCounter(m_Controller, true),
                            new SetState(m_Controller, EAtassutState.Awake.ToString())
                        }),

                        // use a spell
                        new TaskUseSpell(m_Controller, ESpell.DarkstarDescent),
                        new TaskUseSpell(m_Controller, ESpell.ChaosOrb),
                        new TaskWait(m_Controller)
                    })
                }),

                // AWAKE STATE      ===================================================
                new Sequence(new List<Node> {
                    new CheckBTState(m_Controller, EAtassutState.Awake.ToString()),

                    // check spell to use
                    new Selector(new List<Node>
                    {
                        // ULTIMATE
                        new Sequence(new List<Node> {
                            new CheckCanBeCasted(m_Controller, m_Controller.SpellHandler.Ultimate),
                            new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),
                            new SetState(m_Controller, EAtassutState.Counter.ToString())
                        }),

                        // USE SPECIAL ABILITY
                        new TaskUseSpell(m_Controller, ESpell.Scythefall, delay: 8f),
            
                        // ASTRAL ICEFALL
                        new TaskUseSpell(m_Controller, ESpell.AstralIcefall, delay: 12f),

                        // GREAT VORTEX
                        new TaskUseSpell(m_Controller, ESpell.GreatVortex, delay: 15f),

                        // AUTO ATTACK
                        new TaskUseSpell(m_Controller, ESpell.ChaosOrb),

                        // MOVE
                        new TaskMove(m_Controller, checkZones: false, checkProjectiles: false),

                        // Stand Still if cant move
                        new TaskWait(m_Controller),
                    })
                }),
            });
        }

        #endregion


        #region State 

        public override void OnStateChanged(string state)
        {
            base.OnStateChanged(state);

            if (state == EAtassutState.Counter.ToString())
            {
                if (m_Controller.StateHandler.HasState("ShadowVeil"))
                    m_Controller.StateHandler.RemoveStateEffect("ShadowVeil"); 
            } 
            else if (state == EAtassutState.Awake.ToString())
            {
                m_Controller.StateHandler.AddStateEffect("ShadowVeil", m_Controller, m_Controller.CharacterLevel);
            }
        }

        #endregion
    }
}

