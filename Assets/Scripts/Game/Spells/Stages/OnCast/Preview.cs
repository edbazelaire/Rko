using Data.GameManagement;
using Enums;
using Game.SpellGFXs;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class Preview : SpellGFX
    {
        #region Members

        [SerializeField] protected bool m_UpdatePosition;

        #endregion


        #region Init & End

        protected override void ApplyPostProcessing()
        {
            base.ApplyPostProcessing();
        }

        #endregion


        #region Update Manipulators

        protected virtual void Update()
        {
            UpdatePosition();
        }

        protected virtual void UpdatePosition() 
        {
            if (!m_UpdatePosition)
                return;

            var direction = ArenaManager.GetAreaMovementDirection(m_Controller.Team, true);
            
            switch (m_SpellData.SpellTarget)
            {
                case ESpellTarget.Mirror:
                    transform.position = new Vector3(-m_Controller.transform.position.x, 0f, 0f);
                    break;

                case ESpellTarget.Fixed:
                    transform.position = new Vector3(m_Controller.transform.position.x + direction * Settings.SpellFixedDistance, 0f, 0f);
                    break;

                case ESpellTarget.FirstEnemy:
                    transform.position = new Vector3(GameManager.Instance.GetFirstEnemy(m_Controller.Team).transform.position.x, 0f, 0f);
                    break;

                case ESpellTarget.FirstAlly:
                    transform.position = new Vector3(m_Controller.transform.position.x, 0f, 0f);
                    break;

                default:
                    ErrorHandler.Error("Unhandled case : " + m_SpellData.SpellTarget);
                    return;
            }

            // add offset
            transform.position += new Vector3(direction * m_PrefabSpawn.Offset.x, m_PrefabSpawn.Offset.y, 0);
        }

        #endregion
    }
}