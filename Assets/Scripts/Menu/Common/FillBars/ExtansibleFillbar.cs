using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Tools;
using UnityEngine;
using UnityEngine.UI;

public class ExtansibleFillbar : MObject
{
    [SerializeField]
    float m_AnimationDuration = 1.0f;

    protected Coroutine m_Animation;
    protected int m_MaxValue = 1;

    // Container
    protected GameObject m_Container;
    protected RectTransform m_ContainerRect;

    // Liste dynamique de barres
    protected List<Image> m_Bars = new List<Image>();
    protected List<RectTransform> m_BarRects = new List<RectTransform>();

    // Prefab de barre (à assigner dans l’inspecteur)
    [SerializeField] protected GameObject m_BarPrefab;

    protected override void FindComponents()
    {
        base.FindComponents();
        m_Container = Finder.Find(gameObject, "Container");
        m_ContainerRect = Finder.FindComponent<RectTransform>(m_Container);
    }

    /// <summary>
    /// Initialise avec une liste de valeurs/couleurs
    /// </summary>
    public virtual void Initialize(List<(int value, Color color)> values, int maxValue, bool withAnimation = false)
    {
        base.Initialize();

        m_MaxValue = Mathf.Max(1, maxValue);

        // Clear anciennes barres
        foreach (var bar in m_Bars)
            Destroy(bar.gameObject);

        // clean content before displaying
        UIHelper.CleanContent(m_Container);
        m_Bars.Clear();
        m_BarRects.Clear();

        // Crée une barre par entrée
        for (int i = values.Count - 1; i >= 0; i--)
        {
            var bar = Instantiate(m_BarPrefab, m_Container.transform).GetComponent<Image>();
            bar.color = values[i].color;
            bar.gameObject.SetActive(true);

            m_Bars.Add(bar);
            m_BarRects.Add(bar.rectTransform);
        }

        if (withAnimation)
        {
            if (m_Animation != null)
                StopCoroutine(m_Animation);
            m_Animation = StartCoroutine(UpdateListAnimationCoroutine(values));
        }
        else
        {
            UpdateListBarSize(values);
        }
    }

    void UpdateListBarSize(List<(int value, Color color)> values)
    {
        int cumulative = 0;
        for (int i = 0; i < values.Count; i++)
        {
            int reversedIndex = values.Count - 1 - i;   // bars are set in reversed order 
            cumulative += values[i].value;
            float fill = Mathf.Clamp01((float)cumulative / m_MaxValue);

            m_BarRects[reversedIndex].sizeDelta = new Vector2(m_ContainerRect.rect.width * fill, m_BarRects[reversedIndex].sizeDelta.y);
        }
    }

    IEnumerator UpdateListAnimationCoroutine(List<(int value, Color color)> targetValues)
    {
        // On part de 0
        float elapsedTime = 0f;
        List<int> startValues = new List<int>(new int[targetValues.Count]);

        while (elapsedTime < m_AnimationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = Mathf.Clamp01(elapsedTime / m_AnimationDuration);

            List<(int, Color)> interpolated = new List<(int, Color)>();
            for (int i = 0; i < targetValues.Count; i++)
            {
                int current = Mathf.RoundToInt(Mathf.Lerp(startValues[i], targetValues[i].value, t));
                interpolated.Add((current, targetValues[i].color));
            }

            UpdateListBarSize(interpolated);
            yield return null;
        }

        UpdateListBarSize(targetValues);
        m_Animation = null;
    }
}
