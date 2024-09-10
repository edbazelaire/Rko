using Enums;
using Game;
using System.ComponentModel;
using Tools;
using UnityEngine;

namespace Data.Spells
{
    [CreateAssetMenu(fileName = "InstantSpell", menuName = "Game/Spells/InstantSpellData")]
    public class InstantSpellData : SpellData
    {
        [Header("InstantSpellData")]
        [Description("Is the spell graphics attached to the target")]
        public bool IsAttachedToTarget = false;


        #region Targetting

        protected override Transform FindParent(ulong clientId)
        {
            if (! IsAttachedToTarget)
                return null;

            switch (SpellTarget)
            {
                case ESpellTarget.Self:
                    return GameManager.Instance.GetPlayer(clientId).transform;

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(GameManager.Instance.GetPlayer(clientId).Team, clientId).transform;

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(GameManager.Instance.GetPlayer(clientId).Team).transform;

                default:
                    ErrorHandler.Error("Unhandled case : " + SpellTarget);
                    return null;
            }
        }

        #endregion
    }
}