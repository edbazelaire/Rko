using Data;
using Data.Spells;
using UnityEngine;

namespace Game.Spells
{
    public class Teleportation : Spell
    {
        #region Members

        TeleportationData m_SpellData => m_BaseSpellData as TeleportationData;
        Vector3 m_OriginalPosition;
        float m_Timer;

        #endregion


        #region Init & End

        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            base.Initialize(clientId, target, spellData);

            m_Timer = m_SpellData.Duration;
            m_OriginalPosition = m_Controller.transform.position;

            // if character is not in his area during cast (can happen for some special animations) get center of his area as original position
            if (!ArenaManager.IsInAreaBounds(m_OriginalPosition.x, m_Controller.Team, false))
                m_OriginalPosition = new Vector3(ArenaManager.GetTargettableAreaTransform(m_Controller.Team, false).transform.position.x, 0f, 0f);

            // hide character
            m_Controller.GFXHandler.HideCharacter(true);

            target.y = 0.1f + ((CapsuleCollider2D)m_Controller.Collider).size.y / 2;
            transform.position = target;

            // make player untargatable, unmovable and unrotatable
            if (!IsServer)
                return;

            m_Controller.StateHandler.SetStateJump(true);
            m_Controller.SpellHandler.ForceBlockCast(true);
            m_Controller.Movement.ForceBlockMovement(true);
        }


        protected override void End()
        {
            if (IsServer)
            {
                // re activate collider
                m_Controller.Collider.enabled = true;

                // set character (not hidden) at the position of the spell
                m_Controller.GFXHandler.HideCharacterClientRPC(false);
                m_Controller.transform.position = transform.position;
            }

            base.End();
        }

        public override void OnNetworkDespawn()
        {
            // check if spell went throught the "End()" method
            if (IsServer && ! m_IsOver)
            {
                m_Controller.Collider.enabled = true;
                m_Controller.GFXHandler.HideCharacterClientRPC(false);
            }

            // reset player position
            m_OriginalPosition.y = 0.1f;
            m_Controller.transform.position = m_OriginalPosition;

            base.OnNetworkDespawn();

            if (!IsServer)
                return;

            // reset jump state
            m_Controller.StateHandler.SetStateJump(false);
            m_Controller.SpellHandler.ForceBlockCast(false);
            m_Controller.Movement.ForceBlockMovement(false);
        }

        #endregion


        #region Update & Collision

        protected override void Update()
        {
            base.Update();

            // only server can check for distance and update the Controller position
            if (!IsServer)
                return;

            m_Timer -= Time.deltaTime;

            // check if the spell has reached its max distance
            if (m_Timer <= 0)
                End();
        }

        #endregion


        #region Position & Targetting

        public virtual void RecalculatePosition(ref Vector3 position, Vector3 target, ulong clientId) 
        {
            position = target;
        }

        #endregion
    }
}