using Data.GameManagement;
using Enums;
using Game.AI;
using Game.Spells;
using System.Collections.Generic;
using UnityEngine;

public class ImmediatThreatTrigger : MonoBehaviour
{
    #region Members

    [SerializeField] private Controller         m_Controller;
    [SerializeField] private SpriteRenderer     m_SpriteRenderer;
    [SerializeField] private CapsuleCollider2D  m_Collider;

    private List<Spell> m_Spells = new List<Spell>();

    public bool IsTriggered { get; protected set; }
    public CapsuleCollider2D Collider => m_Collider;

    #endregion


    #region Update

    private void Update()
    {
        // no need to check if sprite renderer not active
        if (! m_SpriteRenderer.gameObject.activeSelf) 
            return;

        IsTriggered = false;
        
        if (CheckTriggerSpellSpawn(0.2f))
            IsTriggered = true;

        if (CheckTriggerProjectile(false))
            IsTriggered = true;

        UpdateColor();
    }

    #endregion


    #region Check collision

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // check if is spell
        var spell = collision.GetComponent<Spell>();
        if (spell == null)
            return;

        // check is enemy
        if (spell.Caster.Team == m_Controller.Team)
            return;

        m_Spells.Add(spell);
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        // check if is spell
        var spell = collision.GetComponent<Spell>();
        if (spell == null)
            return;

        // check is enemy
        if (spell.Caster.Team == m_Controller.Team)
            return;

        m_Spells.Remove(spell);
    }

    private void UpdateColor()
    {
        var color = IsTriggered ? Color.green : Color.white;
        color.a = 0.6f;
        m_SpriteRenderer.color = color;
    }

    #endregion


    #region Check Trigger

    public bool CheckTriggerProjectile(bool ignoreAutoAttacks = false)
    {
        foreach (Spell spell in m_Spells)
        {
            // can happen if spell is deactivated by the PoolManager
            if (spell.SpellData == null)
                continue;

            // check if spell is a projectile type
            if (spell.SpellData.SpellType != ESpellType.Projectile && spell.SpellData.SpellType != ESpellType.MultiProjectiles)
                continue;

            // check if is in the path of the projectile
            if (! ProjectileTrigger.IsCharacterInProjectilePath((Projectile)spell, m_Controller))
                continue;

            // check if the spell is an AutoAttack
            if (ignoreAutoAttacks && spell.IsAutoAttack)
                continue;

            // if is past the xPos of the controller, skip
            if (spell.transform.position.x > m_Controller.transform.position.x)
                continue;

            // check if is closer to the ground than to the character (in that case no trigger)
            if (spell.transform.position.y - Settings.SpellSizeFactor * spell.SpellData.Size / 2 < m_Controller.transform.position.x - m_Collider.size.x / 2 - spell.transform.position.x)
                continue;

            return true;
        }

        return false;
    }

    public bool CheckTriggerZone()
    {
        foreach (Spell spell in m_Spells)
        {
            // check if spell is a projectile type
            if (spell.SpellData.SpellType != ESpellType.Zone)
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Check if there is a spell spawn in Immediat Threats.
    /// </summary>
    /// <param name="atTimer"></param>
    /// <returns></returns>
    public bool CheckTriggerSpellSpawn(float atTimer = -1)
    {
        // Check for collisions within a circle with variableRadius radius
        Collider2D[] colliders = CollisionChecker.GetColliderCollisions(m_Collider, new List<ELayer> { ELayer.SpellSpawn } );

        // Iterate through all colliders found
        foreach (Collider2D collider in colliders)
        {
            var onCastAoe = collider.GetComponent<PreviewGrowing>();
            if (onCastAoe == null)
                continue;

            if (atTimer <= 0)
                return true;

            if (onCastAoe.Timer <= atTimer)
                return true;
        }

        return false;
    }

    #endregion
}
