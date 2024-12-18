using System;
using System.Collections;
using AI;
using Enums;
using Game;
using Game.Spells;
using Game.UI;
using UnityEngine;

public class TutorialBT : BehaviorTree
{
    protected override void SetupTree(EArenaDifficulty arenaDifficulty)
    {
        m_Root = new Node();
    }

    protected override void Update()
    {
        return;
    }

    public void Hide()
    {
        transform.position = TutoGameManager.EnemySpawnPosition;
        StopAttacking();
        gameObject.SetActive(false);
    }

    public void Pause(bool pause)
    {
        if (pause)
        {
            Move(0);
            StopAttacking();
            m_Controller.AnimationHandler.CancelCurrentAnimation();
        }
        else
        {
            StartCoroutine(Attack(interval:2f));
        }
    }

    public IEnumerator StartMovement()
    {
        // deactivate behaviors
        m_Controller.AutoAttackHandler.Activate(false);

        // activate Graphism and move player to the center
        gameObject.SetActive(true);
        yield return MoveToCenter();

        // TODO : re-activate wall collider

    }

    public IEnumerator MoveToCenter()
    {
        Move(1);
        while (transform.position.x > ArenaManager.Instance.Spawns[1][0].position.x) 
        {
            yield return null;
        }

        Move(0);
    } 

    public IEnumerator Attack(int n = -1, float interval = 0f)
    {
        Move(0);
        m_Controller.AutoAttackHandler.Activate(true, interval);

        if (n < 0)
            yield break;
        
        int autoAttackCount = 0;
        m_Controller.AutoAttackHandler.AutoAttackEvent += () => autoAttackCount++;

        while (autoAttackCount < n)
        {
            // if get deactivated : stop this coroutine
            if (! m_Controller.AutoAttackHandler.isActiveAndEnabled)
            {
                m_Controller.AutoAttackHandler.AutoAttackEvent -= () => autoAttackCount++;
                yield break;
            }

            yield return null;
        }

        m_Controller.AutoAttackHandler.AutoAttackEvent -= () => autoAttackCount++;
        StopAttacking();
    }

    public void StopAttacking()
    {
        m_Controller.AutoAttackHandler.Activate(false);
    }

    public void Move(int moveX)
    {
        m_Controller.Movement.SetMovement(moveX);
    }

    public void Cast(ESpell spell)
    {
        m_Controller.SpellHandler.TryStartCastSpell(spell);
    }


    #region Listeners

    #endregion
}
