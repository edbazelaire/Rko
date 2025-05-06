using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using Managers;
using System;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public enum EDefaultTreeVariables
    {
        DodgeWeightBias,
        AttackWeightBias,
    }

    public class DefaultBotBT : DefaultBT
    {
        #region Members

        protected ELeague m_League;

        protected SBotData m_BotData => m_Controller.BehaviorTree.BotData;

        #endregion


        #region Constructor

        public DefaultBotBT(Controller controller, ELeague league) : base(controller, EArenaDifficulty.Normal)
        {
            m_League = league;
        }

        #endregion


        #region Tree Loading

        public override Node LoadTree()
        {
            switch (m_League)
            {
                case ELeague.Iron:
                case ELeague.Bronze:
                case ELeague.Silver:
                    return LoadBasicTree();

                default:
                    return LoadBasicTree();
            }
        }

        public static List<string> GetExtraVariables(ELeague league)
        {
            switch (league)
            {
                case ELeague.Iron:
                case ELeague.Bronze:
                case ELeague.Silver:
                    return Enum.GetNames(typeof(EDefaultTreeVariables)).ToList();

                default:
                    return Enum.GetNames(typeof(EDefaultTreeVariables)).ToList();
            }
        }

        Node LoadBasicTree()
        {
            ErrorHandler.Log("Loading BASIC Tree for " + m_Controller.Character);

            return new Selector(new List<Node>
            {
                // Check Immediat Threats (Zones & Projectiles)
                new Sequence(new List<Node> {
                    new CheckImmediatThreat(m_Controller),
                    new SelectorWeight(new List<Node>
                    {
                        // Try use defensive spell
                        new Selector(new List<Node>
                        {
                            new TaskCounter(m_Controller),
                            new TaskJump(m_Controller),
                        }, weight: () => { return 1 - m_BotData.Randomness; }),

                        // Ignore threat and attack
                        AttackGroupNode(weight: () => { return m_BotData.Randomness; }),
                    })
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(m_Controller),
                    new TaskExitZone(m_Controller),
                }),

                // Check if character has imperative to dodge
                new Sequence(new List<Node> {
                    new CheckDodge(m_Controller),
                    new SelectorWeight(new List<Node>
                    {
                        // DODGE : "+0.5f" is weight bias towards Dodging
                        new TaskDodge(m_Controller, checkZones: false, weight: () => { return m_BotData.GetExtraVar(EDefaultTreeVariables.DodgeWeightBias) + 1f - ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),
                        
                        // IGNORE DODGE : attack instead
                        new TaskAttack(m_Controller, weight: () => { return ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),
                    })
                }),

                new SelectorWeight(new List<Node> {
                    // "+Xf" weight bias towards Attacking
                    AttackGroupNode(weight: () => { return m_BotData.GetExtraVar(EDefaultTreeVariables.AttackWeightBias) + ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),

                    // Move
                    new TaskMove(m_Controller, weight: () => { return 1 - ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),
                }),

                // Default action : AutoAttack
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),
            });
        }

        Node LoadAdvancedTree()
        {
            ErrorHandler.Log("Loading ADVANCED BOT Tree for " + m_Controller.Character);

            return new Selector(new List<Node>
            {
                new Sequence(new List<Node> {
                    new CheckRandom(m_Controller),
                    new Selector(new List<Node>
                    {
                        new TaskAttack(m_Controller),
                        new TaskCounter(m_Controller),
                        new TaskJump(m_Controller),
                        new TaskMove(m_Controller),
                        new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),
                    }, random: true),
                }),

                // Check Immadiat Threats (Zones & Projectiles)
                new Sequence(new List<Node> {
                    new CheckImmediatThreat(m_Controller),
                    new Selector(new List<Node>
                    {
                        new TaskCounter(m_Controller),
                        new TaskJump(m_Controller),
                    }),
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(m_Controller),
                    new TaskExitZone(m_Controller),
                }),

                // Check if character has imperative to dodge
                new Sequence(new List<Node> {
                    new CheckDodge(m_Controller),
                    new TaskDodge(m_Controller),
                }),

                new SelectorWeight(new List<Node> {
                    new TaskAttack(m_Controller, weight: () => { return ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),
                    new TaskMove(m_Controller, weight: () => { return 1 - ((CharacterBT)m_Controller.BehaviorTree).OffensiveMeter; }),
                })
            });
        }

        #endregion


        #region Node Groups

        Node AttackGroupNode(Func<float> weight = null)
        {
            return new Selector(new List<Node> { 
                // Use a Special Ability
                new Sequence(new List<Node> {
                    new CheckTimer(m_Controller, "TaskAttack", () => { return UnityEngine.Random.Range(0f, 3f); }),
                    new TaskAttack(m_Controller),
                    new ResetTimer(m_Controller, "TaskAttack", () => { return UnityEngine.Random.Range(0.5f, 3f); })
                }),

                // AutoAttack
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),


            }, weight: weight);
        }

        #endregion
    }
}