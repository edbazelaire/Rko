using Enums;
using Game;
using UnityEditor;
using UnityEngine;

namespace Tools.Helpers
{
    public static class TargetHelper
    {
        public static Controller GetTargetController(ulong casterId, ESpellTarget spellTarget, ulong? targetId = null, bool throwError = true)
        {
            if (! GameManager.Exists)
                return null;

            Controller controller = GameManager.Instance.GetPlayer(casterId);

            switch (spellTarget)
            {
                case ESpellTarget.Self:
                    return controller;

                case ESpellTarget.FirstAlly:
                    return GameManager.Instance.GetFirstAlly(controller.Team, casterId);

                case ESpellTarget.FirstEnemy:
                    return GameManager.Instance.GetFirstEnemy(controller.Team);

                case ESpellTarget.CurrentTarget:
                    if (targetId == null)
                    {
                        ErrorHandler.Error("SpellTarget is CurrentTarget but no target id was provided");
                        return null;
                    }
                    return GameManager.Instance.GetPlayer(targetId.Value);

                default:
                    if (throwError)
                        ErrorHandler.Warning("Unhandled case " + spellTarget);
                    return null;
            }
        }
    }
}