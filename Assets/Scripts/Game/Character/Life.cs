using System;
using Unity.Netcode;
using UnityEngine;

public class Life : NetworkBehaviour
{
    #region Members

    // DEBUG
    float debugTimer;

    // ===================================================================================
    // EVENTS
    /// <summary> thrown when the character dies </summary>
    public Action                   DiedEvent;

    // ===================================================================================
    // NETWORK VARIABLES
    NetworkVariable<int>            m_Hp = new (0);

    // ===================================================================================
    // PRIVATE VARIABLES
    /// <summary> Controller of the Owner</summary>
    Controller                      m_Controller;

    /// <summary> initial health points </summary>
    int                             m_InitialHp = 50;

    // ===================================================================================
    // PUBLIC ACCESSORS 
    /// <summary> Current health points </summary>
    public NetworkVariable<int> Hp { get { return m_Hp; } }  

    /// <summary> Initial hp of the player </summary>
    public int InitialHp { get { return m_InitialHp; } }

    /// <summary> Is the character alive </summary>
    public bool IsAlive { get { return m_Hp.Value > 0; } }

    #endregion


    #region Initialization

    /// <summary>
    /// 
    /// </summary>
    public override void OnNetworkSpawn()
    {
        m_Hp.Value = m_InitialHp;
        m_Controller = GetComponent<Controller>();
    }

    #endregion


    #region Inherited Manipulators

    /// <summary>
    /// 
    /// </summary>
    void Update()
    {
        //DisplayLife(2f);
    }

    #endregion


    #region Public Manipulators

    /// <summary>
    /// Apply damage to the character
    /// </summary>
    /// <param name="damage"> amount of damages </param>
    public void Hit(int damage)
    {
        // only server can apply damages
        if (!IsServer)
            return;

        // check provided value
        if (damage < 0)
        {
            Debug.LogError($"Damages ({damage}) < 0");
            return;
        }

        // apply damages
        m_Hp.Value -= damage;
    }

    /// <summary>
    /// Apply healing to the character
    /// </summary>
    /// <param name="heal"></param>
    public void Heal(int heal)
    {
        // only server can apply heals
        if (!IsServer)
            return;

        // check provided value
        if (heal < 0)
        {
            Debug.LogError($"Healing ({heal}) < 0");
            return;
        }

        // apply heals
        if (m_Hp.Value + heal > m_InitialHp)
            m_Hp.Value = m_InitialHp;
        else
            m_Hp.Value += heal;
    }

    #endregion


    #region Debug

    void DisplayLife(float timer = 2f)
    {
        if (debugTimer > 0f)
        {
            debugTimer -= Time.deltaTime;
            return;
        }

        print("Client: " + OwnerClientId);
        print("     + Life: " + m_Hp.Value);

        debugTimer = timer;
    }

    #endregion
}
