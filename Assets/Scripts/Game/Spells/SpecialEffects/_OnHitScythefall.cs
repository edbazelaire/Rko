using Data;
using Enums;
using System;
using Tools;
using UnityEngine;

namespace Game.Spells.SpecialEffects
{
    public class _OnHitScythefall : SpecialEffect
    {
        #region Members

        float m_Radius => m_Spell.SpellData.Size / 2;

        Zone m_Spell;

        #endregion


        #region Init & End

        protected override void FindComponents()
        {
            base.FindComponents();

            m_Spell = Finder.FindComponent<Zone>(gameObject);
        }

        public override void Initialize(int level)
        {
            base.Initialize(level);

            CheckVoidMines();
        }
        #endregion


        #region Check Mines

        void CheckVoidMines()
        {
            int nStacks = 0;
            // Check for collisions within a circle with variableRadius radius
            Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, m_Radius);
            foreach (Collider2D collider in colliders)
            {
                if (! CheckCollision(collider, out Mine spell))
                    continue;

                if (!spell.TryEnd())
                    continue;

                nStacks++;
            }

            if (nStacks == 0)
                return;

            m_Spell.Controller.StateHandler.AddStateEffect(new SStateEffectData(EStateEffect.DarkRetribution, nStacks), m_Spell.Controller, m_Level, ESpell.Scythefall.ToString());
        }

        bool CheckCollision(Collider2D collider, out Mine spell) 
        {
            spell = null;

            if (collider.gameObject.layer != LayerMask.NameToLayer("Spell"))
                return false;

            // check that players has controller 
            spell = Finder.FindComponent<Mine>(collider.gameObject, throwError: false);
            if (spell == null)
                return false;

            return spell.SpellData.Name == "_VoidMine";
        }

        #endregion
    }
}