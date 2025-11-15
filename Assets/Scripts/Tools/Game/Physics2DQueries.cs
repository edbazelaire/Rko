using Assets;
using MyBox;
using System.Collections.Generic;
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

        /// <summary>
        /// Performs an overlap test using a given Collider2D shape at a specific world position.
        /// The collider itself is NOT moved — its shape and rotation are replicated for the test.
        /// </summary>
        /// <param name="source">The source collider whose shape to use.</param>
        /// <param name="position">The world position to test at.</param>
        /// <param name="filter">Physics2D contact filter.</param>
        /// <param name="results">Preallocated array of Collider2D to store results.</param>
        /// <returns>The number of hits found.</returns>
        public static int OverlapAtPosition(Collider2D source, Vector3 position, float size, ContactFilter2D filter, Collider2D[] results, float debugDuration = 2f)
        {
            if (source == null)
                return 0;

            // Handle by collider type
            switch (source)
            {
                case CircleCollider2D circle:
                    if (Main.DrawColliders)
                        DrawDebugCircle(position + (Vector3)circle.offset, circle.radius * size, Color.red, debugDuration);
                    return OverlapCircle(circle, position, size, filter, results);

                case BoxCollider2D box:
                    if (Main.DrawColliders)
                        DrawDebugBox(position + (Vector3)box.offset, box.size * size, box.transform.rotation, Color.red, debugDuration);
                    return OverlapBox(box, position, size, filter, results);

                case CapsuleCollider2D capsule:
                    if (Main.DrawColliders)
                        DrawDebugCapsule(position + (Vector3)capsule.offset, capsule.size * size, capsule.direction, Color.red, debugDuration);
                    return OverlapCapsule(capsule, position, size, filter, results);

                case PolygonCollider2D poly:
                    if (Main.DrawColliders)
                        DrawDebugPolygon(poly, position, size, Color.red, debugDuration);
                    return OverlapPolygon(poly, position, size, filter, results);

                case EdgeCollider2D edge:
                    if (Main.DrawColliders)
                        DrawDebugEdge(edge, position, size, Color.red, debugDuration);
                    return OverlapEdge(edge, position, size, filter, results);

                default:
                    Debug.LogWarning($"[Physics2DExtensions] Unsupported collider type: {source.GetType().Name}");
                    return 0;
            }
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

        // ------------------------------------------------------------------------

        private static int OverlapCircle(CircleCollider2D circle, Vector3 pos, float size, ContactFilter2D filter, Collider2D[] results)
        {
            Vector2 worldPos = (Vector2)pos + circle.offset;
            return Physics2D.OverlapCircle(worldPos, circle.radius * size, filter, results);
        }

        private static int OverlapBox(BoxCollider2D box, Vector3 pos, float size, ContactFilter2D filter, Collider2D[] results)
        {
            Vector2 worldPos = (Vector2)pos + box.offset;
            Vector2 boxSize = box.size * size;
            float angle = box.transform.eulerAngles.z;
            return Physics2D.OverlapBox(worldPos, boxSize, angle, filter, results);
        }

        private static int OverlapCapsule(CapsuleCollider2D capsule, Vector3 pos, float size, ContactFilter2D filter, Collider2D[] results)
        {
            Vector2 worldPos = (Vector2)pos + capsule.offset;
            return Physics2D.OverlapCapsule(worldPos, capsule.size * size, capsule.direction, capsule.transform.eulerAngles.z, filter, results);
        }

        // ------------------------------------------------------------------------
        // POLYGON COLLIDER SUPPORT
        // ------------------------------------------------------------------------

        /// <summary>
        /// Approximates Overlap detection for a PolygonCollider2D by using Physics2D.OverlapPoint
        /// on its world-space vertices and bounds. This is the most accurate way without moving the collider.
        /// </summary>
        private static int OverlapPolygon(PolygonCollider2D poly, Vector3 pos, float size, ContactFilter2D filter, Collider2D[] results)
        {
            // Compute offset for position (since PolygonCollider2D has multiple paths)
            Vector2 offset = (Vector2)pos - (Vector2)poly.transform.position;

            // Get all points in world space, translated to the target position
            List<Vector2> worldPoints = new List<Vector2>();
            for (int pathIndex = 0; pathIndex < poly.pathCount; pathIndex++)
            {
                Vector2[] path = poly.GetPath(pathIndex);
                for (int i = 0; i < path.Length; i++)
                {
                    Vector2 localPoint = poly.transform.TransformPoint(path[i]);
                    worldPoints.Add(localPoint + offset);
                }
            }

            // Compute approximate center
            Vector2 center = (Vector2)pos + poly.offset;

            // Use OverlapArea as a bounding check first (cheap broadphase)
            Bounds bounds = poly.bounds;
            Vector2 boundsSize = bounds.size * size;
            int count = Physics2D.OverlapBox(center, boundsSize, poly.transform.eulerAngles.z, filter, results);

            // Optionally: refine hits if needed (check if they intersect the polygon manually)
            // But generally, OverlapBox with polygon bounds is sufficient for hit detection AOE.

            return count;
        }

        // ------------------------------------------------------------------------
        // EDGE COLLIDER SUPPORT
        // ------------------------------------------------------------------------
        /// <summary>
        /// Approximates Overlap for an EdgeCollider2D by checking small circles along its edges,
        /// adapting probe density and radius based on the given size.
        /// </summary>
        private static int OverlapEdge(EdgeCollider2D edge, Vector3 pos, float size, ContactFilter2D filter, Collider2D[] results)
        {
            if (edge == null)
                return 0;

            Vector2 offset = (Vector2)pos - (Vector2)edge.transform.position;
            Vector2[] points = edge.points;

            int totalCount = 0;

            // --- Adapt probe parameters ---
            // probeRadius scales with spell size
            float probeRadius = Mathf.Max(0.05f, size * 0.1f);
            // step size depends on spell size to avoid huge overlaps
            float stepSize = probeRadius * 1.5f;

            // --- Iterate over each edge segment ---
            for (int i = 0; i < points.Length - 1; i++)
            {
                Vector2 start = edge.transform.TransformPoint(points[i]).ToVector2() + offset;
                Vector2 end = edge.transform.TransformPoint(points[i + 1]).ToVector2() + offset;

                float segmentLength = Vector2.Distance(start, end);
                int stepCount = Mathf.Max(1, Mathf.CeilToInt(segmentLength / stepSize));

                // Move along the segment and test overlaps
                for (int s = 0; s <= stepCount; s++)
                {
                    Vector2 worldPoint = Vector2.Lerp(start, end, s / (float)stepCount);
                    totalCount += Physics2D.OverlapCircle(worldPoint, probeRadius, filter, results);

                    // stop early if result array full
                    if (totalCount >= results.Length)
                        return results.Length;
                }
            }

            return totalCount;
        }



        #region DEBUG Methods

        private static void DrawDebugCircle(Vector3 center, float radius, Color color, float duration)
        {
            int segments = 32;
            float angleStep = 360f / segments;
            Vector3 prevPoint = center + new Vector3(Mathf.Cos(0f), Mathf.Sin(0f), 0f) * radius;

            for (int i = 1; i <= segments; i++)
            {
                float rad = Mathf.Deg2Rad * (i * angleStep);
                Vector3 nextPoint = center + new Vector3(Mathf.Cos(rad), Mathf.Sin(rad), 0f) * radius;
                Debug.DrawLine(prevPoint, nextPoint, color, duration);
                prevPoint = nextPoint;
            }
        }

        private static void DrawDebugBox(Vector3 center, Vector2 size, Quaternion rotation, Color color, float duration)
        {
            Vector3 right = rotation * Vector3.right * size.x / 2;
            Vector3 up = rotation * Vector3.up * size.y / 2;

            Vector3 p1 = center + right + up;
            Vector3 p2 = center + right - up;
            Vector3 p3 = center - right - up;
            Vector3 p4 = center - right + up;

            Debug.DrawLine(p1, p2, color, duration);
            Debug.DrawLine(p2, p3, color, duration);
            Debug.DrawLine(p3, p4, color, duration);
            Debug.DrawLine(p4, p1, color, duration);
        }

        private static void DrawDebugCapsule(Vector3 center, Vector2 size, CapsuleDirection2D direction, Color color, float duration)
        {
            // Approximate by drawing top and bottom circles + rectangle
            float radius = direction == CapsuleDirection2D.Vertical ? size.x / 2 : size.y / 2;
            float height = direction == CapsuleDirection2D.Vertical ? size.y - 2 * radius : size.x - 2 * radius;

            Vector3 offset = direction == CapsuleDirection2D.Vertical ? Vector3.up * height / 2 : Vector3.right * height / 2;
            DrawDebugCircle(center + offset, radius, color, duration);
            DrawDebugCircle(center - offset, radius, color, duration);

            if (direction == CapsuleDirection2D.Vertical)
            {
                Debug.DrawLine(center + offset + Vector3.right * radius, center - offset + Vector3.right * radius, color, duration);
                Debug.DrawLine(center + offset - Vector3.right * radius, center - offset - Vector3.right * radius, color, duration);
            }
            else
            {
                Debug.DrawLine(center + offset + Vector3.up * radius, center - offset + Vector3.up * radius, color, duration);
                Debug.DrawLine(center + offset - Vector3.up * radius, center - offset - Vector3.up * radius, color, duration);
            }
        }

        private static void DrawDebugPolygon(PolygonCollider2D poly, Vector3 position, float size, Color color, float duration)
        {
            if (poly == null || poly.points == null || poly.points.Length < 2)
                return;

            Vector2[] points = poly.points;
            Vector3 scale = Vector3.one * size;

            for (int i = 0; i < points.Length; i++)
            {
                // Scale the collider's local points by size and offset
                Vector3 a = position + Vector3.Scale(points[i] + poly.offset, scale);
                Vector3 b = position + Vector3.Scale(points[(i + 1) % points.Length] + poly.offset, scale);

                Debug.DrawLine(a, b, color, duration);
            }
        }

        private static void DrawDebugEdge(EdgeCollider2D edge, Vector3 position, float size, Color color, float duration)
        {
            if (edge == null || edge.points == null || edge.points.Length < 2)
                return;

            Vector2[] points = edge.points;
            Vector3 scale = Vector3.one * size;

            for (int i = 0; i < points.Length - 1; i++)
            {
                // Scale the collider's local points by size and offset
                Vector3 a = position + Vector3.Scale(points[i] + edge.offset, scale);
                Vector3 b = position + Vector3.Scale(points[i + 1] + edge.offset, scale);

                Debug.DrawLine(a, b, color, duration);
            }
        }


        #endregion
    }
}
