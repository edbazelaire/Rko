using Data;
using Tools;
using UnityEngine;

namespace Game.Spells
{
    public class InstantSpell : Spell
    {
        #region Members

        SpellData m_SpellData => m_BaseSpellData as SpellData;

        #endregion


        #region Inherited Manipulators

        /// <summary>
        /// 
        /// </summary>
        /// <param name="target"></param>
        /// <param name="spellName"></param>
        public override void Initialize(ulong clientId, Vector3 target, SpellData spellData)
        {
            base.Initialize(clientId, target, spellData);

            if (!IsServer)
                return;

            var controller = GetTargetController();
            if (controller != null)
                OnHit(controller);
            else
                ErrorHandler.Error("Instant spell should Target a Controller : " + m_SpellData.Name);

            End();
        }

        #endregion
    }
}