using UnityEngine;

namespace Tools
{
    /// <summary>
    /// Wrappers "no-alloc" to avoid overlaps 2D
    /// </summary>
    public static class Physics2DQueries
    {
        static readonly Collider2D[] s_Circle = new Collider2D[64];
        static readonly Collider2D[] s_Box = new Collider2D[64];
        static readonly Collider2D[] s_Capsule = new Collider2D[64];

        public static ContactFilter2D BuildFilter(int? layerMask = null, bool? includeTriggers = null)
        {
            var f = new ContactFilter2D();

            if (layerMask.HasValue)
            {
                f.useLayerMask = true;
                f.SetLayerMask(layerMask.Value);
            }

            // by default, no filters on deepth/angles -> no useless alloc/filtering
            f.useDepth = false;
            f.useNormalAngle = false;

            // Important : triggers
            f.useTriggers = includeTriggers ?? Physics2D.queriesHitTriggers;
            return f;
        }

        public static int OverlapCircle(Vector2 center, float radius, in ContactFilter2D filter, out Collider2D[] results)
        {
            results = s_Circle;
            return Physics2D.OverlapCircle(center, radius, filter, results);
        }

        public static int OverlapBox(Vector2 center, Vector2 size, float angleDeg, in ContactFilter2D filter, out Collider2D[] results)
        {
            results = s_Box;
            return Physics2D.OverlapBox(center, size, angleDeg, filter, results);
        }

        public static int OverlapCapsule(Vector2 point, Vector2 size, CapsuleDirection2D dir, float angleDeg, in ContactFilter2D filter, out Collider2D[] results)
        {
            results = s_Capsule;
            return Physics2D.OverlapCapsule(point, size, dir, angleDeg, filter, results);
        }
    }
}
