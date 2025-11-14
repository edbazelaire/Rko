using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using System;
using System.Collections.Generic;
using Tools;

namespace Game.AI.BehaviorTrees
{
    public class DefaultSpawnBT : DefaultBT
    {
        ESpawn m_Spawn;

        public DefaultSpawnBT(Controller controller, string spawnName) : base(controller, EArenaDifficulty.Normal)
        {
            if (! Enum.TryParse(spawnName, out m_Spawn))
                ErrorHandler.Error("Unable to load " + spawnName + " as spawn");
        }

        public override Node LoadTree()
        {
            switch (m_Spawn)
            {
                case ESpawn.Noctrelle:
                case ESpawn.Toxstinger:
                case ESpawn.DoomCrystal:
                    ErrorHandler.Log("Loading FLYER TREE for " + m_Controller.Character, ELogTag.AITree);
                    return GetDefaultFlyerTree();

                default:
                    ErrorHandler.Log("Loading DEFAULT SPAWN TREE for " + m_Controller.Character, ELogTag.AITree);
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

        Node GetDefaultFlyerTree()
        {
            return new Selector(new List<Node>
            {
                // Start by moving to the enemy area
                new Sequence(new List<Node>()
                {
                    new CheckCount(m_Controller, "MoveToEnemy", 1),
                    new TaskMoveToArea(m_Controller, ESpellTarget.EnemyZone),
                    new IncreaseCounter(m_Controller, "MoveToEnemy")
                }),

                // CHECK : is channeling/casting
                new CheckState(m_Controller, EGlobalState.Casting),

                new TaskAttack(m_Controller),
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, delay: 2),
                new TaskMove(m_Controller, false, false, ignoreInvisibleWalls: true),
                new TaskWait(m_Controller),
            });
        }

        Node GetFullMapFlyingTree()
        {
            return new Selector(new List<Node>
            {
                // CHECK : is channeling/casting
                new CheckState(m_Controller, EGlobalState.Casting),

                new TaskAttack(m_Controller),
                new TaskUseSpell(m_Controller, m_Controller.SpellHandler.AutoAttack, delay: 3),
                new TaskMove(m_Controller, checkZones: false, checkProjectiles: false, ignoreInvisibleWalls: true),
                new TaskWait(m_Controller),
            });
        }
    }
}