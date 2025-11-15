using Assets.Scripts.Game;
using Data.GameManagement;
using Enums;
using Game.Spells;
using System;
using Tools;
using UnityEngine;


namespace Data.DataStructures.SpellSubStructures
{
    [Serializable]
    public class SSpawnPosition
    {
        #region Members

        public ESpawnTarget     SpawnTarget;
        public ESpawnLocation   SpawnLocation;
        public EBodyPart        BodyPart;
        public bool             IsFollowing;
        public Vector2          Offset;

        #endregion


        #region Instantiation

        public GameObject InstantiatePrefab(GameObject prefab, Controller caster, Spell spell, Controller targetController, Vector3 callFromPosition, Vector3 targetPos)
        {
            var parent      = CalculateParent(caster, spell, targetController);
            var position    = CalculatePosition(parent, caster, callFromPosition, targetPos);

            // SAFETY : do not display GFX spawning in void
            if (position == Vector3.zero && SpawnTarget != ESpawnTarget.MapCenter)
            {
                ErrorHandler.Warning($"Spell GFX spawned in void - spell : {(spell != null ? spell.name : "null")} | position : {position} | callFromPosition : {callFromPosition} | targetPos : {targetPos} ");
                return null;
            }

            return PoolManager.Pool(prefab, position, Quaternion.identity, IsFollowing ? parent : null);
        }

        #endregion


        #region Position

        /// <summary>
        /// Based on configuration, find out the spawn parent
        /// </summary>
        /// <param name="prefabSpawn"></param>
        /// <param name="caster"></param>
        /// <param name="spell"></param>
        /// <param name="targetController"></param>
        /// <returns></returns>
        public Transform CalculateParent(Controller caster, Spell spell, Controller targetController)
        {
            switch (SpawnTarget)
            {
                case ESpawnTarget.None:
                case ESpawnTarget.MapCenter:
                case ESpawnTarget.TargetPos:
                    return null;

                case ESpawnTarget.Caster:
                    if (BodyPart != EBodyPart.None)
                    {
                        if (caster.GFXHandler.TryGetBodyPart(BodyPart, out GameObject bodyPartGO, trackError: true))
                            return bodyPartGO.transform;
                    }
                    return caster.transform;

                case ESpawnTarget.Target:
                    if (targetController == null)
                    {
                        ErrorHandler.Error("Trying to spawn on TargetHit but targetController is null");
                        return null;
                    }

                    // check specific body part
                    if (BodyPart != EBodyPart.None)
                    {
                        if (targetController.GFXHandler.TryGetBodyPart(BodyPart, out GameObject bodyPartGO, trackError: true))
                            return bodyPartGO.transform;
                    }
                    return targetController.transform;

                case ESpawnTarget.OnSpell:
                    if (spell == null)
                    {
                        ErrorHandler.Error("Trying to spawn on OnSpell but spell is null");
                        return null;
                    }
                    return spell.GraphicsContainer ? spell.GraphicsContainer.transform : spell.transform;


                default:
                    ErrorHandler.Warning("SPrefabSpawn::Spawn() - Unhandled spawn Target " + SpawnTarget);
                    return null;
            }
        }

        /// <summary>
        /// Based on the configuration, find out the position relative to the parent
        /// </summary>
        /// <param name="parent"></param>
        /// <param name="prefabSpawn"></param>
        /// <param name="controller"></param>
        /// <param name="callFromPosition"></param>
        /// <returns></returns>
        public Vector3 CalculatePosition(Transform parent, Controller controller, Vector3 callFromPosition, Vector3 targetPosition)
        {
            Vector3 basePos = callFromPosition;
            if (parent != null)
                basePos = parent.transform.position;

            else if (SpawnTarget == ESpawnTarget.TargetPos)
                basePos = targetPosition;

            else if (SpawnTarget == ESpawnTarget.MapCenter)
                basePos = Vector3.zero;

            switch (SpawnLocation)
            {
                case ESpawnLocation.None:
                case ESpawnLocation.Center:
                    break;

                case ESpawnLocation.Ground:
                    basePos.y = 0;
                    break;

                case ESpawnLocation.Hight:
                    basePos.y = Settings.SPELL_DIAGONAL_POS_Y;
                    break;

                case ESpawnLocation.Sky:
                    basePos.y = Settings.SPELL_HIGHT_POS_Y;
                    break;

                default:
                    ErrorHandler.Warning("SPrefabSpawn::Spawn() - Unknown spawn location " + SpawnLocation);
                    break;
            }

            int direction = 1;
            if (controller != null && controller.Team == 1)
                direction = -1;

            return basePos + new Vector3(direction * Offset.x, Offset.y, 0);
        }

        #endregion


        #region Override

        public void Override(ESpellProperty spellProperty, string value)
        {
            switch (spellProperty)
            {
                case ESpellProperty.MSpawnPosition_SpawnTarget:
                    if (!Enum.TryParse(value, out ESpawnTarget spawnTarget))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as ESpawnTarget");
                        return;
                    }
                    SpawnTarget = spawnTarget;
                    return;

                case ESpellProperty.MSpawnPosition_SpawnLocation:
                    if (!Enum.TryParse(value, out ESpawnLocation spawnLocation))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as ESpawnLocation");
                        return;
                    }
                    SpawnLocation = spawnLocation;
                    return;

                case ESpellProperty.MSpawnPosition_BodyPart:
                    if (!Enum.TryParse(value, out EBodyPart bodyPart))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as EBodyPart");
                        return;
                    }
                    BodyPart = bodyPart;
                    return;

                case ESpellProperty.MSpawnPosition_IsFollowing:
                    if (!bool.TryParse(value, out bool isFollowing))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as bool");
                        return;
                    }
                    IsFollowing = isFollowing;
                    return;

                case ESpellProperty.MSpawnPosition_Offset:
                    if (!TextHandler.TryParseVector2(value, out Vector2 offset))
                    {
                        ErrorHandler.Warning("Trying to override " + spellProperty + " with value " + value + " - but this is not recognized as Vector2");
                        return;
                    }
                    Offset = offset;
                    return;

                default:
                    ErrorHandler.Error("Unhandled case : " + spellProperty);
                    return;
            }
        }

        #endregion
    }
}
