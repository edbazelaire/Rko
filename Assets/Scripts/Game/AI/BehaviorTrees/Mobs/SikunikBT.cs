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


        #region Default Tree

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {           
                // DEBUG               ===================================================
                //new TaskWait(m_Controller),
                // DEBUG               ===================================================


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
                                new TaskUseSpell(m_Controller, ESpell.DragonicRest, spellEvent: ESpellEvent.None),
                                new SetState(m_Controller, ESikunikState.Sleeping.ToString())
                            }),
                        }),

                        // SLEEPING STATE           ===================================================
                        new Sequence(new List<Node> {
                            new CheckBTState(m_Controller, ESikunikState.Sleeping.ToString()),

                            new Selector(new List<Node>
                            {
                                // CHECK : Should start phase 2 ?
                                CheckStartPhase2(),

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
                                    new TaskUseSpell(m_Controller,      ESpell.AzurePowerOrbs, resetCooldown: true),
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
                        LandingPhase(),
                    }),
                }),

                // =========================================================================================
                // PHASE 2            
                new Sequence(new List<Node> {
                    new CheckPhase(m_Controller, 2),

                    new Selector(new List<Node> {
                        new TaskUseSpell(m_Controller, ESpell.Disintegrate, spellEvent: ESpellEvent.OnEnd, globalCooldown: 2f),
                        new TaskUseSpell(m_Controller, ESpell.Rainballs, globalCooldown: 2f),
                        new TaskUseSpell(m_Controller, ESpell.Eruptions, globalCooldown: 2f),
                        new TaskUseSpell(m_Controller, ESpell.AzureDeflagration, globalCooldown: 2f),
                    }, saveCurrentNode: true, random: true),

                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, ignoreGlobalCooldown: true),
                    new TaskWait(m_Controller),
                }),

                new TaskWait(m_Controller),
            });
        }

        #endregion


        #region Landing Phase

        Sequence LandingPhase()
        {
            return new Sequence(new List<Node> {
                new CheckBTState(m_Controller, ESikunikState.Landing.ToString()),

                new Selector(new List<Node>
                {
                    // CHECK : while "landing phase" is not over, attack
                    new Sequence(new List<Node>
                    {
                        new CheckTimer(m_Controller, "LandingPhase", GetLandingPhaseDuration()),

                        // CHECK : Should set next state or next phase ?
                        new Selector(new List<Node> {
                            // check - should start phase 2 ?
                            CheckStartPhase2(),

                            // otherwise - go back to sleep
                            new SetState(m_Controller, ESikunikState.CastingSleep.ToString())
                        }),
                    }),

                    // SELECT : one of the spells
                    new Selector(new List<Node> {
                        new TaskUseSpell(m_Controller, ESpell.AzureDeflagration, globalCooldown: 2f),
                        new TaskUseSpell(m_Controller, ESpell.Disintegrate, spellEvent: ESpellEvent.OnEnd, globalCooldown: 2f),
                        new TaskUseSpell(m_Controller, ESpell.Eruptions, globalCooldown: 2f),
                    }, saveCurrentNode: true, random: true),

                    new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, ignoreGlobalCooldown: true),
                    new TaskWait(m_Controller),
                })
            });
        }

        float GetLandingPhaseDuration()
        {
            if (m_ArenaDifficulty <= EArenaDifficulty.Painful)
                return 0f;

            if (m_ArenaDifficulty == EArenaDifficulty.Brutal)
                return 15f;

            if (m_ArenaDifficulty >= EArenaDifficulty.Torment)
                return 25f;

            ErrorHandler.Warning("Unhanlded case : " + m_ArenaDifficulty);
            return 0f;
        }

        Node CheckStartPhase2()
        {
            return new Sequence(new List<Node> {
                new Selector(new List<Node>
                {
                    // HP : <= 50%
                    new CheckProperty(m_Controller, EStateEffectProperty.Hp, 0.5f, isPerc: true, relation: "<="),
                    // Timer : 240 sec
                    new CheckTimer(m_Controller, "Phase2", 240),                
                }),
                new SetPhase(m_Controller, 2)
            });
        }

        #endregion


        #region State Management

        public override void OnStateChanged(string stringState)
        {
            base.OnStateChanged(stringState);

            if (!Enum.TryParse(stringState, out ESikunikState state))
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
                    m_Controller.CounterHandler.EndCounter(ESpell.DragonicRest);
                    return;

                case ESikunikState.Soaring:
                    return;

                case ESikunikState.Landing:
                    m_Controller.BehaviorTree.ResetTimer("LandingPhase", GetLandingPhaseDuration());
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

        #endregion
    }
}

