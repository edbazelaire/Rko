using AI;
using Enums;
using System.Collections;
using System.Collections.Generic;
using Tools;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    public class DefaultSpawnBT : DefaultBT
    {
        public DefaultSpawnBT(Controller controller) : base(controller, EArenaDifficulty.Normal)
        {
        }

        public override Node LoadTree()
        {
            switch (m_ArenaDifficulty)
            {
                default:
                    ErrorHandler.Log("Loading DEFAULT SPAWN TREE for " + m_Controller.Character);
                    return GetDefaultTree();
            }
        }

        public override Node GetDefaultTree()
        {
            return new Selector(new List<Node>
            {
                new TaskAttack(m_Controller),
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, delay: 2),
                new TaskMove(m_Controller, false, false),
                new TaskWait(m_Controller),
            });
        }
    }
}