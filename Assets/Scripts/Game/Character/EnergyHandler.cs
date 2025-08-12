using Tools;
using Unity.Netcode;
using Unity.VisualScripting;
using Unity.VisualScripting.FullSerializer;
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
        // Local Variables
        Controller m_Controller;
        int m_PassiveEnergy = 0;
        float m_PassiveEnergyTick = 1f;
        float m_PassiveEnergyTimer = 0f;

        // ===================================================================================
        // PUBLIC ACCESSORS 
        /// <summary> Current health points </summary>
        public NetworkVariable<int> Energy => m_Energy;

        /// <summary> Initial hp of the player </summary>
        public NetworkVariable<int> MaxEnergy => m_MaxEnergy;

        #endregion


        #region Initialization

        public void Initialize(int energy, int maxEnergy, int passiveEnergy)
        {
            if (!IsServer)
                return;

            m_MaxEnergy.Value = maxEnergy;
            m_Energy.Value = energy;
            m_PassiveEnergy = passiveEnergy;

            m_Controller = Finder.FindComponent<Controller>(gameObject);
        }

        #endregion


        #region Update

        private void Update()
        {
            if (!IsServer)
                return;

            m_PassiveEnergyTimer += Time.deltaTime;
            if (m_PassiveEnergyTimer > m_PassiveEnergyTick)
            {
                m_PassiveEnergyTimer = 0f;
                AddEnergy(m_PassiveEnergy);
            }
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

            if (energy == 0)
                return;

            if (!m_Controller.StateHandler.CanGainEnergy)
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