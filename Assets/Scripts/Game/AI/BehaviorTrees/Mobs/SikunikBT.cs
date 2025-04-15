using System;
using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using Tools;

namespace Game.AI.BehaviorTrees
{
    enum ESikunikState
    {
        None,

        // -- P1
        CastingSleep,
        Sleeping,
        CastingUltimate,
        Soaring,
        Landing,

        // -- P2
    }

    public class SikunikBT : DefaultBT
    {
        public SikunikBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                // NONE STATE               ===================================================
                new Sequence(new List<Node> {
                    new CheckBTState(m_Controller, ESikunikState.None.ToString()),
                    new SetPhase(m_Controller, 1),
                    new SetState(m_Controller, ESikunikState.CastingSleep.ToString())
                }),

                // =========================================================================================
                // PHASE 1     
                new Sequence(new List<Node> { 
                    new CheckPhase(m_Controller, 1),

                    new Selector(new List<Node>
                    {
                        // CASTING SLEEP STATE      ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.CastingSleep.ToString()),
                            new Sequence(new List<Node> {
                                new TaskUseSpell(m_Controller, ESpell.DragonicRest),
                                new SetState(m_Controller, ESikunikState.Sleeping.ToString())
                            }),
                        }),

                        // SLEEPING STATE           ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.Sleeping.ToString()),

                            new Selector(new List<Node>
                            {
                                // CHECK : Ultimate ready ?
                                new Sequence(new List<Node> {
                                    new CheckCanBeCasted(m_Controller, ESpell.Soaring),
                                    new SetState(m_Controller, ESikunikState.CastingUltimate.ToString())
                                }),

                                // Otherwise : Wait
                                new TaskWait(m_Controller)
                            })
                        }),

                        // CASTING ULTIMATE STATE            ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.CastingUltimate.ToString()),

                            new Selector(new List<Node>
                            {
                                // CAST : AzurPowerOrbs
                                new Sequence(new List<Node> {
                                    new CheckCount(m_Controller,        ESpell.AzurePowerOrbs.ToString(), 1),
                                    new TaskUseSpell(m_Controller,      ESpell.AzurePowerOrbs, spellEvent: ESpellEvent.OnCast, resetCooldown: true),
                                    new IncreaseCounter(m_Controller,   ESpell.AzurePowerOrbs.ToString()),
                                    new GainEnergy(m_Controller,        100)   // re-set energy to max to be sure that the value is maxed
                                }),
                                
                                // CAST : Soaring
                                new Sequence(new List<Node> {
                                    new CheckCount(m_Controller,        ESpell.Soaring.ToString(), 1),
                                    new TaskUseSpell(m_Controller,      ESpell.Soaring),
                                    new IncreaseCounter(m_Controller,   ESpell.Soaring.ToString())
                                }),

                                // CHECK : START JUMP STATE (wait until seeing jump state)
                                new Sequence(new List<Node> {
                                    new CheckHasState(m_Controller, "Jump"),
                                    new SetState(m_Controller, ESikunikState.Soaring.ToString())
                                }),

                                // Otherwise : wait
                                new TaskWait(m_Controller),
                            })
                        }),

                        // SOARING STATE            ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.Soaring.ToString()),

                            new Selector(new List<Node>
                            {
                                // CHECK : END JUMP STATE (wait until the "Jump" state is gone)
                                new Sequence(new List<Node> {
                                    new CheckHasState(m_Controller, "Jump", isReversed: true),
                                    new SetState(m_Controller, ESikunikState.Landing.ToString())
                                }),

                                new TaskWait(m_Controller),
                            })
                        }),

                        // LANDING STATE            ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.Landing.ToString()),

                            new Selector(new List<Node>
                            {
                                // CAST : Azure Deflagration
                                new Sequence(new List<Node> {
                                    // check Azure Deflagration has been used
                                    new CheckCount(m_Controller, ESpell.AzureDeflagration.ToString(), 1),
                            
                                    // WAIT SUCCESS on Azure Deflagration
                                    new Selector(new List<Node> {
                                        new Sequence(new List<Node> {
                                            new TaskUseSpell(m_Controller, ESpell.AzureDeflagration, resetCooldown: true),
                                            new IncreaseCounter(m_Controller, ESpell.AzureDeflagration.ToString()),
                                        }),
                                        new TaskWait(m_Controller),
                                    })
                                }),

                                // CHECK : Should set next state or next phase ?
                                new Sequence(new List<Node> {
                                    new CheckProperty(m_Controller, EStateEffectProperty.Hp, 0.5f, isPerc: true, relation: "<="),
                                    new SetPhase(m_Controller, 2),
                                }),

                                new SetState(m_Controller, ESikunikState.CastingSleep.ToString())
                            })
                        }),
                    }),
                }),

                // =========================================================================================
                // PHASE 2            
                new Sequence(new List<Node> {
                    new CheckPhase(m_Controller, 2),

                    new Selector(new List<Node> {
                        new TaskUseSpell(m_Controller, ESpell.AzureDeflagration, delay: 5f),

                        new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),
                    }),
                }),

                new TaskWait(m_Controller),
            });
        }

        public override void OnStateChanged(string stringState)
        {
            base.OnStateChanged(stringState);

            if (! Enum.TryParse(stringState, out ESikunikState state))
            {
                ErrorHandler.Error("Unable to parse " + stringState + " into a ESikunikState state");
                return;
            }

            switch (state)
            {
                case ESikunikState.None:
                    return;
                    
                case ESikunikState.CastingSleep:
                    return;

                case ESikunikState.Sleeping:
                    return;

                case ESikunikState.CastingUltimate:
                    return;

                case ESikunikState.Soaring:
                    return;

                case ESikunikState.Landing:
                    return;

                default:
                    ErrorHandler.Warning("Unhanlded case : " + stringState);
                    return;
            }
        }

        public void OnPhaseChanged(int phase)
        { 
            switch (phase)
            {
                case 1:
                    break;

                case 2:
                    break;
            }
        }
    }
}

