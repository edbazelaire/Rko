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

                // use Special Ability in 10 seconds
                new TaskUseSpell(controller, controller.SpellHandler.SpecialAbility, delay: 10f),

                // Auto Attack
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 1.5f, nTimes: 6),

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
                // ULTI : as soon as available
                new TaskUseSpell(controller, controller.SpellHandler.Ultimate),
                
                // use Special Ability in 5 seconds
                new TaskUseSpell(controller, controller.SpellHandler.SpecialAbility, delay: 5f),

                // Attack 5 times every 3 seconds
                new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, delay: 1f, nTimes: 5),

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

                // use Consummable Attacks in priority as soon as available
                new TaskAttack(controller, allowedSpellCategories: new() { ESpellTypeCategory.ConsumeStateEffect }),

                // Check one of Extra Spells
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "TaskAttack", 1f),
                    new TaskAttack(controller),
                    new ResetTimer(controller, "TaskAttack", Random.Range(0.5f, 7f))
                }),

                //// Check if character is currently in a ZoneSpell
                //new Sequence(new List<Node> {
                //    new CheckInZone(controller),
                //    new TaskExitZone(controller),
                //}),

                // Attack 4 times 
                new Sequence(new List<Node> {
                    new CheckTimer(controller, "TaskAutoAttack", 0.5f),
                    new TaskUseSpell(controller, controller.SpellHandler.AutoAttack, nTimes: 4, spellEvent: ESpellEvent.OnCast),
                    new ResetTimer(controller, "TaskAutoAttack", 2.5f)
                }),

                // MOVE
                new TaskMove(controller, checkZones: false, checkProjectiles: false),         

                // Default - if cant move (should not be used most of the time)
                new TaskWait(controller),
            });
        }

        #endregion
    }
}

