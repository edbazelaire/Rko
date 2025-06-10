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
                // COUNTER STATE      ===================================================
                new Sequence(new List<Node> {
                    new CheckHasCounter(m_Controller),

                    // check spell to use
                    new Selector(new List<Node>
                    {
                        // use a spell
                        new TaskUseSpell(m_Controller, ESpell.DarkstarDescent),
                        new TaskUseSpell(m_Controller, ESpell.ChaosOrb),
                        new TaskWait(m_Controller)
                    })
                }),

                // AWAKE STATE      ===================================================
                new Sequence(new List<Node> {
                    new CheckHasCounter(m_Controller, reversed: true),

                    // check spell to use
                    new Selector(new List<Node>
                    {
                        // ULTIMATE
                        new Sequence(new List<Node> {
                            new CheckCanBeCasted(m_Controller, m_Controller.SpellHandler.Ultimate),
                            new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),
                            new SetState(m_Controller, EAtassutState.Counter.ToString())
                        }),

                        // SHADOW VEIL
                        new Sequence(new List<Node>
                        {
                            // 1) Check that DO NOT have effect active
                            new CheckHasState(m_Controller, "ShadowVeil", isReversed: true),

                            // 2) Start timer - (0f the first time)
                            new CheckTimer(m_Controller, "ShadowVeil", timer: 0f),

                            // 3) Once timer is over - cast spell as soon as available
                            new TaskUseSpell(m_Controller, ESpell.ShadowVeil),

                            // 4) Reset timer 
                            new ResetTimer(m_Controller, "ShadowVeil", timer: 8f),
                        }),
                        
                        // USE SPECIAL ABILITY
                        new TaskUseSpell(m_Controller, ESpell.Scythefall, delay: 8f),
            
                        // ASTRAL ICEFALL
                        new TaskUseSpell(m_Controller, ESpell.AstralIcefall, delay: 12f),

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
        }

        #endregion
    }
}

