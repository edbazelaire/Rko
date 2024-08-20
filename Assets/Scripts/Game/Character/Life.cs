using Enums;
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
    NetworkVariable<int>            m_MaxHp     = new (1);
    NetworkVariable<int>            m_Hp        = new (0);
    NetworkVariable<int>            m_Shield    = new (0);

    // ===================================================================================
    // PRIVATE VARIABLES
    /// <summary> Controller of the Owner</summary>
    Controller                      m_Controller;

    // ===================================================================================
    // PUBLIC ACCESSORS 
    public NetworkVariable<int> MaxHp   => m_MaxHp;  
    public NetworkVariable<int> Hp      => m_Hp;
    public NetworkVariable<int> Shield  => m_Shield;

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
        m_Shield.Value = shield;
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

        // calculate damages after shield
        damage = HitShield(damage);

        // apply damages (after shield)
        m_Hp.Value -= damage;

        if (m_Hp.Value <= 0)
        {
            if (! m_Controller.TriggerEffectHandler.OnDeathEffect())
            {
                DiedEvent?.Invoke();
            }
        }

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

    public int HitShield(int damages)
    {
        damages = m_Controller.StateHandler.HitShield(damages);
        if (damages == 0)
            return 0;

        if (m_Shield.Value <= 0)
            return damages;

        m_Shield.Value -= damages;
        if (m_Shield.Value >= 0)
            return 0;

        damages = -m_Shield.Value;
        m_Shield.Value = 0;

        return damages;
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
