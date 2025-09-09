using Assets.Scripts.Game;
using Data.GameManagement;
using Enums;
using Save;
using System.Collections;
using TMPro;
using Tools;
using Unity.VisualScripting;
using UnityEngine;

/// <summary>
/// Component for floating hit text.
/// Handles color, scaling, and animation depending on hit type.
/// </summary>
public class FloatingTextUI : MonoBehaviour
{
    public TMP_Text Text;              // TextMeshPro text component
    public float Duration = 1f;        // Duration of the float animation
    public Vector3 FloatOffset = new Vector3(0, 1f, 0); // Base offset direction
    [Tooltip("Values between which the text size will scale")]
    public Vector2 TextScale = new Vector2(0, 300);
    [Tooltip("Max scaling value of the text")]
    public float TextScaleFactor = 1.5f;

    private EHitCategory m_SpellCategory;

    /// <summary>
    /// Initialize floating text with hit data.
    /// Sets color, size, and animation.
    /// </summary>
    public void SetText(int damage, EHitType hitType, EHitCategory spellCategory)
    {
        m_SpellCategory = spellCategory;
        Text.text = damage.ToString();
        Text.color = PlayerSettings.GetHitTypeColor(hitType, spellCategory);

        // Scale text size proportionally to damage value (min -> 1/scale ; max -> scale)
        float min = TextScale.x * Mathf.Pow(1.1f, ProfileCloudData.AccountLevel - 1);   
        float max = TextScale.y * Mathf.Pow(1.1f, ProfileCloudData.AccountLevel - 1);
        float normalized = Mathf.Clamp01((damage - min) / (max - min));
        transform.localScale = Vector3.one * Mathf.Lerp(1 / TextScaleFactor, TextScaleFactor, normalized * 2f);

        // 🪄 Choose animation depending on category
        if (spellCategory == EHitCategory.Tick)
            StartCoroutine(FloatCurve());
        else
            StartCoroutine(FloatUp());
    }

    /// <summary>
    /// Standard floating animation (straight upward).
    /// </summary>
    private IEnumerator FloatUp()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + FloatOffset;
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            transform.position = Vector3.Lerp(startPos, endPos, elapsed / Duration);
            yield return null;
        }

        PoolManager.ReturnObject(gameObject);
    }

    /// <summary>
    /// Curved floating animation (used for ticks).
    /// Adds a side-to-side sine movement.
    /// </summary>
    private IEnumerator FloatCurve()
    {
        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + FloatOffset + new Vector3(Random.Range(-0.5f, 0.5f), 0.2f, 0f);
        float elapsed = 0f;

        while (elapsed < Duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / Duration;
            transform.position = Vector3.Lerp(startPos, endPos, t)
                               + new Vector3(Mathf.Sin(t * Mathf.PI) * 0.2f, 0, 0);
            yield return null;
        }

        PoolManager.ReturnObject(gameObject);
    }
}
