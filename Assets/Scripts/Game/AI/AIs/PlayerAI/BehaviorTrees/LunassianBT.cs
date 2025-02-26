using System.Collections.Generic;
using AI;
using AI.Checkers;
using Enums;
using Game.AI.Tasks.Variables;
using UnityEngine;

namespace Game.AI.BehaviorTrees
{
    enum ELunassianState
    {
        None,

        // -- P1
        Moving,
        Attacking,
        SpecialAttack,

        // -- P2

    }
    public class LunassianBT: DefaultBT
    {
        public LunassianBT(Controller controller, EArenaDifficulty arenaDifficulty) : base(controller, arenaDifficulty) { }

        public override Node LoadTree()
        {
            switch (m_ArenaDifficulty)
            {
                case EArenaDifficulty.Easy:
                    return GetTree_Easy(m_Controller);

                case EArenaDifficulty.Normal:
                    return GetTree_Normal(m_Controller);

                default:
                    return GetTree_Hard(m_Controller);
            }
        }

        #region Trees

        static Node GetTree_Easy(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // ULTI : as soon as available
                new TaskUseSpell(controller, controller.SpellHandler.Ultimate),

                // use Special Ability in 10f seconds
                new TaskUseSpell(controller, controller.SpellHandler.SpecialAbility, delay: 10f),

                // Auto Attack
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 3f, nTimes: 10),

                // MOVE
                new TaskMove(controller, checkZones: false, checkProjectiles: false),         
                
                // Default
                new TaskWait(controller),
            });
        }

        static Node GetTree_Normal(Controller controller)
        {
            return new Selector(new List<Node>
            {
                new TaskUseSpell(controller, ESpell.FerociousBite, delay: 10),

                // Attack 5 times every 3 seconds
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 3f, nTimes: 5),

                // MOVE
                new TaskMove(controller, checkZones: false, checkProjectiles: false),         
                
                // Default
                new TaskWait(controller),
            });
        }

        static Node GetTree_Hard(Controller controller)
        {
            return new Selector(new List<Node>
            {
                // check ULTIMATE
                new TaskUseSpell(controller, controller.SpellHandler.Ultimate),

                // use Special Ability in 5f seconds
                new TaskUseSpell(controller, controller.SpellHandler.SpecialAbility, delay: 5f),

                // Check one of Extra Spells
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "TaskAttack", 10f),
                    new TaskAttack(controller),
                    new ResetTimer(controller, "TaskAttack", 5f)
                }),

                // Check if character is currently in a ZoneSpell
                new Sequence(new List<Node> {
                    new CheckInZone(controller),
                    new TaskExitZone(controller),
                }),

                // Attack 4 times 
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 3f, nTimes: 4),

                // MOVE
                new TaskMove(controller, checkZones: true, checkProjectiles: false),         

                // Default - if cant move (should not be used most of the time)
                new TaskWait(controller),
            });
        }

        #endregion
    }
}

