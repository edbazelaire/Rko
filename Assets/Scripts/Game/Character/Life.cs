using Assets.Scripts.Game;
using Enums;
using Game.StateEffects.Interfaces;
using System;
using Tools;
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
    public Action                               DiedEvent;
    public Action<int, ulong>                   OnHealedEvent;
    public Action<int, ulong, ESpellCategory>   OnHittedEvent;

    // ===================================================================================
    // NETWORK VARIABLES
    NetworkVariable<int>            m_MaxHp         = new (1);
    NetworkVariable<int>            m_Hp            = new (0);
    NetworkVariable<int>            m_FinalShield   = new (0);

    // ===================================================================================
    // PRIVATE VARIABLES
    /// <summary> Controller of the Owner</summary>
    Controller                      m_Controller;
    int                             m_Shield =  0;

    // ===================================================================================
    // PUBLIC ACCESSORS 
    public NetworkVariable<int> MaxHp       => m_MaxHp;  
    public NetworkVariable<int> Hp          => m_Hp;
    public NetworkVariable<int> FinalShield => m_FinalShield;

    public Controller Controller    => m_Controller;
    public int Shield               => m_Shield;
    public float PercHp             => Mathf.Clamp((float)m_Hp.Value / m_MaxHp.Value, 0f, 1f);

    /// <summary> Is the character alive </summary>
    public bool IsAlive             => m_Hp.Value > 0;


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

        RecalculateShield();
    }

    #endregion


    #region Public Manipulator

    public bool Kill(bool ignoreDeathEffects = false)
    {
        if (!ignoreDeathEffects && m_Controller.TriggerEffectHandler.OnDeathEffect())
        {
            return false;
        }

        DiedEvent?.Invoke();
        return true;
    }

    /// <summary>
    /// Apply damage to the character
    /// </summary>
    /// <param name="damage"> amount of damages </param>
    public int Hit(int damage, ulong casterId, string source, ESpellCategory spellCategory, bool ignoreRes = false)
    {
        // only server can apply damages
        if (! IsServer || ! IsAlive)
            return 0;

        if (m_Controller != null && m_Controller.StateHandler.IsInvulnerable)
            return 0;

        // check provided value
        if (damage < 0)
        {
            ErrorHandler.Error($"Damage ({damage}) < 0");
            return 0;
        }

        // calculate damages after resistance
        damage = ignoreRes ? damage : m_Controller.StateHandler.ApplyResistance(damage);

        // check provided value
        if (damage <= 0)
            return 0;

        // -- call event that the player received damage
        OnHittedEvent?.Invoke(damage, casterId, spellCategory);
        // -- call analytics & damage display
        GameAnalyticsManager.Instance.OnSpellHit(casterId, m_Controller.PlayerId, source, damage, EHitType.Damage, spellCategory);

        // calculate damages after shield
        var damages = HitShield(damage);
        if (damages == 0)
            return damage;

        // apply damages (after shield)
        m_Hp.Value -= damages;

        if (m_Hp.Value <= 0)
        {
            Kill(false);
        }

        return damage;
    }

    /// <summary>
    /// Apply healing to the character
    /// </summary>
    /// <param name="heal"></param>
    public int Heal(int heal, ulong casterId, string source, ESpellCategory spellCategory)
    {
        // only server can apply heals
        if (!IsServer)
            return 0;

        // check provided value
        if (heal < 0)
        {
            ErrorHandler.Warning("Provided heal with value (" + heal + ")< 0");
            return 0;
        }

        // Apply reductions
        heal = m_Controller.StateHandler.ApplyBonusHealReceived(heal);

        // Interception (before apply)
        foreach (var effect in m_Controller.StateHandler.StateEffects)
        {
            if (effect is IHealInterceptor interceptor)
            {
                interceptor.OnPreHeal(ref heal, casterId);
            }
        }

        if (heal <= 0)
            return 0;

        // check max heal
        if (m_Hp.Value + heal > m_MaxHp.Value)
            heal = m_MaxHp.Value - m_Hp.Value;

        // check heal is not <= 0
        if (heal <= 0)
            return 0;

        m_Hp.Value += heal;
        GameAnalyticsManager.Instance.OnSpellHit(casterId, m_Controller.PlayerId, source, heal, EHitType.Heal, spellCategory);
        OnHealedEvent?.Invoke(heal, casterId);

        return heal;
    }

    public int AddShield(int shield, ulong casterId, string source, ESpellCategory spellCategory)
    {
        if (shield <= 0)
            return 0;

        m_Shield += shield;
        GameAnalyticsManager.Instance.OnSpellHit(casterId, m_Controller.PlayerId, source, shield, EHitType.Shield, spellCategory);

        RecalculateShield();

        return shield;
    }

    public int HitShield(int damages)
    {
        if (m_Controller == null)
            return damages;

        damages = m_Controller.StateHandler.HitShield(damages);
        if (damages == 0)
            return 0;

        if (m_Shield <= 0)
            return damages;

        m_Shield -= damages;

        if (m_Shield >= 0)
        {
            RecalculateShield();
            return 0;
        }

        damages = -m_Shield;
        m_Shield = 0;
        m_FinalShield.Value = 0;

        RecalculateShield();
        return damages;
    }

    #endregion


    #region Listeners & Events
    
    public void RecalculateShield()
    {
        if (m_Controller == null)
            return;

        m_FinalShield.Value = m_Shield + m_Controller.StateHandler.RemainingShield + m_Controller.CounterHandler.RemainingShield;
    }

    #endregion
}
