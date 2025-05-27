using Enums;
using System.Collections.Generic;
using UnityEngine;

namespace Game.Spells
{
    [CreateAssetMenu(fileName = "Overdose", menuName = "Game/StateEffects/SpecialEffects/Overdose")]
    public class Overdose : StateEffect
    {
        protected override void ApplyPreProcessing()
        {
            base.ApplyPreProcessing();

            int consumedStacks = 0;                 // currently consumed stacks
            int maxStacks = m_MaxStacks - Stacks;   // maximum number of stacks to add

            // Create modifiable pool of enemy controllers
            var potentialTargets = new List<Controller>(GameManager.Instance.GetAllEnemies(m_Controller.Team, spawnIncluded: true));

            while (consumedStacks < maxStacks && potentialTargets.Count > 0)
            {
                // Select a random enemy
                int index = Random.Range(0, potentialTargets.Count);
                var target = potentialTargets[index];
                var stateHandler = target.StateHandler;

                // Check if the enemy has at least one of the effects
                bool hasPoison = stateHandler.HasState(EStateEffect.Poison);
                bool hasInfected = stateHandler.HasState(EStateEffect.Infected);

                if (!hasPoison && !hasInfected)
                {
                    potentialTargets.RemoveAt(index);
                    continue;
                }

                // Determine how many stacks to consume this round (1 to 3, capped by remaining maxStacks)
                int toConsume = Mathf.Min(Random.Range(1, 4), maxStacks - consumedStacks);

                // If room left and target has infected
                if (toConsume > 0 && hasInfected)
                {
                    int infectedRemoved = stateHandler.RemoveStateEffect(EStateEffect.Infected.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += 3 * infectedRemoved;
                    toConsume -= infectedRemoved;
                }

                // Try to consume from Poison first
                if (hasPoison)
                {
                    int poisonRemoved = stateHandler.RemoveStateEffect(EStateEffect.Poison.ToString(), consume: true, maxStacks: toConsume);
                    consumedStacks += poisonRemoved;
                }
            }

            Refresh(consumedStacks, m_Level);
        }
    }
}