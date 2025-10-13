using Tools;
using UnityEngine;

public class SpawnUI : MonoBehaviour
{
    [Header("Placement")]
    public float aboveHeadFactor = 0.15f;    // fraction of collider HEIGHT
    public float minAbove = 0.2f;            // world units minimum over head

    [Tooltip("World Y clamp if clampInViewport == false")]
    public Vector2 worldYClamp = new Vector2(0.5f, 2f);

    bool m_Initialized;
    Transform m_Target;                 // spawn root
    Collider2D m_TargetCollider;        // collider that defines size
    Vector2 m_Size;
    float m_SizeScale;

    float m_Scale => m_Size.x * m_SizeScale;

    public void Initialize(float sizeScale)
    {
        // Cache main camera if available
        m_SizeScale = sizeScale;

        // Default target = parent
        if (!m_Target) m_Target = transform.parent;

        // Find collider on target or its children
        if (!m_TargetCollider && m_Target)
            m_TargetCollider = Finder.FindComponent<Collider2D>(m_Target.gameObject);

        if (!m_TargetCollider)
        {
            Debug.LogError($"[SpawnUI] No Collider2D found on '{(m_Target ? m_Target.name : "null")}'.");
            return;
        }

        if (m_TargetCollider is CapsuleCollider2D capsuleCollider2D)
        {
            m_Size = capsuleCollider2D.size;
        }
        else if (m_TargetCollider is CircleCollider2D circleCollider2D)
        {
            m_Size = Vector2.one * circleCollider2D.radius * 2;
        }
        else
        {
            m_Size = m_TargetCollider.bounds.size;
        }

        // rescale
        transform.localScale *= 2f ;

        // call init is done
        m_Initialized = true;
    }

    void LateUpdate()
    {
        if (!m_Initialized)
            return;

        // Height of collider in world units
        float worldHeight = m_Size.y * m_Target.transform.lossyScale.y;

        // Offset au-dessus de la tête
        float above = Mathf.Max(minAbove, aboveHeadFactor * worldHeight);

        // Position finale : au-dessus de la tête
        Vector3 desired = m_Target.position + new Vector3(0f, worldHeight / 2f + above, 0f);

        // Clamp uniquement sur l'axe Y
        desired.y = Mathf.Clamp(desired.y, worldYClamp.x, worldYClamp.y);

        transform.position = desired;

        // Toujours face à la caméra (ou fixe dans ton cas)
        transform.rotation = Quaternion.identity;
    }

}
