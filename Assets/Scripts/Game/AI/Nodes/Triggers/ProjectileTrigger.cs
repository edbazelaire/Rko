using Data;
using Data.GameManagement;
using Enums;
using Game;
using Game.Spells;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class ProjectileTrigger : Sensor
{
    #region Members

    protected List<Projectile> m_Projectiles;

    public List<Projectile> Projectiles => m_Projectiles;

    #endregion


    #region Init & End

    public override void Initialize()
    {
        base.Initialize();

        m_Projectiles = new List<Projectile>();
    }

    #endregion


    #region Update

    private void Update()
    {
        IsTriggered = false;                            // reset IsTrigger
        m_Projectiles.Clear();                          // reset list of Pro jectiles
        List<Spell> spellsToRemove = new List<Spell>(); // reset list of spells to remove from list of spells

        foreach (Spell spell in m_Spells)
        {
            if (spell.IsDestroyed())
            {
                spellsToRemove.Add(spell);
                continue;
            }

            if (spell.SpellData.SpellType != ESpellType.Projectile 
                && spell.SpellData.SpellType != ESpellType.MultiProjectiles
                && spell.SpellData.SpellType != ESpellType.Jump)
                continue;

            if (! IsCharacterInProjectilePath((Projectile)spell, m_Controller))
                continue;

            m_Projectiles.Add((Projectile)spell);
            IsTriggered = true;
        }

        foreach (var spell in spellsToRemove)
        {
            m_Spells.Remove(spell);
        }

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

    #endregion


    #region Projectiles Path

    public static bool IsCharacterInProjectilePath(Projectile projectile, Controller controller)
    {
        // projectile not an issue if hasn't reached max hight
        if (projectile.IsDestroyed())
        {
            return false;
        }

        ProjectileData projectileData = projectile.SpellData as ProjectileData;
        var position = projectile.transform.position;       // init position
        float distance = 10f;                               // init distance value

        if (projectileData.Trajectory == ESpellTrajectory.Curve && projectile.transform.rotation.x < 0)
            position = ArenaManager.Instance.TargetHight.position;

        // CHECK : max distance
        if (projectileData.Trajectory == ESpellTrajectory.Straight)
        {
            // if Projectile has a Max Distance => calcul remaining distance
            if (projectileData.Distance > 0)
                distance = projectileData.Distance - (projectile.transform.position.x - projectile.OriginalPosition.x);
            else if (projectileData.StopOnTargetPos)
                distance = projectile.Target.x - projectile.transform.position.x;
        }

        if (distance <= 0f)
            return false;

        RaycastHit2D hit = Physics2D.BoxCast(
            origin: position,
            size: new Vector2(1f, Settings.SpellSizeFactor * projectile.SpellData.Size),
            angle: 0f,
            direction: (projectile.Target - projectile.transform.position).normalized,
            distance: distance,
            layerMask: LayerMask.GetMask("Player")
        );


        return hit.collider != null && hit.collider.GetComponent<Controller>() == controller;
    }

    /// <summary>
    /// Check if Sensor has straight projectiles and provides first safe pos to dodge if any
    /// </summary>
    /// <param name="xPos"></param>
    /// <returns></returns>
    public bool CheckStraightProjectiles(out float xPos)
    {
        xPos = 0f;
        bool hasStraight = false;

        foreach (Projectile projectile in m_Projectiles)
        {
            ProjectileData projectileData = projectile.SpellData as ProjectileData;
            if (projectileData.Trajectory != ESpellTrajectory.Straight)
                continue;

            if (projectileData.Distance > 0)
                xPos = projectile.OriginalPosition.x + projectileData.Distance;
            
            else if (projectileData.StopOnTargetPos)
                xPos = projectile.Target.x;

            hasStraight = true;
        }
        
        return hasStraight;
    }

    public static bool CheckControllerInBetweenPos(Vector2 posStart, Vector2 posEnd, Controller controller, float offset = 0)
    {
        RaycastHit2D hit = Physics2D.Raycast(posStart + new Vector2(0, offset), (posEnd + new Vector2(offset, 0) - posStart).normalized, 10f, LayerMask.GetMask("Player"));
        if (hit.collider != null && hit.collider.GetComponent<Controller>() == controller)
        {
            Debug.DrawLine(posStart, posStart, Color.red, 0.5f);
            return true;
        }

        return false;
    }

    #endregion


    #region Projectiles Distance

    public static (float startX, float endX) CalculateProjectileSafeBounds(Projectile projectile, Controller controller)
    {
        float SAFETY = 0.2f;

        ProjectileData projectileData = projectile.SpellData as ProjectileData;
        var characterHeight = controller.Collider.offset.y + controller.Collider.bounds.extents.y;
        float startX = (-SAFETY) + projectile.Target.x - projectileData.Size / 2 + controller.Collider.offset.x - controller.Collider.bounds.extents.x;
        float endX = (+SAFETY) + projectile.Target.x + projectileData.Size / 2 + controller.Collider.offset.x + controller.Collider.bounds.extents.x;

        switch (projectileData.Trajectory)
        {
            case ESpellTrajectory.Straight:
                startX = 0f;
                endX += controller.Collider.offset.x + controller.Collider.bounds.extents.x;
                break;

            case ESpellTrajectory.Diagonal:
            case ESpellTrajectory.DiagonalMiddle:
            case ESpellTrajectory.Curve:
                // get first X where the height of the character is not in the trajectory of the 
                startX -= characterHeight * projectile.transform.position.x / projectile.transform.position.y;
                break;

            default:
                break;
        }

        return (startX, endX);
    }

    #endregion
}
