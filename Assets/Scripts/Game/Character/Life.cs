using Enums;
using System;
using Unity.Collections.LowLevel.Unsafe;
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
    public Action<int, int>         FinalShieldChangedEvent;
    public Action                   DiedEvent;

    // ===================================================================================
    // NETWORK VARIABLES
    NetworkVariable<int>            m_MaxHp     = new (1);
    NetworkVariable<int>            m_Hp        = new (0);

    // ===================================================================================
    // PRIVATE VARIABLES
    /// <summary> Controller of the Owner</summary>
    Controller                      m_Controller;
    int                             m_Shield =  0;

    // ===================================================================================
    // PUBLIC ACCESSORS 
    public NetworkVariable<int> MaxHp   => m_MaxHp;  
    public NetworkVariable<int> Hp      => m_Hp;
    
    public int FinalShield => m_Shield + m_Controller.StateHandler.RemainingShield + m_Controller.CounterHandler.RemainingShield;
    public int Shield  => m_Shield;

    /// <summary> Is the character alive </summary>
    public bool IsAlive => m_Hp.Value > 0;


    #endregion


    #region Initialization

    /// <summary>
    /// 
    /// </summary>
    public override void OnNetworkSpawn()
    {
        m_Controller = GetComponent<Controller>();
    }

    public void Initialize(int hp, int shield)
    {
        if (!IsServer)
            return;

        m_MaxHp.Value = hp;
        m_Hp.Value = hp;
        m_Shield = shield;
    }

    #endregion


    #region Public Manipulator

    /// <summary>
    /// Apply damage to the character
    /// </summary>
    /// <param name="damage"> amount of damages </param>
    public int Hit(int damage, bool ignoreRes = false)
    {
        // only server can apply damages
        if (! IsServer || ! IsAlive)
            return 0;

        if (m_Controller.StateHandler.IsInvulnerable)
            return 0;

        // calculate damages after resistance
        damage = ignoreRes ? damage : m_Controller.StateHandler.ApplyResistance(damage);

        // check provided value
        if (damage < 0)
        {
            Debug.LogError($"Damages ({damage}) < 0");
            return 0;
        }

        int previousShield = FinalShield;

        // calculate damages after shield
        damage = HitShield(damage);

        if (damage == 0)
            return damage;

        // apply damages (after shield)
        m_Hp.Value -= damage;

        if (m_Hp.Value <= 0)
        {
            if (! m_Controller.TriggerEffectHandler.OnDeathEffect())
            {
                DiedEvent?.Invoke();
            }
        }

        RecheckShield(previousShield);

        return damage;
    }

    /// <summary>
    /// Apply healing to the character
    /// </summary>
    /// <param name="heal"></param>
    public int Heal(int heal)
    {
        // only server can apply heals
        if (!IsServer)
            return 0;

        // check provided value
        if (heal < 0)
        {
            Debug.LogError($"Healing ({heal}) < 0");
            return 0;
        }

        // apply heals
        if (m_Hp.Value + heal > m_MaxHp.Value)
            m_Hp.Value = m_MaxHp.Value;
        else
            m_Hp.Value += heal;

        return heal;
    }

    public int AddShield(int shield)
    {
        if (shield <= 0)
            return 0;

        m_Shield += shield;

        return shield;
    }

    public int HitShield(int damages)
    {
        damages = m_Controller.StateHandler.HitShield(damages);
        if (damages == 0)
            return 0;

        if (m_Shield <= 0)
            return damages;

        m_Shield -= damages;
        if (m_Shield >= 0)
            return 0;

        damages = -m_Shield;
        m_Shield = 0;

        return damages;
    }

    #endregion


    #region Listeners & Events

    public void RecheckShield(int previousShield)
    {
        int currentShield = FinalShield;
        if (currentShield != previousShield)
        {
            FinalShieldChangedEvent?.Invoke(previousShield, currentShield);
            FinalShieldChangedEventClientRPC(previousShield, currentShield);
        }
    }

    [ClientRpc]
    void FinalShieldChangedEventClientRPC(int oldValue, int newValue)
    {
        FinalShieldChangedEvent?.Invoke(oldValue, newValue);
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
