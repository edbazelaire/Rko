using Enums;
using Game.Spells;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Tools;


namespace Game.AI
{
    public enum ELayer
    {
        Default,

        Player,
        Wall,
        InvisibleWall,
        Spell,
        SpellSpawn,
        Structure,
    }

    public static class CollisionChecker
    {
        #region Members

        public static List<ELayer> OBSTACLES_LAYERS => new() { ELayer.Wall, ELayer.InvisibleWall, ELayer.Structure };

        #endregion


        #region Controller Capsule Collider

        public static float GetCharacterWidth(Controller controller)
        {
            return GetColliderWidth(controller.gameObject.GetComponent<CapsuleCollider2D>());
        }

        public static float GetColliderWidth(CapsuleCollider2D capsuleCollider)
        {
            return capsuleCollider.size.x;
        }

        public static Collider2D[] GetControllerCollisions(Controller controller, ELayer layer)
        {
            return GetColliderCollisions(controller.gameObject.GetComponent<CapsuleCollider2D>(), new List<ELayer>() { layer });
        }

        /// <summary>
        /// Get all collisions with the Controller's CapsuleCollider
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="layers"></param>
        /// <returns></returns>
        public static Collider2D[] GetColliderCollisions(CapsuleCollider2D capsuleCollider2D, List<ELayer> layers)
        {
            int layerMask = (layers != null && layers.Count > 0)
                ? LayerMask.GetMask(layers.Select(l => l.ToString()).ToArray())
                : Physics2D.AllLayers;

            var filter = Physics2DQueries.BuildFilter(layerMask);

            int count = Physics2DQueries.OverlapCapsule(
                capsuleCollider2D.transform.position,
                capsuleCollider2D.size,
                capsuleCollider2D.direction,
                capsuleCollider2D.transform.rotation.eulerAngles.z,
                filter,
                out Collider2D[] buf
            );

            if (count <= 0) return System.Array.Empty<Collider2D>();

            var results = new Collider2D[count];
            System.Array.Copy(buf, results, count);
            return results;
        }

        #endregion


        #region Obstacles

        public static Collider2D[] GetCollidersInDistance(float basePositionX, float distance, ELayer layer)
        {
            return GetCollidersInDistance(basePositionX, distance, new List<ELayer>() { layer });
        }

        public static Collider2D[] GetCollidersInDistance(float basePositionX, float distance, List<ELayer> layers = default)
        {
            return GetCollidersBetween(basePositionX, basePositionX + distance, layers);
        }

        public static Collider2D[] GetCollidersBetween(float basePositionX, float endPositionX, ELayer layer)
        {
            return GetCollidersBetween(basePositionX, endPositionX, new List<ELayer>() { layer });
        }

        public static Collider2D[] GetCollidersBetween(float basePositionX, float endPositionX, List<ELayer> layers = default)
        {
            int layerMask = (layers != null && layers.Count > 0)
                ? LayerMask.GetMask(layers.Select(l => l.ToString()).ToArray())
                : Physics2D.AllLayers;

            float distance = endPositionX - basePositionX;
            var center = new Vector2(basePositionX + distance / 2f, 0f);
            var size = new Vector2(Mathf.Abs(distance), 1f);

            var filter = Physics2DQueries.BuildFilter(layerMask);

            Collider2D[] buf;
            int count = Physics2DQueries.OverlapBox(center, size, 0f, filter, out buf);

            if (count <= 0) return System.Array.Empty<Collider2D>();

            var results = new Collider2D[count];
            System.Array.Copy(buf, results, count);
            return results;
        }


        #endregion


        #region Filter Colliders

        public static List<Spell> FilterSpells(Collider2D[] colliders, ESpellType spellType, int? ofTeam = null)
        {
            List<Spell> spells = new List<Spell>();

            // Iterate through all colliders found
            foreach (Collider2D collider in colliders)
            {
                Spell spell = collider.GetComponent<Spell>();
                if (spell == null)
                    continue;

                // ignore allies spells
                if (ofTeam.HasValue && spell.Caster.Team != ofTeam)
                    continue;

                if (spell.SpellData.SpellType != spellType)
                    continue;

                spells.Add(spell);
            }

            return spells;
        }
        #endregion

    }
}