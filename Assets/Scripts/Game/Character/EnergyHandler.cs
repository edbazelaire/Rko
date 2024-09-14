using Tools;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;

namespace Game.Character
{
    public class EnergyHandler : NetworkBehaviour
    {
        #region Members

        // ===================================================================================
        // NETWORK VARIABLES
        NetworkVariable<int> m_MaxEnergy    = new(0);
        NetworkVariable<int> m_Energy       = new(0);

        // ===================================================================================
        // PUBLIC ACCESSORS 
        /// <summary> Current health points </summary>
        public NetworkVariable<int> Energy => m_Energy;

        /// <summary> Initial hp of the player </summary>
        public NetworkVariable<int> MaxEnergy => m_MaxEnergy;

        #endregion


        #region Initialization

        public void Initialize(int energy, int maxEnergy)
        {
            if (!IsServer)
                return;

            m_MaxEnergy.Value = maxEnergy;
            m_Energy.Value = energy;
        }

        #endregion


        #region Public Manipulators

        /// <summary>
        /// Apply damage to the character
        /// </summary>
        /// <param name="damage"> amount of damages </param>
        public void AddEnergy(int energy)
        {
            // only server can apply energy changes
            if (!IsServer)
                return;

            // apply energy (min maxed between 0 and max energy)    
            m_Energy.Value = Mathf.Clamp(m_Energy.Value + energy, 0, m_MaxEnergy.Value);
        }

        /// <summary>
        /// Apply damage to the character
        /// </summary>
        /// <param name="damage"> amount of damages </param>
        public void SpendEnergy(int energy)
        {
            // only server can apply energy changes
            if (!IsServer)
                return;

            if (m_Energy.Value < energy)
            {
                ErrorHandler.Error("energy (" + m_Energy.Value + ") is inf to Energy cost of the spell " + energy);
            }

            // apply energy (min maxed between 0 and max energy)    
            m_Energy.Value = Mathf.Clamp(m_Energy.Value - energy, 0, m_MaxEnergy.Value);
        }

        #endregion
    }
}