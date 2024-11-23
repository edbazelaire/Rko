using AI;
using Enums;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public class DefaultBT
    {
        protected Controller m_Controller;
        protected EArenaDifficulty m_ArenaDifficulty;

        public DefaultBT(Controller controller, EArenaDifficulty arenaDifficulty) 
        { 
            m_Controller = controller;
            m_ArenaDifficulty = arenaDifficulty;
        }

        public virtual Node LoadTree()
        {
            switch (m_ArenaDifficulty)
            {
                default:
                    ErrorHandler.Log("Loading DEFAULT TREE for " + m_Controller.Character);
                    return GetDefaultTree();
            }
        }

        public virtual void OnStateChanged(string state)
        {
            m_Controller.BehaviorTree.ResetCounters();
            m_Controller.BehaviorTree.ResetTimers();
        }

        public virtual Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
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

                new TaskAttack(m_Controller),

                new TaskWait(m_Controller)
            });
        }
    }
}