using System;
using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using Tools;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    enum EFenrisState
    {
        None,

        // -- P1

        // -- P2
    }

    public class FenrisBT : DefaultBT
    {
        public FenrisBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }

        public override Node GetDefaultTree()
        {
            Debug.Log("GetDefaultTree() : Fenris");

            return new Selector(new List<Node>
            {
                // NONE STATE               ===================================================
                new Sequence(new List<Node> {
                    new CheckPhase(m_Controller, 0),
                    new SetPhase(m_Controller, 1),
                }),

                // =========================================================================================
                // PHASE 1     
                new Sequence(new List<Node> { 
                    new CheckPhase(m_Controller, 1),

                    new Selector(new List<Node>
                    {
                        new TaskUseSpell(m_Controller, m_Controller.SpellHandler.Ultimate),
                        new TaskUseSpell(m_Controller, ESpell.PackHunt),
                        new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack),
                        new TaskMove(m_Controller, checkZones: false, checkProjectiles: false),
                    }),
                }),

                new TaskWait(m_Controller),
            });
        }

        public override void OnStateChanged(string stringState)
        {
            base.OnStateChanged(stringState);

            if (! Enum.TryParse(stringState, out EFenrisState state))
            {
                ErrorHandler.Error("Unable to parse " + stringState + " into a EFenrisState state");
                return;
            }

            switch (state)
            {
                case EFenrisState.None:
                    return;
                    
                default:
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

